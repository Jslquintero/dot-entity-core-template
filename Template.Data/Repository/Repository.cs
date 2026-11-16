using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Template.Data.Repository;

/// <summary>Basic persistence operations. Each write saves immediately; use an explicit transaction for multi-step work.</summary>
public class Repository<TEntity>(ApplicationDbContext context) : IRepository<TEntity> where TEntity : class
{
    protected DbSet<TEntity> Entities => context.Set<TEntity>();
    public IQueryable<TEntity> Table => Entities;
    public IQueryable<TEntity> TableNoTracking => Entities.AsNoTracking();

    public Task<bool> Exists(Expression<Func<TEntity, bool>> predicate) => Entities.AnyAsync(predicate);

    private IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? filter = null, string[]? includes = null)
    {
        var query = TableNoTracking;
        foreach (var include in includes ?? [])
            query = query.Include(include.Trim());
        return filter is null ? query : query.Where(filter);
    }

    public TEntity? GetOne(Expression<Func<TEntity, bool>> filter) => GetOne(filter, null);
    public TEntity? GetOne(Expression<Func<TEntity, bool>> filter, string[]? includes) => Query(filter, includes).FirstOrDefault();
    public Task<TEntity?> GetOneAsync(Expression<Func<TEntity, bool>> filter) => GetOneAsync(filter, null);
    public Task<TEntity?> GetOneAsync(Expression<Func<TEntity, bool>> filter, string[]? includes) => Query(filter, includes).FirstOrDefaultAsync();
    public List<TEntity> GetList() => Query().ToList();
    public List<TEntity> GetList(string[] includes) => Query(includes: includes).ToList();
    public List<TEntity> GetList(Expression<Func<TEntity, bool>> filter) => Query(filter).ToList();
    public List<TEntity> GetList(Expression<Func<TEntity, bool>>? filter, string[]? includes) => Query(filter, includes).ToList();
    public Task<List<TEntity>> GetListAsync() => Query().ToListAsync();
    public Task<List<TEntity>> GetListAsync(string[] includes) => Query(includes: includes).ToListAsync();
    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> filter) => Query(filter).ToListAsync();
    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? filter, string[]? includes) => Query(filter, includes).ToListAsync();

    public void Insert(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Add(entity);
        context.SaveChanges();
    }

    public void Insert(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Entities.AddRange(entities);
        context.SaveChanges();
    }

    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Update(entity);
        context.SaveChanges();
    }

    public void Update(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Entities.UpdateRange(entities);
        context.SaveChanges();
    }

    public async Task UpdateAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Update(entity);
        await context.SaveChangesAsync();
    }

    public void Delete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Remove(entity);
        context.SaveChanges();
    }

    public void Delete(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Entities.RemoveRange(entities);
        context.SaveChanges();
    }

    public async Task DeleteAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Remove(entity);
        await context.SaveChangesAsync();
    }
}
