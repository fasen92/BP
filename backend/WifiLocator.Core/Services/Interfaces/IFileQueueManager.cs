using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WifiLocator.Core.Models;

namespace WifiLocator.Core.Services.Interfaces
{
    public interface IFileQueueManager
    {
        Guid EnqueueFileProcessing(string filePath);
        bool TryGetNextFile(out FileProcessModel fileProcessModel);
        FileProcessModel SaveProcessed(FileProcessModel fileProcessModel);
        void RemoveFileFromQueue(FileProcessModel fileProcessModel);
        void SaveQueue();
        void LoadQueue();
        FileProcessModel? GetFile(Guid filePath);
        Task TryDeleteFile(FileProcessModel file, int retryCount = 5, int delayMs = 500);
    }

}
