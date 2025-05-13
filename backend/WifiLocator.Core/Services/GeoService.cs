using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WifiLocator.Core.Models;
using WifiLocator.Core.Services.Interfaces;

namespace WifiLocator.Core.Services
{
    public class GeoService(HttpClient httpClient) : IGeoService
    {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<AddressModel> GetAddressFromLocation(WifiModel wifiModel)
        {
            if (wifiModel.ApproximatedLatitude == null || wifiModel.ApproximatedLongitude == null)
            {
                return AddressModel.Empty;
            }

            string url = $"https://nominatim.openstreetmap.org/reverse?format=json" +
                $"&lat={wifiModel.ApproximatedLatitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                $"&lon={wifiModel.ApproximatedLongitude.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "C# Web API");

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            await Task.Delay(1000); // Rate limit 1s

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to retrieve location data.");
                return AddressModel.Empty; 
            }

            AddressModel addressModel = AddressModel.Empty;

            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("address", out JsonElement addressElement))
            {
                addressModel.Country = addressElement.TryGetProperty("country", out var country)
                    ? country.GetString() ?? string.Empty
                    : string.Empty;

                addressModel.City = addressElement.TryGetProperty("city", out var city)
                    ? city.GetString() ?? string.Empty
                    : (addressElement.TryGetProperty("town", out var town)
                        ? town.GetString() ?? string.Empty
                        : string.Empty);

                addressModel.Road = addressElement.TryGetProperty("road", out var road)
                    ? road.GetString() ?? string.Empty
                    : string.Empty;

                addressModel.Region = addressElement.TryGetProperty("state", out var state)
                    ? state.GetString() ?? string.Empty
                    : string.Empty;

                addressModel.PostalCode = addressElement.TryGetProperty("postcode", out var postcode)
                    ? postcode.GetString() ?? string.Empty
                    : string.Empty;
            }
            else
            {
                Console.WriteLine("API response missing 'address' field.");
            }

            return addressModel;
        }
    }
}