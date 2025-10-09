# Allowance Tracker - MVP Overview (.NET)

## Project Vision
A simple, fast allowance tracking application for families, built with .NET 8 and Blazor Server for real-time updates.

## MVP Scope
Focus on core functionality that delivers immediate value:
- Parents can manage children's allowances
- Track money in/out with simple transactions
- Real-time balance updates
- Basic wish list for kids
- Mobile-responsive web interface

## Technical Stack (Simple & Effective)

### Core Technologies
- **Backend**: ASP.NET Core 8.0 (single project, no complex layers)
- **Frontend**: Blazor Server (real-time, no JavaScript needed)
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: ASP.NET Core Identity (built-in, simple)
- **Real-time**: SignalR (comes with Blazor Server)
- **Background Jobs**: IHostedService (built-in, no external dependencies)
- **Testing**: xUnit + basic integration tests
- **Deployment**: Railway or Azure App Service

### Why These Choices?
- **Blazor Server**: Real-time by default, single language (C#), fast development
- **Entity Framework Core**: Migrations, LINQ queries, good enough performance
- **Built-in DI**: No need for external containers, works great
- **PostgreSQL**: Free tier on Railway, reliable, good with EF Core
- **Minimal external dependencies**: Faster to build, easier to maintain

## Simple Project Structure

```
AllowanceTracker/
├── Data/                  # EF Core DbContext and migrations
│   ├── AllowanceContext.cs
│   └── Migrations/
├── Models/               # Domain models
│   ├── User.cs
│   ├── Family.cs
│   ├── Child.cs
│   └── Transaction.cs
├── Services/             # Business logic
│   ├── FamilyService.cs
│   ├── TransactionService.cs
│   └── AllowanceService.cs
├── Pages/                # Blazor pages
│   ├── Index.razor
│   ├── Dashboard.razor
│   ├── Children/
│   └── Transactions/
├── Shared/               # Blazor components
│   ├── MainLayout.razor
│   └── Components/
├── Api/                  # API controllers (if needed for mobile)
│   └── TransactionsController.cs
└── Program.cs            # Startup and DI configuration
```

## Core Features (MVP)

### Phase 1: Authentication & Families (Week 1)
- User registration/login with ASP.NET Core Identity
- Create family account
- Add children to family
- Basic role separation (Parent/Child)

### Phase 2: Transactions & Balances (Week 2)
- Add/subtract money transactions
- View current balance
- Transaction history
- Real-time balance updates

### Phase 3: Allowances & Automation (Week 3)
- Set weekly allowance amount
- Manual "Pay Allowance" button (automation later)
- Basic reporting (this week's transactions)

### Phase 4: Wish Lists (Week 4)
- Children add items to wish list
- Parents can see wish lists
- Mark items as purchased
- Calculate if affordable

## Data Model (Simplified)

### Core Entities
```csharp
public class Family
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<User> Members { get; set; }
}

public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public UserRole Role { get; set; } // Parent or Child
    public Guid FamilyId { get; set; }
}

public class Child
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal WeeklyAllowance { get; set; }
    public decimal CurrentBalance { get; set; }
    public List<Transaction> Transactions { get; set; }
}

public class Transaction
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; } // Credit or Debit
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedById { get; set; }
}
```

## Performance Goals (Reasonable)

### Target Metrics
- Page load: < 1 second
- Transaction save: < 200ms
- Support 100 concurrent users
- Database queries: < 50ms

### Simple Optimizations
- Use `AsNoTracking()` for read-only queries
- Index foreign keys and commonly searched fields
- Cache family data in memory (IMemoryCache)
- Use compiled queries for hot paths
- Enable response compression

## Security (Essential Only)

### Basic Security
- HTTPS only
- Authentication required for all pages
- Parents can only see their family's data
- Children can only see their own data
- Validate all inputs
- Use parameterized queries (EF Core does this)

## Development Approach

### Test-Driven Development (Practical)
1. Write tests for critical paths (transactions, balances)
2. Basic integration tests for API endpoints
3. Manual testing for UI (Blazor components)
4. Target 70% code coverage (good enough for MVP)

### Deployment Strategy
- Single deployment unit (one .NET project)
- Environment variables for configuration
- GitHub Actions for CI/CD
- Railway for hosting (or Azure App Service)

## MVP Success Criteria

### Must Have
- ✅ Parents can create accounts and add children
- ✅ Track transactions with real-time balance updates
- ✅ Set weekly allowance amounts
- ✅ Children can view their balance and history
- ✅ Basic wish list functionality
- ✅ Mobile-responsive design

### Nice to Have (Post-MVP)
- Automated weekly allowance payments
- Email notifications
- Chore tracking
- Spending categories and reports
- Native mobile app

## Technology Decisions Explained

### Why Blazor Server?
- Real-time updates built-in (no separate WebSocket setup)
- No JavaScript required (all C#)
- Simpler than Blazor WebAssembly for MVP
- Great for internal/family apps with known user base

### Why Not Microservices/Clean Architecture?
- Overkill for family app MVP
- Adds complexity without benefit at this scale
- Can refactor later if needed
- Single deployment is simpler

### Why Entity Framework Core?
- Fastest way to get started with .NET
- Migrations handle schema changes
- LINQ is productive for queries
- Performance is fine for our scale

## Risk Mitigation (Simple)

### Technical Risks
- **Performance**: Profile if issues arise, optimize then
- **Data Loss**: Daily database backups
- **Security**: Use built-in ASP.NET Core features
- **Scaling**: Worry about it when we have 1000+ users

## Development Timeline

### Week 1: Foundation
- Project setup
- Authentication with Identity
- Family management
- Basic Blazor layout

### Week 2: Core Features
- Transaction management
- Balance calculations
- Real-time updates
- Transaction history

### Week 3: Allowances
- Allowance configuration
- Manual allowance payments
- Basic reporting

### Week 4: Polish
- Wish list feature
- UI improvements
- Bug fixes
- Deployment

## Total: 4 Weeks to MVP

## Next Steps After MVP
1. Get user feedback
2. Add automated allowance payments
3. Improve UI/UX based on usage
4. Add notifications
5. Consider mobile app if needed