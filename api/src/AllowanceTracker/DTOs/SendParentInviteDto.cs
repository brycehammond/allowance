using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Request to send an invite to a co-parent
/// </summary>
public record SendParentInviteDto(
    [Required]
    [EmailAddress]
    string Email,

    [Required]
    [StringLength(50, MinimumLength = 1)]
    string FirstName,

    [Required]
    [StringLength(50, MinimumLength = 1)]
    string LastName);
