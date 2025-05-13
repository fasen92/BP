using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Models
{
    public record AddressModel
    {
        public required Guid Id { get; set; }
        public required string Country { get; set; }
        public required string City { get; set; }
        public required string Road { get; set; }
        public required string Region { get; set; }
        public required string PostalCode { get; set; }


        public static AddressModel Empty => new()
        {
            Id = Guid.Empty,
            Country = String.Empty,
            City = String.Empty,
            Road = String.Empty,
            Region = String.Empty,
            PostalCode = String.Empty
        };
    }
}
