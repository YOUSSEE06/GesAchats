using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByLoginAsync(string login);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByLoginOrEmailAsync(string loginOrEmail);
}
