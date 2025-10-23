# GitHub Actions Quick Start Guide

**TL;DR**: Push to GitHub and your builds will work immediately. No setup required! 🎉

## ✅ What Works Right Now (No Configuration)

Your GitHub Actions workflows are already configured and will automatically:

1. **Build everything** when you push to `main` or `develop`:
   - .NET API (with tests)
   - Azure Functions
   - React frontend
   - iOS app (when iOS files change)

2. **Run quality checks**:
   - Code formatting (dotnet format, ESLint)
   - Type checking (TypeScript)
   - Security scanning (vulnerable packages, npm audit)
   - Code style (SwiftLint for iOS)

3. **Publish artifacts** (for 30 days):
   - Compiled API
   - Compiled Azure Function
   - Built React app

**No secrets. No variables. Just push and go!** ✨

## 🚀 Getting Started

```bash
# That's it! Just push your code
git push origin main

# Watch the builds
gh run watch
# or visit: https://github.com/YOUR_USERNAME/allowance/actions
```

## 📊 View Build Status

### In GitHub UI
1. Go to your repository
2. Click **Actions** tab
3. See all workflow runs, logs, and artifacts

### In Pull Requests
- Status checks show directly in PR
- Red ❌ = build failed
- Green ✅ = build passed

### Via GitHub CLI
```bash
# List recent runs
gh run list

# Watch current run
gh run watch

# View specific run
gh run view <run-id>
```

## 🔧 Workflows Explained

| Workflow | Triggers On | What It Does |
|----------|-------------|--------------|
| **CI Pipeline** (`ci.yml`) | Push to main/develop | Builds everything in parallel |
| **API CI** (`api.yml`) | Changes to `src/**` | Builds and tests API only |
| **Web CI** (`web.yml`) | Changes to `web/**` | Builds React frontend only |
| **Function CI** (`function.yml`) | Changes to functions | Builds Azure Function only |
| **iOS CI** (`ios.yml`) | Changes to `ios/**` | Builds iOS app on macOS |

**Smart path filtering** = Only runs when relevant files change!

## 📦 Artifacts

After each successful build, download artifacts:

1. Go to Actions → Select workflow run
2. Scroll to **Artifacts** section
3. Download:
   - `api-{SHA}` - Ready to deploy API
   - `function-{SHA}` - Ready to deploy Function
   - `react-{SHA}` - Built React app (dist folder)

**Retention**: 30 days for builds, 7 days for test results

## 🎯 Common Tasks

### Check Build Status
```bash
# Via GitHub CLI
gh run list --limit 5

# Via web
# Visit: https://github.com/YOUR_USERNAME/allowance/actions
```

### Re-run Failed Build
```bash
# Via GitHub CLI
gh run rerun <run-id>

# Via web
# Actions → Failed run → Re-run jobs
```

### Download Artifacts
```bash
# Via GitHub CLI
gh run download <run-id>

# Via web
# Actions → Workflow run → Artifacts section
```

### View Test Results
```bash
# Download test artifacts
gh run download <run-id> -n api-test-results-SHA

# Open TRX files with Visual Studio or convert to HTML
```

## ⚙️ Optional Configuration

### Want Production API URL in React Builds?

Add a secret:
1. Settings → Secrets and variables → Actions
2. New repository secret: `VITE_API_URL`
3. Value: `https://your-api-url.com`

See [docs/GITHUB-SECRETS-SETUP.md](docs/GITHUB-SECRETS-SETUP.md)

### Want Automated Azure Deployment?

Create deployment workflows (not included by default):
- See [docs/GITHUB-SECRETS-SETUP.md](docs/GITHUB-SECRETS-SETUP.md)
- Add Azure credentials as secrets
- Create `.github/workflows/deploy-*.yml` files

## 🛠️ Customizing Workflows

All workflows are in `.github/workflows/`:

```bash
.github/workflows/
├── ci.yml         # Main comprehensive CI pipeline
├── api.yml        # API-focused (runs on src/** changes)
├── web.yml        # React-focused (runs on web/** changes)
├── function.yml   # Function-focused (runs on function changes)
└── ios.yml        # iOS-focused (runs on ios/** changes)
```

Edit any file to customize triggers, steps, or jobs.

## 🔍 Troubleshooting

### Build Failing?

1. **Check workflow logs**:
   - Actions → Failed run → Click on job → Expand failed step

2. **Common issues**:
   - Syntax errors in code
   - Missing dependencies in `package.json` or `.csproj`
   - Test failures
   - Code formatting violations

3. **Fix locally first**:
   ```bash
   # .NET
   dotnet build
   dotnet test
   dotnet format

   # React
   cd web
   npm run lint
   npm run type-check
   npm run build
   ```

### No Workflows Running?

- Check you pushed to `main` or `develop` branch
- Check path filters (workflows only run on relevant file changes)
- Check workflow YAML syntax is valid

### Artifacts Not Uploading?

- Check workflow completed successfully
- Artifacts only kept for 30 days (test results 7 days)
- Check artifact name in download command matches upload

## 📖 Documentation

| Document | Purpose |
|----------|---------|
| [docs/github-actions-ci.md](docs/github-actions-ci.md) | Complete CI/CD documentation |
| [docs/GITHUB-SECRETS-SETUP.md](docs/GITHUB-SECRETS-SETUP.md) | Secrets and environment variables |
| [docs/MIGRATION-TO-GITHUB-ACTIONS.md](docs/MIGRATION-TO-GITHUB-ACTIONS.md) | Migration from Azure Pipelines |

## 🎉 You're Ready!

The workflows are configured and ready to use. Just:

1. Write code
2. Commit changes
3. Push to GitHub
4. Watch builds run automatically

No secrets needed. No configuration required. It just works! ✨

---

**Questions?** See [docs/github-actions-ci.md](docs/github-actions-ci.md) for detailed documentation.
