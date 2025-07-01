using Template.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Template.Data.Repository
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        #region Fields
        private readonly ApplicationDbContext _context;
        private DbSet<TEntity> _entities;
        #endregion

        #region Ctor
        public Repository(ApplicationDbContext context)
        {
            this._context = context;
        }
        #endregion

        #region Utilities
        protected string GetFullErrorTextAndRollBackEntityChanges(DbUpdateException exception)
        {
            if (_context is DbContext dbContext)
            {
                var entries = dbContext.ChangeTracker.Entries()
                    .Where(entry => entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified).ToList();
                entries.ForEach(entry => entry.State = EntityState.Unchanged);
            }
            _context.SaveChanges();
            return exception.ToString();
        }
        #endregion

        #region Methods

        public virtual async Task<bool> Exists(Expression<Func<TEntity, bool>> predicate)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();
            return await query.AnyAsync(predicate);
        }

        public virtual TEntity GetOne(Expression<Func<TEntity, Boolean>> Filter)
        {
            return GetOne(Filter, null);
        }

        public virtual TEntity GetOne(Expression<Func<TEntity, Boolean>> Filter, String[] Includes)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            if (Includes != null)
            {
                foreach (String include in Includes)
                {
                    query = query.Include(include.Trim());
                }
            }

            return query.Where(Filter).AsNoTracking().FirstOrDefault();
        }

        // Fixed: Added single entity Insert method
        public virtual void Insert(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                Entities.Add(entity);
                _context.SaveChanges();
            }
            catch (DbUpdateException exception)
            {
                //ensure that the detailed error text is saved in the Log
                throw new Exception(GetFullErrorTextAndRollBackEntityChanges(exception), exception);
            }
        }

        public virtual void Insert(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            try
            {
                Entities.AddRange(entities);
                _context.SaveChanges();
            }
            catch (DbUpdateException exception)
            {
                //ensure that the detailed error text is saved in the Log
                throw new Exception(GetFullErrorTextAndRollBackEntityChanges(exception), exception);
            }
        }

        public virtual void Update(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                Entities.Update(entity);
                _context.SaveChanges();
            }
            catch (DbUpdateException exception)
            {
                //ensure that the detailed error text is saved in the Log
                throw new Exception(GetFullErrorTextAndRollBackEntityChanges(exception), exception);
            }
        }

        public virtual void Update(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            try
            {
                Entities.UpdateRange(entities);
                _context.SaveChanges();
            }
            catch (DbUpdateException exception)
            {
                //ensure that the detailed error text is saved in the Log
                throw new Exception(GetFullErrorTextAndRollBackEntityChanges(exception), exception);
            }
        }

        public virtual void Delete(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            try
            {
                Entities.Remove(entity);
                _context.SaveChanges();
            }
            catch (DbUpdateException exception)
            {
                //ensure that the detailed error text is saved in the Log
                throw new Exception(GetFullErrorTextAndRollBackEntityChanges(exception), exception);
            }
        }

        public virtual void Delete(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            try
            {
                Entities.RemoveRange(entities);
                _context.SaveChanges();
            }
            catch (DbUpdateException exception)
            {
                //ensure that the detailed error text is saved in the Log
                throw new Exception(GetFullErrorTextAndRollBackEntityChanges(exception), exception);
            }
        }

        public virtual List<TEntity> GetList()
        {
            return GetList(null, null);
        }

        public virtual List<TEntity> GetList(String[] Includes)
        {
            return GetList(null, Includes);
        }

        public virtual List<TEntity> GetList(Expression<Func<TEntity, bool>> Filter)
        {
            return GetList(Filter, null);
        }

        // Fixed: Added missing overload with correct parameter name
        public virtual List<TEntity> GetList(Expression<Func<TEntity, bool>> Filter, String[] Includes)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            if (Includes != null)
            {
                foreach (String include in Includes)
                {
                    query = query.Include(include.Trim());
                }
            }

            if (Filter != null)
            {
                return query.Where(Filter).AsNoTracking().ToList();
            }
            else
            {
                return query.AsNoTracking().ToList();
            }
        }

        // Fixed: Corrected method name from GetOneAync to GetOneAsync
        public virtual async Task<TEntity> GetOneAsync(Expression<Func<TEntity, Boolean>> Filter)
        {
            return await GetOneAsync(Filter, null);
        }

        public virtual async Task<TEntity> GetOneAsync(Expression<Func<TEntity, Boolean>> Filter, String[] Includes)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            if (Includes != null)
            {
                foreach (String include in Includes)
                {
                    query = query.Include(include.Trim());
                }
            }

            return await query.Where(Filter).AsNoTracking().FirstOrDefaultAsync();
        }

        public virtual async Task<List<TEntity>> GetListAsync()
        {
            return await GetListAsync(null, null);
        }

        public virtual async Task<List<TEntity>> GetListAsync(String[] Includes)
        {
            return await GetListAsync(null, Includes);
        }

        public virtual async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> Filter)
        {
            return await GetListAsync(Filter, null);
        }

        public virtual async Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> Filter, String[] Includes)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            if (Includes != null)
            {
                foreach (String include in Includes)
                {
                    query = query.Include(include.Trim());
                }
            }

            if (Filter != null)
            {
                return await query.Where(Filter).AsNoTracking().ToListAsync();
            }
            else
            {
                return await query.AsNoTracking().ToListAsync();
            }
        }

        public async Task<TEntity> InsertAsync(TEntity entity)
        {
            try
            {
                _context.Set<TEntity>().Add(entity);
                await _context.SaveChangesAsync();

                return entity;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public async Task UpdateAsync(TEntity entity)
        {
            _context.Entry(entity).State = EntityState.Detached;
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Properties
        public virtual IQueryable<TEntity> Table => Entities;
        public virtual IQueryable<TEntity> TableNoTracking => Entities.AsNoTracking();
        protected virtual DbSet<TEntity> Entities
        {
            get
            {
                if (_entities == null)
                    _entities = _context.Set<TEntity>();

                return _entities;
            }
        }
        #endregion
    }
}