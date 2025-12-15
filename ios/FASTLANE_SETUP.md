# iOS Fastlane Setup Guide

This guide walks through the one-time setup required for iOS TestFlight deployments using Fastlane.

## Prerequisites

- Apple Developer account with App Store Connect access
- GitHub repository access for storing certificates (private repo recommended)
- Xcode 16+ installed locally

## Step 1: Create App Store Connect API Key

1. Go to [App Store Connect](https://appstoreconnect.apple.com/)
2. Navigate to **Users and Access** → **Keys** tab → **App Store Connect API**
3. Click the **+** button to create a new key
4. Enter a name (e.g., "Fastlane CI")
5. Select **Admin** access (or App Manager for more restricted access)
6. Click **Generate**
7. **Download the .p8 file immediately** - you can only download it once!
8. Note down:
   - **Key ID**: Displayed in the keys list
   - **Issuer ID**: Displayed at the top of the keys page

## Step 2: Get Your Apple Team ID

1. Go to [Apple Developer Portal](https://developer.apple.com/account)
2. Your **Team ID** is displayed in the top-right corner, or under **Membership**
3. It's a 10-character alphanumeric string (e.g., "ABC123XYZ9")

## Step 3: Create Certificates Repository

Fastlane Match stores your certificates and profiles in a git repository.

1. Create a **private** repository on GitHub:
   - Name: `certificates` (or similar)
   - Visibility: **Private**
   - Example: `github.com/brycehammond/certificates`

2. The repository URL is already configured in `ios/fastlane/Matchfile`:
   ```ruby
   git_url("https://github.com/brycehammond/certificates.git")
   ```

## Step 4: Initialize Match (First Time Only)

Run this locally to create your certificates and profiles:

```bash
cd ios

# Create your local .env file
cp fastlane/.env.default fastlane/.env

# Edit .env with your values:
# - APP_STORE_CONNECT_API_KEY_ID=your_key_id
# - APP_STORE_CONNECT_API_ISSUER_ID=your_issuer_id
# - MATCH_PASSWORD=choose_a_strong_password (this encrypts your certs)

# Copy your API key to fastlane directory
cp /path/to/AuthKey_XXXXXX.p8 fastlane/AuthKey.p8

# Initialize Match and create certificates
bundle exec fastlane match appstore

# This will:
# 1. Create a distribution certificate
# 2. Create an App Store provisioning profile
# 3. Encrypt and push them to your certificates repo
```

## Step 5: Register App on App Store Connect

If you haven't registered the app yet:

```bash
cd ios
bundle exec fastlane create_app
```

Or manually create it in App Store Connect:
1. Go to **Apps** → **+** → **New App**
2. Platform: iOS
3. Name: Earn & Learn
4. Primary Language: English
5. Bundle ID: `com.fludivisiondesign.earnandlearn`
6. SKU: `earnandlearn001`

## Step 6: Configure GitHub Secrets

Add these secrets to your GitHub repository:

Go to **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `APP_STORE_CONNECT_API_KEY` | Base64 encoded .p8 file | `base64 -i AuthKey.p8` |
| `APP_STORE_CONNECT_API_KEY_ID` | Key ID from Step 1 | App Store Connect Keys page |
| `APP_STORE_CONNECT_API_ISSUER_ID` | Issuer ID from Step 1 | App Store Connect Keys page |
| `APPLE_TEAM_ID` | Your Team ID | Step 2 |
| `MATCH_PASSWORD` | Password for cert encryption | Same as used in Step 4 |
| `MATCH_GIT_BASIC_AUTHORIZATION` | Base64 PAT for git | See below |

### Creating MATCH_GIT_BASIC_AUTHORIZATION

1. Create a GitHub Personal Access Token (PAT):
   - Go to GitHub → **Settings** → **Developer settings** → **Personal access tokens** → **Fine-grained tokens**
   - Name: "Fastlane Match"
   - Repository access: Select your certificates repo
   - Permissions: **Contents** → Read and write
   - Generate and copy the token

2. Create the base64-encoded auth string:
   ```bash
   echo -n "your-github-username:your-pat-token" | base64
   ```

3. Add this as the `MATCH_GIT_BASIC_AUTHORIZATION` secret

## Step 7: Test the Setup

### Local Test
```bash
cd ios

# Build for TestFlight
bundle exec fastlane build_release

# Full deploy to TestFlight
bundle exec fastlane beta
```

### GitHub Actions Test
1. Go to **Actions** → **iOS TestFlight Deploy**
2. Click **Run workflow**
3. Select branch and whether to distribute externally
4. Click **Run workflow**

Or push a tag:
```bash
git tag ios-v1.0.0
git push origin ios-v1.0.0
```

## Troubleshooting

### "No signing certificate found"
Run `bundle exec fastlane match appstore --force` to regenerate certificates.

### "Bundle identifier doesn't match"
Ensure the bundle ID in `project.yml` matches what's in App Store Connect and Match.

### "Invalid API key"
- Verify the .p8 file was base64 encoded correctly
- Check that the Key ID and Issuer ID are correct
- Ensure the API key has Admin access

### Match can't clone repository
- Verify `MATCH_GIT_BASIC_AUTHORIZATION` is correctly base64 encoded
- Check the PAT has read/write access to the certificates repo

## Available Fastlane Lanes

```bash
# Sync certificates
bundle exec fastlane sync_certs          # Both dev and appstore
bundle exec fastlane sync_dev_certs      # Development only
bundle exec fastlane sync_appstore_certs # App Store only

# Build
bundle exec fastlane build_dev           # Development build
bundle exec fastlane build_release       # Release build

# Deploy
bundle exec fastlane beta                # Upload to TestFlight (internal)
bundle exec fastlane beta_external       # TestFlight + external testers

# Utilities
bundle exec fastlane test                # Run tests
bundle exec fastlane bump_version        # Increment version
bundle exec fastlane create_app          # Register app on ASC
```

## File Structure

```
ios/
├── Gemfile                 # Ruby dependencies
├── fastlane/
│   ├── Appfile            # App metadata
│   ├── Fastfile           # Build/deploy lanes
│   ├── Matchfile          # Certificate config
│   ├── .env.default       # Environment template
│   ├── .env               # Your local config (gitignored)
│   └── AuthKey.p8         # API key (gitignored)
└── AllowanceTracker/
    └── project.yml        # XcodeGen config with signing
```
