using AllowanceTracker.DTOs.Gifting;

namespace AllowanceTracker.Services;

public interface IThankYouNoteService
{
    /// <summary>
    /// Gets pending thank you notes (approved gifts without notes) for a child
    /// </summary>
    Task<List<PendingThankYouDto>> GetPendingThankYousAsync(Guid childId);

    /// <summary>
    /// Gets a thank you note by gift ID
    /// </summary>
    Task<ThankYouNoteDto?> GetNoteByGiftIdAsync(Guid giftId);

    /// <summary>
    /// Creates a thank you note for a gift
    /// </summary>
    Task<ThankYouNoteDto> CreateNoteAsync(Guid giftId, CreateThankYouNoteDto dto, Guid childId);

    /// <summary>
    /// Updates a thank you note (only if not yet sent)
    /// </summary>
    Task<ThankYouNoteDto?> UpdateNoteAsync(Guid giftId, UpdateThankYouNoteDto dto, Guid childId);

    /// <summary>
    /// Sends a thank you note to the giver via email
    /// </summary>
    Task<ThankYouNoteDto> SendNoteAsync(Guid giftId, Guid childId);

    /// <summary>
    /// Gets all thank you notes for a child
    /// </summary>
    Task<List<ThankYouNoteDto>> GetChildNotesAsync(Guid childId);
}
