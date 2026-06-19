using System.Linq.Expressions;
using GesAchats.Data.Context;
using GesAchats.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

/// <summary>
/// Implémentation générique du pattern Repository utilisant EF Core
/// </summary>
/// <typeparam name="TEntity">Le type de l'entité</typeparam>
public class Repository<TEntity> : GesAchats.Core.Interfaces.IRepository<TEntity> where TEntity : class
{
    protected readonly GesAchatsDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(GesAchatsDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> GetAllIncludingAsync(params Expression<Func<TEntity, object?>>[] includeProperties)
    {
        return await GetAllIncludingAsync(false, includeProperties);
    }

    public async Task<IEnumerable<TEntity>> GetAllIncludingAsync(bool noTracking, params Expression<Func<TEntity, object?>>[] includeProperties)
    {
        IQueryable<TEntity> query = _dbSet;
        if (noTracking)
        {
            query = query.AsNoTracking();
        }
        foreach (var includeProperty in includeProperties)
        {
            query = query.Include(includeProperty);
        }
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        _dbSet.Attach(entity);
        _context.Entry(entity).State = EntityState.Modified;
    }

    public void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public IQueryable<TEntity> GetQueryable()
    {
        return _dbSet;
    }

    public IQueryable<TEntity> GetQueryable(bool noTracking)
    {
        return noTracking ? _dbSet.AsNoTracking() : _dbSet;
    }
}
