using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Models
{
    public class Bounds
    {
        public double Latitude1 { get; set; }  
        public double Longitude1 { get; set; } 
        public double Latitude2 { get; set; }  
        public double Longitude2 { get; set; } 
    }

    public class RangeRequest
    {
        public required Bounds OuterBounds { get; set; }
        public Bounds? InnerBounds { get; set; }
    }
}
