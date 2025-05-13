using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Models
{
    public class FilterRequest
    {
        public required Bounds OuterBounds { get; set; }
        public Bounds? InnerBounds { get; set; }
        public string? Bssid { get; set; }
        public string? Ssid { get; set; }
        public DateTime? DateStart { get; set; }
        public DateTime? DateEnd { get; set; }
    }
}
