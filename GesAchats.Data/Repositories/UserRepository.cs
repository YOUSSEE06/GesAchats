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
}
