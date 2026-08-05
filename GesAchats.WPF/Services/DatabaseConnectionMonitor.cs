using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.Services;

/// <summary>
/// Surveille en continu la connexion à la base de données Supabase (PostgreSQL).
/// Sonde la base à intervalle régulier, expose l'état courant (connecté / déconnecté)
/// et notifie les abonnés (changement d'état, connexion perdue).
/// </summary>
public interface IDatabaseConnectionMonitor : IDisposable
{
    bool IsConnected { get; }
    event Action<bool>? ConnectionStateChanged;
    event Action? ConnectionLost;
    void Start();
    Task<bool> CheckConnectionAsync();
}

public class DatabaseConnectionMonitor : IDatabaseConnectionMonitor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly object _lock = new();
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _probeTimeout = TimeSpan.FromSeconds(5);
    private Timer? _timer;
    private bool _checking;
    private bool _started;
    private bool _disposed;
    private bool _isConnected;

    public bool IsConnected
    {
        get
        {
            lock (_lock) return _isConnected;
        }
    }

    public event Action<bool>? ConnectionStateChanged;
    public event Action? ConnectionLost;

    public DatabaseConnectionMonitor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>Démarre la surveillance périodique de la connexion.</summary>
    public void Start()
    {
        lock (_lock)
        {
            if (_started || _disposed) return;
            _started = true;
        }

        _timer = new Timer(_ => _ = RunCheckAsync(), null, TimeSpan.Zero, _interval);
    }

    /// <summary>Probe immédiate de la connexion (utilisée par le bouton « Actualiser »).</summary>
    public async Task<bool> CheckConnectionAsync()
    {
        return await RunCheckAsync();
    }

    private async Task<bool> RunCheckAsync()
    {
        bool wasConnected;
        lock (_lock)
        {
            if (_disposed) return false;
            if (_checking) return _isConnected;
            _checking = true;
            wasConnected = _isConnected;
        }

        var connected = false;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GesAchatsDbContext>();
            using var cts = new CancellationTokenSource(_probeTimeout);
            connected = await context.Database.CanConnectAsync(cts.Token);
        }
        catch
        {
            connected = false;
        }
        finally
        {
            lock (_lock) _checking = false;
        }

        bool previous;
        lock (_lock)
        {
            previous = _isConnected;
            _isConnected = connected;
        }

        if (connected && !previous)
        {
            ConnectionStateChanged?.Invoke(true);
        }
        else if (!connected && previous)
        {
            ConnectionStateChanged?.Invoke(false);
            ConnectionLost?.Invoke();
        }

        return connected;
    }

    public void Dispose()
    {
        if (_disposed) return;
        lock (_lock) _disposed = true;
        _timer?.Dispose();
        _timer = null;
        ConnectionStateChanged = null;
        ConnectionLost = null;
    }
}