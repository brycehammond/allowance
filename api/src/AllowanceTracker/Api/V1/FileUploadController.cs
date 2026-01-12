using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

[ApiController]
[Route("api/v1/files")]
[Authorize]
public class FileUploadController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(
        IBlobStorageService blobStorageService,
        ILogger<FileUploadController> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a photo for task proof or other purposes
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <param name="folder">Optional folder path (e.g., "tasks", "avatars")</param>
    /// <returns>URL of the uploaded file</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
    public async Task<ActionResult<FileUploadResponse>> UploadFile(
        IFormFile file,
        [FromQuery] string? folder = "photos")
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        // Validate content type
        if (!_blobStorageService.AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return BadRequest(new
            {
                error = $"File type '{file.ContentType}' is not allowed",
                allowedTypes = _blobStorageService.AllowedContentTypes
            });
        }

        // Validate file size
        if (file.Length > _blobStorageService.MaxFileSizeBytes)
        {
            return BadRequest(new
            {
                error = $"File size exceeds maximum allowed size of {_blobStorageService.MaxFileSizeBytes / 1024 / 1024} MB"
            });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var url = await _blobStorageService.UploadAsync(
                stream,
                file.FileName,
                file.ContentType,
                folder);

            _logger.LogInformation("File uploaded successfully: {Url}", url);

            return Ok(new FileUploadResponse(url, file.FileName, file.Length));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "File upload validation failed");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed");
            return StatusCode(500, new { error = "Failed to upload file" });
        }
    }

    /// <summary>
    /// Delete an uploaded file
    /// </summary>
    /// <param name="url">Full URL of the file to delete</param>
    [HttpDelete]
    public async Task<ActionResult> DeleteFile([FromQuery] string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest(new { error = "URL is required" });
        }

        try
        {
            var deleted = await _blobStorageService.DeleteAsync(url);

            if (deleted)
            {
                _logger.LogInformation("File deleted successfully: {Url}", url);
                return NoContent();
            }
            else
            {
                return NotFound(new { error = "File not found" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File deletion failed for URL: {Url}", url);
            return StatusCode(500, new { error = "Failed to delete file" });
        }
    }

    /// <summary>
    /// Get information about upload limits and allowed types
    /// </summary>
    [HttpGet("upload-info")]
    [AllowAnonymous]
    public ActionResult<FileUploadInfo> GetUploadInfo()
    {
        return Ok(new FileUploadInfo(
            _blobStorageService.AllowedContentTypes.ToList(),
            _blobStorageService.MaxFileSizeBytes,
            _blobStorageService.MaxFileSizeBytes / 1024 / 1024));
    }
}

/// <summary>
/// Response returned after successful file upload
/// </summary>
public record FileUploadResponse(
    string Url,
    string OriginalFileName,
    long SizeBytes);

/// <summary>
/// Information about upload limits
/// </summary>
public record FileUploadInfo(
    List<string> AllowedContentTypes,
    long MaxFileSizeBytes,
    long MaxFileSizeMB);
