# Transaction Search & Filters Specification

## Overview

This specification implements advanced search and filtering capabilities for transactions, enabling users to find specific transactions quickly, create custom saved filters, and export filtered results. The system provides full-text search, date range filters, category filters, and saved filter views for frequently used searches.

## Goals

1. **Fast Search**: Full-text search on transaction descriptions with database indexing
2. **Advanced Filtering**: Filter by date range, categories, types, amounts, and approval status
3. **Saved Filters**: Create and save custom filter combinations for quick access
4. **Export**: Export filtered results to CSV for external analysis
5. **Autocomplete**: Search suggestions based on previous transaction descriptions
6. **Performance**: Efficient queries with proper indexing and pagination
7. **TDD Approach**: 22 comprehensive tests

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Entity Framework Core
- **Database**: PostgreSQL with full-text search and GIN indexes
- **UI**: Blazor Server with Radzen DatePicker
- **Export**: CsvHelper library for CSV generation
- **Testing**: xUnit, FluentAssertions, Moq

---

## Phase 1: Database Schema

### 1.1 Add Full-Text Search Index

**Migration**: `AddTransactionSearchIndex`

```csharp
public partial class AddTransactionSearchIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add GIN index for full-text search on Description
        migrationBuilder.Sql(@"
            CREATE INDEX IX_Transactions_Description_FullText
            ON ""Transactions""
            USING GIN (to_tsvector('english', ""Description""));
        ");

        // Add composite index for common filter combinations
        migrationBuilder.CreateIndex(
            name: "IX_Transactions_ChildId_CreatedAt_Type",
            table: "Transactions",
            columns: new[] { "ChildId", "CreatedAt", "Type" });

        // Add index for amount range queries
        migrationBuilder.CreateIndex(
            name: "IX_Transactions_Amount",
            table: "Transactions",
            column: "Amount");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Transactions_Description_FullText");
        migrationBuilder.DropIndex(name: "IX_Transactions_ChildId_CreatedAt_Type", table: "Transactions");
        migrationBuilder.DropIndex(name: "IX_Transactions_Amount", table: "Transactions");
    }
}
```

### 1.2 SavedFilter Model

```csharp
namespace AllowanceTracker.Models;

/// <summary>
/// Represents a saved filter configuration for transaction searches
/// </summary>
public class SavedFilter
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// JSON serialized filter criteria
    /// </summary>
    public string FilterCriteriaJson { get; set; } = string.Empty;

    /// <summary>
    /// Is this filter shared with family members?
    /// </summary>
    public bool IsShared { get; set; } = false;

    /// <summary>
    /// Display order for saved filters
    /// </summary>
    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### 1.3 Update AllowanceContext

```csharp
public class AllowanceContext : DbContext
{
    // Existing DbSets...

    public DbSet<SavedFilter> SavedFilters { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SavedFilter configuration
        modelBuilder.Entity<SavedFilter>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.FilterCriteriaJson)
                .IsRequired()
                .HasColumnType("jsonb"); // PostgreSQL JSONB for efficient queries

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.SortOrder });
        });
    }
}
```

---

## Phase 2: DTOs & Filter Models

### 2.1 TransactionFilterDto

```csharp
namespace AllowanceTracker.DTOs;

/// <summary>
/// Filter criteria for transaction searches
/// </summary>
public record TransactionFilterDto
{
    /// <summary>
    /// Full-text search on description
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// Filter by child ID (null = all children in family)
    /// </summary>
    public Guid? ChildId { get; init; }

    /// <summary>
    /// Date range start (inclusive)
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Date range end (inclusive)
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// Filter by transaction type (null = all)
    /// </summary>
    public TransactionType? Type { get; init; }

    /// <summary>
    /// Filter by transaction categories (null or empty = all)
    /// </summary>
    public List<TransactionCategory>? Categories { get; init; }

    /// <summary>
    /// Minimum amount (inclusive)
    /// </summary>
    public decimal? AmountMin { get; init; }

    /// <summary>
    /// Maximum amount (inclusive)
    /// </summary>
    public decimal? AmountMax { get; init; }

    /// <summary>
    /// Filter by approval status (for chores, future feature)
    /// </summary>
    public ApprovalStatus? ApprovalStatus { get; init; }

    /// <summary>
    /// Sort column
    /// </summary>
    public TransactionSortColumn SortBy { get; init; } = TransactionSortColumn.CreatedAt;

    /// <summary>
    /// Sort direction
    /// </summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;

    /// <summary>
    /// Page number (1-indexed)
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size (max 100)
    /// </summary>
    public int PageSize { get; init; } = 25;
}

public enum TransactionSortColumn
{
    CreatedAt,
    Amount,
    Description,
    BalanceAfter,
    Type
}

public enum SortDirection
{
    Ascending,
    Descending
}

public enum ApprovalStatus
{
    All,
    Pending,
    Approved,
    Rejected
}
```

### 2.2 TransactionSearchResultDto

```csharp
namespace AllowanceTracker.DTOs;

public record TransactionSearchResultDto(
    List<TransactionDto> Transactions,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    TransactionFilterDto AppliedFilters);

public record TransactionDto(
    Guid Id,
    Guid ChildId,
    string ChildName,
    decimal Amount,
    TransactionType Type,
    TransactionCategory? Category,
    string Description,
    decimal BalanceAfter,
    DateTime CreatedAt,
    string CreatedByName);
```

### 2.3 SavedFilterDto

```csharp
namespace AllowanceTracker.DTOs;

public record CreateSavedFilterDto(
    string Name,
    string Description,
    TransactionFilterDto FilterCriteria,
    bool IsShared = false);

public record UpdateSavedFilterDto(
    string? Name = null,
    string? Description = null,
    TransactionFilterDto? FilterCriteria = null,
    bool? IsShared = null,
    int? SortOrder = null);

public record SavedFilterDto(
    Guid Id,
    string Name,
    string Description,
    TransactionFilterDto FilterCriteria,
    bool IsShared,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt);
```

---

## Phase 3: Service Layer (TDD)

### 3.1 ITransactionSearchService Interface

```csharp
namespace AllowanceTracker.Services;

public interface ITransactionSearchService
{
    /// <summary>
    /// Search transactions with advanced filters
    /// </summary>
    Task<TransactionSearchResultDto> SearchTransactionsAsync(
        Guid familyId,
        TransactionFilterDto filter);

    /// <summary>
    /// Get autocomplete suggestions for search
    /// </summary>
    Task<List<string>> GetSearchSuggestionsAsync(
        Guid familyId,
        string partialText,
        int maxResults = 10);

    /// <summary>
    /// Export filtered transactions to CSV
    /// </summary>
    Task<byte[]> ExportToCsvAsync(
        Guid familyId,
        TransactionFilterDto filter);

    /// <summary>
    /// Save a filter for reuse
    /// </summary>
    Task<SavedFilter> CreateSavedFilterAsync(
        Guid userId,
        CreateSavedFilterDto dto);

    /// <summary>
    /// Get all saved filters for a user
    /// </summary>
    Task<List<SavedFilterDto>> GetSavedFiltersAsync(Guid userId);

    /// <summary>
    /// Get a specific saved filter
    /// </summary>
    Task<SavedFilterDto> GetSavedFilterAsync(Guid filterId);

    /// <summary>
    /// Update a saved filter
    /// </summary>
    Task<SavedFilter> UpdateSavedFilterAsync(
        Guid filterId,
        UpdateSavedFilterDto dto);

    /// <summary>
    /// Delete a saved filter
    /// </summary>
    Task DeleteSavedFilterAsync(Guid filterId);

    /// <summary>
    /// Apply a saved filter and get results
    /// </summary>
    Task<TransactionSearchResultDto> ApplySavedFilterAsync(
        Guid familyId,
        Guid filterId,
        int page = 1,
        int pageSize = 25);
}
```

### 3.2 TransactionSearchService Implementation

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace AllowanceTracker.Services;

public class TransactionSearchService : ITransactionSearchService
{
    private readonly AllowanceContext _context;
    private readonly ILogger<TransactionSearchService> _logger;

    public TransactionSearchService(
        AllowanceContext context,
        ILogger<TransactionSearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TransactionSearchResultDto> SearchTransactionsAsync(
        Guid familyId,
        TransactionFilterDto filter)
    {
        // Start with base query for family's children
        var query = _context.Transactions
            .Include(t => t.Child)
                .ThenInclude(c => c.User)
            .Include(t => t.CreatedBy)
            .Where(t => t.Child.FamilyId == familyId)
            .AsNoTracking();

        // Apply filters
        query = ApplyFilters(query, filter);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = ApplySorting(query, filter.SortBy, filter.SortDirection);

        // Apply pagination
        var pageSize = Math.Min(filter.PageSize, 100); // Max 100 items per page
        var skip = (filter.Page - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        // Execute query
        var transactions = await query.ToListAsync();

        // Map to DTOs
        var transactionDtos = transactions.Select(t => new TransactionDto(
            t.Id,
            t.ChildId,
            $"{t.Child.User.FirstName} {t.Child.User.LastName}",
            t.Amount,
            t.Type,
            null, // TODO: Add category when implemented
            t.Description,
            t.BalanceAfter,
            t.CreatedAt,
            $"{t.CreatedBy.FirstName} {t.CreatedBy.LastName}"
        )).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new TransactionSearchResultDto(
            transactionDtos,
            totalCount,
            filter.Page,
            pageSize,
            totalPages,
            filter);
    }

    private IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> query,
        TransactionFilterDto filter)
    {
        // Full-text search on description
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            // PostgreSQL full-text search
            query = query.Where(t =>
                EF.Functions.ToTsVector("english", t.Description)
                    .Matches(EF.Functions.PlainToTsQuery("english", filter.SearchText)));
        }

        // Child filter
        if (filter.ChildId.HasValue)
        {
            query = query.Where(t => t.ChildId == filter.ChildId.Value);
        }

        // Date range filter
        if (filter.StartDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            // End of day
            var endOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.CreatedAt <= endOfDay);
        }

        // Transaction type filter
        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        // Amount range filter
        if (filter.AmountMin.HasValue)
        {
            query = query.Where(t => t.Amount >= filter.AmountMin.Value);
        }

        if (filter.AmountMax.HasValue)
        {
            query = query.Where(t => t.Amount <= filter.AmountMax.Value);
        }

        return query;
    }

    private IQueryable<Transaction> ApplySorting(
        IQueryable<Transaction> query,
        TransactionSortColumn sortBy,
        SortDirection direction)
    {
        var ascending = direction == SortDirection.Ascending;

        return sortBy switch
        {
            TransactionSortColumn.CreatedAt => ascending
                ? query.OrderBy(t => t.CreatedAt)
                : query.OrderByDescending(t => t.CreatedAt),

            TransactionSortColumn.Amount => ascending
                ? query.OrderBy(t => t.Amount).ThenByDescending(t => t.CreatedAt)
                : query.OrderByDescending(t => t.Amount).ThenByDescending(t => t.CreatedAt),

            TransactionSortColumn.Description => ascending
                ? query.OrderBy(t => t.Description).ThenByDescending(t => t.CreatedAt)
                : query.OrderByDescending(t => t.Description).ThenByDescending(t => t.CreatedAt),

            TransactionSortColumn.BalanceAfter => ascending
                ? query.OrderBy(t => t.BalanceAfter).ThenByDescending(t => t.CreatedAt)
                : query.OrderByDescending(t => t.BalanceAfter).ThenByDescending(t => t.CreatedAt),

            TransactionSortColumn.Type => ascending
                ? query.OrderBy(t => t.Type).ThenByDescending(t => t.CreatedAt)
                : query.OrderByDescending(t => t.Type).ThenByDescending(t => t.CreatedAt),

            _ => query.OrderByDescending(t => t.CreatedAt)
        };
    }

    public async Task<List<string>> GetSearchSuggestionsAsync(
        Guid familyId,
        string partialText,
        int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(partialText) || partialText.Length < 2)
            return new List<string>();

        var suggestions = await _context.Transactions
            .Include(t => t.Child)
            .Where(t => t.Child.FamilyId == familyId)
            .Where(t => t.Description.Contains(partialText))
            .Select(t => t.Description)
            .Distinct()
            .OrderBy(d => d)
            .Take(maxResults)
            .ToListAsync();

        return suggestions;
    }

    public async Task<byte[]> ExportToCsvAsync(
        Guid familyId,
        TransactionFilterDto filter)
    {
        // Get all matching transactions (no pagination for export)
        var allRecordsFilter = filter with { Page = 1, PageSize = int.MaxValue };
        var result = await SearchTransactionsAsync(familyId, allRecordsFilter);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        // Write CSV data
        csv.WriteRecords(result.Transactions.Select(t => new
        {
            Date = t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            Child = t.ChildName,
            Type = t.Type.ToString(),
            Amount = t.Amount,
            Description = t.Description,
            BalanceAfter = t.BalanceAfter,
            CreatedBy = t.CreatedByName
        }));

        await writer.FlushAsync();
        return memoryStream.ToArray();
    }

    public async Task<SavedFilter> CreateSavedFilterAsync(
        Guid userId,
        CreateSavedFilterDto dto)
    {
        var filterJson = JsonSerializer.Serialize(dto.FilterCriteria);

        var savedFilter = new SavedFilter
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name,
            Description = dto.Description,
            FilterCriteriaJson = filterJson,
            IsShared = dto.IsShared,
            SortOrder = await GetNextSortOrderAsync(userId),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SavedFilters.Add(savedFilter);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created saved filter {FilterId} for user {UserId}", savedFilter.Id, userId);

        return savedFilter;
    }

    public async Task<List<SavedFilterDto>> GetSavedFiltersAsync(Guid userId)
    {
        var filters = await _context.SavedFilters
            .Where(f => f.UserId == userId || f.IsShared)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();

        return filters.Select(f => new SavedFilterDto(
            f.Id,
            f.Name,
            f.Description,
            JsonSerializer.Deserialize<TransactionFilterDto>(f.FilterCriteriaJson)!,
            f.IsShared,
            f.SortOrder,
            f.CreatedAt,
            f.UpdatedAt
        )).ToList();
    }

    public async Task<SavedFilterDto> GetSavedFilterAsync(Guid filterId)
    {
        var filter = await _context.SavedFilters.FindAsync(filterId)
            ?? throw new NotFoundException("Saved filter not found");

        return new SavedFilterDto(
            filter.Id,
            filter.Name,
            filter.Description,
            JsonSerializer.Deserialize<TransactionFilterDto>(filter.FilterCriteriaJson)!,
            filter.IsShared,
            filter.SortOrder,
            filter.CreatedAt,
            filter.UpdatedAt
        );
    }

    public async Task<SavedFilter> UpdateSavedFilterAsync(
        Guid filterId,
        UpdateSavedFilterDto dto)
    {
        var filter = await _context.SavedFilters.FindAsync(filterId)
            ?? throw new NotFoundException("Saved filter not found");

        if (dto.Name != null) filter.Name = dto.Name;
        if (dto.Description != null) filter.Description = dto.Description;
        if (dto.IsShared.HasValue) filter.IsShared = dto.IsShared.Value;
        if (dto.SortOrder.HasValue) filter.SortOrder = dto.SortOrder.Value;

        if (dto.FilterCriteria != null)
        {
            filter.FilterCriteriaJson = JsonSerializer.Serialize(dto.FilterCriteria);
        }

        filter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return filter;
    }

    public async Task DeleteSavedFilterAsync(Guid filterId)
    {
        var filter = await _context.SavedFilters.FindAsync(filterId)
            ?? throw new NotFoundException("Saved filter not found");

        _context.SavedFilters.Remove(filter);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted saved filter {FilterId}", filterId);
    }

    public async Task<TransactionSearchResultDto> ApplySavedFilterAsync(
        Guid familyId,
        Guid filterId,
        int page = 1,
        int pageSize = 25)
    {
        var savedFilter = await GetSavedFilterAsync(filterId);

        // Apply pagination to saved filter
        var filter = savedFilter.FilterCriteria with
        {
            Page = page,
            PageSize = pageSize
        };

        return await SearchTransactionsAsync(familyId, filter);
    }

    private async Task<int> GetNextSortOrderAsync(Guid userId)
    {
        var maxOrder = await _context.SavedFilters
            .Where(f => f.UserId == userId)
            .MaxAsync(f => (int?)f.SortOrder);

        return (maxOrder ?? 0) + 1;
    }
}
```

### 3.3 Test Cases (22 Tests)

```csharp
namespace AllowanceTracker.Tests.Services;

public class TransactionSearchServiceTests
{
    // Search Tests (8 tests)
    [Fact] SearchTransactions_NoFilters_ReturnsAllFamilyTransactions
    [Fact] SearchTransactions_WithSearchText_ReturnsMatchingDescriptions
    [Fact] SearchTransactions_WithDateRange_ReturnsTransactionsInRange
    [Fact] SearchTransactions_WithChildFilter_ReturnsOnlyChildTransactions
    [Fact] SearchTransactions_WithTypeFilter_ReturnsOnlyMatchingType
    [Fact] SearchTransactions_WithAmountRange_ReturnsTransactionsInRange
    [Fact] SearchTransactions_WithPagination_ReturnsCorrectPage
    [Fact] SearchTransactions_WithSorting_ReturnsSortedResults

    // Autocomplete Tests (3 tests)
    [Fact] GetSearchSuggestions_ValidPartialText_ReturnsSuggestions
    [Fact] GetSearchSuggestions_ShortText_ReturnsEmpty
    [Fact] GetSearchSuggestions_LimitsResults_ToMaxResults

    // Export Tests (2 tests)
    [Fact] ExportToCsv_WithFilters_GeneratesValidCsv
    [Fact] ExportToCsv_IncludesAllColumns

    // Saved Filter Tests (9 tests)
    [Fact] CreateSavedFilter_ValidData_CreatesSuccessfully
    [Fact] CreateSavedFilter_SerializesFilterCriteria
    [Fact] GetSavedFilters_ReturnsUserFilters
    [Fact] GetSavedFilters_IncludesSharedFilters
    [Fact] GetSavedFilter_ValidId_ReturnsFilter
    [Fact] GetSavedFilter_InvalidId_ThrowsNotFoundException
    [Fact] UpdateSavedFilter_UpdatesName
    [Fact] DeleteSavedFilter_RemovesFilter
    [Fact] ApplySavedFilter_AppliesFilterAndReturnsResults
}
```

---

## Phase 4: API Controllers

### 4.1 TransactionSearchController

```csharp
[ApiController]
[Route("api/v1/transactions/search")]
[Authorize]
public class TransactionSearchController : ControllerBase
{
    private readonly ITransactionSearchService _searchService;
    private readonly ICurrentUserService _currentUserService;

    [HttpPost]
    [ProducesResponseType(typeof(TransactionSearchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionSearchResultDto>> SearchTransactions(
        [FromBody] TransactionFilterDto filter)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var result = await _searchService.SearchTransactionsAsync(user.FamilyId!.Value, filter);
        return Ok(result);
    }

    [HttpGet("suggestions")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetSuggestions(
        [FromQuery] string partialText,
        [FromQuery] int maxResults = 10)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var suggestions = await _searchService.GetSearchSuggestionsAsync(
            user.FamilyId!.Value, partialText, maxResults);
        return Ok(suggestions);
    }

    [HttpPost("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToCsv([FromBody] TransactionFilterDto filter)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var csvData = await _searchService.ExportToCsvAsync(user.FamilyId!.Value, filter);

        return File(csvData, "text/csv", $"transactions_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpPost("saved-filters")]
    [ProducesResponseType(typeof(SavedFilter), StatusCodes.Status201Created)]
    public async Task<ActionResult<SavedFilter>> CreateSavedFilter(
        [FromBody] CreateSavedFilterDto dto)
    {
        var userId = _currentUserService.GetUserId();
        var filter = await _searchService.CreateSavedFilterAsync(userId, dto);
        return CreatedAtAction(nameof(GetSavedFilter), new { id = filter.Id }, filter);
    }

    [HttpGet("saved-filters")]
    [ProducesResponseType(typeof(List<SavedFilterDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SavedFilterDto>>> GetSavedFilters()
    {
        var userId = _currentUserService.GetUserId();
        var filters = await _searchService.GetSavedFiltersAsync(userId);
        return Ok(filters);
    }

    [HttpGet("saved-filters/{id}")]
    [ProducesResponseType(typeof(SavedFilterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SavedFilterDto>> GetSavedFilter(Guid id)
    {
        var filter = await _searchService.GetSavedFilterAsync(id);
        return Ok(filter);
    }

    [HttpPatch("saved-filters/{id}")]
    [ProducesResponseType(typeof(SavedFilter), StatusCodes.Status200OK)]
    public async Task<ActionResult<SavedFilter>> UpdateSavedFilter(
        Guid id,
        [FromBody] UpdateSavedFilterDto dto)
    {
        var filter = await _searchService.UpdateSavedFilterAsync(id, dto);
        return Ok(filter);
    }

    [HttpDelete("saved-filters/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSavedFilter(Guid id)
    {
        await _searchService.DeleteSavedFilterAsync(id);
        return NoContent();
    }

    [HttpPost("saved-filters/{id}/apply")]
    [ProducesResponseType(typeof(TransactionSearchResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionSearchResultDto>> ApplySavedFilter(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var user = await _currentUserService.GetCurrentUserAsync();
        var result = await _searchService.ApplySavedFilterAsync(
            user.FamilyId!.Value, id, page, pageSize);
        return Ok(result);
    }
}
```

---

## Phase 5: Blazor Components

### 5.1 TransactionSearchPage.razor

```razor
@page "/transactions/search"
@inject ITransactionSearchService SearchService
@inject ICurrentUserService CurrentUserService

<PageTitle>Search Transactions</PageTitle>

<div class="transaction-search-page">
    <h2>Transaction Search</h2>

    <!-- Filter Panel -->
    <div class="filter-panel card p-3 mb-3">
        <h5>Filters</h5>

        <div class="row">
            <!-- Search Text -->
            <div class="col-md-6 mb-3">
                <label>Search</label>
                <RadzenAutoComplete @bind-Value="@SearchText"
                                    Data="@Suggestions"
                                    Placeholder="Search descriptions..."
                                    Change="@OnSearchTextChanged" />
            </div>

            <!-- Date Range -->
            <div class="col-md-3 mb-3">
                <label>Start Date</label>
                <RadzenDatePicker @bind-Value="@StartDate" />
            </div>
            <div class="col-md-3 mb-3">
                <label>End Date</label>
                <RadzenDatePicker @bind-Value="@EndDate" />
            </div>

            <!-- Type Filter -->
            <div class="col-md-4 mb-3">
                <label>Type</label>
                <RadzenDropDown @bind-Value="@SelectedType"
                                Data="@TransactionTypes"
                                AllowClear="true"
                                Placeholder="All Types" />
            </div>

            <!-- Amount Range -->
            <div class="col-md-4 mb-3">
                <label>Min Amount</label>
                <RadzenNumeric @bind-Value="@MinAmount" />
            </div>
            <div class="col-md-4 mb-3">
                <label>Max Amount</label>
                <RadzenNumeric @bind-Value="@MaxAmount" />
            </div>
        </div>

        <!-- Filter Actions -->
        <div class="d-flex gap-2">
            <button class="btn btn-primary" @onclick="ApplyFilters">
                Apply Filters
            </button>
            <button class="btn btn-secondary" @onclick="ClearFilters">
                Clear
            </button>
            <button class="btn btn-outline-primary" @onclick="SaveCurrentFilter">
                Save Filter
            </button>
        </div>
    </div>

    <!-- Saved Filters -->
    @if (SavedFilters.Any())
    {
        <div class="saved-filters mb-3">
            <h6>Saved Filters</h6>
            <div class="d-flex gap-2 flex-wrap">
                @foreach (var filter in SavedFilters)
                {
                    <button class="btn btn-sm btn-outline-secondary"
                            @onclick="() => ApplySavedFilter(filter.Id)">
                        @filter.Name
                    </button>
                }
            </div>
        </div>
    }

    <!-- Results -->
    @if (Loading)
    {
        <div class="spinner-border"></div>
    }
    else if (SearchResults != null)
    {
        <div class="results-header d-flex justify-content-between">
            <div>
                <strong>@SearchResults.TotalCount</strong> transactions found
            </div>
            <button class="btn btn-sm btn-outline-primary" @onclick="ExportResults">
                Export to CSV
            </button>
        </div>

        <TransactionTable Transactions="@SearchResults.Transactions" />

        <!-- Pagination -->
        <RadzenPager Count="@SearchResults.TotalCount"
                     PageSize="@SearchResults.PageSize"
                     PageNumbersCount="5"
                     PageChanged="@OnPageChanged" />
    }
</div>

@code {
    // Component implementation...
}
```

---

## Success Metrics

- ✅ All 22 tests passing
- ✅ Full-text search returns results in <200ms
- ✅ Filters apply correctly and efficiently
- ✅ CSV export generates valid files
- ✅ Saved filters persist and load correctly
- ✅ Autocomplete provides relevant suggestions
- ✅ Pagination works smoothly
- ✅ Database indexes improve query performance

---

**Total Implementation Time**: 2-3 weeks following TDD methodology
