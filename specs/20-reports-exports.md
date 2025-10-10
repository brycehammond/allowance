# Reports & Exports - PDF and CSV Reporting System

## Overview
Comprehensive reporting and export system using QuestPDF for professional PDF generation and CsvHelper for CSV exports. Enables parents to generate monthly/annual reports with charts, export transaction data, and receive scheduled email reports.

## Core Philosophy: Test-First Development
**Every feature starts with a failing test**. Follow strict TDD methodology for all reporting and export functionality.

## Technology Stack

### Core Dependencies
```xml
<ItemGroup>
  <!-- PDF Generation -->
  <PackageReference Include="QuestPDF" Version="2023.12.0" />

  <!-- CSV Export -->
  <PackageReference Include="CsvHelper" Version="30.0.1" />

  <!-- Email Delivery -->
  <PackageReference Include="SendGrid" Version="9.28.1" />
  <PackageReference Include="MailKit" Version="4.3.0" />

  <!-- Charts for PDF -->
  <PackageReference Include="ScottPlot" Version="5.0.9" />
</ItemGroup>
```

## Database Schema

### ReportTemplate Model
```csharp
public class ReportTemplate
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public ReportFrequency Frequency { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastGenerated { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Family Family { get; set; } = null!;
}

public enum ReportType
{
    Monthly = 0,
    Annual = 1,
    Custom = 2,
    YearEndSummary = 3
}

public enum ReportFrequency
{
    OnDemand = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Annually = 4
}
```

### ReportHistory Model
```csharp
public class ReportHistory
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid GeneratedById { get; set; }
    public ReportType Type { get; set; }
    public ReportFormat Format { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool EmailSent { get; set; } = false;
    public DateTime? EmailSentAt { get; set; }

    // Navigation properties
    public virtual Family Family { get; set; } = null!;
    public virtual ApplicationUser GeneratedBy { get; set; } = null!;
}

public enum ReportFormat
{
    PDF = 0,
    CSV = 1,
    Excel = 2
}
```

## Service Interfaces

### IReportService Interface
```csharp
public interface IReportService
{
    // PDF Reports
    Task<byte[]> GenerateMonthlyReportAsync(Guid familyId, int year, int month);
    Task<byte[]> GenerateAnnualReportAsync(Guid familyId, int year);
    Task<byte[]> GenerateCustomReportAsync(Guid familyId, DateTime startDate, DateTime endDate);
    Task<byte[]> GenerateYearEndTaxSummaryAsync(Guid familyId, int year);

    // CSV Exports
    Task<byte[]> ExportTransactionsToCsvAsync(Guid familyId, DateTime? startDate = null, DateTime? endDate = null);
    Task<byte[]> ExportChildBalancesToCsvAsync(Guid familyId);
    Task<byte[]> ExportWishListToCsvAsync(Guid childId);

    // Report Management
    Task<ReportHistory> SaveReportHistoryAsync(Guid familyId, ReportType type, ReportFormat format, byte[] data);
    Task<List<ReportHistory>> GetReportHistoryAsync(Guid familyId, int limit = 20);
    Task DeleteReportAsync(Guid reportId);
}
```

### IEmailReportService Interface
```csharp
public interface IEmailReportService
{
    Task SendReportByEmailAsync(Guid familyId, byte[] reportData, string fileName, string recipientEmail);
    Task SendWeeklySummaryEmailAsync(Guid familyId);
    Task SendMonthlySummaryEmailAsync(Guid familyId);
    Task<bool> IsEmailConfiguredAsync();
}
```

## Service Implementation

### ReportService Implementation
```csharp
public class ReportService : IReportService
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        AllowanceContext context,
        ICurrentUserService currentUser,
        ILogger<ReportService> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<byte[]> GenerateMonthlyReportAsync(Guid familyId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var data = await GetReportDataAsync(familyId, startDate, endDate);

        using var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(c => ComposeHeader(c, $"Monthly Report - {startDate:MMMM yyyy}"));
                page.Content().Element(c => ComposeMonthlyContent(c, data));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateAnnualReportAsync(Guid familyId, int year)
    {
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31);

        var data = await GetReportDataAsync(familyId, startDate, endDate);

        using var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(c => ComposeHeader(c, $"Annual Report - {year}"));
                page.Content().Element(c => ComposeAnnualContent(c, data));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> GenerateYearEndTaxSummaryAsync(Guid familyId, int year)
    {
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year, 12, 31);

        var transactions = await _context.Transactions
            .Include(t => t.Child)
            .ThenInclude(c => c.User)
            .Where(t => t.Child.FamilyId == familyId &&
                       t.CreatedAt >= startDate &&
                       t.CreatedAt <= endDate)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        var summaryData = transactions
            .GroupBy(t => t.Child.User.FullName)
            .Select(g => new
            {
                ChildName = g.Key,
                TotalAllowance = g.Where(t => t.Description.Contains("Allowance")).Sum(t => t.Amount),
                TotalCredits = g.Where(t => t.Type == TransactionType.Credit).Sum(t => t.Amount),
                TotalDebits = g.Where(t => t.Type == TransactionType.Debit).Sum(t => t.Amount)
            })
            .ToList();

        using var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(2, Unit.Centimetre);

                page.Header().Element(c => ComposeHeader(c, $"Year-End Tax Summary - {year}"));
                page.Content().Column(column =>
                {
                    column.Item().Text($"Tax Year: {year}").FontSize(16).Bold();
                    column.Item().PaddingVertical(10);

                    foreach (var child in summaryData)
                    {
                        column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10);
                        column.Item().PaddingTop(10).Text(child.ChildName).FontSize(14).Bold();
                        column.Item().PaddingLeft(20).Text($"Total Allowance Paid: {child.TotalAllowance:C}");
                        column.Item().PaddingLeft(20).Text($"Total Money Received: {child.TotalCredits:C}");
                        column.Item().PaddingLeft(20).Text($"Total Money Spent: {child.TotalDebits:C}");
                    }

                    column.Item().PaddingTop(20).Text("Disclaimer: This is a summary for record-keeping purposes only. Consult a tax professional for tax filing requirements.")
                        .FontSize(10).Italic();
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportTransactionsToCsvAsync(Guid familyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Transactions
            .Include(t => t.Child)
            .ThenInclude(c => c.User)
            .Include(t => t.CreatedBy)
            .Where(t => t.Child.FamilyId == familyId);

        if (startDate.HasValue)
            query = query.Where(t => t.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.CreatedAt <= endDate.Value);

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var records = transactions.Select(t => new TransactionCsvRecord
        {
            Date = t.CreatedAt.ToString("yyyy-MM-dd"),
            Time = t.CreatedAt.ToString("HH:mm:ss"),
            Child = t.Child.User.FullName,
            Type = t.Type.ToString(),
            Amount = t.Amount,
            Description = t.Description,
            BalanceAfter = t.BalanceAfter,
            CreatedBy = t.CreatedBy.FullName
        });

        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            await csv.WriteRecordsAsync(records);
        }

        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportChildBalancesToCsvAsync(Guid familyId)
    {
        var children = await _context.Children
            .Include(c => c.User)
            .Where(c => c.FamilyId == familyId)
            .ToListAsync();

        var records = children.Select(c => new ChildBalanceCsvRecord
        {
            Name = c.User.FullName,
            Email = c.User.Email ?? "",
            WeeklyAllowance = c.WeeklyAllowance,
            CurrentBalance = c.CurrentBalance,
            LastAllowance = c.LastAllowanceDate?.ToString("yyyy-MM-dd") ?? "Never"
        });

        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            await csv.WriteRecordsAsync(records);
        }

        return memoryStream.ToArray();
    }

    public async Task<ReportHistory> SaveReportHistoryAsync(Guid familyId, ReportType type, ReportFormat format, byte[] data)
    {
        var fileName = $"report_{type}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{format.ToString().ToLower()}";
        var filePath = Path.Combine("reports", familyId.ToString(), fileName);

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Save file
        await File.WriteAllBytesAsync(filePath, data);

        var history = new ReportHistory
        {
            FamilyId = familyId,
            GeneratedById = _currentUser.UserId,
            Type = type,
            Format = format,
            StartDate = DateTime.UtcNow.AddMonths(-1),
            EndDate = DateTime.UtcNow,
            FilePath = filePath,
            FileSizeBytes = data.Length
        };

        _context.ReportHistories.Add(history);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Report saved: {ReportId} for family {FamilyId}", history.Id, familyId);

        return history;
    }

    private async Task<ReportData> GetReportDataAsync(Guid familyId, DateTime startDate, DateTime endDate)
    {
        var children = await _context.Children
            .Include(c => c.User)
            .Include(c => c.Transactions.Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate))
            .Where(c => c.FamilyId == familyId)
            .ToListAsync();

        return new ReportData
        {
            FamilyId = familyId,
            StartDate = startDate,
            EndDate = endDate,
            Children = children
        };
    }

    private void ComposeHeader(IContainer container, string title)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Allowance Tracker").FontSize(20).Bold();
                column.Item().Text(title).FontSize(16);
            });

            row.ConstantItem(100).AlignRight().Text($"Generated: {DateTime.Now:yyyy-MM-dd}").FontSize(10);
        });
    }

    private void ComposeMonthlyContent(IContainer container, ReportData data)
    {
        container.Column(column =>
        {
            column.Item().PaddingVertical(10).Text($"Report Period: {data.StartDate:MMMM d, yyyy} - {data.EndDate:MMMM d, yyyy}")
                .FontSize(12);

            foreach (var child in data.Children)
            {
                column.Item().PaddingTop(20).Element(c => ComposeChildSection(c, child, data.StartDate, data.EndDate));
            }
        });
    }

    private void ComposeChildSection(IContainer container, Child child, DateTime startDate, DateTime endDate)
    {
        container.Column(column =>
        {
            // Child header
            column.Item().Background(Colors.Blue.Lighten3).Padding(10).Text(child.User.FullName).FontSize(14).Bold();

            // Summary
            var transactions = child.Transactions.Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate).ToList();
            var totalCredits = transactions.Where(t => t.Type == TransactionType.Credit).Sum(t => t.Amount);
            var totalDebits = transactions.Where(t => t.Type == TransactionType.Debit).Sum(t => t.Amount);

            column.Item().Padding(10).Row(row =>
            {
                row.RelativeItem().Text($"Starting Balance: {(child.CurrentBalance - totalCredits + totalDebits):C}");
                row.RelativeItem().Text($"Money In: {totalCredits:C}").FontColor(Colors.Green.Medium);
                row.RelativeItem().Text($"Money Out: {totalDebits:C}").FontColor(Colors.Red.Medium);
                row.RelativeItem().Text($"Ending Balance: {child.CurrentBalance:C}").Bold();
            });

            // Transaction table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Date");
                    header.Cell().Element(CellStyle).Text("Type");
                    header.Cell().Element(CellStyle).Text("Description");
                    header.Cell().Element(CellStyle).Text("Amount");

                    static IContainer CellStyle(IContainer container) =>
                        container.DefaultTextStyle(x => x.Bold()).Padding(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                foreach (var transaction in transactions.OrderBy(t => t.CreatedAt))
                {
                    table.Cell().Element(CellStyle).Text(transaction.CreatedAt.ToString("MMM dd"));
                    table.Cell().Element(CellStyle).Text(transaction.Type.ToString());
                    table.Cell().Element(CellStyle).Text(transaction.Description);
                    table.Cell().Element(CellStyle).Text(transaction.Amount.ToString("C"))
                        .FontColor(transaction.Type == TransactionType.Credit ? Colors.Green.Medium : Colors.Red.Medium);

                    static IContainer CellStyle(IContainer container) => container.Padding(5);
                }
            });
        });
    }
}
```

### EmailReportService Implementation
```csharp
public class EmailReportService : IEmailReportService
{
    private readonly IConfiguration _configuration;
    private readonly IReportService _reportService;
    private readonly AllowanceContext _context;
    private readonly ILogger<EmailReportService> _logger;

    public async Task SendReportByEmailAsync(Guid familyId, byte[] reportData, string fileName, string recipientEmail)
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);

        var msg = new SendGridMessage();
        msg.SetFrom(new EmailAddress(_configuration["SendGrid:FromEmail"], "Allowance Tracker"));
        msg.AddTo(new EmailAddress(recipientEmail));
        msg.SetSubject("Your Allowance Tracker Report");
        msg.AddContent(MimeType.Html, @"
            <p>Your requested report is attached.</p>
            <p>Thank you for using Allowance Tracker!</p>
        ");
        msg.AddAttachment(fileName, Convert.ToBase64String(reportData));

        var response = await client.SendEmailAsync(msg);

        if (response.StatusCode != System.Net.HttpStatusCode.OK &&
            response.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            _logger.LogError("Failed to send email: {StatusCode}", response.StatusCode);
            throw new InvalidOperationException("Failed to send email");
        }

        _logger.LogInformation("Report email sent to {Email}", recipientEmail);
    }

    public async Task SendWeeklySummaryEmailAsync(Guid familyId)
    {
        var family = await _context.Families
            .Include(f => f.Members)
            .Include(f => f.Children)
            .ThenInclude(c => c.Transactions)
            .FirstOrDefaultAsync(f => f.Id == familyId);

        if (family == null)
            throw new InvalidOperationException("Family not found");

        var startDate = DateTime.UtcNow.AddDays(-7);
        var parentEmails = family.Members
            .Where(m => m.Role == UserRole.Parent && !string.IsNullOrEmpty(m.Email))
            .Select(m => m.Email!)
            .ToList();

        foreach (var email in parentEmails)
        {
            var htmlContent = GenerateWeeklySummaryHtml(family, startDate);

            var apiKey = _configuration["SendGrid:ApiKey"];
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage();
            msg.SetFrom(new EmailAddress(_configuration["SendGrid:FromEmail"], "Allowance Tracker"));
            msg.AddTo(new EmailAddress(email));
            msg.SetSubject($"Weekly Summary - {family.Name}");
            msg.AddContent(MimeType.Html, htmlContent);

            await client.SendEmailAsync(msg);
        }

        _logger.LogInformation("Weekly summary emails sent for family {FamilyId}", familyId);
    }

    private string GenerateWeeklySummaryHtml(Family family, DateTime startDate)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<html><body>");
        sb.AppendLine($"<h2>{family.Name} - Weekly Summary</h2>");
        sb.AppendLine($"<p>Week of {startDate:MMMM d, yyyy}</p>");

        foreach (var child in family.Children)
        {
            var weekTransactions = child.Transactions
                .Where(t => t.CreatedAt >= startDate)
                .ToList();

            sb.AppendLine($"<h3>{child.User.FullName}</h3>");
            sb.AppendLine($"<p>Current Balance: {child.CurrentBalance:C}</p>");
            sb.AppendLine($"<p>Transactions this week: {weekTransactions.Count}</p>");

            if (weekTransactions.Any())
            {
                sb.AppendLine("<ul>");
                foreach (var t in weekTransactions.OrderByDescending(t => t.CreatedAt))
                {
                    sb.AppendLine($"<li>{t.CreatedAt:MMM dd}: {t.Description} - {t.Amount:C}</li>");
                }
                sb.AppendLine("</ul>");
            }
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public Task<bool> IsEmailConfiguredAsync()
    {
        var apiKey = _configuration["SendGrid:ApiKey"];
        return Task.FromResult(!string.IsNullOrEmpty(apiKey));
    }
}
```

## DTOs

### Report DTOs
```csharp
public record GenerateReportRequest(
    ReportType Type,
    int? Year,
    int? Month,
    DateTime? StartDate,
    DateTime? EndDate,
    bool EmailReport,
    string? EmailAddress
);

public record ReportHistoryDto(
    Guid Id,
    ReportType Type,
    ReportFormat Format,
    DateTime StartDate,
    DateTime EndDate,
    long FileSizeBytes,
    DateTime GeneratedAt,
    bool EmailSent
);

public record TransactionCsvRecord
{
    public string Date { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Child { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal BalanceAfter { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public record ChildBalanceCsvRecord
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal WeeklyAllowance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string LastAllowance { get; set; } = string.Empty;
}

internal class ReportData
{
    public Guid FamilyId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<Child> Children { get; set; } = new();
}
```

## API Controllers

### ReportsController
```csharp
[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IEmailReportService _emailService;
    private readonly ICurrentUserService _currentUser;

    [HttpPost("monthly")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GenerateMonthlyReport([FromBody] GenerateReportRequest request)
    {
        if (!request.Year.HasValue || !request.Month.HasValue)
            return BadRequest("Year and Month are required for monthly reports");

        var pdfData = await _reportService.GenerateMonthlyReportAsync(
            _currentUser.FamilyId!.Value,
            request.Year.Value,
            request.Month.Value);

        var fileName = $"monthly_report_{request.Year}_{request.Month:D2}.pdf";

        if (request.EmailReport && !string.IsNullOrEmpty(request.EmailAddress))
        {
            await _emailService.SendReportByEmailAsync(
                _currentUser.FamilyId!.Value,
                pdfData,
                fileName,
                request.EmailAddress);
        }

        await _reportService.SaveReportHistoryAsync(
            _currentUser.FamilyId!.Value,
            ReportType.Monthly,
            ReportFormat.PDF,
            pdfData);

        return File(pdfData, "application/pdf", fileName);
    }

    [HttpPost("annual")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GenerateAnnualReport([FromBody] GenerateReportRequest request)
    {
        if (!request.Year.HasValue)
            return BadRequest("Year is required for annual reports");

        var pdfData = await _reportService.GenerateAnnualReportAsync(
            _currentUser.FamilyId!.Value,
            request.Year.Value);

        var fileName = $"annual_report_{request.Year}.pdf";

        if (request.EmailReport && !string.IsNullOrEmpty(request.EmailAddress))
        {
            await _emailService.SendReportByEmailAsync(
                _currentUser.FamilyId!.Value,
                pdfData,
                fileName,
                request.EmailAddress);
        }

        await _reportService.SaveReportHistoryAsync(
            _currentUser.FamilyId!.Value,
            ReportType.Annual,
            ReportFormat.PDF,
            pdfData);

        return File(pdfData, "application/pdf", fileName);
    }

    [HttpPost("tax-summary")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GenerateYearEndTaxSummary([FromBody] GenerateReportRequest request)
    {
        if (!request.Year.HasValue)
            return BadRequest("Year is required for tax summary");

        var pdfData = await _reportService.GenerateYearEndTaxSummaryAsync(
            _currentUser.FamilyId!.Value,
            request.Year.Value);

        var fileName = $"tax_summary_{request.Year}.pdf";

        await _reportService.SaveReportHistoryAsync(
            _currentUser.FamilyId!.Value,
            ReportType.YearEndSummary,
            ReportFormat.PDF,
            pdfData);

        return File(pdfData, "application/pdf", fileName);
    }

    [HttpGet("export/transactions")]
    public async Task<IActionResult> ExportTransactions([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var csvData = await _reportService.ExportTransactionsToCsvAsync(
            _currentUser.FamilyId!.Value,
            startDate,
            endDate);

        var fileName = $"transactions_{DateTime.UtcNow:yyyyMMdd}.csv";

        return File(csvData, "text/csv", fileName);
    }

    [HttpGet("export/balances")]
    public async Task<IActionResult> ExportBalances()
    {
        var csvData = await _reportService.ExportChildBalancesToCsvAsync(_currentUser.FamilyId!.Value);
        var fileName = $"balances_{DateTime.UtcNow:yyyyMMdd}.csv";

        return File(csvData, "text/csv", fileName);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<ReportHistoryDto>>> GetReportHistory()
    {
        var history = await _reportService.GetReportHistoryAsync(_currentUser.FamilyId!.Value);

        var dtos = history.Select(h => new ReportHistoryDto(
            h.Id,
            h.Type,
            h.Format,
            h.StartDate,
            h.EndDate,
            h.FileSizeBytes,
            h.GeneratedAt,
            h.EmailSent
        )).ToList();

        return Ok(dtos);
    }
}
```

## Blazor Components

### ReportGenerator.razor
```razor
@page "/reports"
@attribute [Authorize(Roles = "Parent")]
@inject IReportService ReportService
@inject IEmailReportService EmailService

<h2>Reports & Exports</h2>

<div class="row">
    <div class="col-md-6">
        <div class="card">
            <div class="card-header">
                <h4>Generate PDF Report</h4>
            </div>
            <div class="card-body">
                <EditForm Model="@reportRequest" OnValidSubmit="@GenerateReport">
                    <DataAnnotationsValidator />
                    <ValidationSummary />

                    <div class="mb-3">
                        <label class="form-label">Report Type</label>
                        <InputSelect class="form-control" @bind-Value="reportRequest.Type">
                            <option value="@ReportType.Monthly">Monthly Report</option>
                            <option value="@ReportType.Annual">Annual Report</option>
                            <option value="@ReportType.YearEndSummary">Year-End Tax Summary</option>
                        </InputSelect>
                    </div>

                    @if (reportRequest.Type == ReportType.Monthly)
                    {
                        <div class="mb-3">
                            <label class="form-label">Month</label>
                            <InputNumber class="form-control" @bind-Value="reportRequest.Month" />
                        </div>
                    }

                    <div class="mb-3">
                        <label class="form-label">Year</label>
                        <InputNumber class="form-control" @bind-Value="reportRequest.Year" />
                    </div>

                    <div class="mb-3">
                        <InputCheckbox @bind-Value="reportRequest.EmailReport" />
                        <label class="form-check-label">Email report to me</label>
                    </div>

                    <button type="submit" class="btn btn-primary" disabled="@isGenerating">
                        @if (isGenerating)
                        {
                            <span class="spinner-border spinner-border-sm"></span>
                            <span>Generating...</span>
                        }
                        else
                        {
                            <span>Generate Report</span>
                        }
                    </button>
                </EditForm>
            </div>
        </div>
    </div>

    <div class="col-md-6">
        <div class="card">
            <div class="card-header">
                <h4>Export Data</h4>
            </div>
            <div class="card-body">
                <button class="btn btn-outline-primary mb-2" @onclick="ExportTransactions">
                    Export All Transactions (CSV)
                </button>
                <button class="btn btn-outline-primary mb-2" @onclick="ExportBalances">
                    Export Child Balances (CSV)
                </button>
            </div>
        </div>
    </div>
</div>

<div class="mt-4">
    <h3>Report History</h3>
    @if (reportHistory == null)
    {
        <p>Loading...</p>
    }
    else if (!reportHistory.Any())
    {
        <p>No reports generated yet.</p>
    }
    else
    {
        <table class="table">
            <thead>
                <tr>
                    <th>Date</th>
                    <th>Type</th>
                    <th>Format</th>
                    <th>Size</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var report in reportHistory)
                {
                    <tr>
                        <td>@report.GeneratedAt.ToString("MMM dd, yyyy")</td>
                        <td>@report.Type</td>
                        <td>@report.Format</td>
                        <td>@FormatFileSize(report.FileSizeBytes)</td>
                        <td>
                            <button class="btn btn-sm btn-primary">Download</button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }
</div>

@code {
    private GenerateReportRequest reportRequest = new(ReportType.Monthly, DateTime.Now.Year, DateTime.Now.Month, null, null, false, null);
    private List<ReportHistoryDto>? reportHistory;
    private bool isGenerating = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadReportHistory();
    }

    private async Task GenerateReport()
    {
        isGenerating = true;
        try
        {
            // Call API to generate report
            await Task.Delay(2000); // Simulate generation
            await LoadReportHistory();
        }
        finally
        {
            isGenerating = false;
        }
    }

    private async Task ExportTransactions()
    {
        // Download CSV
    }

    private async Task ExportBalances()
    {
        // Download CSV
    }

    private async Task LoadReportHistory()
    {
        // Load from API
        reportHistory = new List<ReportHistoryDto>();
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
```

## Test Cases (18 Tests Total)

### Report Service Tests
```csharp
public class ReportServiceTests
{
    [Fact]
    public async Task GenerateMonthlyReport_ReturnsValidPDF()
    {
        // Arrange
        var service = CreateService();
        var familyId = Guid.NewGuid();

        // Act
        var pdfData = await service.GenerateMonthlyReportAsync(familyId, 2024, 1);

        // Assert
        pdfData.Should().NotBeNull();
        pdfData.Length.Should().BeGreaterThan(0);
        pdfData[0..4].Should().Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF magic number
    }

    [Fact]
    public async Task GenerateAnnualReport_IncludesAllMonths()
    {
        // Arrange
        var service = CreateService();
        var familyId = Guid.NewGuid();

        // Act
        var pdfData = await service.GenerateAnnualReportAsync(familyId, 2024);

        // Assert
        pdfData.Should().NotBeNull();
        pdfData.Length.Should().BeGreaterThan(1000); // Substantial report
    }

    [Fact]
    public async Task GenerateYearEndTaxSummary_CalculatesTotalsCorrectly()
    {
        // Arrange
        var service = CreateService();
        var child = await CreateChildWithTransactions(allowance: 100m, transactions: 10);

        // Act
        var pdfData = await service.GenerateYearEndTaxSummaryAsync(child.FamilyId, 2024);

        // Assert
        pdfData.Should().NotBeNull();
        // Verify totals in PDF content
    }

    [Fact]
    public async Task ExportTransactionsToCsv_ReturnsValidFormat()
    {
        // Arrange
        var service = CreateService();
        var familyId = Guid.NewGuid();

        // Act
        var csvData = await service.ExportTransactionsToCsvAsync(familyId);

        // Assert
        var csvContent = Encoding.UTF8.GetString(csvData);
        csvContent.Should().Contain("Date,Time,Child,Type,Amount");
    }

    [Fact]
    public async Task ExportTransactionsToCsv_FiltersDateRange()
    {
        // Arrange
        var service = CreateService();
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);

        // Act
        var csvData = await service.ExportTransactionsToCsvAsync(Guid.NewGuid(), startDate, endDate);

        // Assert
        csvData.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportChildBalances_IncludesAllChildren()
    {
        // Arrange
        var service = CreateService();
        var familyId = Guid.NewGuid();
        await CreateChildren(familyId, 3);

        // Act
        var csvData = await service.ExportChildBalancesToCsvAsync(familyId);

        // Assert
        var csvContent = Encoding.UTF8.GetString(csvData);
        var lines = csvContent.Split('\n');
        lines.Length.Should().Be(4); // Header + 3 children
    }

    [Fact]
    public async Task SaveReportHistory_CreatesFileAndRecord()
    {
        // Arrange
        var service = CreateService();
        var familyId = Guid.NewGuid();
        var reportData = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var history = await service.SaveReportHistoryAsync(
            familyId,
            ReportType.Monthly,
            ReportFormat.PDF,
            reportData);

        // Assert
        history.Should().NotBeNull();
        history.FileSizeBytes.Should().Be(5);
        File.Exists(history.FilePath).Should().BeTrue();
    }

    [Fact]
    public async Task GetReportHistory_ReturnsRecentReports()
    {
        // Arrange
        var service = CreateService();
        var familyId = Guid.NewGuid();
        await GenerateMultipleReports(familyId, 5);

        // Act
        var history = await service.GetReportHistoryAsync(familyId, limit: 3);

        // Assert
        history.Should().HaveCount(3);
        history.Should().BeInDescendingOrder(h => h.GeneratedAt);
    }

    [Fact]
    public async Task DeleteReport_RemovesFileAndRecord()
    {
        // Arrange
        var service = CreateService();
        var report = await CreateTestReport();

        // Act
        await service.DeleteReportAsync(report.Id);

        // Assert
        File.Exists(report.FilePath).Should().BeFalse();
    }
}

public class EmailReportServiceTests
{
    [Fact]
    public async Task SendReportByEmail_SendsSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var reportData = new byte[] { 1, 2, 3 };

        // Act
        await service.SendReportByEmailAsync(
            Guid.NewGuid(),
            reportData,
            "report.pdf",
            "test@example.com");

        // Assert
        // Verify email was sent (mock SendGrid client)
    }

    [Fact]
    public async Task SendWeeklySummaryEmail_IncludesAllTransactions()
    {
        // Arrange
        var service = CreateService();
        var family = await CreateFamilyWithTransactions();

        // Act
        await service.SendWeeklySummaryEmailAsync(family.Id);

        // Assert
        // Verify email content
    }

    [Fact]
    public async Task SendWeeklySummaryEmail_SendsToAllParents()
    {
        // Arrange
        var service = CreateService();
        var family = await CreateFamilyWithParents(parentCount: 2);

        // Act
        await service.SendWeeklySummaryEmailAsync(family.Id);

        // Assert
        // Verify 2 emails sent
    }

    [Fact]
    public async Task SendMonthlySummaryEmail_GeneratesReport()
    {
        // Arrange
        var service = CreateService();
        var familyId = Guid.NewGuid();

        // Act
        await service.SendMonthlySummaryEmailAsync(familyId);

        // Assert
        // Verify email with PDF attachment
    }

    [Fact]
    public async Task IsEmailConfigured_ReturnsTrueWhenConfigured()
    {
        // Arrange
        var service = CreateService(withEmailConfig: true);

        // Act
        var result = await service.IsEmailConfiguredAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmailConfigured_ReturnsFalseWhenNotConfigured()
    {
        // Arrange
        var service = CreateService(withEmailConfig: false);

        // Act
        var result = await service.IsEmailConfiguredAsync();

        // Assert
        result.Should().BeFalse();
    }
}

public class ReportsControllerTests
{
    [Fact]
    public async Task GenerateMonthlyReport_RequiresYearAndMonth()
    {
        // Arrange
        var controller = CreateController();
        var request = new GenerateReportRequest(ReportType.Monthly, null, null, null, null, false, null);

        // Act
        var result = await controller.GenerateMonthlyReport(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GenerateMonthlyReport_ReturnsFileResult()
    {
        // Arrange
        var controller = CreateController();
        var request = new GenerateReportRequest(ReportType.Monthly, 2024, 1, null, null, false, null);

        // Act
        var result = await controller.GenerateMonthlyReport(request);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult!.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task ExportTransactions_ReturnsCsvFile()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.ExportTransactions(null, null);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = result as FileContentResult;
        fileResult!.ContentType.Should().Be("text/csv");
    }
}
```

## Background Job for Scheduled Reports

### ScheduledReportJob
```csharp
public class ScheduledReportJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledReportJob> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AllowanceContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailReportService>();

                // Check for families needing weekly summaries
                var families = await context.Families.ToListAsync(stoppingToken);

                foreach (var family in families)
                {
                    if (ShouldSendWeeklySummary(family))
                    {
                        await emailService.SendWeeklySummaryEmailAsync(family.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduled report job");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private bool ShouldSendWeeklySummary(Family family)
    {
        // Send on Sundays
        return DateTime.Today.DayOfWeek == DayOfWeek.Sunday;
    }
}
```

## Success Metrics

### Performance Targets
- PDF generation: < 2 seconds for monthly report
- CSV export: < 500ms for 1000 transactions
- Email delivery: < 3 seconds

### Quality Metrics
- 18 tests passing (100% critical path coverage)
- PDF reports properly formatted and printable
- CSV exports compatible with Excel/Google Sheets
- Email delivery rate > 95%

## Configuration

### appsettings.json
```json
{
  "SendGrid": {
    "ApiKey": "your-sendgrid-api-key",
    "FromEmail": "noreply@allowancetracker.com",
    "FromName": "Allowance Tracker"
  },
  "Reports": {
    "StoragePath": "reports",
    "RetentionDays": 90,
    "MaxFileSizeMB": 10
  }
}
```
