using System.Windows;
using System.Windows.Media;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.Views;

/// <summary>
/// Fenêtre d'alerte affichée automatiquement lorsque la connexion à la base de
/// données est perdue. Propose deux actions : « Actualiser » (re-tester) et
/// « Se connecter » (tenter de rétablir la connexion). Se ferme automatiquement
/// dès que la connexion est rétablie.
/// </summary>
public partial class ConnectionErrorWindow : Window
{
    private static readonly SolidColorBrush CheckingDotBrush = FrozenBrush(0xF5, 0x9E, 0x0B);
    private static readonly SolidColorBrush CheckingBgBrush = FrozenBrush(0xFE, 0xF3, 0xC7);
    private static readonly SolidColorBrush ConnectedDotBrush = FrozenBrush(0x22, 0xC5, 0x5E);
    private static readonly SolidColorBrush ConnectedBgBrush = FrozenBrush(0xDC, 0xFC, 0xE7);
    private static readonly SolidColorBrush FailedDotBrush = FrozenBrush(0xDC, 0x26, 0x26);
    private static readonly SolidColorBrush FailedBgBrush = FrozenBrush(0xFE, 0xE2, 0xE2);

    private readonly IDatabaseConnectionMonitor _connectionMonitor;

    public ConnectionErrorWindow(IDatabaseConnectionMonitor connectionMonitor)
    {
        InitializeComponent();
        _connectionMonitor = connectionMonitor;
        _connectionMonitor.ConnectionStateChanged += OnConnectionStateChanged;

        // Déclenché après l'affichage de la fenêtre (sinon Close() avant Show() lèverait une exception).
        Dispatcher.BeginInvoke(() =>
        {
            if (!_connectionMonitor.IsConnected)
            {
                _ = CheckAsync();
            }
            else
            {
                ConnectionRestored();
            }
        });
    }

    private void OnConnectionStateChanged(bool connected)
    {
        if (!connected) return;

        if (Dispatcher.CheckAccess())
        {
            ConnectionRestored();
        }
        else
        {
            Dispatcher.Invoke(ConnectionRestored);
        }
    }

    private void ConnectionRestored()
    {
        _connectionMonitor.ConnectionStateChanged -= OnConnectionStateChanged;
        if (IsVisible)
        {
            Close();
        }
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await CheckAsync();
    }

    private async void OnConnectClick(object sender, RoutedEventArgs e)
    {
        await CheckAsync();
    }

    private async Task CheckAsync()
    {
        SetButtonsEnabled(false);
        ShowStatus("Vérification en cours…", CheckingDotBrush, CheckingBgBrush, "Connexion en cours…");

        var connected = await _connectionMonitor.CheckConnectionAsync();

        if (connected)
        {
            ShowStatus("Connexion rétablie", ConnectedDotBrush, ConnectedBgBrush, "La connexion à la base de données a été rétablie.");
            _connectionMonitor.ConnectionStateChanged -= OnConnectionStateChanged;
            await Task.Delay(600);
            Close();
            return;
        }

        SetButtonsEnabled(true);
        ShowStatus("Échec de la connexion", FailedDotBrush, FailedBgBrush,
            "La base de données est toujours inaccessible. Vérifiez votre connexion Internet, puis réessayez.");
    }

    private void ShowStatus(string statusText, Brush dotBrush, Brush backgroundBrush, string message)
    {
        StatusText.Text = statusText;
        StatusDot.Fill = dotBrush;
        StatusBorder.Background = backgroundBrush;
        StatusMessageText.Text = message;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        RefreshButton.IsEnabled = enabled;
        ConnectButton.IsEnabled = enabled;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _connectionMonitor.ConnectionStateChanged -= OnConnectionStateChanged;
        base.OnClosed(e);
    }

    private static SolidColorBrush FrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}