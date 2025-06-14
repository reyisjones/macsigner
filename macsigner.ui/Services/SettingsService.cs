using System;
using System.IO;
using System.Threading.Tasks;
using MacSigner.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MacSigner.Services
{
    public interface ISettingsService
    {
        Task<AppSettings> LoadSettingsAsync();
        Task SaveSettingsAsync(AppSettings settings);
        string GetSettingsFilePath();
    }

    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private readonly string _settingsDirectory;
        private readonly string _settingsFilePath;

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Use application data directory
            _settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MacSigner");
            
            _settingsFilePath = Path.Combine(_settingsDirectory, "appsettings.json");
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    _logger.LogInformation("Settings file not found, creating default settings");
                    var defaultSettings = new AppSettings();
                    await SaveSettingsAsync(defaultSettings);
                    return defaultSettings;
                }

                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                
                _logger.LogInformation("Settings loaded successfully");
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings, using defaults");
                return new AppSettings();
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            try
            {
                var settingsDirectory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(settingsDirectory) && !Directory.Exists(settingsDirectory))
                {
                    Directory.CreateDirectory(settingsDirectory);
                }

                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                await File.WriteAllTextAsync(_settingsFilePath, json);
                
                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                throw;
            }
        }

        public string GetSettingsFilePath()
        {
            return _settingsFilePath;
        }
    }
}
