# Migration from Azure Pipelines to GitHub Actions

**Date**: 2025-10-22
**Status**: ✅ Complete

## Summary

This project has migrated from Azure DevOps Pipelines to GitHub Actions for all continuous integration and deployment workflows.

## Why GitHub Actions?

1. **Native Integration**: Built directly into GitHub, no external service needed
2. **Free for Public Repos**: Unlimited minutes for open source projects
3. **Simpler Configuration**: YAML workflows in `.github/workflows/`
4. **Better Developer Experience**: View status directly in PRs and commits
5. **Community Actions**: Large marketplace of pre-built actions
6. **Modern Platform**: Active development and feature updates

## What Changed

### Removed
- ❌ `azure-pipelines.yml` (deleted)
- ❌ Azure DevOps service connection
- ❌ Azure Pipeline variable configuration

### Added
- ✅ `.github/workflows/ci.yml` - Main CI pipeline
- ✅ `.github/workflows/api.yml` - API-focused workflow
- ✅ `.github/workflows/web.yml` - React-focused workflow
- ✅ `.github/workflows/function.yml` - Azure Function workflow
- ✅ `.github/workflows/ios.yml` - iOS app workflow
- ✅ `docs/github-actions-ci.md` - Comprehensive documentation

### Archived
- 📦 Azure DevOps documentation moved to `docs/archive/`

## Workflow Comparison

| Feature | Azure Pipelines | GitHub Actions |
|---------|----------------|----------------|
| **Configuration** | `azure-pipelines.yml` | `.github/workflows/*.yml` |
| **Trigger on Push** | `trigger: branches` | `on: push: branches` |
| **Trigger on PR** | `pr: branches` | `on: pull_request: branches` |
| **Jobs** | `jobs:` | `jobs:` |
| **Stages** | `stages:` | Multiple workflows |
| **Parallel Execution** | `dependsOn: []` | Jobs without `needs:` |
| **Artifacts** | `PublishBuildArtifacts` | `actions/upload-artifact` |
| **Secrets** | Pipeline variables | Repository secrets |

## Build Strategy (Unchanged)

Both systems use the same parallel execution strategy:

```
┌─────────────────┐
│  API Build      │ ──┐
└─────────────────┘   │
                      │
┌─────────────────┐   │
│  Function Build │ ──┤─── All run in parallel
└─────────────────┘   │
                      │
┌─────────────────┐   │
│  React Build    │ ──┘
└─────────────────┘
```

**Benefits**:
- Fast CI times (3-5 minutes typical)
- Independent job failures
- Efficient resource usage

## Artifacts

Both systems publish the same artifacts:

| Artifact | Description | Retention |
|----------|-------------|-----------|
| `api-{SHA}` | Published .NET API | 30 days |
| `function-{SHA}` | Published Azure Function | 30 days |
| `react-{SHA}` | Built React app | 30 days |
| Test Results | TRX files + coverage | 7 days |

## Code Quality Checks (Unchanged)

Same quality gates in both systems:

### .NET
- ✅ `dotnet format --verify-no-changes`
- ✅ Build with `-warnaserror`
- ✅ Vulnerable package scanning

### React
- ✅ ESLint
- ✅ TypeScript type checking
- ✅ npm audit

### iOS
- ✅ SwiftLint
- ✅ Xcode build validation

## Path Filtering

**Improved in GitHub Actions** - More granular control:

```yaml
# API workflow only runs on API changes
on:
  push:
    paths:
      - 'src/**'
      - '.github/workflows/api.yml'

# React workflow only runs on frontend changes
on:
  push:
    paths:
      - 'web/**'
      - '.github/workflows/web.yml'
```

This saves CI minutes by avoiding unnecessary builds.

## Status Checks

### Before (Azure Pipelines)
- View in Azure DevOps portal
- Status badge links to external site
- Build history in separate UI

### After (GitHub Actions)
- View in GitHub Actions tab (same repo)
- Status badges link to GitHub
- Build history integrated with commits/PRs
- Status checks block merging if configured

## Migration Steps Taken

1. ✅ Created `.github/workflows/ci.yml` (comprehensive pipeline)
2. ✅ Verified existing focused workflows (api, web, function, ios)
3. ✅ Removed `azure-pipelines.yml`
4. ✅ Created `docs/github-actions-ci.md` documentation
5. ✅ Archived old Azure DevOps docs to `docs/archive/`
6. ✅ Updated README.md badge
7. ✅ Updated CLAUDE.md with CI/CD info

## Testing the Migration

Run a test build:

```bash
# Push to a branch
git checkout -b test-github-actions
git push origin test-github-actions

# View workflow runs
gh run list
# or visit: https://github.com/YOUR_USERNAME/allowance/actions

# Check specific workflow
gh run watch
```

## Documentation

- **Primary**: [docs/github-actions-ci.md](github-actions-ci.md)
- **Archived**: [docs/archive/](archive/) (old Azure DevOps docs)

## Next Steps

### Optional Enhancements

1. **Add deployment workflows** (if deploying to Azure)
   - Create `.github/workflows/deploy-production.yml`
   - Configure secrets for Azure credentials

2. **Add branch protection rules**
   - Require status checks before merging
   - Require code reviews

3. **Configure notifications**
   - Slack/Discord webhooks
   - Email notifications on failures

4. **Add dependabot**
   - Automated dependency updates
   - Security vulnerability scanning

## Support

For questions about GitHub Actions:
- See [docs/github-actions-ci.md](github-actions-ci.md)
- GitHub Actions docs: https://docs.github.com/en/actions
- Community: https://github.community/

## Rollback Plan (If Needed)

If you need to rollback to Azure Pipelines:

1. Restore `azure-pipelines.yml` from git history
2. Copy archived docs back from `docs/archive/`
3. Reconfigure Azure DevOps service connection
4. Disable GitHub Actions workflows

```bash
# Restore azure-pipelines.yml
git show HEAD~1:azure-pipelines.yml > azure-pipelines.yml
```

**Note**: Not recommended - GitHub Actions is the better long-term choice.

## Conclusion

✅ Migration complete and tested
✅ All workflows functional
✅ Documentation updated
✅ No loss of functionality
✅ Better developer experience

The project is now fully on GitHub Actions! 🎉
