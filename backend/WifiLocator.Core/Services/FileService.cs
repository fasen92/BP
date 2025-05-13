using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WifiLocator.Core.Models;
using WifiLocator.Core.Services.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using System.IO.Compression;

namespace WifiLocator.Core.Services
{
    public partial class FileService : IFileService
    {
        private readonly string _tempFolderPath = Path.Combine(Path.GetTempPath(), "WifiLocatorUploads");
        public async IAsyncEnumerable<(WifiModel wifi, LocationModel locations)> ProcessCSVAsync(string filePath, int proccessedWifiRecords,
            Action<int>? setTotalRecords = null) 
        {

            using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader streamReader = new(stream);
            using CsvReader csvReader = new(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null
            });

            // First line of the file contains metadata about the app, device, and its parameters
            string? fileInfo = await streamReader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(fileInfo))
            {
                throw new ArgumentException("Invalid file. Please upload a CSV file with Wifi records.");
            }

            string[] validHeaders =
            [
                "MAC", "SSID", "AuthMode", "FirstSeen", "Channel", "Frequency", "RSSI",
                "CurrentLatitude", "CurrentLongitude",  "AltitudeMeters", "AccuracyMeters",
                "RCOIs", "MfgrId", "Type"
            ];

            // Check headers
            if (!await csvReader.ReadAsync() || !csvReader.ReadHeader())
            {
                throw new ArgumentException("Uploaded file is not valid, missing headers.");
            }

            if (csvReader.HeaderRecord == null)
            {
                throw new ArgumentException("Uploaded file does not contain a valid header row.");
            }

            string[] missingHeaders = validHeaders
                .Where(header => !csvReader.HeaderRecord.Contains(header))
                .ToArray();

            if (missingHeaders.Length > 0)
            {
                throw new ArgumentException($"Uploaded file is not valid, missing headers: {string.Join(", ", missingHeaders)}");
            }

            Regex netIdRegex = BSSIDRegex();
            int wifiRecordsFound = 0;
            bool totalSet = false;

            while (await csvReader.ReadAsync()) 
            {
                if (csvReader.GetRecord<dynamic>() is not IDictionary<string, object> recordDict) continue;

                // Filter out non-wifi records
                string type = string.Empty;

                if (recordDict.TryGetValue("Type", out var typeValue) && typeValue != null)
                {
                    type = (typeValue.ToString() ?? string.Empty).Trim();
                }
                if (!type.Equals("WIFI", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Validation of important records
                if (!recordDict.TryGetValue("SSID", out var ssid) || string.IsNullOrWhiteSpace(ssid?.ToString()))
                {
                    continue;
                }

                if (!recordDict.TryGetValue("MAC", out var netIdObj) || netIdObj is not string rawNetId)
                {
                    continue;
                }

                string netIdString = rawNetId.Trim();

                if (string.IsNullOrWhiteSpace(netIdString) || !netIdRegex.IsMatch(netIdString))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(netIdString) || !netIdRegex.IsMatch(netIdString))
                {
                    continue;
                }

                if (!double.TryParse(recordDict.TryGetValue("CurrentLatitude", out var latitudeValue) ? latitudeValue?.ToString()
                    : null, NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude)
                    || latitude < -90 || latitude > 90)
                {
                    continue;
                }

                if (!double.TryParse(recordDict.TryGetValue("CurrentLongitude", out var longitudeValue) ? longitudeValue?.ToString()
                    : null, NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude)
                    || longitude < -180 || longitude > 180)
                {
                    continue;
                }

                if (!double.TryParse(recordDict.TryGetValue("RSSI", out var signalValue) ? signalValue?.ToString()
                    : null, NumberStyles.Float, CultureInfo.InvariantCulture, out double signal)
                    || signal < -110 || signal > 0)
                {
                    continue;
                }

                if (!double.TryParse(recordDict.TryGetValue("AccuracyMeters", out var accuracyValue) ? accuracyValue?.ToString()
                    : null, NumberStyles.Float, CultureInfo.InvariantCulture, out double accuracy)
                    || accuracy < 0)
                {
                    continue;
                }

                if (!recordDict.TryGetValue("AltitudeMeters", out var altitudeValue) ||
                    !double.TryParse(altitudeValue?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double altitude))
                {
                    continue;
                }

                if (!recordDict.TryGetValue("Frequency", out var frequencyValue) ||
                    !int.TryParse(frequencyValue?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int frequency))
                {
                    continue;
                }

                if (!recordDict.TryGetValue("Channel", out var ChannelValue) ||
                    !int.TryParse(ChannelValue?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int channel))
                {
                    continue;
                }

                string authMode = recordDict["AuthMode"]?.ToString() ?? string.Empty;

                WifiModel wifiModel = new()
                {
                    Id = Guid.Empty,
                    Ssid = ssid?.ToString() ?? String.Empty,
                    Bssid = netIdString,
                    ApproximatedLatitude = null,
                    ApproximatedLongitude = null,
                    UncertaintyRadius = null,
                    Channel = channel,
                    FirstSeen = DateTime.TryParse(recordDict["FirstSeen"]?.ToString(), out var firstSeen) ? firstSeen : DateTime.MinValue,
                    LastSeen = DateTime.MaxValue,
                    Encryption = ExtractExncryption(authMode)
                };

                LocationModel locationModel = new()
                {
                    Id = Guid.Empty,
                    Altitude = (int)Math.Round(altitude),
                    Accuracy = accuracy,
                    Latitude = latitude,
                    Longitude = longitude,
                    Seen = wifiModel.FirstSeen,
                    SignaldBm = signal,
                    FrequencyMHz = frequency,
                    EncryptionValue = wifiModel.Encryption,
                    UsedForApproximation = false
                };

                wifiRecordsFound++;

                if (wifiRecordsFound <= proccessedWifiRecords) // skipping already processed wifi records
                {
                    continue;
                }

                yield return (wifiModel, locationModel);
            }

            if (!totalSet && wifiRecordsFound > 0)
            {
                setTotalRecords?.Invoke(wifiRecordsFound);
                totalSet = true;
            }

        }

        public static string ExtractExncryption(string exncryptionString)
        {
            Regex encryptRegex = EncryptionRegex();

            var match = encryptRegex.Match(exncryptionString);

            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        public async Task<string> SaveTemporaryFileAsync(IFormFile file)
        {
            try
            {
                if (!Directory.Exists(_tempFolderPath))
                {
                    Directory.CreateDirectory(_tempFolderPath);
                }

                string originalFileName = Path.GetFileName(file.FileName);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
                string extension = Path.GetExtension(originalFileName).ToLowerInvariant();

                // Normalize .csv.gz filenames
                bool isGzip = originalFileName.EndsWith(".csv.gz", StringComparison.OrdinalIgnoreCase);

                string fileName = $"{Guid.NewGuid()}_{(isGzip ? fileNameWithoutExtension : originalFileName)}";
                string filePath = Path.Combine(_tempFolderPath, fileName);

                if (isGzip)
                {
                    using var gzipStream = new GZipStream(file.OpenReadStream(), CompressionMode.Decompress);
                    using var outputStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                    await gzipStream.CopyToAsync(outputStream);
                }
                else
                {
                    using var outputStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                    await file.CopyToAsync(outputStream);
                }

                return filePath; 
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save file: {ex.Message}", ex);
            }
        }

        [GeneratedRegex(@"\[(.*?)\-")]
        private static partial Regex EncryptionRegex();

        [GeneratedRegex(@"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$")]
        private static partial Regex BSSIDRegex();
    }
}
