using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Models;

namespace WifiLocator.Core.Approximation
{
    public class WifiDataObject(WifiModel wifiModel, List<LocationModel> locationsAll)
    {
        public WifiModel WifiModel { get; set; } = wifiModel;
        public List<LocationModel> LocationsAll { get; set; } = locationsAll;
        public List<LocationModel> LocationsToSave { get; set; } = [];
        public List<LocationModel> LocationsForApproximation { get; set; } = [];
        public HashSet<LocationModel> PreviousUsedLocations { get; set; } = [];
    }
}
