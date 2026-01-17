using AllowanceTracker.Data;
using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class ThankYouNoteService : IThankYouNoteService
{
    private readonly AllowanceContext _context;
    private readonly IEmailService? _emailService;

    public ThankYouNoteService(AllowanceContext context, IEmailService? emailService = null)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<List<PendingThankYouDto>> GetPendingThankYousAsync(Guid childId)
    {
        // Return only approved gifts that don't have a thank you note yet
        var approvedGifts = await _context.Gifts
            .Include(g => g.ThankYouNote)
            .Where(g => g.ChildId == childId && g.Status == GiftStatus.Approved && g.ThankYouNote == null)
            .OrderByDescending(g => g.ProcessedAt)
            .ToListAsync();

        return approvedGifts.Select(g => new PendingThankYouDto(
            g.Id,
            g.GiverName,
            g.GiverRelationship,
            g.Amount,
            g.Occasion,
            g.CustomOccasion,
            g.ProcessedAt ?? g.CreatedAt,
            (int)(DateTime.UtcNow - (g.ProcessedAt ?? g.CreatedAt)).TotalDays,
            false // HasNote is always false for pending thank yous
        )).ToList();
    }

    public async Task<ThankYouNoteDto?> GetNoteByGiftIdAsync(Guid giftId)
    {
        var note = await _context.ThankYouNotes
            .Include(n => n.Gift)
            .Include(n => n.Child)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(n => n.GiftId == giftId);

        return note == null ? null : ToDto(note);
    }

    public async Task<ThankYouNoteDto> CreateNoteAsync(Guid giftId, CreateThankYouNoteDto dto, Guid childId)
    {
        var child = await _context.Children
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
        {
            throw new InvalidOperationException("Child not found.");
        }

        var gift = await _context.Gifts
            .Include(g => g.Child)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(g => g.Id == giftId);

        if (gift == null)
        {
            throw new InvalidOperationException("Gift not found.");
        }

        if (gift.ChildId != childId)
        {
            throw new InvalidOperationException("You are not authorized to create a thank you note for this gift.");
        }

        // Check if note already exists
        var existingNote = await _context.ThankYouNotes.FirstOrDefaultAsync(n => n.GiftId == giftId);
        if (existingNote != null)
        {
            throw new InvalidOperationException("A thank you note already exists for this gift.");
        }

        var note = new ThankYouNote
        {
            Id = Guid.NewGuid(),
            GiftId = giftId,
            ChildId = childId,
            Message = dto.Message,
            ImageUrl = dto.ImageUrl,
            IsSent = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ThankYouNotes.Add(note);
        await _context.SaveChangesAsync();

        // Reload with includes for DTO
        await _context.Entry(note).Reference(n => n.Gift).LoadAsync();
        await _context.Entry(note).Reference(n => n.Child).LoadAsync();
        await _context.Entry(note.Child).Reference(c => c.User).LoadAsync();

        return ToDto(note);
    }

    public async Task<ThankYouNoteDto?> UpdateNoteAsync(Guid giftId, UpdateThankYouNoteDto dto, Guid childId)
    {
        var note = await _context.ThankYouNotes
            .Include(n => n.Gift)
            .Include(n => n.Child)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(n => n.GiftId == giftId);

        if (note == null)
        {
            return null;
        }

        if (note.ChildId != childId)
        {
            throw new InvalidOperationException("You are not authorized to update this thank you note.");
        }

        if (note.IsSent)
        {
            throw new InvalidOperationException("This thank you note has already been sent and cannot be modified.");
        }

        if (dto.Message != null)
        {
            note.Message = dto.Message;
        }

        if (dto.ImageUrl != null)
        {
            note.ImageUrl = dto.ImageUrl;
        }

        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ToDto(note);
    }

    public async Task<ThankYouNoteDto> SendNoteAsync(Guid giftId, Guid childId)
    {
        var note = await _context.ThankYouNotes
            .Include(n => n.Gift)
            .Include(n => n.Child)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(n => n.GiftId == giftId);

        if (note == null)
        {
            throw new InvalidOperationException("Thank you note not found.");
        }

        if (note.ChildId != childId)
        {
            throw new InvalidOperationException("You are not authorized to send this thank you note.");
        }

        if (note.IsSent)
        {
            throw new InvalidOperationException("This thank you note has already been sent.");
        }

        if (string.IsNullOrEmpty(note.Gift.GiverEmail))
        {
            throw new InvalidOperationException("Cannot send thank you note: the giver has no email address.");
        }

        // Send email
        if (_emailService != null)
        {
            await _emailService.SendThankYouNoteEmailAsync(
                note.Gift.GiverEmail,
                note.Gift.GiverName,
                note.Child.User.FirstName,
                note.Message,
                note.ImageUrl);
        }

        note.IsSent = true;
        note.SentAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ToDto(note);
    }

    public async Task<List<ThankYouNoteDto>> GetChildNotesAsync(Guid childId)
    {
        var notes = await _context.ThankYouNotes
            .Include(n => n.Gift)
            .Include(n => n.Child)
                .ThenInclude(c => c.User)
            .Where(n => n.ChildId == childId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notes.Select(ToDto).ToList();
    }

    private static ThankYouNoteDto ToDto(ThankYouNote note)
    {
        return new ThankYouNoteDto(
            note.Id,
            note.GiftId,
            note.ChildId,
            note.Child.User.FirstName,
            note.Gift.GiverName,
            note.Message,
            note.ImageUrl,
            note.IsSent,
            note.SentAt,
            note.CreatedAt,
            note.UpdatedAt
        );
    }
}
