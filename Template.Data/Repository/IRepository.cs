using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Template.Data.Repository
{
   public interface IRepository<TEntity>
    {
        #region Methods

        Task<bool> Exists(Expression<Func<TEntity, bool>> predicate);

        void Insert(TEntity entity);
        void Insert(IEnumerable<TEntity> entities);
        void Update(TEntity entity);
        void Update(IEnumerable<TEntity> entities);

        void Delete(TEntity entity);

        void Delete(IEnumerable<TEntity> entities);

        List<TEntity> GetList();
        List<TEntity> GetList(String[] Includes);
        List<TEntity> GetList(Expression<Func<TEntity, bool>> Filter);

        TEntity GetOne(Expression<Func<TEntity, Boolean>> Filter);
        TEntity GetOne(Expression<Func<TEntity, Boolean>> Filter, String[] Includes);
        Task<TEntity> GetOneAsync(Expression<Func<TEntity, Boolean>> Filter);
        Task<TEntity> GetOneAsync(Expression<Func<TEntity, Boolean>> Filter, String[] Includes);

        Task<List<TEntity>> GetListAsync();
        Task<List<TEntity>> GetListAsync(String[] Includes);
        Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> Filter);
        Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> Filter, String[] Includes);

        Task<TEntity> InsertAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);

        #endregion

        #region Properties
        IQueryable<TEntity> Table { get; }
        IQueryable<TEntity> TableNoTracking { get; }

        #endregion
    }
}
