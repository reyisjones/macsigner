using AutoFixture.Xunit2;
using MacSigner.Models;

namespace MacSigner.Tests.Models;

public class SigningRequestTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var request = new SigningRequest();

        // Assert
        request.RequestId.Should().NotBeNullOrEmpty();
        request.Files.Should().NotBeNull();
        request.Files.Should().BeEmpty();
        request.Status.Should().Be(SigningStatus.NotSigned);
        request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        request.ErrorMessage.Should().BeNull();
        request.TotalFiles.Should().Be(0);
        request.ProcessedFiles.Should().Be(0);
    }

    [Fact]
    public void Constructor_GeneratesUniqueRequestIds()
    {
        // Act
        var request1 = new SigningRequest();
        var request2 = new SigningRequest();

        // Assert
        request1.RequestId.Should().NotBe(request2.RequestId);
    }

    [Fact]
    public void AddFile_AddsFileToCollection()
    {
        // Arrange
        var request = new SigningRequest();
        var file = new SignableFile("/test/file.exe");

        // Act
        request.Files.Add(file);

        // Assert
        request.Files.Should().Contain(file);
        request.Files.Should().HaveCount(1);
        request.TotalFiles.Should().Be(1);
    }

    [Fact]
    public void GetProgress_WithNoFiles_ReturnsZero()
    {
        // Arrange
        var request = new SigningRequest();

        // Act
        var progress = request.GetProgress();

        // Assert
        progress.Should().Be(0);
        request.Progress.Should().Be(0);
    }

    [Fact]
    public void GetProgress_WithAllCompletedFiles_ReturnsHundred()
    {
        // Arrange
        var request = new SigningRequest();
        request.Files.Add(new SignableFile("/test/file1.exe") { Status = SigningStatus.Completed });
        request.Files.Add(new SignableFile("/test/file2.exe") { Status = SigningStatus.Completed });
        request.Files.Add(new SignableFile("/test/file3.exe") { Status = SigningStatus.Completed });

        // Act
        var progress = request.GetProgress();

        // Assert
        progress.Should().Be(100);
        request.Progress.Should().Be(100);
    }

    [Fact]
    public void GetProgress_WithMixedStatusFiles_ReturnsCorrectPercentage()
    {
        // Arrange
        var request = new SigningRequest();
        request.Files.Add(new SignableFile("/test/file1.exe") { Status = SigningStatus.Completed });
        request.Files.Add(new SignableFile("/test/file2.exe") { Status = SigningStatus.Completed });
        request.Files.Add(new SignableFile("/test/file3.exe") { Status = SigningStatus.InProgress });
        request.Files.Add(new SignableFile("/test/file4.exe") { Status = SigningStatus.NotSigned });

        // Act
        var progress = request.GetProgress();

        // Assert
        progress.Should().Be(50); // 2 out of 4 files completed
    }

    [Fact]
    public void GetProgress_WithFailedFiles_CountsAsProcessed()
    {
        // Arrange
        var request = new SigningRequest();
        request.Files.Add(new SignableFile("/test/file1.exe") { Status = SigningStatus.Completed });
        request.Files.Add(new SignableFile("/test/file2.exe") { Status = SigningStatus.Failed });

        // Act
        var progress = request.GetProgress();

        // Assert
        progress.Should().Be(100); // Both completed and failed count as processed
    }

    [Theory]
    [InlineData(SigningStatus.NotSigned)]
    [InlineData(SigningStatus.Queued)]
    [InlineData(SigningStatus.InProgress)]
    [InlineData(SigningStatus.Completed)]
    [InlineData(SigningStatus.Failed)]
    public void Status_CanBeSetToAllValidValues(SigningStatus status)
    {
        // Arrange
        var request = new SigningRequest();

        // Act
        request.Status = status;

        // Assert
        request.Status.Should().Be(status);
    }

    [Theory]
    [AutoData]
    public void ErrorMessage_CanBeSetAndRetrieved(string errorMessage)
    {
        // Arrange
        var request = new SigningRequest();

        // Act
        request.ErrorMessage = errorMessage;

        // Assert
        request.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void TotalFiles_ReturnsFileCount()
    {
        // Arrange
        var request = new SigningRequest();
        request.Files.Add(new SignableFile("/test/file1.exe"));
        request.Files.Add(new SignableFile("/test/file2.exe"));
        request.Files.Add(new SignableFile("/test/file3.exe"));

        // Act
        var totalFiles = request.TotalFiles;

        // Assert
        totalFiles.Should().Be(3);
    }

    [Fact]
    public void ProcessedFiles_ReturnsCountOfCompletedAndFailedFiles()
    {
        // Arrange
        var request = new SigningRequest();
        request.Files.Add(new SignableFile("/test/file1.exe") { Status = SigningStatus.Completed });
        request.Files.Add(new SignableFile("/test/file2.exe") { Status = SigningStatus.Failed });
        request.Files.Add(new SignableFile("/test/file3.exe") { Status = SigningStatus.InProgress });
        request.Files.Add(new SignableFile("/test/file4.exe") { Status = SigningStatus.NotSigned });

        // Act
        var processedFiles = request.ProcessedFiles;

        // Assert
        processedFiles.Should().Be(2); // Completed + Failed
    }

    [Fact]
    public void RequestId_IsValidGuid()
    {
        // Arrange
        var request = new SigningRequest();

        // Act & Assert
        Guid.TryParse(request.RequestId, out _).Should().BeTrue();
    }

    [Fact]
    public void CreatedAt_CanBeSetAndRetrieved()
    {
        // Arrange
        var request = new SigningRequest();
        var newCreatedAt = DateTime.UtcNow.AddHours(-1);

        // Act
        request.CreatedAt = newCreatedAt;

        // Assert
        request.CreatedAt.Should().Be(newCreatedAt);
    }

    [Fact]
    public void Progress_PropertyReturnsGetProgressValue()
    {
        // Arrange
        var request = new SigningRequest();
        request.Files.Add(new SignableFile("/test/file1.exe") { Status = SigningStatus.Completed });
        request.Files.Add(new SignableFile("/test/file2.exe") { Status = SigningStatus.InProgress });

        // Act & Assert
        request.Progress.Should().Be(request.GetProgress());
        request.Progress.Should().Be(50);
    }
}
