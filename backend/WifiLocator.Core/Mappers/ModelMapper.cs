using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Mappers
{
    public interface ModelMapper<TEntity, TModel>
    {
        TModel MapToModel(TEntity? entity);
        TEntity MapToEntity(TModel model, Guid joinId);
        TEntity MapToEntity(TModel model);
    }
}
