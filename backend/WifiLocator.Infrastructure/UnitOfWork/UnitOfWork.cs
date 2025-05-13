using Microsoft.EntityFrameworkCore;
using WifiLocator.Infrastructure.Repositories;
using WifiLocator.Infrastructure.Entities;
using WifiLocator.Infrastructure.Mappers;

namespace WifiLocator.Infrastructure.UnitOfWork
{
    public sealed class UnitOfWork(WifiLocatorDbContext dbContext) : IUnitOfWork
    {
        private readonly WifiLocatorDbContext _dbContext = dbContext;

        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class, IEntity { 
            return new Repository<TEntity>(_dbContext);
        }

        public async Task SaveChangesAsync()
        {
            await _dbContext.SaveChangesAsync();
        }
        public void Dispose() {
            _dbContext.Dispose();
        }
    }
}
