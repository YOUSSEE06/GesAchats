using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;
using GesAchats.Core.DTOs;

namespace GesAchats.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(GesAchatsDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByLoginAsync(string login)
    {
        return await _dbSet.Include(u => u.Role)
                           .FirstOrDefaultAsync(u => u.Login == login);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.Include(u => u.Role)
                           .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByLoginOrEmailAsync(string loginOrEmail)
    {
        return await _dbSet.Include(u => u.Role)
                           .FirstOrDefaultAsync(u => u.Login == loginOrEmail || u.Email == loginOrEmail);
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync()
    {
        return await _dbSet.Include(u => u.Role)
                           .Where(u => u.Role.Code != "ADMIN")
                           .Select(u => new EmployeeDto
                           {
                               Id = u.Id,
                               FullName = u.FullName ?? string.Empty,
                               Email = u.Email,
                               RoleCode = u.Role.Code,
                               RoleLabel = u.Role.Label,
                               IsEmailVerified = true, // No column yet, assume true for now
                               IsActive = u.IsActive,
                               CreatedAt = u.CreatedAt
                           })
                           .ToListAsync();
    }

    public async Task<User?> GetActiveUserByRoleAsync(string roleCode, int? excludeUserId = null)
    {
        var query = _dbSet.Include(u => u.Role)
                          .Where(u => u.Role.Code == roleCode && u.IsActive);
        
        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId);
        }
        
        return await query.FirstOrDefaultAsync();
    }
}
