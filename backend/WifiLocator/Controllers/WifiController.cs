using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using WifiLocator.Core.Models;
using WifiLocator.Core.Services.Interfaces;


namespace WifiLocator.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WifiController(IWifiService wifiService, ILocationService locationService, IFileService fileService, IFileQueueManager fileQueueManager) : ControllerBase
    {
        private readonly IWifiService _wifiService = wifiService;
        private readonly ILocationService _locationService = locationService;
        private readonly IFileService _fileService = fileService;
        private readonly IFileQueueManager _fileQueueManager = fileQueueManager;

        [HttpGet("filter")]
        public async Task<ActionResult<List<WifiDisplayModel>>> GetByFiltersAsync(
            [FromQuery] double latitude1,
            [FromQuery] double longitude1,
            [FromQuery] double latitude2,
            [FromQuery] double longitude2,
            [FromQuery] double? innerLatitude1,
            [FromQuery] double? innerLongitude1,
            [FromQuery] double? innerLatitude2,
            [FromQuery] double? innerLongitude2,
            [FromQuery] string? ssid,
            [FromQuery] string? bssid,
            [FromQuery] DateTime? dateStart,
            [FromQuery] DateTime? dateEnd)
        {
            FilterRequest filter = new FilterRequest{
                OuterBounds = new Bounds{
                        Latitude1 = latitude1,
                        Longitude1 = longitude1,
                        Latitude2 = latitude2,
                        Longitude2 = longitude2
                },

                InnerBounds = (innerLatitude1.HasValue && innerLongitude1.HasValue && innerLatitude2.HasValue && innerLongitude2.HasValue)
                ? new Bounds
                {
                    Latitude1 = innerLatitude1.Value,
                    Longitude1 = innerLongitude1.Value,
                    Latitude2 = innerLatitude2.Value,
                    Longitude2 = innerLongitude2.Value
                }
                : null,

                Ssid = ssid,
                Bssid = bssid,
                DateStart = dateStart,
                DateEnd = dateEnd
            };

            try
            {
                List<WifiDisplayModel> wifiList = await _wifiService.GetByFiltersAsync(filter);  
                if (wifiList == null || wifiList.Count == 0)
                {
                    return Ok(new List<WifiDisplayModel>());
                }

                return Ok(wifiList);
            }
            catch (Exception)
            {
                return NotFound($"For this combination of filters no WiFi has been found.");
            }
        }

        [HttpGet("range")]
        public async Task<ActionResult<List<WifiDisplayModel>>> GetByRangeAsync(
            [FromQuery] double latitude1,
            [FromQuery] double longitude1,
            [FromQuery] double latitude2,
            [FromQuery] double longitude2,
            [FromQuery] double? innerLatitude1,
            [FromQuery] double? innerLongitude1,
            [FromQuery] double? innerLatitude2,
            [FromQuery] double? innerLongitude2)
        {
            var request = new RangeRequest
            {
                OuterBounds = new Bounds
                {
                    Latitude1 = latitude1,
                    Longitude1 = longitude1,
                    Latitude2 = latitude2,
                    Longitude2 = longitude2
                },
                InnerBounds = (innerLatitude1.HasValue && innerLongitude1.HasValue && innerLatitude2.HasValue && innerLongitude2.HasValue)
            ? new Bounds
            {
                Latitude1 = innerLatitude1.Value,
                Longitude1 = innerLongitude1.Value,
                Latitude2 = innerLatitude2.Value,
                Longitude2 = innerLongitude2.Value
            }
            : null
            };
            try
            {
                List<WifiDisplayModel> wifiList = await _wifiService.GetByRangeAsync(request);

                if (wifiList == null || wifiList.Count == 0)
                {
                    return Ok(new List<WifiDisplayModel>());
                }

                return Ok(wifiList);
            }
            catch (Exception)
            {
                return NotFound("An error occurred while fetching wifi records.");
            }
        }

        [HttpGet("locations")]
        public async Task<ActionResult<List<LocationModel>>> GetLocationsAsync(
            [FromQuery] string bssid)
        {
            try
            {
                List<LocationModel> locationList = await _locationService.GetLocationsByBssidAsync(bssid);
                if (locationList == null || locationList.Count == 0)
                {
                    return NotFound("No locations found for given WiFi.");
                }

                return Ok(locationList);
            }
            catch (Exception)
            {
                return NotFound("No locations found for given WiFi.");
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadCsvAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Invalid file.");
                }

                var fileName = file.FileName.ToLowerInvariant();

                if (!(fileName.EndsWith(".csv") || fileName.EndsWith(".csv.gz")))
                {
                    return BadRequest("Invalid file format. Only .csv or .csv.gz files are allowed.");
                }

                string tempFilePath = await _fileService.SaveTemporaryFileAsync(file);

                Guid fileId = _fileQueueManager.EnqueueFileProcessing(tempFilePath);

                return Accepted(new { message = "File processing started", fileId });

            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing the file.");
            }
        }

        [HttpGet("status")]
        public IActionResult GetFileProcessingStatus([FromQuery] Guid fileId)
        {
            var fileStatus = _fileQueueManager.GetFile(fileId);

            if (fileStatus == null)
            {
                return NotFound(new { message = "File not found in processing queue." });
            }

            return Ok(new
            {
                fileStatus.Id,
                fileStatus.ProcessedRecords,
                fileStatus.IsCompleted
            });
        }
    }
}
