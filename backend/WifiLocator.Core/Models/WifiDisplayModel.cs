using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Models
{
    public record WifiDisplayModel
    {
        public required string Ssid { get; set; }
        public required string Bssid { get; set; }
        public required double? ApproximatedLatitude { get; set; }
        public required double? ApproximatedLongitude { get; set; }
        public required DateTime FirstSeen { get; set; }
        public required DateTime LastSeen {  get; set; }
        public required string Encryption {  get; set; }
        public required int Channel {  get; set; }
        public required double? UncertaintyRadius { get; set; }

        public string? Address { get; set; }
        

        public static WifiDisplayModel Empty => new()
        {
            Ssid = string.Empty,
            Bssid = string.Empty,
            ApproximatedLatitude = null,
            ApproximatedLongitude = null,
            FirstSeen = DateTime.MinValue,
            LastSeen = DateTime.MinValue,
            Encryption = string.Empty,
            Channel = 0,
            Address = string.Empty,
            UncertaintyRadius = null,
        };
    }
}
