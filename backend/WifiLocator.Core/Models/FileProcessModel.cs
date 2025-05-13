using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WifiLocator.Core.Models
{
    public record FileProcessModel
    {
        public Guid Id { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public int ProcessedRecords { get; set; } = 0;
        public int TotalRecords { get; set; } = 0;
        public bool IsCompleted => TotalRecords > 0 && ProcessedRecords >= TotalRecords;
        public bool Error { get; set; } = false;
    }
}
