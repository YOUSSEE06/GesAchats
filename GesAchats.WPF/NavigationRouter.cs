using System.Windows;
using GesAchats.Core.Entities;
using GesAchats.WPF.Views.Magasinier;
using GesAchats.WPF.Views.Acheteur;
using GesAchats.WPF.Views.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF;

public static class NavigationRouter
{
    public static void GoToRole(User user)
    {
        Window? shellWindow = null;
        var serviceProvider = ((App)Application.Current).ServiceProvider;

        // On utilise le code du rôle pour le switch
        switch (user.Role?.Code?.ToUpper())
        {
            case "MAGASINIER":
                shellWindow = serviceProvider.GetRequiredService<MagasinierShell>();
                break;
            
            case "ACHETEUR":
                shellWindow = serviceProvider.GetRequiredService<AcheteurShell>();
                break;
            
            case "RESPONSABLE_ACHATS":
                shellWindow = serviceProvider.GetRequiredService<AcheteurShell>();
                break;

            case "COMPTABLE":
                shellWindow = serviceProvider.GetRequiredService<GesAchats.WPF.Views.Comptable.ComptableShell>();
                break;
            
            // Les autres rôles seront ajoutés ici plus tard
            // case "ADMIN": ...
            
            default:
                MessageBox.Show($"Accès non configuré pour le rôle : {user.Role?.Label ?? "Inconnu"}", 
                                "Erreur d'accès", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
        }

        if (shellWindow != null)
        {
            // On définit le DataContext si nécessaire ou on laisse faire l'injection
            shellWindow.Show();
            
            // On ferme la fenêtre de login
            var loginWindow = Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault();
            loginWindow?.Close();
            
            // On définit la nouvelle fenêtre comme fenêtre principale
            Application.Current.MainWindow = shellWindow;
        }
    }
}
