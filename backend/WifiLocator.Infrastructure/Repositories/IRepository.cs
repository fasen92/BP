using WifiLocator.Infrastructure.Entities;

namespace WifiLocator.Infrastructure.Repositories
{
    public interface IRepository<TEntity>
        where TEntity : class, IEntity
    {
        public IQueryable<TEntity> Query();
        Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);
        Task<List<TEntity>> BulkSaveAsync(IEnumerable<TEntity> entities);
        Task<TEntity> GetByIdAsync(Guid Id, params string[]? navigationPaths);
        Task<TEntity> AddAsync(TEntity entity);
        ValueTask<bool> ExistsAsync(TEntity entity);
        Task<TEntity> Update(TEntity entity);
        void Delete(TEntity entity);
    }
}
