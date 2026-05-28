using System.Linq.Expressions;

namespace GesAchats.Core.Interfaces;

/// <summary>
/// Interface générique pour le pattern Repository (Définie dans Core pour éviter la dépendance circulaire)
/// </summary>
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(int id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> GetAllIncludingAsync(params Expression<Func<TEntity, object?>>[] includeProperties);
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task AddAsync(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
