using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Request to transfer family ownership to another parent
/// </summary>
public record TransferOwnershipDto(
    [Required]
    Guid NewOwnerId);
