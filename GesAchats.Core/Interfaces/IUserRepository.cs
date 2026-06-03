using GesAchats.Core.Entities;
using GesAchats.Core.DTOs;

namespace GesAchats.Core.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByLoginAsync(string login);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByLoginOrEmailAsync(string loginOrEmail);
    Task<IEnumerable<EmployeeDto>> GetEmployeesAsync();
    Task<User?> GetActiveUserByRoleAsync(string roleCode, int? excludeUserId = null);
}
