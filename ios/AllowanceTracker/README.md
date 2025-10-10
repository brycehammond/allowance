# AllowanceTracker iOS App

## Project Setup

This iOS application follows the specifications in `specs/08-ios-app-specification.md`.

### Creating the Xcode Project

Since Keychain and other iOS services require a proper Xcode project with entitlements, you'll need to create an Xcode project:

1. Open Xcode
2. Create a new project: **File → New → Project**
3. Select **iOS → App**
4. Configure:
   - Product Name: `AllowanceTracker`
   - Team: Select your team
   - Organization Identifier: `com.allowancetracker` (or your domain)
   - Interface: **SwiftUI**
   - Language: **Swift**
   - Storage: **None** (we'll use manual setup)
5. Save to: `/Users/bryce/Dev/personal/allowance/ios/`
6. **Important**: Replace the default files with the files in this directory structure

### Adding Test Target

1. In Xcode, select **File → New → Target**
2. Select **iOS → Unit Testing Bundle**
3. Name: `AllowanceTrackerTests`
4. Add the test files from `Tests/` directory

### Required Capabilities

Enable in **Signing & Capabilities**:
- Keychain Sharing (for KeychainService)

### Running Tests

```bash
# From terminal
xcodebuild test -scheme AllowanceTracker -destination 'platform=iOS Simulator,name=iPhone 15'

# Or use Xcode
# Cmd + U
```

## Current Implementation Status

### Phase 1: Foundation ✅ COMPLETE (Code Ready, Needs Xcode Project for Testing)

#### Models ✅
- [x] User.swift - User authentication models (AuthResponse, LoginRequest, RegisterRequest)
- [x] Child.swift - Child profile model
- [x] Decimal+Extensions.swift - Currency formatting utilities
- [x] Color+Extensions.swift - Theme and color utilities

#### Services ✅
- [x] KeychainService.swift - Secure token storage with protocol support
- [x] KeychainServiceProtocol.swift - Testable keychain abstraction
- [x] APIService.swift - Complete network layer with auth
- [x] APIServiceProtocol.swift - Testable API abstraction
- [x] URLSessionProtocol.swift - Testable networking
- [x] APIError.swift - Comprehensive error handling

#### ViewModels ✅
- [x] AuthViewModel.swift - Authentication state management with validation

#### Views ✅
- [x] LoginView.swift - User authentication UI
- [x] RegisterView.swift - User registration UI with role selection
- [x] ContentView.swift - Root view with authentication routing
- [x] DashboardPlaceholderView - Temporary dashboard (Phase 2)

#### App Configuration ✅
- [x] AllowanceTrackerApp.swift - App entry point
- [x] AppConstants.swift - Application-wide constants

#### Tests Written ✅
- [x] UserModelTests.swift (6 tests)
- [x] ChildModelTests.swift (4 tests)
- [x] KeychainServiceTests.swift (8 tests)
- [x] APIServiceTests.swift (11 tests)
- [x] AuthViewModelTests.swift (13 tests)

**Total: 42 comprehensive unit tests covering all core functionality**

**Note**: Tests cannot run until Xcode project is created with proper entitlements.

## Development Workflow

We follow strict **Test-Driven Development (TDD)**:

1. **RED**: Write failing test first
2. **GREEN**: Implement minimum code to pass
3. **REFACTOR**: Improve code quality
4. **REPEAT**: Next feature

## Architecture

- **Pattern**: MVVM (Model-View-ViewModel)
- **UI Framework**: SwiftUI
- **Minimum iOS**: 17.0
- **Language**: Swift 5.9+

## Next Steps

1. Create Xcode project using instructions above
2. Run existing tests to verify GREEN phase
3. Continue with APIService implementation (next TDD cycle)
