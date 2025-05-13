using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Models;
using WifiLocator.Infrastructure.Entities;

namespace WifiLocator.Core.Mappers
{
    public class CompositeWifiMapper(WifiMapper wifiMapper, WifiDisplayMapper displayMapper)
    {
        private readonly WifiMapper _wifiMapper = wifiMapper;
        private readonly WifiDisplayMapper _displayMapper = displayMapper;

        public WifiModel MapToWifiModel(WifiEntity entity)
        {
            return _wifiMapper.MapToModel(entity);
        }

        public WifiDisplayModel MapToDisplayModel(WifiEntity entity)
        {
            return _displayMapper.MapToModel(entity);
        }

        public WifiEntity MapToEntity(WifiModel model, Guid? joinId)
        {
            return _wifiMapper.MapToEntity(model, joinId);
        }
    }
}
