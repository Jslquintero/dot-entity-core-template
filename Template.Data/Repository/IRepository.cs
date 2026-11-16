using System.Linq.Expressions;

namespace Template.Data.Repository;

public interface IRepository<TEntity> where TEntity : class
{
    Task<bool> Exists(Expression<Func<TEntity, bool>> predicate);
    void Insert(TEntity entity);
    void Insert(IEnumerable<TEntity> entities);
    void Update(TEntity entity);
    void Update(IEnumerable<TEntity> entities);
    void Delete(TEntity entity);
    void Delete(IEnumerable<TEntity> entities);
    List<TEntity> GetList();
    List<TEntity> GetList(string[] includes);
    List<TEntity> GetList(Expression<Func<TEntity, bool>> filter);
    TEntity? GetOne(Expression<Func<TEntity, bool>> filter);
    TEntity? GetOne(Expression<Func<TEntity, bool>> filter, string[]? includes);
    Task<TEntity?> GetOneAsync(Expression<Func<TEntity, bool>> filter);
    Task<TEntity?> GetOneAsync(Expression<Func<TEntity, bool>> filter, string[]? includes);
    Task<List<TEntity>> GetListAsync();
    Task<List<TEntity>> GetListAsync(string[] includes);
    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> filter);
    Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? filter, string[]? includes);
    Task<TEntity> InsertAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
    IQueryable<TEntity> Table { get; }
    IQueryable<TEntity> TableNoTracking { get; }
}
