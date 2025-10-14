# iOS App Configuration Guide

## API Base URL Configuration

The iOS app reads the API base URL from `App/Info.plist`. This allows you to easily switch between different environments without changing code.

### Current Configuration

The API base URL is set in `App/Info.plist`:

```xml
<key>API_BASE_URL</key>
<string>https://api.allowancetracker.com</string>
```

### Changing Environments

#### Option 1: Edit Info.plist Directly

Open `App/Info.plist` and change the `API_BASE_URL` value:

**Development (Local):**
```xml
<key>API_BASE_URL</key>
<string>http://localhost:5000</string>
```

**Staging:**
```xml
<key>API_BASE_URL</key>
<string>https://staging-api.allowancetracker.com</string>
```

**Production:**
```xml
<key>API_BASE_URL</key>
<string>https://api.allowancetracker.com</string>
```

#### Option 2: Use Xcode Build Configurations (Recommended)

For a more robust setup, create separate build configurations:

1. **Open Xcode Project Settings**
   - Select the project in the navigator
   - Go to Info tab
   - Duplicate the existing configurations

2. **Create Configurations:**
   - Debug → Keep for local development
   - Staging → New configuration for staging
   - Release → Use for production

3. **Create Multiple Info.plist Files:**
   ```
   App/
   ├── Info.plist (Production)
   ├── Info-Staging.plist
   └── Info-Development.plist
   ```

4. **Set Info.plist per Configuration:**
   - Select target → Build Settings
   - Search for "Info.plist File"
   - Set different plist for each configuration

### Using Configuration in Code

The app automatically reads the API base URL from Info.plist via the `Configuration` helper:

```swift
// API base URL is automatically loaded
let apiService = APIService()

// Access configuration values
Configuration.apiBaseURL      // Current API URL
Configuration.environment     // Auto-detected environment
Configuration.appVersion      // App version from Info.plist
Configuration.buildNumber     // Build number
```

### Environment Detection

The app automatically detects the environment based on the API URL:

- **Development**: URLs containing `localhost` or `127.0.0.1`
- **Staging**: URLs containing `staging` or `dev`
- **Production**: All other URLs

### Debug Information

In **DEBUG builds**, the About screen displays:
- ✅ App version and build number
- ✅ Current environment (Development/Staging/Production)
- ✅ API URL hostname

This information is **hidden in Release builds** for security.

### Common API URLs

| Environment | URL | Notes |
|-------------|-----|-------|
| Local (.NET) | `http://localhost:5001` | Use `dotnet run` |
| Azure Staging | `https://allowancetracker-staging.azurewebsites.net` | Azure staging slot |
| Azure Production | `https://api.allowancetracker.com` | Production |

### Testing Different Environments

```swift
// In Debug builds, print configuration on app launch
#if DEBUG
Configuration.printConfiguration()
#endif
```

This prints:
```
╔══════════════════════════════════════════════════════════╗
║               ALLOWANCE TRACKER CONFIG                   ║
╠══════════════════════════════════════════════════════════╣
║ App Name:         Allowance Tracker                      ║
║ Version:          1.0.0 (1)                              ║
║ Bundle ID:        com.allowancetracker.app               ║
║ Environment:      Development                            ║
║ API Base URL:     http://localhost:5000                  ║
╚══════════════════════════════════════════════════════════╝
```

### Network Security

The app enforces HTTPS by default (via App Transport Security):

```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <false/>
</dict>
```

**To allow HTTP for local development:**

Add this to Info.plist temporarily:

```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsLocalNetworking</key>
    <true/>
</dict>
```

⚠️ **Never ship to App Store with `NSAllowsArbitraryLoads` set to `true`!**

### Quick Start

**For Local Development:**

1. Start your .NET backend:
   ```bash
   cd /path/to/AllowanceTracker
   dotnet run
   ```

2. Update Info.plist:
   ```xml
   <key>API_BASE_URL</key>
   <string>http://localhost:5001</string>
   ```

3. Build and run in Xcode (⌘+R)

**For Production Testing:**

1. Deploy backend to Azure

2. Update Info.plist with production URL:
   ```xml
   <key>API_BASE_URL</key>
   <string>https://api.allowancetracker.com</string>
   ```

3. Build and run

### Troubleshooting

**Problem: "API_BASE_URL not configured in Info.plist" crash**

- **Solution**: Ensure `API_BASE_URL` key exists in `App/Info.plist`

**Problem: Network requests fail with "Invalid Response"**

- **Solution**: Check API URL is correct and backend is running
- **Check**: Run `curl https://your-api-url/health` to verify backend

**Problem: Can't connect to localhost**

- **Solution**: Add `NSAllowsLocalNetworking` to Info.plist (see Network Security section)
- **Note**: Simulator and Mac share localhost, so `localhost:5001` works

**Problem: Certificate errors on staging**

- **Solution**: Ensure staging uses valid SSL certificate
- **Workaround**: Use Azure App Service (provides free SSL)

### Best Practices

1. ✅ **Never commit credentials** to Info.plist
2. ✅ **Use HTTPS in production**
3. ✅ **Create separate build configurations** for dev/staging/prod
4. ✅ **Test on device, not just simulator** (networking behaves differently)
5. ✅ **Use ngrok** for mobile device testing with local backend

### Related Files

- `App/Info.plist` - Configuration file
- `Utilities/Configuration.swift` - Configuration helper
- `Services/Network/APIService.swift` - API client

---

**Last Updated**: October 11, 2025
