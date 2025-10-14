# Local Development Guide

This guide will help you get the Allowance Tracker application running on your local machine for development.

## Architecture Overview

The application consists of two parts that run independently:

```
┌─────────────────────────────────────────────┐
│           Your Local Machine                │
│                                             │
│  ┌──────────────────┐  ┌─────────────────┐ │
│  │  .NET API        │  │  React Frontend │ │
│  │  Port: 7071      │◄─┤  Port: 5173     │ │
│  │  (ASP.NET Core)  │  │  (Vite Dev)     │ │
│  └────────┬─────────┘  └─────────────────┘ │
│           │                                  │
│           ▼                                  │
│  ┌──────────────────┐                       │
│  │  SQL Server      │                       │
│  │  Port: 1433      │                       │
│  │  (Docker or      │                       │
│  │   Local Install) │                       │
│  └──────────────────┘                       │
└─────────────────────────────────────────────┘
```

---

## Prerequisites

### Required Software

1. **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)**
   ```bash
   # Check version
   dotnet --version
   # Should output: 8.0.x or higher
   ```

2. **[Node.js 20.x](https://nodejs.org/)** (LTS version recommended)
   ```bash
   # Check version
   node --version
   # Should output: v20.x.x or higher

   npm --version
   # Should output: 10.x.x or higher
   ```

3. **SQL Server** - Choose one option:

   **Option A: Docker (Recommended)**
   - [Docker Desktop](https://www.docker.com/products/docker-desktop)
   - Easy to set up and tear down
   - No system-wide SQL Server installation needed

   **Option B: Local SQL Server**
   - [SQL Server 2022 Developer Edition](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Free)
   - [Azure Data Studio](https://docs.microsoft.com/sql/azure-data-studio/download) or [SQL Server Management Studio](https://docs.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms) (optional, for managing database)

4. **Code Editor** (Choose one):
   - [Visual Studio Code](https://code.visualstudio.com/) (Recommended)
   - [Visual Studio 2022](https://visualstudio.microsoft.com/)
   - [Rider](https://www.jetbrains.com/rider/)

### Recommended VS Code Extensions

- **C# Dev Kit** - C# language support
- **ESLint** - JavaScript/TypeScript linting
- **Tailwind CSS IntelliSense** - Tailwind CSS autocompletion
- **REST Client** - Test API endpoints

---

## Step 1: Clone the Repository

```bash
git clone https://github.com/yourusername/allowance-tracker.git
cd allowance-tracker
```

---

## Step 2: Set Up SQL Server Database

### Option A: Using Docker (Recommended)

This is the easiest way to get a SQL Server database running:

```bash
# Start SQL Server in a Docker container
docker run -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 \
  --name allowancetracker-sql \
  -d mcr.microsoft.com/azure-sql-edge:latest

# Verify it's running
docker ps | grep allowancetracker-sql
```

**Connection String:**
```
Server=localhost,1433;Database=AllowanceTracker;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;MultipleActiveResultSets=true
```

### Option B: Using Local SQL Server

If you installed SQL Server locally:

1. **Ensure SQL Server is running:**
   - Windows: Check "SQL Server (MSSQLSERVER)" service is started
   - macOS/Linux: Check `systemctl status mssql-server`

2. **Connection String** (Windows Authentication):
   ```
   Server=localhost;Database=AllowanceTracker;Trusted_Connection=true;MultipleActiveResultSets=true
   ```

3. **Connection String** (SQL Authentication):
   ```
   Server=localhost;Database=AllowanceTracker;User Id=sa;Password=YourPassword;TrustServerCertificate=true;MultipleActiveResultSets=true
   ```

---

## Step 3: Configure the .NET API

### 3.1 Update Connection String

Edit the file: `src/AllowanceTracker/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=AllowanceTracker;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Important:** Replace with your actual SQL Server connection details.

### 3.2 Run Database Migrations

Navigate to the API project and apply migrations:

```bash
cd src/AllowanceTracker

# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create the database and apply migrations
dotnet ef database update

# You should see output like:
# Build started...
# Build succeeded.
# Applying migration '20250101000000_InitialCreate'.
# Done.
```

**Verify Database Creation:**

```bash
# Using SQL command line
sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT name FROM sys.databases WHERE name = 'AllowanceTracker'"

# Or use Azure Data Studio / SSMS to connect and browse
```

### 3.3 Start the API

```bash
# From src/AllowanceTracker directory
dotnet run

# You should see:
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: https://localhost:7071
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:7070
# info: Microsoft.Hosting.Lifetime[0]
#       Application started. Press Ctrl+C to shut down.
```

**Test the API:**

Open your browser to:
- **Swagger UI:** https://localhost:7071/swagger
- **Health Check:** https://localhost:7071/api/health

You should see the Swagger documentation page with all API endpoints.

---

## Step 4: Configure the React Frontend

### 4.1 Install Dependencies

```bash
# Navigate to web directory
cd web

# Install npm packages
npm install

# This may take a minute...
```

### 4.2 Configure Environment Variables

Create a `.env.development` file in the `web` directory:

```bash
# From web directory
cat > .env.development << EOF
VITE_API_URL=https://localhost:7071
EOF
```

**Windows (PowerShell):**
```powershell
# From web directory
@"
VITE_API_URL=https://localhost:7071
"@ | Out-File -FilePath .env.development -Encoding utf8
```

### 4.3 Start the Development Server

```bash
# From web directory
npm run dev

# You should see:
#   VITE v7.1.7  ready in 423 ms
#
#   ➜  Local:   http://localhost:5173/
#   ➜  Network: use --host to expose
#   ➜  press h + enter to show help
```

**Open the App:**

Navigate to http://localhost:5173 in your browser.

---

## Step 5: Create Your First Account

### 5.1 Register a Parent Account

1. Open http://localhost:5173 in your browser
2. Click **"Sign Up"** or go to http://localhost:5173/register
3. Fill in the registration form:
   - Email: `parent@example.com`
   - Password: `Test123!`
   - Confirm Password: `Test123!`
   - First Name: `John`
   - Last Name: `Doe`
4. Click **"Create Account"**

You'll be automatically logged in and redirected to the dashboard.

### 5.2 Add a Child

1. On the Dashboard, click **"Add Child"**
2. Fill in the child details:
   - Email: `alice@example.com`
   - Password: `Test123!`
   - Confirm Password: `Test123!`
   - First Name: `Alice`
   - Last Name: `Doe`
   - Weekly Allowance: `10.00`
3. Click **"Add Child"**

The child will appear on your dashboard!

### 5.3 Create a Transaction

1. Click on the child's card to view details
2. Go to the **"Transactions"** tab
3. Click **"Add Transaction"**
4. Fill in:
   - Amount: `25.00`
   - Type: Credit
   - Category: Chores
   - Description: `Mowed the lawn`
5. Click **"Add Transaction"**

You'll see the transaction appear and the balance update immediately in your browser!

**Note:** Without SignalR, updates are only reflected locally. Other users need to refresh the page to see changes.

---

## Development Workflow

### Running Both Simultaneously

You need **two terminal windows**:

**Terminal 1 - API:**
```bash
cd src/AllowanceTracker
dotnet run
# or for auto-reload on changes:
dotnet watch run
```

**Terminal 2 - Frontend:**
```bash
cd web
npm run dev
```

### Making Code Changes

#### .NET API Changes

The API uses **hot reload** with `dotnet watch`:
```bash
cd src/AllowanceTracker
dotnet watch run
```

Now when you edit any `.cs` file, the app will automatically reload!

#### React Frontend Changes

Vite has **hot module replacement (HMR)** built-in:
- Edit any `.tsx` or `.ts` file
- Save the file
- Browser automatically updates (no refresh needed!)

### Running Tests

#### .NET Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests in watch mode
cd src/AllowanceTracker.Tests
dotnet watch test

# Run specific test
dotnet test --filter "FullyQualifiedName~TransactionServiceTests"
```

#### React Tests (when available)

```bash
cd web
npm run test
```

---

## Troubleshooting

### Issue: API Won't Start

**Error: "Unable to connect to database"**

**Solution:**
1. Verify SQL Server is running:
   ```bash
   docker ps | grep allowancetracker-sql
   # or for local install:
   # Windows: Check Services
   # Linux: systemctl status mssql-server
   ```

2. Test connection string:
   ```bash
   sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q "SELECT 1"
   ```

3. Check connection string in `appsettings.Development.json`

**Error: "Port 7071 is already in use"**

**Solution:**
```bash
# Find process using the port
lsof -ti:7071
# or on Windows:
netstat -ano | findstr :7071

# Kill the process
kill -9 <PID>
# or on Windows:
taskkill /PID <PID> /F

# Or change the port in launchSettings.json
```

### Issue: Migrations Fail

**Error: "Build failed" or "Unable to create migration"**

**Solution:**
```bash
# Ensure you're in the correct directory
cd src/AllowanceTracker

# Clean and rebuild
dotnet clean
dotnet build

# Try migration again
dotnet ef database update

# If still failing, check for SQL Server connectivity
```

### Issue: Frontend Can't Connect to API

**Error: "Network Error" or "CORS Error"**

**Solution:**

1. **Check API is running:**
   - Visit https://localhost:7071/swagger
   - Should see Swagger UI

2. **Check CORS configuration** in `src/AllowanceTracker/Program.cs`:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddDefaultPolicy(policy =>
       {
           policy.WithOrigins("http://localhost:5173")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
       });
   });
   ```

3. **Check environment variable:**
   ```bash
   # In web/.env.development
   cat web/.env.development
   # Should output: VITE_API_URL=https://localhost:7071
   ```

4. **Restart both API and frontend**

### Issue: SSL Certificate Warnings

**Solution:**

Trust the .NET development certificate:
```bash
dotnet dev-certs https --trust
```

Then restart your browser.

### Issue: React App Shows Blank Page

**Solution:**

1. **Check browser console for errors** (F12 → Console tab)

2. **Verify Vite is running:**
   ```bash
   cd web
   npm run dev
   ```

3. **Clear browser cache and reload:**
   - Press `Ctrl+Shift+R` (Windows/Linux)
   - Press `Cmd+Shift+R` (macOS)

4. **Reinstall dependencies:**
   ```bash
   cd web
   rm -rf node_modules package-lock.json
   npm install
   npm run dev
   ```

---

## IDE Setup

### Visual Studio Code

**Recommended `launch.json`** for debugging:

Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET API",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/AllowanceTracker/bin/Debug/net8.0/AllowanceTracker.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/AllowanceTracker",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

**Recommended `tasks.json`:**

Create `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/src/AllowanceTracker/AllowanceTracker.csproj",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

### Visual Studio 2022

1. **Open solution:** `allowance.sln`
2. **Set startup projects:**
   - Right-click solution → Properties
   - Select "Multiple startup projects"
   - Set both `AllowanceTracker` and `web` to "Start"
3. **Press F5** to debug

---

## Environment Variables Reference

### .NET API (`appsettings.Development.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=AllowanceTracker;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-chars-long-for-hmac-sha256-change-in-production",
    "Issuer": "AllowanceTracker",
    "Audience": "AllowanceTracker",
    "ExpiryInDays": 1
  },
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### React Frontend (`web/.env.development`)

```env
VITE_API_URL=https://localhost:7071
```

---

## Database Management

### View Current Database

```bash
# Connect to SQL Server
sqlcmd -S localhost -U sa -P YourStrong@Passw0rd

# List databases
SELECT name FROM sys.databases;
GO

# Use the database
USE AllowanceTracker;
GO

# List tables
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;
GO
```

### Reset Database

```bash
cd src/AllowanceTracker

# Drop database
dotnet ef database drop

# Recreate and apply migrations
dotnet ef database update
```

### Seed Sample Data (Manual)

After creating your parent account, you can use the Swagger UI to create test data:

1. Go to https://localhost:7071/swagger
2. Authenticate using the `/api/v1/auth/login` endpoint
3. Use the `/api/v1/children` POST endpoint to add children
4. Use the `/api/v1/transactions` POST endpoint to add transactions

---

## Useful Commands

### .NET API

```bash
# Build
dotnet build

# Run (with hot reload)
dotnet watch run

# Run tests
dotnet test

# Create migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Clean build artifacts
dotnet clean
```

### React Frontend

```bash
# Install dependencies
npm install

# Start dev server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run linter
npm run lint

# Type check
npm run type-check
```

### Docker (SQL Server)

```bash
# Start SQL Server
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 --name allowancetracker-sql -d mcr.microsoft.com/azure-sql-edge:latest

# Stop SQL Server
docker stop allowancetracker-sql

# Start existing container
docker start allowancetracker-sql

# Remove container
docker rm allowancetracker-sql

# View logs
docker logs allowancetracker-sql
```

---

## Next Steps

- **Explore the API:** Visit https://localhost:7071/swagger
- **Read the specs:** Check the `specs/` directory for detailed documentation
- **Review the code:** Start with `src/AllowanceTracker/Program.cs`
- **Add features:** Follow TDD practices (write tests first!)
- **Deploy:** See [AZURE-DEPLOYMENT.md](AZURE-DEPLOYMENT.md) for production deployment

---

## Getting Help

- **Documentation:** [specs/](specs/) folder
- **Issues:** [GitHub Issues](https://github.com/yourusername/allowance-tracker/issues)
- **API Reference:** https://localhost:7071/swagger (when running locally)

---

**Happy Coding! 🚀**
