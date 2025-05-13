namespace WifiLocatorWeb.Models
{
    public class UploadStatus
    {
        public required string Id { get; set; }
        public required string FileName { get; set; }
        public int ProcessedRecords { get; set; }
        public bool IsCompleted { get; set; }
        public bool Error { get; set; }
    }
}
