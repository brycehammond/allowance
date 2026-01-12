namespace AllowanceTracker.Services;

/// <summary>
/// Service for uploading and managing files in Azure Blob Storage
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Upload a file to blob storage
    /// </summary>
    /// <param name="stream">File stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME content type</param>
    /// <param name="folder">Optional folder path within the container</param>
    /// <returns>Public URL of the uploaded blob</returns>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType, string? folder = null);

    /// <summary>
    /// Delete a file from blob storage
    /// </summary>
    /// <param name="blobUrl">Full URL of the blob to delete</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(string blobUrl);

    /// <summary>
    /// Check if a blob exists
    /// </summary>
    /// <param name="blobUrl">Full URL of the blob</param>
    /// <returns>True if exists</returns>
    Task<bool> ExistsAsync(string blobUrl);

    /// <summary>
    /// Get list of allowed content types for upload
    /// </summary>
    IReadOnlyList<string> AllowedContentTypes { get; }

    /// <summary>
    /// Maximum file size in bytes
    /// </summary>
    long MaxFileSizeBytes { get; }
}

/// <summary>
/// Result of a file upload operation
/// </summary>
public record UploadResult(
    bool Success,
    string? Url,
    string? ErrorMessage);
