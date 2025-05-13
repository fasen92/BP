using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Models
{
    public record LocationModel
    {
        public Guid Id { get; set; }
        public required int Altitude { get; set; }
        public required double Accuracy { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
        public required DateTime Seen { get; set; }
        public required double SignaldBm { get; set; }
        public required int FrequencyMHz { get; set; }
        public required string EncryptionValue { get; set; }
        public required bool UsedForApproximation {  get; set; }

        public static LocationModel Empty => new()
        {
            Id = Guid.Empty,
            Altitude = 0, 
            Accuracy = 0, 
            Latitude = 0, 
            Longitude = 0,
            Seen = DateTime.Today,
            SignaldBm = 0,
            FrequencyMHz = 0,
            EncryptionValue = string.Empty,
            UsedForApproximation = false
        };
    }
}
