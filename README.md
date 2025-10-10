# Allowance Tracker

A modern, real-time allowance management system built with ASP.NET Core 8.0 and Blazor Server. Helps parents manage children's allowances, track spending, and teach financial responsibility through an intuitive web interface and REST API for mobile apps.

[![Build & Test](https://github.com/yourusername/allowance-tracker/workflows/CI%2FCD/badge.svg)](https://github.com/yourusername/allowance-tracker/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## ✨ Features

### For Parents
- 👨‍👩‍👧‍👦 **Family Management**: Manage multiple children in one family account
- 💰 **Transaction Control**: Add money (chores, gifts) or deduct spending
- 📅 **Automated Allowances**: Set weekly allowances that pay automatically
- 📊 **Real-Time Updates**: See balance changes instantly across all devices
- 🔐 **Secure Authentication**: ASP.NET Core Identity with role-based access

### For Children
- 💵 **Track Balance**: See current balance and transaction history
- 🎯 **Wish List**: Save for things they want (coming soon)
- 📱 **Mobile Access**: REST API ready for mobile app integration

### Technical Highlights
- ⚡ **Real-Time**: SignalR integration for instant updates
- 🧪 **Test-Driven**: 73 comprehensive tests with >90% coverage
- 🐳 **Docker Ready**: One-command deployment with docker-compose
- 🚀 **CI/CD**: Automated testing and deployment via GitHub Actions
- 🔒 **JWT Authentication**: Secure API access for mobile apps
- 💾 **PostgreSQL**: Reliable data persistence with EF Core migrations

## 🏗️ Architecture

```
ASP.NET Core 8.0 Blazor Server
├── Data Layer (EF Core 8 + PostgreSQL)
│   ├── ApplicationUser (Identity)
│   ├── Family
│   ├── Child
│   └── Transaction (immutable with audit trail)
├── Business Logic (Services)
│   ├── FamilyService
│   ├── TransactionService (atomic operations)
│   └── AllowanceService (background job)
├── UI Layer (Blazor Components)
│   ├── Dashboard (real-time)
│   ├── ChildCard (reusable)
│   └── TransactionForm (validated)
├── API Layer (REST + JWT)
│   └── TransactionsController
└── Real-Time (SignalR)
    └── FamilyHub
```

## 🚀 Quick Start

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 16+](https://www.postgresql.org/download/) or Docker
- Optional: [Docker Desktop](https://www.docker.com/products/docker-desktop) for containerized deployment

### Option 1: Docker (Recommended)

The fastest way to get started:

```bash
# Clone the repository
git clone https://github.com/yourusername/allowance-tracker.git
cd allowance-tracker

# Create environment file
cp .env.example .env
# Edit .env and set your DB_PASSWORD and JWT_SECRET_KEY

# Start the application
docker-compose up -d

# Open browser
open http://localhost:5000
```

That's it! The app and database are running in containers.

### Option 2: Local Development

```bash
# Clone the repository
git clone https://github.com/yourusername/allowance-tracker.git
cd allowance-tracker

# Restore dependencies
dotnet restore

# Update connection string in appsettings.Development.json
# Set your PostgreSQL connection details

# Apply database migrations
cd src/AllowanceTracker
dotnet ef database update

# Run the application
dotnet run

# Open browser
open https://localhost:7001
```

### Option 3: Railway Deployment

Click the button below to deploy to Railway:

[![Deploy on Railway](https://railway.app/button.svg)](https://railway.app/new/template)

See [DEPLOYMENT.md](DEPLOYMENT.md) for detailed deployment instructions.

## 🧪 Running Tests

We follow strict Test-Driven Development (TDD) with 73 comprehensive tests:

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "Category=Unit"

# Watch mode (continuous testing)
dotnet watch test
```

### Test Coverage Breakdown
- **Phase 1**: ApplicationUser (12 tests) + Family (11 tests) = 23 tests
- **Phase 2**: TransactionService (11 tests) = 35 tests total
- **Phase 4**: AllowanceService (10 tests) = 45 tests total
- **Enhancement 1**: JwtService (10 tests) + API (5 tests) = 60 tests total
- **Enhancement 2**: Blazor Components (13 tests) = 73 tests total

## 📖 Usage

### Create a Family Account

1. Navigate to `/register`
2. Create parent account (first user becomes family admin)
3. Add children from the dashboard

### Add a Child

```csharp
// Via UI: Dashboard → "Add Child" button

// Or via API:
POST /api/v1/children
{
  "firstName": "Alice",
  "lastName": "Smith",
  "weeklyAllowance": 10.00
}
```

### Record a Transaction

**Via Blazor UI:**
1. Go to Dashboard
2. Click "Add Transaction" on a child card
3. Enter amount, type (Credit/Debit), and description
4. Click "Save"

**Via API:**
```bash
curl -X POST https://localhost:7001/api/v1/transactions \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "childId": "guid-here",
    "amount": 25.00,
    "type": "Credit",
    "description": "Mowed the lawn"
  }'
```

### Weekly Allowance

Allowances are processed automatically every 24 hours by a background job (`WeeklyAllowanceJob`):
- Checks all children with configured weekly allowances
- Pays if 7+ days since last payment
- Skips children with $0 allowance
- Prevents double-payment within same week

## 🔐 API Authentication

The REST API uses JWT Bearer authentication:

### 1. Obtain Token

```bash
POST /api/auth/login
{
  "email": "parent@example.com",
  "password": "YourPassword123!"
}

# Response:
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2025-10-10T12:00:00Z"
}
```

### 2. Use Token in Requests

```bash
curl -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  https://localhost:7001/api/v1/children/{childId}/balance
```

Tokens expire after 24 hours. Claims included: `UserId`, `Email`, `Role`, `FamilyId`.

## 📁 Project Structure

```
allowance/
├── src/
│   ├── AllowanceTracker/           # Main Blazor Server app
│   │   ├── Data/                   # EF Core DbContext & Migrations
│   │   ├── Models/                 # Domain entities
│   │   ├── Services/               # Business logic
│   │   ├── Pages/                  # Blazor pages
│   │   ├── Shared/                 # Blazor components
│   │   ├── Api/V1/                 # REST API controllers
│   │   ├── Hubs/                   # SignalR hubs
│   │   └── Program.cs              # App startup & DI
│   └── AllowanceTracker.Tests/     # xUnit test project
│       ├── Models/                 # Model tests
│       ├── Services/               # Service tests
│       ├── Api/                    # API controller tests
│       └── Components/             # Blazor component tests (bUnit)
├── specs/                          # Detailed specifications
├── .github/workflows/              # GitHub Actions CI/CD
├── docker-compose.yml              # Docker orchestration
├── Dockerfile                      # Container definition
├── CLAUDE.md                       # Development guide for AI
├── DEPLOYMENT.md                   # Deployment instructions
├── DOCKER.md                       # Docker usage guide
└── README.md                       # This file
```

## 🛠️ Technology Stack

| Category | Technology |
|----------|-----------|
| **Framework** | ASP.NET Core 8.0 |
| **UI** | Blazor Server (real-time, no JavaScript) |
| **Database** | PostgreSQL 16 |
| **ORM** | Entity Framework Core 8 |
| **Auth** | ASP.NET Core Identity + JWT Bearer |
| **Real-Time** | SignalR (built into Blazor) |
| **Background Jobs** | IHostedService (built-in) |
| **Testing** | xUnit + FluentAssertions + Moq + bUnit |
| **Containerization** | Docker + docker-compose |
| **CI/CD** | GitHub Actions + Azure Pipelines |
| **Deployment** | Railway / Azure App Service |

## 🔒 Security

- **Password Hashing**: ASP.NET Core Identity with PBKDF2
- **JWT Tokens**: HMAC-SHA256 signing, 24-hour expiration
- **Role-Based Access**: Parent/Child roles enforced at API level
- **Database Transactions**: Atomic money operations prevent race conditions
- **Audit Trail**: All transactions include `CreatedBy` and `CreatedAt`
- **Input Validation**: Data annotations + FluentValidation
- **Immutable Transactions**: Once created, transactions cannot be modified

## 📊 Database Schema

```sql
-- Core tables
Users (ApplicationUser extends IdentityUser)
  - Id (Guid, PK)
  - Email, FirstName, LastName
  - Role (Parent/Child enum)
  - FamilyId (FK)

Families
  - Id (Guid, PK)
  - Name
  - CreatedAt

Children
  - Id (Guid, PK)
  - UserId (FK to Users)
  - FamilyId (FK to Families)
  - CurrentBalance (decimal)
  - WeeklyAllowance (decimal)
  - LastAllowanceDate (DateTime?)

Transactions
  - Id (Guid, PK)
  - ChildId (FK to Children)
  - Amount (decimal)
  - Type (Credit/Debit enum)
  - Description (string)
  - BalanceAfter (decimal, snapshot)
  - CreatedById (FK to Users, audit)
  - CreatedAt (DateTime, audit)
```

## 🤝 Contributing

We follow strict Test-Driven Development (TDD):

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Write tests first** (RED phase)
4. **Implement minimum code** to pass tests (GREEN phase)
5. **Refactor** while keeping tests green
6. **Commit with descriptive messages**:
   ```bash
   git commit -m "Add feature X with TDD

   - Wrote 5 tests for feature X
   - Implemented minimum code to pass
   - Refactored for clarity

   Tests: 78 passing (+5)
   🤖 Generated with Claude Code"
   ```
7. **Push to branch**: `git push origin feature/amazing-feature`
8. **Open Pull Request**

### Code Standards
- **>90% test coverage** for new features
- **AAA pattern**: Arrange, Act, Assert
- **Async/await** consistently
- **Nullable reference types** enabled
- **Records for DTOs**, classes for entities

## 📝 Documentation

- [**CLAUDE.md**](CLAUDE.md) - Development guide for AI assistants (comprehensive)
- [**DEPLOYMENT.md**](DEPLOYMENT.md) - Deployment instructions (Railway, Azure, Docker)
- [**DOCKER.md**](DOCKER.md) - Docker usage guide
- [**specs/**](specs/) - Detailed specifications:
  - `01-overview.md` - System overview
  - `02-database-schema.md` - EF Core models
  - `03-api-specification.md` - REST API docs
  - `04-implementation-phases.md` - TDD roadmap
  - `05-testing-strategy.md` - Testing approach
  - `06-tdd-best-practices.md` - TDD patterns
  - `07-blazor-ui-specification.md` - UI components
  - `08-remaining-enhancements.md` - Future features

## 🗺️ Roadmap

### Completed ✅
- [x] Phase 1: Foundation with EF Core & Identity
- [x] Phase 2: Transaction Management with Atomic Operations
- [x] Phase 3: Blazor UI with Real-Time Updates
- [x] Phase 4: Weekly Allowance Background Job
- [x] Enhancement 1: JWT Authentication & REST API
- [x] Enhancement 2: Advanced Blazor Components
- [x] Enhancement 3: SignalR Real-Time Broadcasting
- [x] Enhancement 4: Docker + CI/CD + Deployment

### Future Enhancements 🚀
- [ ] Wish List Management (save for goals)
- [ ] Spending Categories & Reports
- [ ] Chore Assignments with Rewards
- [ ] Family Notifications
- [ ] Mobile App (React Native + API)
- [ ] Multi-Currency Support
- [ ] CSV Export for Tax Records

## 🐛 Troubleshooting

### Database Migrations

```bash
# Add new migration
dotnet ef migrations add YourMigrationName

# Apply migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove
```

### JWT Token Issues

```bash
# Verify appsettings.json has valid JWT configuration
# SecretKey must be at least 32 characters for HMAC-SHA256

{
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-chars-long-for-hmac-sha256",
    "Issuer": "AllowanceTracker",
    "Audience": "AllowanceTracker"
  }
}
```

### Docker Issues

```bash
# View logs
docker-compose logs -f

# Rebuild containers
docker-compose down
docker-compose up --build

# Reset everything
docker-compose down -v  # ⚠️ This deletes all data!
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
- UI powered by [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- Database by [PostgreSQL](https://www.postgresql.org/)
- Testing with [xUnit](https://xunit.net/), [FluentAssertions](https://fluentassertions.com/), and [bUnit](https://bunit.dev/)
- Deployed on [Railway](https://railway.app/) or [Azure](https://azure.microsoft.com/)
- Developed with ❤️ using Test-Driven Development

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/allowance-tracker/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/allowance-tracker/discussions)
- **Documentation**: See [specs/](specs/) folder

---

**Built with Test-Driven Development** 🧪
**73 Tests Passing** ✅
**>90% Code Coverage** 📊
**Production Ready** 🚀
