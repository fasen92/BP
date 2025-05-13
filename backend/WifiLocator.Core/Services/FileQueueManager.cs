using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WifiLocator.Core.Models;
using WifiLocator.Core.Services.Interfaces;

namespace WifiLocator.Core.Services
{
    public class FileQueueManager : IFileQueueManager
    {
        private readonly ConcurrentQueue<FileProcessModel> _fileQueue = new();
        private readonly string _queueFilePath = "fileQueue.json";
        private readonly object _lock = new();

        public FileQueueManager()
        {
            LoadQueue(); 
        }

        public Guid EnqueueFileProcessing(string filePath)
        {
            Guid fileId = Guid.Empty;
            lock (_lock)
            {
                if (!_fileQueue.Any(f => f.FilePath == filePath))
                {
                    FileProcessModel fileProcess = new()
                    {
                        Id = Guid.NewGuid(),
                        FilePath = filePath,
                        TotalRecords = 0,
                        ProcessedRecords = 0
                    };
                    _fileQueue.Enqueue(fileProcess);
                    SaveQueue();
                    fileId = fileProcess.Id;
                }
            }
            return fileId;
        }

        public bool TryGetNextFile(out FileProcessModel fileProcessModel)
        {
            lock (_lock)
            {
                fileProcessModel = new FileProcessModel();
                FileProcessModel? fileProcess = _fileQueue.FirstOrDefault(file => !file.IsCompleted && !file.Error);

                if(fileProcess != null)
                {
                    fileProcessModel = fileProcess;
                }

                return fileProcess != null;
            }
        }

        public FileProcessModel SaveProcessed(FileProcessModel fileProcessModel)
        {
            lock (_lock)
            {
                var existingFile = _fileQueue.FirstOrDefault(f => f.FilePath == fileProcessModel.FilePath);
                if (existingFile != null)
                {
                    existingFile.ProcessedRecords = fileProcessModel.ProcessedRecords;
                    existingFile.TotalRecords = fileProcessModel.TotalRecords;

                    SaveQueue();
                    return existingFile;
                }

                return fileProcessModel;
            }
        }

        public void RemoveFileFromQueue(FileProcessModel fileProcessModel)
        {
            lock (_lock)
            {
                var files = _fileQueue.ToList();
                files.RemoveAll(f => f.FilePath == fileProcessModel.FilePath);
                _fileQueue.Clear();
                foreach (var file in files)
                {
                    _fileQueue.Enqueue(file);
                }
                SaveQueue();
            }
        }

        public void SaveQueue()
        {
            lock (_lock)
            {
                try
                {
                    JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };
                    JsonSerializerOptions options = jsonSerializerOptions;
                    string json = JsonSerializer.Serialize(_fileQueue.ToArray(), options);
                    File.WriteAllText(_queueFilePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save queue: {ex.Message}");
                }
            }
        }

        public void LoadQueue()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_queueFilePath))
                    {
                        string json = File.ReadAllText(_queueFilePath);
                        var loadedQueue = JsonSerializer.Deserialize<List<FileProcessModel>>(json);

                        if (loadedQueue != null)
                        {
                            foreach (var file in loadedQueue)
                            {
                                if (File.Exists(file.FilePath))
                                {
                                    if (!file.IsCompleted)
                                    {
                                        _fileQueue.Enqueue(file);
                                    }
                                    else
                                    {
                                        TryDeleteFile(file).Wait(); 
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load queue: {ex.Message}");
                }
            }
        }

        public FileProcessModel? GetFile(Guid fileId)
        {
            FileProcessModel? file = _fileQueue.FirstOrDefault(f => f.Id == fileId); ;
            if (file != null && (file.IsCompleted || file.Error))
            {
                RemoveFileFromQueue(file);
            }
            return file;
        }

        public async Task TryDeleteFile(FileProcessModel file, int retryCount = 5, int delayMs = 500)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    if (File.Exists(file.FilePath))
                    {
                        File.Delete(file.FilePath);
                        
                        return;
                    }
                }
                catch (IOException)
                {
                    await Task.Delay(delayMs); 
                }
            }

            Console.WriteLine($"Could not delete file: {file.FilePath}.");
        }
    }
}
