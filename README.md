# Allowance Tracker

A modern allowance management system built with **React** + **ASP.NET Core 8.0**. Helps parents manage children's allowances, track spending, and teach financial responsibility through an intuitive web interface and REST API.

[![CI Pipeline](https://github.com/yourusername/allowance-tracker/workflows/CI%20Pipeline/badge.svg)](https://github.com/yourusername/allowance-tracker/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## ✨ Features

### For Parents
- 👨‍👩‍👧‍👦 **Family Management**: Manage multiple children in one family account
- 💰 **Transaction Control**: Add money (chores, gifts) or deduct spending
- 📅 **Automated Allowances**: Set weekly allowances that pay automatically
- 📊 **Analytics Dashboard**: View spending trends, income vs expenses, and category breakdowns
- 💾 **Savings Accounts**: Automatic savings transfers with deposits and withdrawals
- 🎯 **Wish Lists**: Help children save for goals
- 🔐 **Secure Authentication**: ASP.NET Core Identity with role-based access

### For Children
- 💵 **Track Balance**: See current balance and transaction history
- 🎯 **Wish List**: Save for things they want with progress tracking
- 💰 **Savings Account**: Build savings with parent oversight
- 📱 **Mobile Ready**: iOS native app coming soon

### Technical Highlights
- ⚛️ **Modern Frontend**: React 19 + TypeScript + Tailwind CSS v4
- 🎨 **Rich Visualizations**: Recharts for analytics (line, bar, pie charts)
- 🧪 **Test-Driven**: 213 comprehensive tests with >90% coverage
- 🐳 **Docker Ready**: Containerized deployment
- 🚀 **CI/CD**: Automated testing and deployment via GitHub Actions
- 🔒 **JWT Authentication**: Secure API access
- 💾 **Azure SQL Server**: Reliable data persistence with EF Core migrations
- ☁️ **Azure Deployment**: API on App Service, Frontend on Storage Static Website
- 📡 **Optional SignalR**: Add real-time updates if needed (see [ADDING_SIGNALR.md](ADDING_SIGNALR.md))

## 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│         React Frontend (SPA)            │
│  React 19 + TypeScript + Vite          │
│  Tailwind CSS v4 + Recharts            │
│  Axios for API calls                    │
└──────────────────┬──────────────────────┘
                   │ HTTP/REST + JWT
                   ▼
┌─────────────────────────────────────────┐
│      ASP.NET Core 8.0 Web API           │
├─────────────────────────────────────────┤
│  ├── Controllers (REST endpoints)       │
│  ├── Services (Business Logic)         │
│  │   ├── FamilyService                 │
│  │   ├── TransactionService            │
│  │   ├── AllowanceService              │
│  │   ├── WishListService               │
│  │   ├── SavingsAccountService         │
│  │   └── TransactionAnalyticsService   │
│  └── Authentication (JWT + Identity)   │
└──────────────────┬──────────────────────┘
                   │ Entity Framework Core 8
                   ▼
┌─────────────────────────────────────────┐
│      Azure SQL Database                 │
│  ├── Users (ApplicationUser)           │
│  ├── Families                           │
│  ├── Children                           │
│  ├── Transactions                       │
│  ├── WishListItems                      │
│  └── SavingsTransactions                │
└──────────────────┬──────────────────────┘
                   ▲
                   │
┌──────────────────────────────────────────┐
│   Azure Function (Timer Trigger)        │
│   Processes Weekly Allowances            │
│   Runs daily at 10:00 AM UTC             │
└──────────────────────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20.x](https://nodejs.org/) (LTS recommended)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) or Docker

### Local Development

For detailed step-by-step instructions, see **[LOCAL_DEVELOPMENT.md](LOCAL_DEVELOPMENT.md)**

**Quick version:**

```bash
# 1. Clone the repository
git clone https://github.com/yourusername/allowance-tracker.git
cd allowance-tracker

# 2. Start SQL Server (using Docker)
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name allowancetracker-sql \
  -d mcr.microsoft.com/azure-sql-edge:latest

# 3. Setup and run the API
cd src/AllowanceTracker
dotnet ef database update
dotnet run
# API runs on https://localhost:7071

# 4. Setup and run the React app (in a new terminal)
cd web
npm install
echo "VITE_API_URL=https://localhost:7071" > .env.development
npm run dev
# Frontend runs on http://localhost:5173
```

**Open your browser to http://localhost:5173**

### Azure Deployment

Deploy to Azure with GitHub Actions:

1. Set up Azure resources (SQL, App Service, Function App, Storage)
2. Configure GitHub Secrets
3. Push to `main` branch - automatic deployment!

See **[GITHUB-ACTIONS-DEPLOYMENT.md](GITHUB-ACTIONS-DEPLOYMENT.md)** for complete instructions.

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

### Test Coverage
- **213 tests passing** (API and Services)
- **>90% code coverage** on critical business logic
- Includes: Models, Services, Controllers, Authentication
- CI/CD runs all tests automatically on every commit

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

**Via React UI:**
1. Go to Dashboard
2. Click on a child's card to view details
3. Go to "Transactions" tab
4. Click "Add Transaction"
5. Enter amount, type (Credit/Debit), category, and description
6. Click "Save"

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

Allowances are processed **automatically** by an Azure Function with timer trigger:
- ✅ Runs daily at 10:00 AM UTC
- ✅ Checks all children with configured weekly allowances
- ✅ Pays if 7+ days since last payment (or never paid before)
- ✅ Creates transaction with category "Allowance"
- ✅ Processes automatic savings transfer (if enabled)
- ✅ Prevents double-payment within same week
- ✅ Logs all activity to Application Insights
- ✅ Nearly free on consumption plan

**See [WEEKLY_ALLOWANCE.md](WEEKLY_ALLOWANCE.md) for complete details and testing instructions.**

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
│   ├── AllowanceTracker/           # ASP.NET Core Web API
│   │   ├── Data/                   # EF Core DbContext & Migrations
│   │   ├── Models/                 # Domain entities
│   │   ├── DTOs/                   # Data transfer objects
│   │   ├── Services/               # Business logic
│   │   ├── Api/V1/                 # REST API controllers
│   │   └── Program.cs              # App startup & DI
│   ├── AllowanceTracker.Functions/ # Azure Functions
│   │   ├── WeeklyAllowanceFunction.cs # Timer trigger
│   │   ├── Program.cs              # Function startup & DI
│   │   └── host.json               # Function configuration
│   └── AllowanceTracker.Tests/     # xUnit test project
│       ├── Models/                 # Model tests
│       ├── Services/               # Service tests
│       └── Api/                    # API controller tests
├── web/                            # React Frontend
│   ├── src/
│   │   ├── components/             # React components
│   │   │   ├── tabs/               # Tab components
│   │   │   └── forms/              # Form components
│   │   ├── pages/                  # Page components
│   │   ├── services/               # API service layer
│   │   ├── contexts/               # React contexts
│   │   ├── types/                  # TypeScript types
│   │   └── App.tsx                 # Main app component
│   ├── package.json
│   └── vite.config.ts
├── ios/                            # iOS Native App (SwiftUI)
├── specs/                          # Detailed specifications
├── .github/workflows/              # GitHub Actions CI/CD workflows
├── GITHUB-ACTIONS-DEPLOYMENT.md    # GitHub Actions deployment guide
├── LOCAL_DEVELOPMENT.md            # Local dev setup guide
├── CLAUDE.md                       # Development guide for AI
└── README.md                       # This file
```

## 🛠️ Technology Stack

| Category | Technology |
|----------|-----------|
| **Backend Framework** | ASP.NET Core 8.0 Web API |
| **Frontend Framework** | React 19 + TypeScript |
| **Build Tool** | Vite 7 |
| **Styling** | Tailwind CSS v4 |
| **Charts** | Recharts |
| **HTTP Client** | Axios |
| **Database** | Azure SQL Server / SQL Server 2022 |
| **ORM** | Entity Framework Core 8 |
| **Auth** | ASP.NET Core Identity + JWT Bearer |
| **Background Jobs** | Azure Functions (Timer Trigger) |
| **Testing** | xUnit + FluentAssertions + Moq |
| **CI/CD** | GitHub Actions |
| **Deployment** | Azure App Service (API) + Azure Functions + Azure Storage (Frontend) |

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

- [**LOCAL_DEVELOPMENT.md**](LOCAL_DEVELOPMENT.md) - **START HERE** - Complete local setup guide
- [**GITHUB-ACTIONS-DEPLOYMENT.md**](GITHUB-ACTIONS-DEPLOYMENT.md) - Deploy to Azure with GitHub Actions
- [**WEEKLY_ALLOWANCE.md**](WEEKLY_ALLOWANCE.md) - Azure Function for automated allowances
- [**ADDING_SIGNALR.md**](ADDING_SIGNALR.md) - Add real-time updates (optional)
- [**CLAUDE.md**](CLAUDE.md) - Development guide for AI assistants
- [**specs/**](specs/) - Detailed specifications:
  - `01-overview.md` - System overview
  - `02-database-schema.md` - EF Core models
  - `03-api-specification.md` - REST API docs
  - `04-implementation-phases.md` - TDD roadmap
  - `05-testing-strategy.md` - Testing approach
  - `06-tdd-best-practices.md` - TDD patterns
  - `08-ios-app-specification.md` - iOS native app (SwiftUI)
  - `09-design-system.md` - Design system and UI patterns

## 🗺️ Roadmap

### Completed ✅
- [x] Phase 1: Foundation with EF Core & Identity
- [x] Phase 2: Transaction Management with Atomic Operations
- [x] Phase 3: React Frontend Migration (from Blazor)
- [x] Phase 4: Weekly Allowance Azure Function
- [x] Phase 5: JWT Authentication & REST API
- [x] Phase 6: Wish List Management
- [x] Phase 7: Analytics & Reports with Charts
- [x] Phase 8: Savings Accounts with Auto-Transfer
- [x] Phase 9: Azure Deployment Pipeline

### In Progress 🚧
- [ ] iOS Native App (SwiftUI)
- [ ] PDF Export for Reports

### Future Enhancements 🚀
- [ ] Chore Assignments with Rewards
- [ ] Family Notifications (Email/Push)
- [ ] Multi-Currency Support
- [ ] Recurring Transactions
- [ ] Budget Goals and Alerts

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
- UI powered by [React](https://react.dev/) with [TypeScript](https://www.typescriptlang.org/)
- Database by [Microsoft SQL Server](https://www.microsoft.com/sql-server)
- Testing with [xUnit](https://xunit.net/) and [FluentAssertions](https://fluentassertions.com/)
- Deployed on [Azure](https://azure.microsoft.com/)
- Developed with ❤️ using Test-Driven Development

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/allowance-tracker/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/allowance-tracker/discussions)
- **Documentation**: See [specs/](specs/) folder

---

**Built with Test-Driven Development** 🧪
**213 Tests Passing** ✅
**>90% Code Coverage** 📊
**Modern Stack: React + .NET** ⚛️
**Production Ready** 🚀
