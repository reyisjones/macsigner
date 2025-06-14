using AutoFixture.Xunit2;
using MacSigner.Models;

namespace MacSigner.Tests.Models;

public class SignableFileTests
{
    [Fact]
    public void Constructor_WithValidPath_SetsPropertiesCorrectly()
    {
        // Arrange
        var filePath = "/path/to/test.exe";

        // Act
        var file = new SignableFile(filePath);

        // Assert
        file.FilePath.Should().Be(filePath);
        file.FileName.Should().Be("test.exe");
        file.FileExtension.Should().Be(".exe");
        file.Status.Should().Be(SigningStatus.NotSigned);
        file.IsSelected.Should().BeTrue();
        file.FileSize.Should().Be(0); // File doesn't exist
        file.SigningRequestId.Should().BeNull();
        file.SignedAt.Should().BeNull();
        file.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("/path/to/file.exe", ".exe")]
    [InlineData("/path/to/library.dll", ".dll")]
    [InlineData("/path/to/app.app", ".app")]
    [InlineData("/path/to/lib.dylib", ".dylib")]
    [InlineData("/path/to/package.msi", ".msi")]
    public void Constructor_WithDifferentExtensions_SetsFileExtensionCorrectly(string filePath, string expectedExtension)
    {
        // Act
        var file = new SignableFile(filePath);

        // Assert
        file.FileExtension.Should().Be(expectedExtension);
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SignableFile(null!));
    }

    [Fact]
    public void Constructor_WithExistingFile_SetsFileSizeCorrectly()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var content = "test content for file size";
        File.WriteAllText(tempFile, content);

        try
        {
            // Act
            var file = new SignableFile(tempFile);

            // Assert
            file.FileSize.Should().BeGreaterThan(0);
            file.FileSize.Should().Be(new FileInfo(tempFile).Length);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetFormattedFileSize_WithDifferentSizes_ReturnsCorrectFormat()
    {
        // Arrange & Act & Assert
        var file1 = new SignableFile("/test/file.exe") { FileSize = 500 };
        file1.GetFormattedFileSize().Should().Be("500 bytes");

        var file2 = new SignableFile("/test/file.exe") { FileSize = 2048 };
        file2.GetFormattedFileSize().Should().Be("2.0 KB");

        var file3 = new SignableFile("/test/file.exe") { FileSize = 2 * 1024 * 1024 };
        file3.GetFormattedFileSize().Should().Be("2.0 MB");

        var file4 = new SignableFile("/test/file.exe") { FileSize = 3L * 1024 * 1024 * 1024 };
        file4.GetFormattedFileSize().Should().Be("3.0 GB");
    }

    [Fact]
    public void FormattedFileSize_ReturnsFormattedString()
    {
        // Arrange
        var file = new SignableFile("/test/file.exe") { FileSize = 1024 };

        // Act & Assert
        file.FormattedFileSize.Should().Be("1.0 KB");
    }

    [Theory]
    [InlineData(".exe", true)]
    [InlineData(".dll", true)]
    [InlineData(".msi", true)]
    [InlineData(".cab", true)]
    [InlineData(".ocx", true)]
    [InlineData(".dylib", true)]
    [InlineData(".app", true)]
    [InlineData(".framework", true)]
    [InlineData(".bundle", true)]
    [InlineData(".jar", true)]
    [InlineData(".apk", true)]
    [InlineData(".ipa", true)]
    [InlineData(".xap", true)]
    [InlineData(".txt", false)]
    [InlineData(".pdf", false)]
    [InlineData(".doc", false)]
    [InlineData("", false)]
    public void IsSignableFileType_WithDifferentExtensions_ReturnsCorrectResult(string extension, bool expected)
    {
        // Arrange
        var file = new SignableFile($"/test/file{extension}");

        // Act & Assert
        file.IsSignableFileType().Should().Be(expected);
    }

    [Theory]
    [InlineData(".EXE", true)]
    [InlineData(".DLL", true)]
    [InlineData(".App", true)]
    public void IsSignableFileType_WithMixedCase_ReturnsTrue(string extension, bool expected)
    {
        // Arrange
        var file = new SignableFile($"/test/file{extension}");

        // Act & Assert
        file.IsSignableFileType().Should().Be(expected);
    }

    [Theory]
    [AutoData]
    public void SigningRequestId_CanBeSetAndRetrieved(string requestId)
    {
        // Arrange
        var file = new SignableFile("/test/file.exe");

        // Act
        file.SigningRequestId = requestId;

        // Assert
        file.SigningRequestId.Should().Be(requestId);
    }

    [Fact]
    public void SignedAt_CanBeSetAndRetrieved()
    {
        // Arrange
        var file = new SignableFile("/test/file.exe");
        var signedTime = DateTime.UtcNow;

        // Act
        file.SignedAt = signedTime;

        // Assert
        file.SignedAt.Should().Be(signedTime);
    }

    [Theory]
    [AutoData]
    public void ErrorMessage_CanBeSetAndRetrieved(string errorMessage)
    {
        // Arrange
        var file = new SignableFile("/test/file.exe");

        // Act
        file.ErrorMessage = errorMessage;

        // Assert
        file.ErrorMessage.Should().Be(errorMessage);
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
        var file = new SignableFile("/test/file.exe");

        // Act
        file.Status = status;

        // Assert
        file.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsSelected_CanBeSetAndRetrieved(bool isSelected)
    {
        // Arrange
        var file = new SignableFile("/test/file.exe");

        // Act
        file.IsSelected = isSelected;

        // Assert
        file.IsSelected.Should().Be(isSelected);
    }
}
