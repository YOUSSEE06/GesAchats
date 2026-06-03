using GesAchats.Core.DTOs;

namespace GesAchats.Core.Interfaces;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetEmployeesAsync();
    Task ActivateEmployeeAsync(int userId);
    Task DeactivateEmployeeAsync(int userId);
    Task<bool> HasAnotherActiveUserWithRoleAsync(int userId, string roleCode);
    Task<(bool success, string message)> SendCreateUserCodeAsync(string fullName, string email, int roleId);
    Task<(bool success, string message)> CreateEmployeeAsync(string email, string code);
}
