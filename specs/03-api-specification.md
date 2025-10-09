# API Specification - ASP.NET Core Web API (MVP)

## Overview
Simple REST API using ASP.NET Core Web API with JWT authentication for potential future mobile app support. For MVP, Blazor Server will handle most interactions directly, but having an API ready makes mobile expansion easier.

## API Configuration

### Program.cs Setup
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Allowance API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter JWT token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
});

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

## DTOs (Data Transfer Objects)

### Request DTOs
```csharp
public record LoginDto(
    string Email,
    string Password);

public record RegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role);

public record CreateTransactionDto(
    Guid ChildId,
    decimal Amount,
    TransactionType Type,
    string Description);

public record CreateWishListItemDto(
    string Name,
    decimal Price,
    string? Url,
    string? Notes);

public record UpdateChildAllowanceDto(
    decimal WeeklyAllowance);
```

### Response DTOs
```csharp
public record AuthResponseDto(
    string Token,
    DateTime ExpiresAt,
    UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid? FamilyId);

public record ChildDto(
    Guid Id,
    string Name,
    decimal WeeklyAllowance,
    decimal CurrentBalance,
    DateTime? LastAllowanceDate);

public record TransactionDto(
    Guid Id,
    decimal Amount,
    TransactionType Type,
    string Description,
    decimal BalanceAfter,
    DateTime CreatedAt,
    string CreatedBy);

public record WishListItemDto(
    Guid Id,
    string Name,
    decimal Price,
    string? Url,
    string? Notes,
    bool IsPurchased,
    bool CanAfford);
```

## API Controllers

### AuthController
```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = dto.Role
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        // Create family if parent
        if (dto.Role == UserRole.Parent)
        {
            var family = new Family { Name = $"{dto.LastName} Family" };
            _context.Families.Add(family);
            user.FamilyId = family.Id;
            await _context.SaveChangesAsync();
        }

        var token = GenerateJwtToken(user);
        return Ok(new AuthResponseDto(token, DateTime.UtcNow.AddDays(1), MapToDto(user)));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized("Invalid credentials");

        var token = GenerateJwtToken(user);
        return Ok(new AuthResponseDto(token, DateTime.UtcNow.AddDays(1), MapToDto(user)));
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim("familyId", user.FamilyId?.ToString() ?? "")
        };

        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddDays(1),
            claims: claims,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### FamilyController
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FamilyController : ControllerBase
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;

    [HttpGet]
    public async Task<ActionResult<FamilyDto>> GetFamily()
    {
        var family = await _context.Families
            .Include(f => f.Children)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(f => f.Id == _currentUser.FamilyId);

        if (family == null)
            return NotFound();

        return Ok(MapToDto(family));
    }

    [HttpGet("children")]
    public async Task<ActionResult<List<ChildDto>>> GetChildren()
    {
        var children = await _context.Children
            .Include(c => c.User)
            .Where(c => c.FamilyId == _currentUser.FamilyId)
            .Select(c => new ChildDto(
                c.Id,
                c.User.FullName,
                c.WeeklyAllowance,
                c.CurrentBalance,
                c.LastAllowanceDate))
            .ToListAsync();

        return Ok(children);
    }

    [HttpPost("children")]
    [Authorize(Policy = "ParentOnly")]
    public async Task<ActionResult<ChildDto>> AddChild(AddChildDto dto)
    {
        // Create user account for child
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = UserRole.Child,
            FamilyId = _currentUser.FamilyId
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        // Create child profile
        var child = new Child
        {
            UserId = user.Id,
            FamilyId = _currentUser.FamilyId.Value,
            WeeklyAllowance = dto.WeeklyAllowance
        };

        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetChildren),
            new ChildDto(child.Id, user.FullName, child.WeeklyAllowance, 0, null));
    }
}
```

### TransactionController
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ICurrentUserService _currentUser;

    [HttpGet("child/{childId}")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(
        Guid childId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // Verify access
        if (!await _currentUser.CanAccessChild(childId))
            return Forbid();

        var transactions = await _context.Transactions
            .Where(t => t.ChildId == childId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
                t.Id,
                t.Amount,
                t.Type,
                t.Description,
                t.BalanceAfter,
                t.CreatedAt,
                t.CreatedBy.FullName))
            .ToListAsync();

        return Ok(transactions);
    }

    [HttpPost]
    [Authorize(Policy = "ParentOnly")]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(CreateTransactionDto dto)
    {
        try
        {
            var transaction = await _transactionService.CreateTransaction(dto);

            // Send real-time update via SignalR
            await _hubContext.Clients.Group($"family-{_currentUser.FamilyId}")
                .SendAsync("TransactionCreated", transaction);

            return Ok(MapToDto(transaction));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("child/{childId}/allowance")]
    [Authorize(Policy = "ParentOnly")]
    public async Task<ActionResult<TransactionDto>> PayAllowance(Guid childId)
    {
        var child = await _context.Children.FindAsync(childId);
        if (child == null || child.FamilyId != _currentUser.FamilyId)
            return NotFound();

        // Check if already paid this week
        if (child.LastAllowanceDate?.Date >= DateTime.UtcNow.StartOfWeek())
            return BadRequest(new { error = "Allowance already paid this week" });

        var transaction = await _transactionService.CreateTransaction(new CreateTransactionDto(
            childId,
            child.WeeklyAllowance,
            TransactionType.Credit,
            "Weekly Allowance"));

        child.LastAllowanceDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(MapToDto(transaction));
    }
}
```

### WishListController
```csharp
[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishListController : ControllerBase
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;

    [HttpGet("child/{childId}")]
    public async Task<ActionResult<List<WishListItemDto>>> GetWishList(Guid childId)
    {
        if (!await _currentUser.CanAccessChild(childId))
            return Forbid();

        var child = await _context.Children.FindAsync(childId);
        var items = await _context.WishListItems
            .Where(w => w.ChildId == childId && !w.IsPurchased)
            .OrderBy(w => w.CreatedAt)
            .Select(w => new WishListItemDto(
                w.Id,
                w.Name,
                w.Price,
                w.Url,
                w.Notes,
                w.IsPurchased,
                w.Price <= child.CurrentBalance))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("child/{childId}")]
    public async Task<ActionResult<WishListItemDto>> AddWishListItem(
        Guid childId,
        CreateWishListItemDto dto)
    {
        // Children can only add to their own list
        if (_currentUser.Role == UserRole.Child && _currentUser.ChildId != childId)
            return Forbid();

        // Parents can add to any child in their family
        if (_currentUser.Role == UserRole.Parent && !await _currentUser.CanAccessChild(childId))
            return Forbid();

        var item = new WishListItem
        {
            ChildId = childId,
            Name = dto.Name,
            Price = dto.Price,
            Url = dto.Url,
            Notes = dto.Notes
        };

        _context.WishListItems.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWishList),
            new { childId },
            MapToDto(item));
    }

    [HttpPut("{itemId}/purchase")]
    [Authorize(Policy = "ParentOnly")]
    public async Task<IActionResult> MarkAsPurchased(Guid itemId)
    {
        var item = await _context.WishListItems
            .Include(w => w.Child)
            .FirstOrDefaultAsync(w => w.Id == itemId);

        if (item == null || item.Child.FamilyId != _currentUser.FamilyId)
            return NotFound();

        item.IsPurchased = true;
        item.PurchasedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{itemId}")]
    public async Task<IActionResult> DeleteWishListItem(Guid itemId)
    {
        var item = await _context.WishListItems
            .Include(w => w.Child)
            .FirstOrDefaultAsync(w => w.Id == itemId);

        if (item == null)
            return NotFound();

        // Check permissions
        if (_currentUser.Role == UserRole.Child && item.Child.UserId != _currentUser.UserId)
            return Forbid();
        if (_currentUser.Role == UserRole.Parent && item.Child.FamilyId != _currentUser.FamilyId)
            return Forbid();

        _context.WishListItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
```

## Minimal APIs (Alternative for Simple Endpoints)

```csharp
// In Program.cs - simpler endpoints can use Minimal APIs
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }))
   .AllowAnonymous();

app.MapGet("/api/children/{childId}/balance", async (Guid childId, AllowanceContext db) =>
{
    var child = await db.Children.FindAsync(childId);
    return child == null ? Results.NotFound() : Results.Ok(new { balance = child.CurrentBalance });
})
.RequireAuthorization();

app.MapPost("/api/children/{childId}/quick-transaction",
    async (Guid childId, QuickTransactionDto dto, ITransactionService service) =>
{
    var result = await service.QuickTransaction(childId, dto.Amount, dto.IsCredit);
    return Results.Ok(result);
})
.RequireAuthorization("ParentOnly");
```

## Authentication & Authorization

### Current User Service
```csharp
public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid? FamilyId { get; }
    UserRole Role { get; }
    Guid? ChildId { get; }
    Task<bool> CanAccessChild(Guid childId);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AllowanceContext _context;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, AllowanceContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;

        var claims = _httpContextAccessor.HttpContext?.User;
        if (claims?.Identity?.IsAuthenticated == true)
        {
            UserId = Guid.Parse(claims.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            FamilyId = Guid.TryParse(claims.FindFirst("familyId")?.Value, out var fid) ? fid : null;
            Role = Enum.Parse<UserRole>(claims.FindFirst("role")?.Value ?? "Parent");
        }
    }

    public Guid UserId { get; }
    public Guid? FamilyId { get; }
    public UserRole Role { get; }

    public Guid? ChildId => _childId ??= _context.Children
        .Where(c => c.UserId == UserId)
        .Select(c => c.Id)
        .FirstOrDefault();

    public async Task<bool> CanAccessChild(Guid childId)
    {
        if (Role == UserRole.Parent)
        {
            return await _context.Children
                .AnyAsync(c => c.Id == childId && c.FamilyId == FamilyId);
        }
        else
        {
            return ChildId == childId;
        }
    }
}
```

### Authorization Policies
```csharp
// In Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ParentOnly", policy =>
        policy.RequireClaim("role", UserRole.Parent.ToString()));

    options.AddPolicy("ChildOnly", policy =>
        policy.RequireClaim("role", UserRole.Child.ToString()));
});
```

## Error Handling

### Global Exception Handler
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            InvalidOperationException => new
            {
                status = 400,
                message = exception.Message
            },
            UnauthorizedAccessException => new
            {
                status = 401,
                message = "Unauthorized"
            },
            _ => new
            {
                status = 500,
                message = "An error occurred"
            }
        };

        context.Response.StatusCode = response.status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

## Model Validation

### Using Data Annotations
```csharp
public class CreateTransactionDto
{
    [Required]
    public Guid ChildId { get; set; }

    [Required]
    [Range(0.01, 10000)]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Description { get; set; }
}
```

### Using FluentValidation (Optional but Better)
```csharp
public class CreateTransactionValidator : AbstractValidator<CreateTransactionDto>
{
    public CreateTransactionValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(10000);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}
```

## Swagger/OpenAPI Documentation

### Swagger Configuration
```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Allowance Tracker API",
        Version = "v1",
        Description = "API for managing family allowances"
    });

    // Add XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add JWT authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

## Response Caching

```csharp
// In Program.cs
builder.Services.AddResponseCaching();

// In Controller
[HttpGet]
[ResponseCache(Duration = 60)] // Cache for 60 seconds
public async Task<ActionResult<List<ChildDto>>> GetChildren()
{
    // Implementation
}
```

## Rate Limiting (New in .NET 7+)

```csharp
// In Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();

// In Controller
[EnableRateLimiting("api")]
public class TransactionController : ControllerBase
{
    // Controller actions
}
```