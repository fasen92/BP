using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using WifiLocatorWeb.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
namespace WifiLocatorWeb.Api
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        public ApiClient(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task<List<WifiDisplayModel>?> GetWifiModelsInRangeAsync(
            double initLatitude1,
            double initLongitude1,
            double initLatitude2,
            double initLongitude2)
        {
            string url = $"range?latitude1={initLatitude1.ToString(CultureInfo.InvariantCulture)}" +
                         $"&longitude1={initLongitude1.ToString(CultureInfo.InvariantCulture)}" +
                         $"&latitude2={initLatitude2.ToString(CultureInfo.InvariantCulture)}" +
                         $"&longitude2={initLongitude2.ToString(CultureInfo.InvariantCulture)}";

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<WifiDisplayModel>>();
            }

            return null;
        }

        public async Task<StatusResponse?> GetStatusAsync(string fileId)
        {
            return await _httpClient.GetFromJsonAsync<StatusResponse>($"status?fileId={fileId}");
        }

        public async Task<UploadResponse?> UploadFileAsync(IBrowserFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                if(file.ContentType == "text/csv")
                {
                    using var reader = new StreamReader(file.OpenReadStream(50 * 1024 * 1024));
                    var csvText = await reader.ReadToEndAsync();
                    var base64Csv = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(csvText));

                    var base64Gzip = await _jsRuntime.InvokeAsync<string>("compressCsvToGzip", base64Csv);
                    var gzipBytes = Convert.FromBase64String(base64Gzip);

                    HttpContent httpContent = new ByteArrayContent(gzipBytes);
                    httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/gzip");

                    content.Add(httpContent, "file", Path.ChangeExtension(file.Name, ".csv.gz"));
                }
                else
                {
                    var streamContent = new StreamContent(file.OpenReadStream(20 * 1024 * 1024));
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    content.Add(streamContent, "file", file.Name);
                }

                var response = await _httpClient.PostAsync("upload", content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UploadResponse>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Upload failed: {error}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error during upload: " + ex.Message, ex);
            }
        }
    }
}
