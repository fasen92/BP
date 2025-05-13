using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WifiLocator.Infrastructure.Entities
{
    public record WifiEntity : IEntity
    {
        public required Guid Id { get; set; }
        public required string Ssid { get; set; }
        public required string Bssid { get; set; }
        public required double? ApproximatedLatitude { get; set; }
        public required double? ApproximatedLongitude { get; set; }
        public required string Encryption {  get; set; }
        public required int Channel {  get; set; }
        public required double? UncertaintyRadius { get; set; }

        public ICollection<LocationEntity> Locations { get; init; } = [];

        public Guid? AddressId { get; set; }
        public AddressEntity? Address { get; set; }
    }
}
