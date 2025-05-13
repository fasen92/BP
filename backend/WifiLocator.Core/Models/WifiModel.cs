using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Models
{
    public record WifiModel
    {
        public Guid Id { get; set; }
        public required string Ssid { get; set; }
        public required string Bssid { get; set; }
        public required double? ApproximatedLatitude { get; set; }
        public required double? ApproximatedLongitude { get; set; }
        public required DateTime FirstSeen { get; set; }
        public required DateTime LastSeen { get; set; }
        public required string Encryption { get; set; }
        public required int Channel {  get; set; }
        public required double? UncertaintyRadius { get; set; }

        public AddressModel? Address { get; set; }

        public ObservableCollection<LocationModel>? Locations { get; init; } = [];

        public static WifiModel Empty => new()
        {
            Id = Guid.Empty,
            Ssid = string.Empty,
            Bssid = string.Empty,
            ApproximatedLatitude = null,
            ApproximatedLongitude = null,
            FirstSeen = DateTime.MinValue,
            LastSeen = DateTime.MaxValue,
            Encryption = string.Empty,
            Channel = 0,
            UncertaintyRadius = null,
        };
    }
}
