using System;

namespace WifiLocator.Infrastructure.Entities
{
    public record LocationEntity : IEntity
    {
        public required Guid Id { get; set; }
        public required int Altitude { get; set; }
        public required double Accuracy { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
        public required DateTime Seen { get; set; }
        public required double SignaldBm { get; set; }
        public required int FrequencyMHz { get; set; }
        public required string EncryptionValue { get; set; }
        public required bool UsedForApproximation { get; set; }

        public WifiEntity? Wifi { get; init; }
        public Guid WifiId { get; set; }

    }
}
