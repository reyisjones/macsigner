using FluentAssertions;
using MacSigner.Models;
using MacSigner.Services;
using MacSigner.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace MacSigner.Tests.ViewModels;

public class MainWindowViewModelTests : IDisposable
{
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<ITrustedSigningService> _mockTrustedSigningService;
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<ILogger<MainWindowViewModel>> _mockLogger;
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelTests()
    {
        _mockFileService = new Mock<IFileService>();
        _mockTrustedSigningService = new Mock<ITrustedSigningService>();
        _mockSettingsService = new Mock<ISettingsService>();
        _mockLogger = new Mock<ILogger<MainWindowViewModel>>();

        // Setup mock to return a default AppSettings to prevent hanging
        _mockSettingsService.Setup(x => x.LoadSettingsAsync())
            .ReturnsAsync(new AppSettings());

        _viewModel = new MainWindowViewModel(
            _mockFileService.Object,
            _mockTrustedSigningService.Object,
            _mockSettingsService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Constructor_WithValidDependencies_InitializesCommands()
    {
        // Wait a moment for async initialization to complete
        await Task.Delay(100);
        
        // Assert
        _viewModel.BrowseCommand.Should().NotBeNull();
        _viewModel.PastePathCommand.Should().NotBeNull();
        _viewModel.ScanCommand.Should().NotBeNull();
        _viewModel.SignCommand.Should().NotBeNull();
        _viewModel.OpenSettingsCommand.Should().NotBeNull();
        _viewModel.RefreshCommand.Should().NotBeNull();
    }

    [Fact]
    public void PastePathCommand_ShouldBeAvailable()
    {
        // Assert
        _viewModel.PastePathCommand.Should().NotBeNull();
        _viewModel.PastePathCommand.CanExecute(null).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectedPath_WithInvalidValues_CanScanShouldBeFalse(string invalidPath)
    {
        // Arrange
        _viewModel.SelectedPath = invalidPath;

        // Assert
        _viewModel.CanScan.Should().BeFalse();
    }

    [Fact]
    public void SelectedPath_WithNullValue_CanScanShouldBeFalse()
    {
        // Arrange
        _viewModel.SelectedPath = null!;

        // Assert
        _viewModel.CanScan.Should().BeFalse();
    }

    [Fact]
    public void SelectedPath_WithValidDirectory_CanScanShouldBeTrue()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        _viewModel.SelectedPath = tempDir;

        // Assert
        _viewModel.CanScan.Should().BeTrue();
    }

    [Fact]
    public void StatusMessage_InitialValue_ShouldBeReady()
    {
        // Assert
        _viewModel.StatusMessage.Should().Be("Ready");
    }

    [Fact]
    public void ProgressPercentage_InitialValue_ShouldBeZero()
    {
        // Assert
        _viewModel.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void TotalFiles_InitialValue_ShouldBeZero()
    {
        // Assert
        _viewModel.TotalFiles.Should().Be(0);
    }

    [Fact]
    public void SelectedFiles_InitialValue_ShouldBeZero()
    {
        // Assert
        _viewModel.SelectedFiles.Should().Be(0);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
