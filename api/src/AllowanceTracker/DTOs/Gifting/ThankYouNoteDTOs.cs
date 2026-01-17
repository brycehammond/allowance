using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs.Gifting;

public record CreateThankYouNoteDto(
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    string Message,

    [Url]
    [StringLength(2048)]
    string? ImageUrl = null
);

public record UpdateThankYouNoteDto(
    [StringLength(2000, MinimumLength = 1)]
    string? Message = null,

    [Url]
    [StringLength(2048)]
    string? ImageUrl = null
);

public record SendThankYouNoteDto(
    bool SendEmail = true
);
