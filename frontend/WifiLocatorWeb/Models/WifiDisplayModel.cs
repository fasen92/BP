namespace WifiLocatorWeb.Models
{
    public record WifiDisplayModel
    {
        public required string Ssid { get; set; }
        public required string Bssid { get; set; }
        public required double ApproximatedLatitude { get; set; }
        public required double ApproximatedLongitude { get; set; }
        public required DateTime FirstSeen { get; set; }
        public required DateTime LastSeen { get; set; }
        public required string Encryption { get; set; }
        public required int Channel {  get; set; }
        public required double UncertaintyRadius { get; set; }
        public string? Address { get; set; }
    }
}
