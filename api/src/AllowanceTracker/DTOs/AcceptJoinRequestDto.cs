using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs;

/// <summary>
/// Request to accept a family join request (for existing users)
/// </summary>
public record AcceptJoinRequestDto(
    [Required]
    string Token);
