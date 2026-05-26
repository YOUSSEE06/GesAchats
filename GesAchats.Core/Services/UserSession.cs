using System.Collections.Generic;
using System.Linq;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Security;

namespace GesAchats.Core.Services
{
    public class UserSession : IUserSession
    {
        public User? CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;

        private readonly Dictionary<string, List<ModulePermission>> _permissions;

        public UserSession()
        {
            _permissions = ConfigureDefaultPermissions();
        }

        public void StartSession(User user)
        {
            CurrentUser = user;
        }

        public void EndSession()
        {
            CurrentUser = null;
        }

        public bool HasAccess(AccessModule module, AccessLevel requiredLevel = AccessLevel.Read)
        {
            if (CurrentUser == null) return false;
            if (IsAdmin) return true;

            var roleCode = CurrentUser.Role?.Code;
            if (string.IsNullOrEmpty(roleCode) || !_permissions.ContainsKey(roleCode)) return false;

            var modulePerm = _permissions[roleCode].FirstOrDefault(p => p.Module == module);
            if (modulePerm == null) return false;

            return modulePerm.Level >= requiredLevel;
        }

        public bool HasRole(string roleCode)
        { 
            if (CurrentUser?.Role == null) return false;
            
            // On vérifie le code du rôle (insensible à la casse)
            return string.Equals(CurrentUser.Role.Code, roleCode, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsAdmin => HasRole("ADMIN") || HasRole("Directeur");

        private Dictionary<string, List<ModulePermission>> ConfigureDefaultPermissions()
        {
            return new PermissionBuilder()
                .ForRole("MAGASINIER")
                    .CanRead(AccessModule.Dashboard, AccessModule.Stock, AccessModule.BonsCommande, AccessModule.StockAnalysis, AccessModule.OrderPlanning, AccessModule.MarketReport)
                    .CanWrite(AccessModule.Besoins, AccessModule.BonsLivraison)
                
                .ForRole("ACHETEUR")
                    .CanRead(AccessModule.Dashboard, AccessModule.Besoins, AccessModule.Stock, AccessModule.BonsLivraison)
                    .CanWrite(AccessModule.Devis, AccessModule.BonsCommande, AccessModule.Fournisseurs)
                
                .ForRole("COMPTABLE")
                    .CanRead(AccessModule.Dashboard, AccessModule.BonsCommande, AccessModule.BonsLivraison)
                    .CanWrite(AccessModule.Factures, AccessModule.Paiements)
                
                .ForRole("ADMIN")
                    .CanManage(Enum.GetValues<AccessModule>())
                
                .Build();
        }
    }
}
