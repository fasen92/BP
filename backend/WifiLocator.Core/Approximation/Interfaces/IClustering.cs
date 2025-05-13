using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Models;

namespace WifiLocator.Core.Approximation.Interfaces
{
    public interface IClustering
    {
        Dictionary<int, List<LocationModel>> DBSCAN(List<LocationModel> points, double epsilon, int minPoints);
    }
}
