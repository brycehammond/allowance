# Photo Attachments - Receipt Photo Uploads

## Overview
Photo attachment system enabling children and parents to upload receipt photos to transactions for better record-keeping and transparency. Includes image storage (Azure Blob Storage and local file system), automatic compression, thumbnail generation, and a photo gallery view.

## Core Philosophy: Test-First Development
**Every feature starts with a failing test**. Follow strict TDD methodology for all photo attachment functionality.

## Technology Stack

### Core Dependencies
```xml
<ItemGroup>
  <!-- Image Processing -->
  <PackageReference Include="SixLabors.ImageSharp" Version="3.1.0" />
  <PackageReference Include="SixLabors.ImageSharp.Web" Version="3.1.0" />

  <!-- Azure Storage (optional) -->
  <PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />

  <!-- File Type Detection -->
  <PackageReference Include="MimeDetective" Version="24.7.1" />
</ItemGroup>
```

## Database Schema Updates

### Transaction Model Updates
```csharp
// Add to existing Transaction model
public class Transaction
{
    // ... existing properties ...

    public string? ReceiptPhotoUrl { get; set; }
    public string? ReceiptPhotoThumbnailUrl { get; set; }
    public long? ReceiptPhotoSizeBytes { get; set; }
    public string? ReceiptPhotoOriginalName { get; set; }
}
```

### PhotoAttachment Model (for future extensibility)
```csharp
public class PhotoAttachment
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Guid UploadedById { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Transaction Transaction { get; set; } = null!;
    public virtual ApplicationUser UploadedBy { get; set; } = null!;
}
```

### DbContext Configuration
```csharp
// Update AllowanceContext.cs

public DbSet<PhotoAttachment> PhotoAttachments { get; set; }

protected override void OnModelCreating(ModelBuilder builder)
{
    // ... existing configuration ...

    // Update Transaction configuration
    builder.Entity<Transaction>(entity =>
    {
        // ... existing configuration ...
        entity.Property(e => e.ReceiptPhotoUrl).HasMaxLength(1000);
        entity.Property(e => e.ReceiptPhotoThumbnailUrl).HasMaxLength(1000);
        entity.Property(e => e.ReceiptPhotoOriginalName).HasMaxLength(255);
    });

    // PhotoAttachment configuration
    builder.Entity<PhotoAttachment>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
        entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(1000);
        entity.Property(e => e.ThumbnailPath).IsRequired().HasMaxLength(1000);
        entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);

        entity.HasOne(e => e.Transaction)
              .WithMany()
              .HasForeignKey(e => e.TransactionId)
              .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.UploadedBy)
              .WithMany()
              .HasForeignKey(e => e.UploadedById)
              .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => e.TransactionId);
        entity.HasIndex(e => e.UploadedById);
    });
}
```

## Service Interfaces

### IPhotoStorageService Interface
```csharp
public interface IPhotoStorageService
{
    Task<PhotoUploadResult> UploadPhotoAsync(Stream photoStream, string fileName, string contentType);
    Task<PhotoUploadResult> UploadPhotoWithThumbnailAsync(Stream photoStream, string fileName, string contentType);
    Task<Stream> GetPhotoAsync(string photoUrl);
    Task<Stream> GetThumbnailAsync(string thumbnailUrl);
    Task DeletePhotoAsync(string photoUrl);
    Task<bool> PhotoExistsAsync(string photoUrl);
    string GetPhotoUrl(string fileName);
}

public class PhotoUploadResult
{
    public string PhotoUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
```

### IPhotoService Interface
```csharp
public interface IPhotoService
{
    Task<PhotoAttachment> AttachPhotoToTransactionAsync(Guid transactionId, IFormFile photo);
    Task<PhotoAttachment?> GetPhotoForTransactionAsync(Guid transactionId);
    Task DeletePhotoFromTransactionAsync(Guid transactionId);
    Task<List<PhotoAttachment>> GetPhotosForChildAsync(Guid childId, int limit = 50);
    Task<bool> ValidatePhotoAsync(IFormFile photo);
    Task<byte[]> CompressImageAsync(Stream imageStream, int maxWidth = 1920, int quality = 85);
    Task<byte[]> GenerateThumbnailAsync(Stream imageStream, int maxWidth = 300);
}
```

## Service Implementation

### LocalFileStorageService Implementation
```csharp
public class LocalFileStorageService : IPhotoStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _storagePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _storagePath = configuration["PhotoStorage:LocalPath"] ?? "wwwroot/uploads/photos";
        _baseUrl = configuration["PhotoStorage:BaseUrl"] ?? "/uploads/photos";

        // Ensure directory exists
        Directory.CreateDirectory(_storagePath);
        Directory.CreateDirectory(Path.Combine(_storagePath, "thumbnails"));
    }

    public async Task<PhotoUploadResult> UploadPhotoAsync(Stream photoStream, string fileName, string contentType)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_storagePath, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await photoStream.CopyToAsync(fileStream);
        }

        var fileInfo = new FileInfo(filePath);

        // Get image dimensions
        using var image = await Image.LoadAsync(filePath);

        return new PhotoUploadResult
        {
            PhotoUrl = $"{_baseUrl}/{uniqueFileName}",
            ThumbnailUrl = string.Empty,
            FileSizeBytes = fileInfo.Length,
            Width = image.Width,
            Height = image.Height
        };
    }

    public async Task<PhotoUploadResult> UploadPhotoWithThumbnailAsync(Stream photoStream, string fileName, string contentType)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(_storagePath, uniqueFileName);
        var thumbnailFileName = $"thumb_{uniqueFileName}";
        var thumbnailPath = Path.Combine(_storagePath, "thumbnails", thumbnailFileName);

        // Load and process image
        using var image = await Image.LoadAsync(photoStream);
        var originalWidth = image.Width;
        var originalHeight = image.Height;

        // Compress and save main image
        var maxWidth = _configuration.GetValue<int>("PhotoStorage:MaxWidth", 1920);
        if (image.Width > maxWidth)
        {
            var ratio = (double)maxWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(maxWidth, newHeight));
        }

        await image.SaveAsync(filePath, new JpegEncoder { Quality = 85 });

        // Generate and save thumbnail
        var thumbnailMaxWidth = _configuration.GetValue<int>("PhotoStorage:ThumbnailWidth", 300);
        var thumbnail = image.Clone();
        if (thumbnail.Width > thumbnailMaxWidth)
        {
            var ratio = (double)thumbnailMaxWidth / thumbnail.Width;
            var newHeight = (int)(thumbnail.Height * ratio);
            thumbnail.Mutate(x => x.Resize(thumbnailMaxWidth, newHeight));
        }

        await thumbnail.SaveAsync(thumbnailPath, new JpegEncoder { Quality = 75 });
        thumbnail.Dispose();

        var fileInfo = new FileInfo(filePath);

        _logger.LogInformation("Photo uploaded: {FileName} ({Size} bytes)", uniqueFileName, fileInfo.Length);

        return new PhotoUploadResult
        {
            PhotoUrl = $"{_baseUrl}/{uniqueFileName}",
            ThumbnailUrl = $"{_baseUrl}/thumbnails/{thumbnailFileName}",
            FileSizeBytes = fileInfo.Length,
            Width = originalWidth,
            Height = originalHeight
        };
    }

    public Task<Stream> GetPhotoAsync(string photoUrl)
    {
        var fileName = Path.GetFileName(photoUrl);
        var filePath = Path.Combine(_storagePath, fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Photo not found", fileName);

        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public Task<Stream> GetThumbnailAsync(string thumbnailUrl)
    {
        var fileName = Path.GetFileName(thumbnailUrl);
        var filePath = Path.Combine(_storagePath, "thumbnails", fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Thumbnail not found", fileName);

        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public Task DeletePhotoAsync(string photoUrl)
    {
        var fileName = Path.GetFileName(photoUrl);
        var filePath = Path.Combine(_storagePath, fileName);
        var thumbnailPath = Path.Combine(_storagePath, "thumbnails", $"thumb_{fileName}");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted photo: {FileName}", fileName);
        }

        if (File.Exists(thumbnailPath))
        {
            File.Delete(thumbnailPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> PhotoExistsAsync(string photoUrl)
    {
        var fileName = Path.GetFileName(photoUrl);
        var filePath = Path.Combine(_storagePath, fileName);
        return Task.FromResult(File.Exists(filePath));
    }

    public string GetPhotoUrl(string fileName)
    {
        return $"{_baseUrl}/{fileName}";
    }
}
```

### AzureBlobStorageService Implementation
```csharp
public class AzureBlobStorageService : IPhotoStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["PhotoStorage:AzureConnectionString"];
        var containerName = configuration["PhotoStorage:ContainerName"] ?? "receipt-photos";

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.Blob);
    }

    public async Task<PhotoUploadResult> UploadPhotoAsync(Stream photoStream, string fileName, string contentType)
    {
        var blobName = $"{Guid.NewGuid()}_{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        using var image = await Image.LoadAsync(photoStream);
        using var outputStream = new MemoryStream();

        // Compress image
        await image.SaveAsync(outputStream, new JpegEncoder { Quality = 85 });
        outputStream.Position = 0;

        await blobClient.UploadAsync(outputStream, new BlobHttpHeaders { ContentType = "image/jpeg" });

        _logger.LogInformation("Photo uploaded to Azure: {BlobName}", blobName);

        return new PhotoUploadResult
        {
            PhotoUrl = blobClient.Uri.ToString(),
            ThumbnailUrl = string.Empty,
            FileSizeBytes = outputStream.Length,
            Width = image.Width,
            Height = image.Height
        };
    }

    public async Task<PhotoUploadResult> UploadPhotoWithThumbnailAsync(Stream photoStream, string fileName, string contentType)
    {
        var blobName = $"{Guid.NewGuid()}_{fileName}";
        var thumbnailBlobName = $"thumb_{blobName}";

        var blobClient = _containerClient.GetBlobClient(blobName);
        var thumbnailBlobClient = _containerClient.GetBlobClient(thumbnailBlobName);

        using var image = await Image.LoadAsync(photoStream);
        var originalWidth = image.Width;
        var originalHeight = image.Height;

        // Upload main image
        if (image.Width > 1920)
        {
            var ratio = 1920.0 / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(1920, newHeight));
        }

        using var mainStream = new MemoryStream();
        await image.SaveAsync(mainStream, new JpegEncoder { Quality = 85 });
        mainStream.Position = 0;
        await blobClient.UploadAsync(mainStream, new BlobHttpHeaders { ContentType = "image/jpeg" });

        // Upload thumbnail
        var thumbnail = image.Clone();
        if (thumbnail.Width > 300)
        {
            var ratio = 300.0 / thumbnail.Width;
            var newHeight = (int)(thumbnail.Height * ratio);
            thumbnail.Mutate(x => x.Resize(300, newHeight));
        }

        using var thumbnailStream = new MemoryStream();
        await thumbnail.SaveAsync(thumbnailStream, new JpegEncoder { Quality = 75 });
        thumbnailStream.Position = 0;
        await thumbnailBlobClient.UploadAsync(thumbnailStream, new BlobHttpHeaders { ContentType = "image/jpeg" });
        thumbnail.Dispose();

        return new PhotoUploadResult
        {
            PhotoUrl = blobClient.Uri.ToString(),
            ThumbnailUrl = thumbnailBlobClient.Uri.ToString(),
            FileSizeBytes = mainStream.Length,
            Width = originalWidth,
            Height = originalHeight
        };
    }

    public async Task<Stream> GetPhotoAsync(string photoUrl)
    {
        var blobName = Path.GetFileName(new Uri(photoUrl).LocalPath);
        var blobClient = _containerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task<Stream> GetThumbnailAsync(string thumbnailUrl)
    {
        return await GetPhotoAsync(thumbnailUrl);
    }

    public async Task DeletePhotoAsync(string photoUrl)
    {
        var blobName = Path.GetFileName(new Uri(photoUrl).LocalPath);
        var blobClient = _containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync();

        // Also delete thumbnail if exists
        var thumbnailBlobName = $"thumb_{blobName}";
        var thumbnailBlobClient = _containerClient.GetBlobClient(thumbnailBlobName);
        await thumbnailBlobClient.DeleteIfExistsAsync();

        _logger.LogInformation("Deleted photo from Azure: {BlobName}", blobName);
    }

    public async Task<bool> PhotoExistsAsync(string photoUrl)
    {
        var blobName = Path.GetFileName(new Uri(photoUrl).LocalPath);
        var blobClient = _containerClient.GetBlobClient(blobName);

        return await blobClient.ExistsAsync();
    }

    public string GetPhotoUrl(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        return blobClient.Uri.ToString();
    }
}
```

### PhotoService Implementation
```csharp
public class PhotoService : IPhotoService
{
    private readonly AllowanceContext _context;
    private readonly IPhotoStorageService _storageService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PhotoService> _logger;

    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".heic" };
    private readonly long _maxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public async Task<PhotoAttachment> AttachPhotoToTransactionAsync(Guid transactionId, IFormFile photo)
    {
        // Validate photo
        if (!await ValidatePhotoAsync(photo))
            throw new InvalidOperationException("Invalid photo file");

        var transaction = await _context.Transactions
            .Include(t => t.Child)
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (transaction == null)
            throw new InvalidOperationException("Transaction not found");

        // Upload photo with thumbnail
        using var photoStream = photo.OpenReadStream();
        var uploadResult = await _storageService.UploadPhotoWithThumbnailAsync(
            photoStream,
            photo.FileName,
            photo.ContentType);

        // Update transaction
        transaction.ReceiptPhotoUrl = uploadResult.PhotoUrl;
        transaction.ReceiptPhotoThumbnailUrl = uploadResult.ThumbnailUrl;
        transaction.ReceiptPhotoSizeBytes = uploadResult.FileSizeBytes;
        transaction.ReceiptPhotoOriginalName = photo.FileName;

        // Create PhotoAttachment record
        var photoAttachment = new PhotoAttachment
        {
            TransactionId = transactionId,
            FileName = Path.GetFileName(uploadResult.PhotoUrl),
            OriginalFileName = photo.FileName,
            StoragePath = uploadResult.PhotoUrl,
            ThumbnailPath = uploadResult.ThumbnailUrl,
            ContentType = photo.ContentType,
            FileSizeBytes = uploadResult.FileSizeBytes,
            Width = uploadResult.Width,
            Height = uploadResult.Height,
            UploadedById = _currentUser.UserId
        };

        _context.PhotoAttachments.Add(photoAttachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Photo attached to transaction {TransactionId}", transactionId);

        return photoAttachment;
    }

    public async Task<PhotoAttachment?> GetPhotoForTransactionAsync(Guid transactionId)
    {
        return await _context.PhotoAttachments
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
    }

    public async Task DeletePhotoFromTransactionAsync(Guid transactionId)
    {
        var transaction = await _context.Transactions.FindAsync(transactionId);
        if (transaction?.ReceiptPhotoUrl == null)
            return;

        // Delete from storage
        await _storageService.DeletePhotoAsync(transaction.ReceiptPhotoUrl);

        // Update transaction
        transaction.ReceiptPhotoUrl = null;
        transaction.ReceiptPhotoThumbnailUrl = null;
        transaction.ReceiptPhotoSizeBytes = null;
        transaction.ReceiptPhotoOriginalName = null;

        // Delete PhotoAttachment record
        var attachment = await _context.PhotoAttachments
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

        if (attachment != null)
        {
            _context.PhotoAttachments.Remove(attachment);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Photo deleted from transaction {TransactionId}", transactionId);
    }

    public async Task<List<PhotoAttachment>> GetPhotosForChildAsync(Guid childId, int limit = 50)
    {
        return await _context.PhotoAttachments
            .Include(p => p.Transaction)
            .Where(p => p.Transaction.ChildId == childId)
            .OrderByDescending(p => p.UploadedAt)
            .Take(limit)
            .ToListAsync();
    }

    public Task<bool> ValidatePhotoAsync(IFormFile photo)
    {
        if (photo == null || photo.Length == 0)
            return Task.FromResult(false);

        // Check file size
        if (photo.Length > _maxFileSizeBytes)
        {
            _logger.LogWarning("Photo exceeds max file size: {Size} bytes", photo.Length);
            return Task.FromResult(false);
        }

        // Check file extension
        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Invalid photo extension: {Extension}", extension);
            return Task.FromResult(false);
        }

        // Check content type
        if (!photo.ContentType.StartsWith("image/"))
        {
            _logger.LogWarning("Invalid content type: {ContentType}", photo.ContentType);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public async Task<byte[]> CompressImageAsync(Stream imageStream, int maxWidth = 1920, int quality = 85)
    {
        using var image = await Image.LoadAsync(imageStream);

        if (image.Width > maxWidth)
        {
            var ratio = (double)maxWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(maxWidth, newHeight));
        }

        using var outputStream = new MemoryStream();
        await image.SaveAsync(outputStream, new JpegEncoder { Quality = quality });

        return outputStream.ToArray();
    }

    public async Task<byte[]> GenerateThumbnailAsync(Stream imageStream, int maxWidth = 300)
    {
        using var image = await Image.LoadAsync(imageStream);

        if (image.Width > maxWidth)
        {
            var ratio = (double)maxWidth / image.Width;
            var newHeight = (int)(image.Height * ratio);
            image.Mutate(x => x.Resize(maxWidth, newHeight));
        }

        using var outputStream = new MemoryStream();
        await image.SaveAsync(outputStream, new JpegEncoder { Quality = 75 });

        return outputStream.ToArray();
    }
}
```

## DTOs

```csharp
public record PhotoAttachmentDto(
    Guid Id,
    Guid TransactionId,
    string FileName,
    string OriginalFileName,
    string StoragePath,
    string ThumbnailPath,
    long FileSizeBytes,
    int Width,
    int Height,
    DateTime UploadedAt
);

public record UploadPhotoRequest
{
    public Guid TransactionId { get; set; }
    public IFormFile Photo { get; set; } = null!;
}
```

## API Controllers

### PhotosController
```csharp
[ApiController]
[Route("api/v1/photos")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;
    private readonly IPhotoStorageService _storageService;
    private readonly ICurrentUserService _currentUser;

    [HttpPost("upload")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<ActionResult<PhotoAttachmentDto>> UploadPhoto([FromForm] UploadPhotoRequest request)
    {
        if (request.Photo == null || request.Photo.Length == 0)
            return BadRequest("Photo file is required");

        if (!await _photoService.ValidatePhotoAsync(request.Photo))
            return BadRequest("Invalid photo file. Must be JPG, PNG, or HEIC under 10MB");

        try
        {
            var attachment = await _photoService.AttachPhotoToTransactionAsync(
                request.TransactionId,
                request.Photo);

            var dto = new PhotoAttachmentDto(
                attachment.Id,
                attachment.TransactionId,
                attachment.FileName,
                attachment.OriginalFileName,
                attachment.StoragePath,
                attachment.ThumbnailPath,
                attachment.FileSizeBytes,
                attachment.Width,
                attachment.Height,
                attachment.UploadedAt
            );

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("transaction/{transactionId}")]
    public async Task<ActionResult<PhotoAttachmentDto>> GetPhotoForTransaction(Guid transactionId)
    {
        var attachment = await _photoService.GetPhotoForTransactionAsync(transactionId);

        if (attachment == null)
            return NotFound();

        var dto = new PhotoAttachmentDto(
            attachment.Id,
            attachment.TransactionId,
            attachment.FileName,
            attachment.OriginalFileName,
            attachment.StoragePath,
            attachment.ThumbnailPath,
            attachment.FileSizeBytes,
            attachment.Width,
            attachment.Height,
            attachment.UploadedAt
        );

        return Ok(dto);
    }

    [HttpGet("child/{childId}")]
    public async Task<ActionResult<List<PhotoAttachmentDto>>> GetPhotosForChild(Guid childId)
    {
        var attachments = await _photoService.GetPhotosForChildAsync(childId);

        var dtos = attachments.Select(a => new PhotoAttachmentDto(
            a.Id,
            a.TransactionId,
            a.FileName,
            a.OriginalFileName,
            a.StoragePath,
            a.ThumbnailPath,
            a.FileSizeBytes,
            a.Width,
            a.Height,
            a.UploadedAt
        )).ToList();

        return Ok(dtos);
    }

    [HttpDelete("transaction/{transactionId}")]
    public async Task<IActionResult> DeletePhoto(Guid transactionId)
    {
        await _photoService.DeletePhotoFromTransactionAsync(transactionId);
        return NoContent();
    }

    [HttpGet("{attachmentId}/download")]
    public async Task<IActionResult> DownloadPhoto(Guid attachmentId)
    {
        var attachment = await _context.PhotoAttachments.FindAsync(attachmentId);

        if (attachment == null)
            return NotFound();

        var stream = await _storageService.GetPhotoAsync(attachment.StoragePath);
        return File(stream, attachment.ContentType, attachment.OriginalFileName);
    }
}
```

## Blazor Components

### PhotoUpload.razor
```razor
@inject IPhotoService PhotoService

<div class="photo-upload">
    @if (string.IsNullOrEmpty(existingPhotoUrl))
    {
        <InputFile OnChange="@HandlePhotoSelected" accept="image/*" capture="environment" class="form-control" />

        @if (selectedPhoto != null)
        {
            <div class="preview mt-3">
                <img src="@previewUrl" alt="Preview" class="img-thumbnail" style="max-height: 200px;" />
                <div class="mt-2">
                    <button class="btn btn-sm btn-primary" @onclick="UploadPhoto" disabled="@isUploading">
                        @if (isUploading)
                        {
                            <span class="spinner-border spinner-border-sm me-1"></span>
                        }
                        Upload Receipt
                    </button>
                    <button class="btn btn-sm btn-outline-secondary ms-2" @onclick="ClearSelection">
                        Clear
                    </button>
                </div>
            </div>
        }

        @if (!string.IsNullOrEmpty(errorMessage))
        {
            <div class="alert alert-danger mt-2">@errorMessage</div>
        }
    }
    else
    {
        <div class="existing-photo">
            <img src="@thumbnailUrl" alt="Receipt" class="img-thumbnail" style="max-height: 150px;" @onclick="ShowFullImage" />
            <div class="mt-2">
                <button class="btn btn-sm btn-outline-primary" @onclick="ShowFullImage">
                    <i class="bi bi-zoom-in"></i> View
                </button>
                <button class="btn btn-sm btn-outline-danger ms-2" @onclick="DeletePhoto">
                    <i class="bi bi-trash"></i> Delete
                </button>
            </div>
        </div>
    }
</div>

@code {
    [Parameter] public Guid TransactionId { get; set; }
    [Parameter] public string? ExistingPhotoUrl { get; set; }
    [Parameter] public string? ExistingThumbnailUrl { get; set; }
    [Parameter] public EventCallback OnPhotoUploaded { get; set; }
    [Parameter] public EventCallback OnPhotoDeleted { get; set; }

    private IBrowserFile? selectedPhoto;
    private string? previewUrl;
    private string? existingPhotoUrl;
    private string? thumbnailUrl;
    private bool isUploading = false;
    private string? errorMessage;

    protected override void OnParametersSet()
    {
        existingPhotoUrl = ExistingPhotoUrl;
        thumbnailUrl = ExistingThumbnailUrl ?? ExistingPhotoUrl;
    }

    private async Task HandlePhotoSelected(InputFileChangeEventArgs e)
    {
        selectedPhoto = e.File;
        errorMessage = null;

        // Validate file size (10 MB)
        if (selectedPhoto.Size > 10 * 1024 * 1024)
        {
            errorMessage = "Photo must be under 10 MB";
            selectedPhoto = null;
            return;
        }

        // Validate file type
        if (!selectedPhoto.ContentType.StartsWith("image/"))
        {
            errorMessage = "Only image files are allowed";
            selectedPhoto = null;
            return;
        }

        // Generate preview
        var resizedImage = await selectedPhoto.RequestImageFileAsync("image/jpeg", 400, 400);
        using var stream = resizedImage.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());
        previewUrl = $"data:image/jpeg;base64,{base64}";
    }

    private async Task UploadPhoto()
    {
        if (selectedPhoto == null) return;

        isUploading = true;
        errorMessage = null;

        try
        {
            // Upload via API
            using var content = new MultipartFormDataContent();
            using var stream = selectedPhoto.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            using var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(selectedPhoto.ContentType);

            content.Add(streamContent, "photo", selectedPhoto.Name);
            content.Add(new StringContent(TransactionId.ToString()), "transactionId");

            // Call API endpoint
            // var response = await HttpClient.PostAsync("/api/v1/photos/upload", content);

            existingPhotoUrl = previewUrl; // Placeholder
            await OnPhotoUploaded.InvokeAsync();

            ClearSelection();
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to upload photo: {ex.Message}";
        }
        finally
        {
            isUploading = false;
        }
    }

    private void ClearSelection()
    {
        selectedPhoto = null;
        previewUrl = null;
    }

    private async Task DeletePhoto()
    {
        if (await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this photo?"))
        {
            // Call API to delete
            existingPhotoUrl = null;
            thumbnailUrl = null;
            await OnPhotoDeleted.InvokeAsync();
        }
    }

    private async Task ShowFullImage()
    {
        // Open modal or new window with full image
        await JSRuntime.InvokeVoidAsync("window.open", existingPhotoUrl, "_blank");
    }
}
```

### PhotoGallery.razor
```razor
@page "/children/{ChildId:guid}/photos"
@inject IPhotoService PhotoService

<h2>Receipt Photo Gallery</h2>

@if (photos == null)
{
    <p>Loading...</p>
}
else if (!photos.Any())
{
    <div class="alert alert-info">
        No receipt photos uploaded yet.
    </div>
}
else
{
    <div class="row">
        @foreach (var photo in photos)
        {
            <div class="col-md-3 col-sm-6 mb-4">
                <div class="card photo-card">
                    <img src="@photo.ThumbnailPath" class="card-img-top" alt="Receipt" @onclick="() => ShowPhoto(photo)" />
                    <div class="card-body">
                        <p class="card-text">
                            <small>@photo.UploadedAt.ToString("MMM dd, yyyy")</small><br />
                            <small>@FormatFileSize(photo.FileSizeBytes)</small>
                        </p>
                        <button class="btn btn-sm btn-primary" @onclick="() => ShowPhoto(photo)">
                            View
                        </button>
                    </div>
                </div>
            </div>
        }
    </div>
}

@if (selectedPhoto != null)
{
    <div class="modal fade show d-block" tabindex="-1" style="background-color: rgba(0,0,0,0.5);">
        <div class="modal-dialog modal-lg modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">Receipt Photo</h5>
                    <button type="button" class="btn-close" @onclick="CloseModal"></button>
                </div>
                <div class="modal-body text-center">
                    <img src="@selectedPhoto.StoragePath" alt="Receipt" class="img-fluid" />
                    <p class="mt-3">
                        <strong>Uploaded:</strong> @selectedPhoto.UploadedAt.ToString("MMMM dd, yyyy h:mm tt")<br />
                        <strong>Size:</strong> @FormatFileSize(selectedPhoto.FileSizeBytes)<br />
                        <strong>Dimensions:</strong> @selectedPhoto.Width x @selectedPhoto.Height
                    </p>
                </div>
                <div class="modal-footer">
                    <a href="@($"/api/v1/photos/{selectedPhoto.Id}/download")" class="btn btn-primary" target="_blank">
                        Download
                    </a>
                    <button type="button" class="btn btn-secondary" @onclick="CloseModal">
                        Close
                    </button>
                </div>
            </div>
        </div>
    </div>
}

@code {
    [Parameter] public Guid ChildId { get; set; }

    private List<PhotoAttachmentDto>? photos;
    private PhotoAttachmentDto? selectedPhoto;

    protected override async Task OnInitializedAsync()
    {
        await LoadPhotos();
    }

    private async Task LoadPhotos()
    {
        // Load from API
        photos = new List<PhotoAttachmentDto>();
    }

    private void ShowPhoto(PhotoAttachmentDto photo)
    {
        selectedPhoto = photo;
    }

    private void CloseModal()
    {
        selectedPhoto = null;
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
```

## Test Cases (15 Tests Total)

### PhotoStorageService Tests
```csharp
public class LocalFileStorageServiceTests
{
    [Fact]
    public async Task UploadPhoto_CreatesFileSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var photoStream = CreateTestImageStream();

        // Act
        var result = await service.UploadPhotoAsync(photoStream, "test.jpg", "image/jpeg");

        // Assert
        result.Should().NotBeNull();
        result.PhotoUrl.Should().NotBeNullOrEmpty();
        result.FileSizeBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task UploadPhotoWithThumbnail_CreatesBothImages()
    {
        // Arrange
        var service = CreateService();
        var photoStream = CreateTestImageStream();

        // Act
        var result = await service.UploadPhotoWithThumbnailAsync(photoStream, "test.jpg", "image/jpeg");

        // Assert
        result.PhotoUrl.Should().NotBeNullOrEmpty();
        result.ThumbnailUrl.Should().NotBeNullOrEmpty();
        await service.PhotoExistsAsync(result.PhotoUrl).Should().BeTrueAsync();
    }

    [Fact]
    public async Task DeletePhoto_RemovesFile()
    {
        // Arrange
        var service = CreateService();
        var result = await UploadTestPhoto(service);

        // Act
        await service.DeletePhotoAsync(result.PhotoUrl);

        // Assert
        await service.PhotoExistsAsync(result.PhotoUrl).Should().BeFalseAsync();
    }
}

public class PhotoServiceTests
{
    [Fact]
    public async Task AttachPhotoToTransaction_UploadsAndLinksPhoto()
    {
        // Arrange
        var service = CreateService();
        var transaction = await CreateTestTransaction();
        var photo = CreateMockFormFile("test.jpg", "image/jpeg", 1024);

        // Act
        var attachment = await service.AttachPhotoToTransactionAsync(transaction.Id, photo);

        // Assert
        attachment.Should().NotBeNull();
        attachment.TransactionId.Should().Be(transaction.Id);
        attachment.FileSizeBytes.Should().BeGreaterThan(0);

        var updatedTransaction = await _context.Transactions.FindAsync(transaction.Id);
        updatedTransaction!.ReceiptPhotoUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AttachPhoto_InvalidFile_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var transaction = await CreateTestTransaction();
        var photo = CreateMockFormFile("test.txt", "text/plain", 1024);

        // Act
        var act = () => service.AttachPhotoToTransactionAsync(transaction.Id, photo);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid photo file");
    }

    [Fact]
    public async Task ValidatePhoto_ValidFile_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();
        var photo = CreateMockFormFile("test.jpg", "image/jpeg", 1024 * 1024); // 1 MB

        // Act
        var result = await service.ValidatePhotoAsync(photo);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePhoto_FileTooLarge_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var photo = CreateMockFormFile("test.jpg", "image/jpeg", 15 * 1024 * 1024); // 15 MB

        // Act
        var result = await service.ValidatePhotoAsync(photo);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePhoto_InvalidExtension_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();
        var photo = CreateMockFormFile("test.exe", "application/exe", 1024);

        // Act
        var result = await service.ValidatePhotoAsync(photo);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CompressImage_ReducesFileSize()
    {
        // Arrange
        var service = CreateService();
        var originalStream = CreateLargeTestImageStream(3000, 2000); // Large image

        // Act
        var compressed = await service.CompressImageAsync(originalStream, maxWidth: 1920, quality: 85);

        // Assert
        compressed.Length.Should().BeLessThan(originalStream.Length);
    }

    [Fact]
    public async Task GenerateThumbnail_CreatesSmallImage()
    {
        // Arrange
        var service = CreateService();
        var imageStream = CreateTestImageStream(1920, 1080);

        // Act
        var thumbnail = await service.GenerateThumbnailAsync(imageStream, maxWidth: 300);

        // Assert
        thumbnail.Length.Should().BeLessThan(imageStream.Length);

        // Verify dimensions
        using var image = Image.Load(thumbnail);
        image.Width.Should().BeLessOrEqualTo(300);
    }

    [Fact]
    public async Task GetPhotosForChild_ReturnsOrderedPhotos()
    {
        // Arrange
        var service = CreateService();
        var child = await CreateTestChild();
        await CreateMultiplePhotosForChild(child.Id, 5);

        // Act
        var photos = await service.GetPhotosForChildAsync(child.Id);

        // Assert
        photos.Should().HaveCount(5);
        photos.Should().BeInDescendingOrder(p => p.UploadedAt);
    }

    [Fact]
    public async Task DeletePhotoFromTransaction_RemovesPhotoAndLink()
    {
        // Arrange
        var service = CreateService();
        var transaction = await CreateTestTransactionWithPhoto();

        // Act
        await service.DeletePhotoFromTransactionAsync(transaction.Id);

        // Assert
        var updatedTransaction = await _context.Transactions.FindAsync(transaction.Id);
        updatedTransaction!.ReceiptPhotoUrl.Should().BeNull();

        var attachment = await _context.PhotoAttachments
            .FirstOrDefaultAsync(p => p.TransactionId == transaction.Id);
        attachment.Should().BeNull();
    }
}

public class PhotosControllerTests
{
    [Fact]
    public async Task UploadPhoto_ValidFile_ReturnsAttachment()
    {
        // Arrange
        var controller = CreateController();
        var transaction = await CreateTestTransaction();
        var photo = CreateMockFormFile("test.jpg", "image/jpeg", 1024);
        var request = new UploadPhotoRequest { TransactionId = transaction.Id, Photo = photo };

        // Act
        var result = await controller.UploadPhoto(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var dto = (result as OkObjectResult)!.Value as PhotoAttachmentDto;
        dto!.TransactionId.Should().Be(transaction.Id);
    }

    [Fact]
    public async Task UploadPhoto_NoFile_ReturnsBadRequest()
    {
        // Arrange
        var controller = CreateController();
        var request = new UploadPhotoRequest { TransactionId = Guid.NewGuid(), Photo = null! };

        // Act
        var result = await controller.UploadPhoto(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPhotoForTransaction_ExistingPhoto_ReturnsDto()
    {
        // Arrange
        var controller = CreateController();
        var transaction = await CreateTestTransactionWithPhoto();

        // Act
        var result = await controller.GetPhotoForTransaction(transaction.Id);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeletePhoto_RemovesPhotoSuccessfully()
    {
        // Arrange
        var controller = CreateController();
        var transaction = await CreateTestTransactionWithPhoto();

        // Act
        var result = await controller.DeletePhoto(transaction.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
```

## Photo Required Rule

### Optional: Enforce photo for large transactions
```csharp
public class TransactionService
{
    public async Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto)
    {
        // ... existing code ...

        // Check if photo is required for large transactions
        var photoRequiredThreshold = _configuration.GetValue<decimal>("Transactions:PhotoRequiredThreshold", 50m);

        if (dto.Amount > photoRequiredThreshold && string.IsNullOrEmpty(dto.ReceiptPhotoUrl))
        {
            throw new InvalidOperationException($"Receipt photo required for transactions over {photoRequiredThreshold:C}");
        }

        // ... rest of implementation ...
    }
}
```

## Success Metrics

### Performance Targets
- Photo upload: < 2 seconds for 5MB file
- Thumbnail generation: < 500ms
- Photo compression: < 1 second
- Gallery loading: < 500ms for 50 photos

### Quality Metrics
- 15 tests passing (100% critical path coverage)
- Image compression maintains quality
- Thumbnails properly sized (< 50KB)
- No photo data loss

## Configuration

### appsettings.json
```json
{
  "PhotoStorage": {
    "Provider": "Local",
    "LocalPath": "wwwroot/uploads/photos",
    "BaseUrl": "/uploads/photos",
    "AzureConnectionString": "",
    "ContainerName": "receipt-photos",
    "MaxWidth": 1920,
    "ThumbnailWidth": 300,
    "MaxFileSizeMB": 10,
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".heic"]
  },
  "Transactions": {
    "PhotoRequiredThreshold": 50.00
  }
}
```

### Program.cs Registration
```csharp
// Choose storage provider based on configuration
var storageProvider = builder.Configuration["PhotoStorage:Provider"];
if (storageProvider == "Azure")
{
    builder.Services.AddScoped<IPhotoStorageService, AzureBlobStorageService>();
}
else
{
    builder.Services.AddScoped<IPhotoStorageService, LocalFileStorageService>();
}

builder.Services.AddScoped<IPhotoService, PhotoService>();
```
