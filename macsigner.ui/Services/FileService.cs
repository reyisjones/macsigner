using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MacSigner.Models;
using Microsoft.Extensions.Logging;

namespace MacSigner.Services
{
    public interface IFileService
    {
        Task<List<SignableFile>> ScanDirectoryAsync(string directoryPath, bool includeSubdirectories = true);
        Task<bool> ValidateFileAsync(string filePath);
        Task<bool> BackupFileAsync(string filePath);
        Task<bool> ReplaceFileWithSignedVersionAsync(string originalPath, byte[] signedFileData);
    }

    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly AppSettings _settings;

        private readonly HashSet<string> _signableExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".msi", ".cab", ".ocx", ".sys", ".scr",
            ".dylib", ".app", ".framework", ".bundle", ".kext",
            ".jar", ".apk", ".ipa", ".xap", ".vsix", ".nupkg"
        };

        public FileService(ILogger<FileService> logger, AppSettings settings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<List<SignableFile>> ScanDirectoryAsync(string directoryPath, bool includeSubdirectories = true)
        {
            var signableFiles = new List<SignableFile>();

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogWarning($"Directory does not exist: {directoryPath}");
                    return signableFiles;
                }

                _logger.LogInformation($"Scanning directory: {directoryPath}");

                var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(directoryPath, "*", searchOption);

                await Task.Run(() =>
                {
                    foreach (var filePath in files)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(filePath);
                            
                            // Skip hidden files unless configured to show them
                            if (!_settings.ShowHiddenFiles && fileInfo.Name.StartsWith("."))
                                continue;

                            // Check if file has a signable extension
                            if (_signableExtensions.Contains(fileInfo.Extension))
                            {
                                var signableFile = new SignableFile(filePath)
                                {
                                    IsSelected = _settings.AutoSelectSignableFiles
                                };
                                signableFiles.Add(signableFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to process file: {filePath}");
                        }
                    }
                });

                _logger.LogInformation($"Found {signableFiles.Count} signable files in {directoryPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to scan directory: {directoryPath}");
            }

            return signableFiles.OrderBy(f => f.FileName).ToList();
        }

        public async Task<bool> ValidateFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"File does not exist: {filePath}");
                    return false;
                }

                var fileInfo = new FileInfo(filePath);
                
                // Check if file is accessible
                using var stream = File.OpenRead(filePath);
                
                // Check file size (arbitrary limit of 2GB)
                if (fileInfo.Length > 2L * 1024 * 1024 * 1024)
                {
                    _logger.LogWarning($"File is too large: {filePath} ({fileInfo.Length} bytes)");
                    return false;
                }

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to validate file: {filePath}");
                return false;
            }
        }

        public async Task<bool> BackupFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Cannot backup file that does not exist: {filePath}");
                    return false;
                }

                var backupPath = $"{filePath}.backup.{DateTime.UtcNow:yyyyMMddHHmmss}";
                await Task.Run(() => File.Copy(filePath, backupPath, overwrite: false));

                _logger.LogInformation($"Created backup: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to backup file: {filePath}");
                return false;
            }
        }

        public async Task<bool> ReplaceFileWithSignedVersionAsync(string originalPath, byte[] signedFileData)
        {
            try
            {
                if (!File.Exists(originalPath))
                {
                    _logger.LogWarning($"Original file does not exist: {originalPath}");
                    return false;
                }

                // Create backup first
                var backupSuccess = await BackupFileAsync(originalPath);
                if (!backupSuccess)
                {
                    _logger.LogError($"Failed to create backup before replacing file: {originalPath}");
                    return false;
                }

                // Replace with signed version
                await File.WriteAllBytesAsync(originalPath, signedFileData);

                _logger.LogInformation($"Successfully replaced file with signed version: {originalPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to replace file with signed version: {originalPath}");
                return false;
            }
        }
    }
}
