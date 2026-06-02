using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

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
}
