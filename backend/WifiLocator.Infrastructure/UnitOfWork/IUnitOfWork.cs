using WifiLocator.Infrastructure.Entities;
using WifiLocator.Infrastructure.Repositories;


namespace WifiLocator.Infrastructure.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<TEntity> GetRepository<TEntity>() where TEntity : class, IEntity;
        Task SaveChangesAsync();
    }
}
