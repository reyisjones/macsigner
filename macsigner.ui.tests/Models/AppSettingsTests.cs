using AutoFixture.Xunit2;
using MacSigner.Models;

namespace MacSigner.Tests.Models;

public class AppSettingsTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.AzureTenantId.Should().BeNull();
        settings.AzureClientId.Should().BeNull();
        settings.AzureClientSecret.Should().BeNull();
        settings.TrustedSigningEndpoint.Should().BeNull();
        settings.CertificateProfileName.Should().BeNull();
        settings.LastSelectedPath.Should().BeNull();
        settings.AutoSelectSignableFiles.Should().BeTrue();
        settings.ShowHiddenFiles.Should().BeFalse();
        settings.MaxConcurrentSigningRequests.Should().Be(5);
    }

    [Theory]
    [AutoData]
    public void Properties_CanBeSetAndRetrieved(
        string tenantId,
        string clientId,
        string clientSecret,
        string endpoint,
        string profileName,
        string lastPath)
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.AzureTenantId = tenantId;
        settings.AzureClientId = clientId;
        settings.AzureClientSecret = clientSecret;
        settings.TrustedSigningEndpoint = endpoint;
        settings.CertificateProfileName = profileName;
        settings.LastSelectedPath = lastPath;

        // Assert
        settings.AzureTenantId.Should().Be(tenantId);
        settings.AzureClientId.Should().Be(clientId);
        settings.AzureClientSecret.Should().Be(clientSecret);
        settings.TrustedSigningEndpoint.Should().Be(endpoint);
        settings.CertificateProfileName.Should().Be(profileName);
        settings.LastSelectedPath.Should().Be(lastPath);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AutoSelectSignableFiles_CanBeSetAndRetrieved(bool value)
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.AutoSelectSignableFiles = value;

        // Assert
        settings.AutoSelectSignableFiles.Should().Be(value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShowHiddenFiles_CanBeSetAndRetrieved(bool value)
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.ShowHiddenFiles = value;

        // Assert
        settings.ShowHiddenFiles.Should().Be(value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void MaxConcurrentSigningRequests_CanBeSetAndRetrieved(int value)
    {
        // Arrange
        var settings = new AppSettings();

        // Act
        settings.MaxConcurrentSigningRequests = value;

        // Assert
        settings.MaxConcurrentSigningRequests.Should().Be(value);
    }

    [Fact]
    public void IsConfigured_WithAllRequiredSettings_ReturnsTrue()
    {
        // Arrange
        var settings = new AppSettings
        {
            AzureTenantId = "tenant-id",
            AzureClientId = "client-id",
            AzureClientSecret = "client-secret",
            TrustedSigningEndpoint = "https://endpoint.com",
            CertificateProfileName = "profile-name"
        };

        // Act & Assert
        settings.IsConfigured().Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "client-id", "client-secret", "https://endpoint.com", "profile-name")]
    [InlineData("tenant-id", null, "client-secret", "https://endpoint.com", "profile-name")]
    [InlineData("tenant-id", "client-id", null, "https://endpoint.com", "profile-name")]
    [InlineData("tenant-id", "client-id", "client-secret", null, "profile-name")]
    [InlineData("tenant-id", "client-id", "client-secret", "https://endpoint.com", null)]
    [InlineData("", "client-id", "client-secret", "https://endpoint.com", "profile-name")]
    [InlineData("tenant-id", "", "client-secret", "https://endpoint.com", "profile-name")]
    [InlineData("tenant-id", "client-id", "", "https://endpoint.com", "profile-name")]
    [InlineData("tenant-id", "client-id", "client-secret", "", "profile-name")]
    [InlineData("tenant-id", "client-id", "client-secret", "https://endpoint.com", "")]
    public void IsConfigured_WithMissingOrEmptyRequiredSettings_ReturnsFalse(
        string? tenantId,
        string? clientId,
        string? clientSecret,
        string? endpoint,
        string? profileName)
    {
        // Arrange
        var settings = new AppSettings
        {
            AzureTenantId = tenantId,
            AzureClientId = clientId,
            AzureClientSecret = clientSecret,
            TrustedSigningEndpoint = endpoint,
            CertificateProfileName = profileName
        };

        // Act & Assert
        settings.IsConfigured().Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithWhitespaceOnlyRequiredSettings_ReturnsFalse()
    {
        // Arrange
        var settings = new AppSettings
        {
            AzureTenantId = "   ",
            AzureClientId = "client-id",
            AzureClientSecret = "client-secret",
            TrustedSigningEndpoint = "https://endpoint.com",
            CertificateProfileName = "profile-name"
        };

        // Act & Assert
        settings.IsConfigured().Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_OptionalSettingsDoNotAffectResult()
    {
        // Arrange
        var settings = new AppSettings
        {
            AzureTenantId = "tenant-id",
            AzureClientId = "client-id",
            AzureClientSecret = "client-secret",
            TrustedSigningEndpoint = "https://endpoint.com",
            CertificateProfileName = "profile-name",
            // Optional settings are null/default
            LastSelectedPath = null,
            AutoSelectSignableFiles = false,
            ShowHiddenFiles = true,
            MaxConcurrentSigningRequests = 1
        };

        // Act & Assert
        settings.IsConfigured().Should().BeTrue();
    }
}
