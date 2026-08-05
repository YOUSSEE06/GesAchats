using System.Windows.Media;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.ViewModels.Base;

/// <summary>
/// Base commune aux ViewModels des fenêtres "Shell" (sidebar).
/// Expose l'état de connexion à la base de données : un point vert (connecté)
/// ou rouge (déconnecté) affiché dans le panneau latéral.
/// </summary>
public abstract class ShellViewModelBase : BaseViewModel
{
    private static readonly Brush ConnectedBrush = CreateFrozenBrush(0x3B, 0xA2, 0x5E);
    private static readonly Brush DisconnectedBrush = CreateFrozenBrush(0xE5, 0x3E, 0x3E);

    private readonly IDatabaseConnectionMonitor _connectionMonitor;
    private bool _isDatabaseConnected;

    public bool IsDatabaseConnected
    {
        get => _isDatabaseConnected;
        private set
        {
            if (SetProperty(ref _isDatabaseConnected, value))
            {
                OnPropertyChanged(nameof(ConnectionDotBrush));
                OnPropertyChanged(nameof(ConnectionStatusText));
            }
        }
    }

    /// <summary>Couleur du point d'état : vert si connecté, rouge sinon.</summary>
    public Brush ConnectionDotBrush => IsDatabaseConnected ? ConnectedBrush : DisconnectedBrush;

    /// <summary>Libellé de l'état de connexion affiché dans la sidebar.</summary>
    public string ConnectionStatusText => IsDatabaseConnected ? "Base connectée" : "Base déconnectée";

    protected ShellViewModelBase(IDatabaseConnectionMonitor connectionMonitor)
    {
        _connectionMonitor = connectionMonitor;
        IsDatabaseConnected = connectionMonitor.IsConnected;
        _connectionMonitor.ConnectionStateChanged += OnConnectionStateChanged;
    }

    private void OnConnectionStateChanged(bool connected)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            IsDatabaseConnected = connected;
        }
        else
        {
            dispatcher.Invoke(() => IsDatabaseConnected = connected);
        }
    }

    private static Brush CreateFrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}