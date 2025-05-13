using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Infrastructure.UnitOfWork
{
    public class UnitOfWorkFactory(IDbContextFactory<WifiLocatorDbContext> dbContextFactory) : IUnitOfWorkFactory
    {
        private readonly IDbContextFactory<WifiLocatorDbContext> _dbContextFactory = dbContextFactory;

        public IUnitOfWork CreateUnitOfWork()
        {
            var dbContext = _dbContextFactory.CreateDbContext(); 
            return new UnitOfWork(dbContext);
        }

        public static void Dispose() {

        }
    }
}
