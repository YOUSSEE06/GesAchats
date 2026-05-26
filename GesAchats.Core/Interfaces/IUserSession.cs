using GesAchats.Core.Entities;
using GesAchats.Core.Security;

namespace GesAchats.Core.Interfaces
{
    public interface IUserSession
    {
        User? CurrentUser { get; }
        bool IsLoggedIn { get; }
        void StartSession(User user);
        void EndSession();
        bool HasAccess(AccessModule module, AccessLevel requiredLevel = AccessLevel.Read);
        bool HasRole(string roleCode);
        bool IsAdmin { get; }
    }
}
