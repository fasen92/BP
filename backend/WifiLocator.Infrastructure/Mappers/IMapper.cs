using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Infrastructure.Entities;

namespace WifiLocator.Infrastructure.Mappers
{
    public interface IMapper<TEntity> 
        where TEntity : class, IEntity
    {
        void MapToEntity(TEntity entity, TEntity existingEntity);
    }
}
