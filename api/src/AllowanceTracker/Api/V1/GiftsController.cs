using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllowanceTracker.Api.V1;

/// <summary>
/// Request model for submitting a gift through the portal
/// </summary>
public class SubmitGiftRequest
{
    public string GiverName { get; set; } = string.Empty;
    public string? GiverEmail { get; set; }
    public string? GiverRelationship { get; set; }
    public decimal Amount { get; set; }
    public GiftOccasion Occasion { get; set; }
    public string? CustomOccasion { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request model for approving a gift
/// </summary>
public class ApproveGiftRequest
{
    public Guid? AllocateToGoalId { get; set; }
    public int? SavingsPercentage { get; set; }
}

/// <summary>
/// Request model for rejecting a gift
/// </summary>
public class RejectGiftRequest
{
    public string? Reason { get; set; }
}

/// <summary>
/// Request model for creating a thank you note
/// </summary>
public class CreateThankYouNoteRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Request model for updating a thank you note
/// </summary>
public class UpdateThankYouNoteRequest
{
    public string? Message { get; set; }
    public string? ImageUrl { get; set; }
}

[ApiController]
[Route("api/v1/gifts")]
public class GiftsController : ControllerBase
{
    private readonly IGiftService _giftService;
    private readonly IThankYouNoteService _thankYouNoteService;
    private readonly ICurrentUserService _currentUserService;

    public GiftsController(
        IGiftService giftService,
        IThankYouNoteService thankYouNoteService,
        ICurrentUserService currentUserService)
    {
        _giftService = giftService;
        _thankYouNoteService = thankYouNoteService;
        _currentUserService = currentUserService;
    }

    // ========== PUBLIC PORTAL ENDPOINTS (No Auth Required) ==========

    /// <summary>
    /// Get portal data for a gift link (public, no auth required)
    /// </summary>
    [HttpGet("portal/{token}")]
    [AllowAnonymous]
    public async Task<ActionResult<GiftPortalDataDto>> GetPortalData(string token)
    {
        try
        {
            var data = await _giftService.GetPortalDataAsync(token);
            return Ok(data);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Submit a gift through the portal (public, no auth required)
    /// </summary>
    [HttpPost("portal/{token}/submit")]
    [AllowAnonymous]
    public async Task<ActionResult<GiftSubmissionResultDto>> SubmitGift(string token, [FromBody] SubmitGiftRequest request)
    {
        var dto = new SubmitGiftDto(
            request.GiverName,
            request.GiverEmail,
            request.GiverRelationship,
            request.Amount,
            request.Occasion,
            request.CustomOccasion,
            request.Message
        );

        try
        {
            var result = await _giftService.SubmitGiftAsync(token, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ========== PARENT ENDPOINTS (Auth Required) ==========

    /// <summary>
    /// Get all pending gifts for the family (parent only)
    /// </summary>
    [HttpGet("pending")]
    [Authorize]
    public async Task<ActionResult<List<GiftDto>>> GetPendingGifts()
    {
        if (!_currentUserService.IsParent)
            return Forbid();

        var familyId = _currentUserService.FamilyId;
        if (familyId == null)
            return BadRequest("User is not part of a family");

        var gifts = await _giftService.GetPendingGiftsAsync(familyId.Value);
        return Ok(gifts);
    }

    /// <summary>
    /// Get a specific gift by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<GiftDto>> GetGift(Guid id)
    {
        var gift = await _giftService.GetGiftByIdAsync(id);
        if (gift == null)
            return NotFound();

        return Ok(gift);
    }

    /// <summary>
    /// Get all gifts for a child
    /// </summary>
    [HttpGet("child/{childId}")]
    [Authorize]
    public async Task<ActionResult<List<GiftDto>>> GetChildGifts(Guid childId)
    {
        var gifts = await _giftService.GetChildGiftsAsync(childId);
        return Ok(gifts);
    }

    /// <summary>
    /// Approve a pending gift (parent only)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize]
    public async Task<ActionResult<GiftDto>> ApproveGift(Guid id, [FromBody] ApproveGiftRequest request)
    {
        if (!_currentUserService.IsParent)
            return Forbid();

        var dto = new ApproveGiftDto(request.AllocateToGoalId, request.SavingsPercentage);

        try
        {
            var gift = await _giftService.ApproveGiftAsync(id, dto, _currentUserService.UserId);
            return Ok(gift);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Reject a pending gift (parent only)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize]
    public async Task<ActionResult<GiftDto>> RejectGift(Guid id, [FromBody] RejectGiftRequest request)
    {
        if (!_currentUserService.IsParent)
            return Forbid();

        var dto = new RejectGiftDto(request.Reason);

        try
        {
            var gift = await _giftService.RejectGiftAsync(id, dto, _currentUserService.UserId);
            return Ok(gift);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ========== THANK YOU NOTE ENDPOINTS (Child or Parent) ==========

    /// <summary>
    /// Get pending thank you notes for the current child
    /// </summary>
    [HttpGet("thank-you/pending")]
    [Authorize]
    public async Task<ActionResult<List<PendingThankYouDto>>> GetPendingThankYous()
    {
        var childId = GetChildIdFromUser();
        if (childId == null)
            return BadRequest("User is not a child");

        var pending = await _thankYouNoteService.GetPendingThankYousAsync(childId.Value);
        return Ok(pending);
    }

    /// <summary>
    /// Get a thank you note for a gift
    /// </summary>
    [HttpGet("{giftId}/thank-you")]
    [Authorize]
    public async Task<ActionResult<ThankYouNoteDto>> GetThankYouNote(Guid giftId)
    {
        var note = await _thankYouNoteService.GetNoteByGiftIdAsync(giftId);
        if (note == null)
            return NotFound();

        return Ok(note);
    }

    /// <summary>
    /// Create a thank you note for a gift (child only)
    /// </summary>
    [HttpPost("{giftId}/thank-you")]
    [Authorize]
    public async Task<ActionResult<ThankYouNoteDto>> CreateThankYouNote(Guid giftId, [FromBody] CreateThankYouNoteRequest request)
    {
        var childId = GetChildIdFromUser();
        if (childId == null)
            return BadRequest("User is not a child");

        var dto = new CreateThankYouNoteDto(request.Message, request.ImageUrl);

        try
        {
            var note = await _thankYouNoteService.CreateNoteAsync(giftId, dto, childId.Value);
            return CreatedAtAction(nameof(GetThankYouNote), new { giftId }, note);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update a thank you note (child only, if not yet sent)
    /// </summary>
    [HttpPut("{giftId}/thank-you")]
    [Authorize]
    public async Task<ActionResult<ThankYouNoteDto>> UpdateThankYouNote(Guid giftId, [FromBody] UpdateThankYouNoteRequest request)
    {
        var childId = GetChildIdFromUser();
        if (childId == null)
            return BadRequest("User is not a child");

        var dto = new UpdateThankYouNoteDto(request.Message, request.ImageUrl);

        try
        {
            var note = await _thankYouNoteService.UpdateNoteAsync(giftId, dto, childId.Value);
            if (note == null)
                return NotFound();

            return Ok(note);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Send a thank you note to the giver via email (child only)
    /// </summary>
    [HttpPost("{giftId}/thank-you/send")]
    [Authorize]
    public async Task<ActionResult<ThankYouNoteDto>> SendThankYouNote(Guid giftId)
    {
        var childId = GetChildIdFromUser();
        if (childId == null)
            return BadRequest("User is not a child");

        try
        {
            var note = await _thankYouNoteService.SendNoteAsync(giftId, childId.Value);
            return Ok(note);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private Guid? GetChildIdFromUser()
    {
        // If user is a child, return their child ID
        if (!_currentUserService.IsParent && _currentUserService.ChildId != null)
        {
            return _currentUserService.ChildId;
        }
        return null;
    }
}
