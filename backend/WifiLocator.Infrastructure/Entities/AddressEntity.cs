using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Infrastructure.Entities
{
    public record AddressEntity : IEntity
    {
        public required Guid Id { get; set; }
        public required string Country { get; set; }
        public required string City { get; set; }
        public required string Road { get; set; }
        public string? Region { get; set; }
        public required string PostalCode { get; set; }
    }
}
