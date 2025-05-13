namespace WifiLocatorWeb.Models
{
    public class WifiListModel
    {
        public required string Ssid { get; set; }
        public required string Bssid { get; set; }
        public string? Address { get; set; }
    }
}
