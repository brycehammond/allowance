using System.Security.Claims;

namespace AllowanceTracker.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userIdClaim != null ? Guid.Parse(userIdClaim) : Guid.Empty;
        }
    }

    public string Email
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        }
    }

    public bool IsParent
    {
        get
        {
            var roleClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
            return roleClaim == "Parent";
        }
    }

    public Guid? FamilyId
    {
        get
        {
            var familyIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("FamilyId")?.Value;
            if (string.IsNullOrEmpty(familyIdClaim))
                return null;
            return Guid.TryParse(familyIdClaim, out var familyId) ? familyId : null;
        }
    }

    public Guid? ChildId
    {
        get
        {
            var childIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("ChildId")?.Value;
            if (string.IsNullOrEmpty(childIdClaim))
                return null;
            return Guid.TryParse(childIdClaim, out var childId) ? childId : null;
        }
    }
}
