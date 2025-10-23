# GitHub Actions CI/CD Documentation

This document describes the Continuous Integration and Continuous Deployment (CI/CD) setup using GitHub Actions for the Allowance Tracker project.

## Overview

The project uses GitHub Actions for automated building, testing, and deployment. All workflows are located in `.github/workflows/`.

## Workflows

### 1. Main CI Pipeline (`ci.yml`)

**Purpose**: Comprehensive CI pipeline that builds and tests all components in parallel.

**Triggers**:
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Excludes documentation files (`docs/**`, `*.md`, `specs/**`)

**Jobs**:

1. **api-build** - Build and test .NET API
   - Restores NuGet packages
   - Builds solution in Release configuration
   - Runs all tests with code coverage
   - Publishes API artifacts
   - Uploads test results and coverage reports

2. **function-build** - Build Azure Function
   - Builds the weekly allowance timer function
   - Publishes function artifacts for deployment

3. **react-build** - Build React frontend
   - Installs npm dependencies
   - Builds production React app with Vite
   - Publishes frontend artifacts

4. **dotnet-quality** - .NET code quality checks
   - Runs `dotnet format` to verify code formatting
   - Builds with warnings as errors
   - Checks for vulnerable NuGet packages

5. **react-quality** - React code quality checks
   - Runs ESLint
   - TypeScript type checking
   - npm audit for security vulnerabilities

6. **ci-success** - Pipeline summary
   - Verifies all builds succeeded
   - Creates a comprehensive summary in GitHub

**Artifacts Published** (retained for 30 days):
- `api-{SHA}` - Published .NET API
- `function-{SHA}` - Published Azure Function
- `react-{SHA}` - Built React app (dist folder)
- Test results (retained for 7 days)
- Code coverage reports (retained for 7 days)

### 2. API CI (`api.yml`)

**Purpose**: Focused CI workflow for .NET API changes only.

**Triggers**:
- Changes to `src/**` or `.github/workflows/api.yml`

**Jobs**:
- `build-and-test` - Build, test, upload results
- `code-quality` - Code formatting and warnings
- `security-scan` - Vulnerability scanning

### 3. React CI (`web.yml`)

**Purpose**: Focused CI workflow for React frontend changes only.

**Triggers**:
- Changes to `web/**` or `.github/workflows/web.yml`

**Jobs**:
- `build-and-test` - Build and quality checks
- `lighthouse` - Lighthouse CI performance audit (PR only)
- `security-scan` - npm audit

### 4. Azure Function CI (`function.yml`)

**Purpose**: Focused CI workflow for Azure Function changes only.

**Triggers**:
- Changes to `src/AllowanceTracker.Functions/**`

**Jobs**:
- `build-and-test` - Build and test
- `code-quality` - Code formatting and warnings

### 5. iOS CI (`ios.yml`)

**Purpose**: Build and test iOS native app.

**Triggers**:
- Changes to `ios/**` or `.github/workflows/ios.yml`

**Jobs**:
- `build-and-test` - Build and test on macOS with Xcode 15.2
- `swiftlint` - Swift code quality checks

**Runs on**: macOS runners (required for Xcode)

## Environment Variables

The workflows use the following environment variables:

```yaml
DOTNET_VERSION: '8.0.x'     # .NET SDK version
NODE_VERSION: '20.x'         # Node.js version
XCODE_VERSION: '15.2'        # Xcode version for iOS
```

## Secrets Configuration

Configure these secrets in GitHub repository settings:

### Required for Deployment (if deploying to Azure)
- `AZURE_WEBAPP_PUBLISH_PROFILE` - API deployment credential
- `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` - Function deployment credential
- `AZURE_STORAGE_CONNECTION_STRING` - Frontend storage credential

### Optional
- `VITE_API_URL` - API URL for React builds (defaults to example URL)

## Parallel Execution

The main CI pipeline runs builds in parallel for maximum speed:

```
├── api-build (runs in parallel)
├── function-build (runs in parallel)
├── react-build (runs in parallel)
├── dotnet-quality (runs in parallel)
└── react-quality (runs in parallel)
    └── ci-success (runs after all jobs)
```

All jobs run simultaneously, reducing total CI time.

## Path Filtering

Workflows only run when relevant files change:

- **API workflow**: Triggers on `src/**` changes
- **React workflow**: Triggers on `web/**` changes
- **Function workflow**: Triggers on `src/AllowanceTracker.Functions/**` changes
- **iOS workflow**: Triggers on `ios/**` changes

This prevents unnecessary builds and saves GitHub Actions minutes.

## Status Badges

Add to README.md:

```markdown
[![API CI](https://github.com/YOUR_USERNAME/allowance/workflows/API%20CI/badge.svg)](https://github.com/YOUR_USERNAME/allowance/actions)
[![Web CI](https://github.com/YOUR_USERNAME/allowance/workflows/Web%20CI/badge.svg)](https://github.com/YOUR_USERNAME/allowance/actions)
[![Function CI](https://github.com/YOUR_USERNAME/allowance/workflows/Azure%20Function%20CI/badge.svg)](https://github.com/YOUR_USERNAME/allowance/actions)
```

## Testing

### Running Tests Locally

```bash
# .NET tests
dotnet test

# .NET tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# React tests (if configured)
cd web
npm test
```

### Test Results in CI

Test results are automatically uploaded as artifacts:
- TRX files for .NET tests
- Code coverage reports (Cobertura format)
- XCTest results for iOS

View in GitHub Actions UI under "Artifacts" section.

## Code Quality

### .NET Code Quality
- **dotnet format**: Enforces consistent code formatting
- **Warnings as errors**: Treats compiler warnings as build failures
- **Vulnerable packages**: Scans for known vulnerabilities

### React Code Quality
- **ESLint**: JavaScript/TypeScript linting
- **TypeScript**: Strict type checking
- **npm audit**: Security vulnerability scanning

### iOS Code Quality
- **SwiftLint**: Swift code style and conventions

## Debugging Failed Builds

### View Logs
1. Go to GitHub Actions tab
2. Click on failed workflow run
3. Click on failed job
4. Expand failed step to view logs

### Common Issues

**Issue**: `dotnet: command not found`
**Solution**: Ensure `setup-dotnet` action is included before dotnet commands

**Issue**: `npm: command not found`
**Solution**: Ensure `setup-node` action is included before npm commands

**Issue**: Tests fail in CI but pass locally
**Solution**: Check for environment-specific issues (connection strings, file paths)

**Issue**: Code coverage upload fails
**Solution**: Verify coverage files are generated in expected location

## Migration from Azure Pipelines

This project previously used Azure DevOps Pipelines. Key differences:

| Feature | Azure Pipelines | GitHub Actions |
|---------|----------------|----------------|
| **Configuration** | `azure-pipelines.yml` | `.github/workflows/*.yml` |
| **Stages** | Multi-stage YAML | Multiple jobs/workflows |
| **Artifacts** | Pipeline artifacts | Workflow artifacts |
| **Triggers** | Branch/path triggers | Same, but in workflow files |
| **Secrets** | Pipeline variables | Repository secrets |
| **Status** | Azure DevOps UI | GitHub Actions tab |

### What Changed
- ✅ Split monolithic pipeline into focused workflows
- ✅ Better path filtering for each component
- ✅ Native GitHub integration (no external service)
- ✅ Free for public repositories
- ✅ Same parallel execution strategy
- ✅ Same artifact retention

### What Stayed the Same
- Same build commands (`dotnet build`, `npm run build`)
- Same test commands
- Same artifact outputs
- Same code quality checks

## Best Practices

1. **Keep workflows focused**: Separate workflows for API, frontend, functions
2. **Use path filters**: Only run workflows when relevant files change
3. **Cache dependencies**: Use `actions/cache` for npm, NuGet
4. **Parallel execution**: Run independent jobs simultaneously
5. **Fail fast**: Don't continue if critical steps fail
6. **Upload artifacts**: Always preserve test results and coverage
7. **Meaningful job names**: Use descriptive names for clarity
8. **Continue on error**: Use for quality checks that shouldn't block builds

## Monitoring

### Check Workflow Status
```bash
# Via GitHub CLI
gh run list
gh run view <run-id>
gh run watch

# Or visit
https://github.com/YOUR_USERNAME/allowance/actions
```

### Notifications
Configure in GitHub Settings → Notifications:
- Email on workflow failures
- Slack/Discord webhooks for team notifications

## Cost Optimization

GitHub Actions is free for public repositories, with limits for private repos:

- **Free tier**: 2,000 minutes/month for private repos
- **Optimization tips**:
  - Use path filters to avoid unnecessary runs
  - Cache dependencies (npm, NuGet)
  - Use matrix builds sparingly
  - Set appropriate artifact retention periods

## Further Reading

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Workflow syntax](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions)
- [Using artifacts](https://docs.github.com/en/actions/using-workflows/storing-workflow-data-as-artifacts)
- [Caching dependencies](https://docs.github.com/en/actions/using-workflows/caching-dependencies-to-speed-up-workflows)

## Support

For issues with GitHub Actions:
1. Check workflow logs in Actions tab
2. Review [GitHub Actions status](https://www.githubstatus.com/)
3. Search [GitHub Community](https://github.community/)
4. File issue in this repository
