using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Models;

namespace WifiLocator.Core.Services.Interfaces
{
    public interface IFileService
    {
        IAsyncEnumerable<(WifiModel wifi, LocationModel locations)> ProcessCSVAsync(string filePath, int proccessedLines, Action<int>? setTotalRecords = null);
        Task<string> SaveTemporaryFileAsync(IFormFile file);
    }
}
