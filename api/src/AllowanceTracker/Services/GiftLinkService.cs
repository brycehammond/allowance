using System.Security.Cryptography;
using AllowanceTracker.Data;
using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AllowanceTracker.Services;

public class GiftLinkService : IGiftLinkService
{
    private readonly AllowanceContext _context;
    private readonly string _baseUrl;

    public GiftLinkService(AllowanceContext context, IConfiguration configuration)
    {
        _context = context;
        _baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://allowance.example.com";
    }

    public async Task<GiftLinkDto> CreateLinkAsync(CreateGiftLinkDto dto, Guid createdById)
    {
        // Verify parent owns the child
        var parent = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == createdById);

        var child = await _context.Children
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == dto.ChildId);

        if (parent == null || child == null || parent.FamilyId != child.FamilyId)
        {
            throw new InvalidOperationException("You are not authorized to create a gift link for this child.");
        }

        var token = GenerateSecureToken();

        var giftLink = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = dto.ChildId,
            FamilyId = child.FamilyId,
            CreatedById = createdById,
            Token = token,
            Name = dto.Name,
            Description = dto.Description,
            Visibility = dto.Visibility,
            IsActive = true,
            ExpiresAt = dto.ExpiresAt,
            MaxUses = dto.MaxUses,
            UseCount = 0,
            MinAmount = dto.MinAmount,
            MaxAmount = dto.MaxAmount,
            DefaultOccasion = dto.DefaultOccasion,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GiftLinks.Add(giftLink);
        await _context.SaveChangesAsync();

        return ToDto(giftLink, child.User.FirstName);
    }

    public async Task<List<GiftLinkDto>> GetFamilyLinksAsync(Guid familyId)
    {
        var links = await _context.GiftLinks
            .Include(gl => gl.Child)
                .ThenInclude(c => c.User)
            .Where(gl => gl.FamilyId == familyId)
            .OrderByDescending(gl => gl.CreatedAt)
            .ToListAsync();

        return links.Select(gl => ToDto(gl, gl.Child.User.FirstName)).ToList();
    }

    public async Task<GiftLinkDto?> GetLinkByIdAsync(Guid linkId)
    {
        var link = await _context.GiftLinks
            .Include(gl => gl.Child)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(gl => gl.Id == linkId);

        if (link == null)
            return null;

        return ToDto(link, link.Child.User.FirstName);
    }

    public async Task<GiftLinkDto?> UpdateLinkAsync(Guid linkId, UpdateGiftLinkDto dto)
    {
        var link = await _context.GiftLinks
            .Include(gl => gl.Child)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(gl => gl.Id == linkId);

        if (link == null)
            return null;

        if (dto.Name != null)
            link.Name = dto.Name;

        if (dto.Description != null)
            link.Description = dto.Description;

        if (dto.Visibility.HasValue)
            link.Visibility = dto.Visibility.Value;

        if (dto.IsActive.HasValue)
            link.IsActive = dto.IsActive.Value;

        if (dto.ExpiresAt.HasValue)
            link.ExpiresAt = dto.ExpiresAt.Value;

        if (dto.MaxUses.HasValue)
            link.MaxUses = dto.MaxUses.Value;

        if (dto.MinAmount.HasValue)
            link.MinAmount = dto.MinAmount.Value;

        if (dto.MaxAmount.HasValue)
            link.MaxAmount = dto.MaxAmount.Value;

        if (dto.DefaultOccasion.HasValue)
            link.DefaultOccasion = dto.DefaultOccasion.Value;

        link.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ToDto(link, link.Child.User.FirstName);
    }

    public async Task<bool> DeactivateLinkAsync(Guid linkId)
    {
        var link = await _context.GiftLinks.FindAsync(linkId);

        if (link == null)
            return false;

        link.IsActive = false;
        link.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<GiftLinkDto?> RegenerateTokenAsync(Guid linkId)
    {
        var link = await _context.GiftLinks
            .Include(gl => gl.Child)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(gl => gl.Id == linkId);

        if (link == null)
            return null;

        link.Token = GenerateSecureToken();
        link.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return ToDto(link, link.Child.User.FirstName);
    }

    public async Task<GiftLink?> ValidateTokenAsync(string token)
    {
        var link = await _context.GiftLinks
            .Include(gl => gl.Child)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(gl => gl.Token == token);

        if (link == null)
            return null;

        // Check if link is active
        if (!link.IsActive)
            return null;

        // Check if link is expired
        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        // Check if max uses exceeded
        if (link.MaxUses.HasValue && link.UseCount >= link.MaxUses.Value)
            return null;

        return link;
    }

    public async Task<GiftLinkStatsDto?> GetLinkStatsAsync(Guid linkId)
    {
        var link = await _context.GiftLinks
            .Include(gl => gl.Gifts)
            .FirstOrDefaultAsync(gl => gl.Id == linkId);

        if (link == null)
            return null;

        var gifts = link.Gifts;
        var approvedGifts = gifts.Where(g => g.Status == GiftStatus.Approved).ToList();

        return new GiftLinkStatsDto(
            linkId,
            gifts.Count,
            gifts.Count(g => g.Status == GiftStatus.Pending),
            approvedGifts.Count,
            gifts.Count(g => g.Status == GiftStatus.Rejected),
            approvedGifts.Sum(g => g.Amount),
            gifts.OrderByDescending(g => g.CreatedAt).FirstOrDefault()?.CreatedAt
        );
    }

    public async Task IncrementUsageCountAsync(Guid linkId)
    {
        var link = await _context.GiftLinks.FindAsync(linkId);

        if (link != null)
        {
            link.UseCount++;
            link.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private GiftLinkDto ToDto(GiftLink link, string childName)
    {
        return new GiftLinkDto(
            link.Id,
            link.ChildId,
            childName,
            link.Token,
            link.Name,
            link.Description,
            link.Visibility,
            link.IsActive,
            link.ExpiresAt,
            link.MaxUses,
            link.UseCount,
            link.MinAmount,
            link.MaxAmount,
            link.DefaultOccasion,
            link.CreatedAt,
            link.UpdatedAt,
            $"{_baseUrl}/gift/{link.Token}"
        );
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
