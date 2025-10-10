using System.ComponentModel.DataAnnotations;

namespace AllowanceTracker.DTOs;

public record LoginDto(
    [Required][EmailAddress] string Email,
    [Required] string Password,
    bool RememberMe = false);
