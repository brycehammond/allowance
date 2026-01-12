using AllowanceTracker.Services;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class BlobStorageServiceTests
{
    private readonly Mock<BlobContainerClient> _mockContainerClient;
    private readonly Mock<ILogger<BlobStorageService>> _mockLogger;
    private readonly BlobStorageService _service;

    public BlobStorageServiceTests()
    {
        _mockContainerClient = new Mock<BlobContainerClient>();
        _mockContainerClient.Setup(c => c.Name).Returns("photos");

        _mockLogger = new Mock<ILogger<BlobStorageService>>();
        _service = new BlobStorageService(_mockContainerClient.Object, _mockLogger.Object);
    }

    #region UploadAsync Tests

    [Fact]
    public async Task UploadAsync_ValidJpegFile_ReturnsUrl()
    {
        // Arrange
        var mockBlobClient = new Mock<BlobClient>();
        var expectedUri = new Uri("https://allowanceuploads.blob.core.windows.net/photos/test.jpg");
        mockBlobClient.Setup(b => b.Uri).Returns(expectedUri);
        mockBlobClient
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        _mockContainerClient
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);

        using var stream = new MemoryStream(new byte[100]);

        // Act
        var result = await _service.UploadAsync(stream, "test.jpg", "image/jpeg");

        // Assert
        result.Should().Be(expectedUri.ToString());
    }

    [Fact]
    public async Task UploadAsync_WithFolder_CreatesBlobInFolder()
    {
        // Arrange
        var mockBlobClient = new Mock<BlobClient>();
        var expectedUri = new Uri("https://allowanceuploads.blob.core.windows.net/photos/tasks/test.jpg");
        mockBlobClient.Setup(b => b.Uri).Returns(expectedUri);
        mockBlobClient
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        string? capturedBlobName = null;
        _mockContainerClient
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Callback<string>(name => capturedBlobName = name)
            .Returns(mockBlobClient.Object);

        using var stream = new MemoryStream(new byte[100]);

        // Act
        await _service.UploadAsync(stream, "test.jpg", "image/jpeg", "tasks");

        // Assert
        capturedBlobName.Should().StartWith("tasks/");
        capturedBlobName.Should().EndWith(".jpg");
    }

    [Fact]
    public async Task UploadAsync_InvalidContentType_ThrowsException()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[100]);

        // Act
        var act = () => _service.UploadAsync(stream, "test.exe", "application/octet-stream");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not allowed*");
    }

    [Fact]
    public async Task UploadAsync_FileTooLarge_ThrowsException()
    {
        // Arrange
        var largeFile = new byte[11 * 1024 * 1024]; // 11 MB
        using var stream = new MemoryStream(largeFile);

        // Act
        var act = () => _service.UploadAsync(stream, "large.jpg", "image/jpeg");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds maximum*");
    }

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/gif", ".gif")]
    [InlineData("image/webp", ".webp")]
    public async Task UploadAsync_DifferentImageTypes_GeneratesCorrectExtension(string contentType, string expectedExtension)
    {
        // Arrange
        var mockBlobClient = new Mock<BlobClient>();
        mockBlobClient.Setup(b => b.Uri).Returns(new Uri("https://test.blob.core.windows.net/photos/test.jpg"));
        mockBlobClient
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

        string? capturedBlobName = null;
        _mockContainerClient
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Callback<string>(name => capturedBlobName = name)
            .Returns(mockBlobClient.Object);

        using var stream = new MemoryStream(new byte[100]);

        // Act
        await _service.UploadAsync(stream, "test", contentType);

        // Assert
        capturedBlobName.Should().EndWith(expectedExtension);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingBlob_ReturnsTrue()
    {
        // Arrange
        var mockBlobClient = new Mock<BlobClient>();
        mockBlobClient
            .Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        _mockContainerClient
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);

        // Act
        var result = await _service.DeleteAsync("https://allowanceuploads.blob.core.windows.net/photos/test.jpg");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingBlob_ReturnsFalse()
    {
        // Arrange
        var mockBlobClient = new Mock<BlobClient>();
        mockBlobClient
            .Setup(b => b.DeleteIfExistsAsync(It.IsAny<DeleteSnapshotsOption>(), It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        _mockContainerClient
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);

        // Act
        var result = await _service.DeleteAsync("https://allowanceuploads.blob.core.windows.net/photos/nonexistent.jpg");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_InvalidUrl_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteAsync("not-a-valid-url");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_ExistingBlob_ReturnsTrue()
    {
        // Arrange
        var mockBlobClient = new Mock<BlobClient>();
        mockBlobClient
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        _mockContainerClient
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);

        // Act
        var result = await _service.ExistsAsync("https://allowanceuploads.blob.core.windows.net/photos/test.jpg");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistingBlob_ReturnsFalse()
    {
        // Arrange
        var mockBlobClient = new Mock<BlobClient>();
        mockBlobClient
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        _mockContainerClient
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(mockBlobClient.Object);

        // Act
        var result = await _service.ExistsAsync("https://allowanceuploads.blob.core.windows.net/photos/nonexistent.jpg");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void AllowedContentTypes_ContainsExpectedTypes()
    {
        // Assert
        _service.AllowedContentTypes.Should().Contain("image/jpeg");
        _service.AllowedContentTypes.Should().Contain("image/png");
        _service.AllowedContentTypes.Should().Contain("image/gif");
        _service.AllowedContentTypes.Should().Contain("image/webp");
    }

    [Fact]
    public void MaxFileSizeBytes_Is10MB()
    {
        // Assert
        _service.MaxFileSizeBytes.Should().Be(10 * 1024 * 1024);
    }

    #endregion
}

public class NoOpBlobStorageServiceTests
{
    [Fact]
    public async Task UploadAsync_ThrowsNotConfiguredException()
    {
        // Arrange
        var service = new NoOpBlobStorageService();
        using var stream = new MemoryStream();

        // Act
        var act = () => service.UploadAsync(stream, "test.jpg", "image/jpeg");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse()
    {
        // Arrange
        var service = new NoOpBlobStorageService();

        // Act
        var result = await service.DeleteAsync("https://test.com/file.jpg");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse()
    {
        // Arrange
        var service = new NoOpBlobStorageService();

        // Act
        var result = await service.ExistsAsync("https://test.com/file.jpg");

        // Assert
        result.Should().BeFalse();
    }
}
