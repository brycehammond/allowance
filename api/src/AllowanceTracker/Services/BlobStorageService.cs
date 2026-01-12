using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AllowanceTracker.Services;

/// <summary>
/// Azure Blob Storage implementation for file uploads
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;

    private static readonly string[] _allowedContentTypes = new[]
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/heic",
        "image/heif"
    };

    // 10 MB max file size
    private const long MaxFileSize = 10 * 1024 * 1024;

    public BlobStorageService(
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureBlobStorage:ConnectionString configuration is required");

        var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "photos";

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    // Constructor for testing with injected container client
    public BlobStorageService(BlobContainerClient containerClient, ILogger<BlobStorageService> logger)
    {
        _containerClient = containerClient;
        _logger = logger;
    }

    public IReadOnlyList<string> AllowedContentTypes => _allowedContentTypes;

    public long MaxFileSizeBytes => MaxFileSize;

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null)
    {
        // Validate content type
        if (!_allowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new InvalidOperationException($"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", _allowedContentTypes)}");
        }

        // Validate file size
        if (stream.Length > MaxFileSize)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024} MB");
        }

        // Generate unique blob name
        var extension = GetExtensionFromContentType(contentType);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var blobName = string.IsNullOrEmpty(folder)
            ? uniqueFileName
            : $"{folder.TrimEnd('/')}/{uniqueFileName}";

        try
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            _logger.LogInformation("Uploaded blob {BlobName} to container {Container}",
                blobName, _containerClient.Name);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob {BlobName}", blobName);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string blobUrl)
    {
        try
        {
            var blobName = GetBlobNameFromUrl(blobUrl);
            if (string.IsNullOrEmpty(blobName))
            {
                _logger.LogWarning("Could not extract blob name from URL: {Url}", blobUrl);
                return false;
            }

            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                _logger.LogInformation("Deleted blob {BlobName}", blobName);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob from URL: {Url}", blobUrl);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string blobUrl)
    {
        try
        {
            var blobName = GetBlobNameFromUrl(blobUrl);
            if (string.IsNullOrEmpty(blobName))
            {
                return false;
            }

            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.ExistsAsync();
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check blob existence for URL: {Url}", blobUrl);
            return false;
        }
    }

    private string? GetBlobNameFromUrl(string blobUrl)
    {
        try
        {
            var uri = new Uri(blobUrl);
            var containerName = _containerClient.Name;

            // URL format: https://{account}.blob.core.windows.net/{container}/{blobName}
            var path = uri.AbsolutePath.TrimStart('/');

            if (path.StartsWith($"{containerName}/"))
            {
                return path.Substring(containerName.Length + 1);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetExtensionFromContentType(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        "image/heif" => ".heif",
        _ => ".bin"
    };
}

/// <summary>
/// No-op implementation for when blob storage is not configured
/// </summary>
public class NoOpBlobStorageService : IBlobStorageService
{
    public IReadOnlyList<string> AllowedContentTypes => Array.Empty<string>();
    public long MaxFileSizeBytes => 0;

    public Task<bool> DeleteAsync(string blobUrl) => Task.FromResult(false);
    public Task<bool> ExistsAsync(string blobUrl) => Task.FromResult(false);

    public Task<string> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null)
    {
        throw new InvalidOperationException("Blob storage is not configured. Set AzureBlobStorage:ConnectionString in configuration.");
    }
}
