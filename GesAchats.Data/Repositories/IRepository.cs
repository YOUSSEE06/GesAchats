using System.Linq.Expressions;

namespace GesAchats.Data.Repositories;

/// <summary>
/// Interface générique pour le pattern Repository
/// </summary>
/// <typeparam name="TEntity">Le type de l'entité</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(int id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task AddAsync(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
