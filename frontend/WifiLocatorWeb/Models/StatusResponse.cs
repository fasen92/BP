namespace WifiLocatorWeb.Models
{
    public class StatusResponse
    {
        public required string Id { get; set; }
        public int ProcessedRecords { get; set; }
        public bool IsCompleted { get; set; }
    }
}
