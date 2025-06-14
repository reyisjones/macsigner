using MacSigner.Models;
using MacSigner.Services;
using Microsoft.Extensions.Logging;

namespace MacSigner.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly SettingsService _settingsService;
    private readonly Mock<ILogger<SettingsService>> _loggerMock;
    private readonly string _tempSettingsFile;

    public SettingsServiceTests()
    {
        _loggerMock = new Mock<ILogger<SettingsService>>();
        _tempSettingsFile = Path.GetTempFileName();
        _settingsService = new SettingsService(_loggerMock.Object);
        
        // Use reflection to set the settings file path for testing
        var settingsPathField = typeof(SettingsService).GetField("_settingsFilePath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        settingsPathField?.SetValue(_settingsService, _tempSettingsFile);
    }

    [Fact]
    public async Task LoadSettingsAsync_WithExistingValidFile_ReturnsSettings()
    {
        // Arrange
        var expectedSettings = new AppSettings
        {
            AzureTenantId = "test-tenant",
            AzureClientId = "test-client",
            AzureClientSecret = "test-secret",
            TrustedSigningEndpoint = "https://test.endpoint.com",
            CertificateProfileName = "test-profile",
            LastSelectedPath = "/test/path",
            AutoSelectSignableFiles = false,
            ShowHiddenFiles = true,
            MaxConcurrentSigningRequests = 10
        };

        var json = System.Text.Json.JsonSerializer.Serialize(expectedSettings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_tempSettingsFile, json);

        // Act
        var result = await _settingsService.LoadSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.AzureTenantId.Should().Be(expectedSettings.AzureTenantId);
        result.AzureClientId.Should().Be(expectedSettings.AzureClientId);
        result.AzureClientSecret.Should().Be(expectedSettings.AzureClientSecret);
        result.TrustedSigningEndpoint.Should().Be(expectedSettings.TrustedSigningEndpoint);
        result.CertificateProfileName.Should().Be(expectedSettings.CertificateProfileName);
        result.LastSelectedPath.Should().Be(expectedSettings.LastSelectedPath);
        result.AutoSelectSignableFiles.Should().Be(expectedSettings.AutoSelectSignableFiles);
        result.ShowHiddenFiles.Should().Be(expectedSettings.ShowHiddenFiles);
        result.MaxConcurrentSigningRequests.Should().Be(expectedSettings.MaxConcurrentSigningRequests);
    }

    [Fact]
    public async Task LoadSettingsAsync_WithNonExistentFile_ReturnsDefaultSettings()
    {
        // Arrange
        File.Delete(_tempSettingsFile);

        // Act
        var result = await _settingsService.LoadSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.AzureTenantId.Should().BeNull();
        result.AzureClientId.Should().BeNull();
        result.AzureClientSecret.Should().BeNull();
        result.TrustedSigningEndpoint.Should().BeNull();
        result.CertificateProfileName.Should().BeNull();
        result.LastSelectedPath.Should().BeNull();
        result.AutoSelectSignableFiles.Should().BeTrue();
        result.ShowHiddenFiles.Should().BeFalse();
        result.MaxConcurrentSigningRequests.Should().Be(5);
    }

    [Fact]
    public async Task LoadSettingsAsync_WithInvalidJson_ReturnsDefaultSettings()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempSettingsFile, "invalid json content");

        // Act
        var result = await _settingsService.LoadSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.AzureTenantId.Should().BeNull();
        // Should return default settings when JSON is invalid
    }

    [Fact]
    public async Task SaveSettingsAsync_WithValidSettings_SavesCorrectly()
    {
        // Arrange
        var settings = new AppSettings
        {
            AzureTenantId = "save-tenant",
            AzureClientId = "save-client",
            AzureClientSecret = "save-secret",
            TrustedSigningEndpoint = "https://save.endpoint.com",
            CertificateProfileName = "save-profile",
            LastSelectedPath = "/save/path",
            AutoSelectSignableFiles = true,
            ShowHiddenFiles = false,
            MaxConcurrentSigningRequests = 3
        };

        // Act
        await _settingsService.SaveSettingsAsync(settings);

        // Assert
        File.Exists(_tempSettingsFile).Should().BeTrue();
        
        var savedJson = await File.ReadAllTextAsync(_tempSettingsFile);
        var savedSettings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(savedJson);
        
        savedSettings.Should().NotBeNull();
        savedSettings!.AzureTenantId.Should().Be(settings.AzureTenantId);
        savedSettings.AzureClientId.Should().Be(settings.AzureClientId);
        savedSettings.AzureClientSecret.Should().Be(settings.AzureClientSecret);
        savedSettings.TrustedSigningEndpoint.Should().Be(settings.TrustedSigningEndpoint);
        savedSettings.CertificateProfileName.Should().Be(settings.CertificateProfileName);
        savedSettings.LastSelectedPath.Should().Be(settings.LastSelectedPath);
        savedSettings.AutoSelectSignableFiles.Should().Be(settings.AutoSelectSignableFiles);
        savedSettings.ShowHiddenFiles.Should().Be(settings.ShowHiddenFiles);
        savedSettings.MaxConcurrentSigningRequests.Should().Be(settings.MaxConcurrentSigningRequests);
    }

    [Fact]
    public async Task SaveSettingsAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _settingsService.SaveSettingsAsync(null!));
    }

    [Fact]
    public async Task SaveSettingsAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var settingsFile = Path.Combine(nonExistentDir, "settings.json");
        
        var settingsPathField = typeof(SettingsService).GetField("_settingsFilePath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        settingsPathField?.SetValue(_settingsService, settingsFile);

        var settings = new AppSettings { AzureTenantId = "test" };

        try
        {
            // Act
            await _settingsService.SaveSettingsAsync(settings);

            // Assert
            Directory.Exists(nonExistentDir).Should().BeTrue();
            File.Exists(settingsFile).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(nonExistentDir))
                Directory.Delete(nonExistentDir, true);
        }
    }

    [Fact]
    public async Task RoundTrip_SaveAndLoad_PreservesAllSettings()
    {
        // Arrange
        var originalSettings = new AppSettings
        {
            AzureTenantId = "roundtrip-tenant",
            AzureClientId = "roundtrip-client",
            AzureClientSecret = "roundtrip-secret",
            TrustedSigningEndpoint = "https://roundtrip.endpoint.com",
            CertificateProfileName = "roundtrip-profile",
            LastSelectedPath = "/roundtrip/path",
            AutoSelectSignableFiles = false,
            ShowHiddenFiles = true,
            MaxConcurrentSigningRequests = 15
        };

        // Act
        await _settingsService.SaveSettingsAsync(originalSettings);
        var loadedSettings = await _settingsService.LoadSettingsAsync();

        // Assert
        loadedSettings.Should().BeEquivalentTo(originalSettings);
    }

    [Fact]
    public async Task LoadSettingsAsync_WithPartialJson_FillsDefaultsForMissingProperties()
    {
        // Arrange
        var partialJson = """
        {
            "AzureTenantId": "partial-tenant",
            "AzureClientId": "partial-client"
        }
        """;
        await File.WriteAllTextAsync(_tempSettingsFile, partialJson);

        // Act
        var result = await _settingsService.LoadSettingsAsync();

        // Assert
        result.Should().NotBeNull();
        result.AzureTenantId.Should().Be("partial-tenant");
        result.AzureClientId.Should().Be("partial-client");
        result.AzureClientSecret.Should().BeNull();
        result.AutoSelectSignableFiles.Should().BeTrue(); // Default value
        result.ShowHiddenFiles.Should().BeFalse(); // Default value
        result.MaxConcurrentSigningRequests.Should().Be(5); // Default value
    }

    [Fact]
    public async Task SaveSettingsAsync_GeneratesFormattedJson()
    {
        // Arrange
        var settings = new AppSettings
        {
            AzureTenantId = "format-test",
            AzureClientId = "format-client"
        };

        // Act
        await _settingsService.SaveSettingsAsync(settings);

        // Assert
        var savedJson = await File.ReadAllTextAsync(_tempSettingsFile);
        savedJson.Should().Contain("{\n"); // Should be formatted with indentation
        savedJson.Should().Contain("  \"AzureTenantId\""); // Should have proper indentation
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempSettingsFile))
                File.Delete(_tempSettingsFile);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
