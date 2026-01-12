# Firebase Push Notifications Setup Guide

This guide explains how to configure Firebase Cloud Messaging (FCM) for push notifications in the AllowanceTracker iOS app.

## Prerequisites

1. Apple Developer Account with push notification capability
2. Firebase account (free tier works)
3. Xcode 15+ installed

## Step 1: Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com)
2. Click "Add Project" or select an existing project
3. Follow the setup wizard (you can disable Google Analytics if not needed)

## Step 2: Add iOS App to Firebase

1. In Firebase Console, click "Add app" and select iOS
2. Enter your iOS bundle ID: `com.yourcompany.AllowanceTracker`
3. Download the `GoogleService-Info.plist` file
4. Add it to your Xcode project root (same level as `AllowanceTrackerApp.swift`)

## Step 3: Add Firebase SDK via Swift Package Manager

1. Open your project in Xcode
2. Go to File > Add Package Dependencies
3. Enter: `https://github.com/firebase/firebase-ios-sdk`
4. Select these packages:
   - `FirebaseCore`
   - `FirebaseMessaging`
5. Add them to the `AllowanceTracker` target

## Step 4: Enable Push Notifications Capability

1. Select your project in Xcode
2. Go to "Signing & Capabilities"
3. Click "+ Capability"
4. Add "Push Notifications"
5. Add "Background Modes" and check:
   - Remote notifications
   - Background fetch

## Step 5: Configure APNs in Firebase

### Option A: APNs Authentication Key (Recommended)

1. Go to [Apple Developer Portal](https://developer.apple.com)
2. Navigate to Certificates, Identifiers & Profiles > Keys
3. Create a new key with "Apple Push Notifications service (APNs)" enabled
4. Download the `.p8` file (save it securely - you can only download once!)
5. Note the Key ID and your Team ID
6. In Firebase Console > Project Settings > Cloud Messaging:
   - Upload the APNs Authentication Key (.p8 file)
   - Enter Key ID and Team ID

### Option B: APNs Certificate

1. Create an APNs certificate in Apple Developer Portal
2. Export as `.p12` file from Keychain Access
3. Upload to Firebase Console > Cloud Messaging

## Step 6: Enable Firebase in Code

Add the `FIREBASE_ENABLED` compilation flag:

1. In Xcode, select your target
2. Go to Build Settings
3. Search for "Swift Compiler - Custom Flags"
4. Add `-DFIREBASE_ENABLED` to "Other Swift Flags" for both Debug and Release

## Step 7: Configure Backend

Ensure your backend API has Firebase Admin SDK configured:

1. In Firebase Console > Project Settings > Service Accounts
2. Click "Generate New Private Key"
3. Download the JSON file
4. Configure in your backend:

```json
{
  "Firebase": {
    "CredentialPath": "path/to/firebase-credentials.json"
  }
}
```

Or via environment variables:
```
FIREBASE__CREDENTIALJSON={"type":"service_account",...}
```

## Testing Push Notifications

### Using Firebase Console

1. Go to Firebase Console > Engage > Messaging
2. Click "Create your first campaign"
3. Select "Firebase Notification messages"
4. Enter a title and message
5. Select your iOS app
6. Send a test message to your device token

### Using the API

```bash
curl -X POST https://your-api.com/api/v1/notifications \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "type": 1,
    "title": "Test Notification",
    "body": "This is a test push notification"
  }'
```

## Troubleshooting

### Notifications not received

1. Check device is registered: Look for "FCM token received" in console logs
2. Verify APNs key/certificate is correctly uploaded in Firebase
3. Ensure Push Notifications capability is enabled
4. Check notification permissions in iOS Settings
5. Verify backend Firebase configuration

### Token registration fails

1. Check network connectivity
2. Verify `GoogleService-Info.plist` is in the correct location
3. Ensure bundle ID matches Firebase configuration

### Silent Notifications

For background updates, the payload must include `content-available: 1`:

```json
{
  "aps": {
    "content-available": 1
  },
  "data": {
    "type": "background_refresh"
  }
}
```

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     iOS App                                  │
├─────────────────────────────────────────────────────────────┤
│  AllowanceTrackerApp                                        │
│    └── AppDelegate (UIApplicationDelegateAdaptor)           │
│          ├── Firebase.configure()                           │
│          ├── UNUserNotificationCenter.delegate              │
│          └── Messaging.delegate                             │
│                                                              │
│  PushNotificationService (Singleton)                        │
│    ├── requestAuthorization()                               │
│    ├── didRegisterForRemoteNotifications()                  │
│    ├── setFCMToken()                                        │
│    └── registerDeviceToken() → API                          │
│                                                              │
│  NotificationViewModel                                       │
│    ├── loadNotifications()                                  │
│    ├── requestPushNotificationPermission()                  │
│    └── registerDeviceToken()                                │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                     Backend API                              │
├─────────────────────────────────────────────────────────────┤
│  POST /api/v1/devices                                       │
│    → Stores FCM token for user                              │
│                                                              │
│  FirebasePushService                                        │
│    → Sends notifications via FCM Admin SDK                  │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                Firebase Cloud Messaging                      │
│                         │                                    │
│                         ▼                                    │
│                    APNs (Apple)                              │
│                         │                                    │
│                         ▼                                    │
│                   iOS Device                                 │
└─────────────────────────────────────────────────────────────┘
```

## Files Modified/Added

- `App/AppDelegate.swift` - Firebase configuration and notification handling
- `App/AllowanceTrackerApp.swift` - UIApplicationDelegateAdaptor integration
- `Services/PushNotificationService.swift` - Push notification management
- `ViewModels/NotificationViewModel.swift` - Added device registration methods
- `Views/Notifications/NotificationSettingsView.swift` - Settings UI

## Without Firebase (APNs Only)

If you don't want to use Firebase, the app will fall back to using APNs tokens directly. The backend should be configured to send notifications via APNs instead of FCM. Simply don't add the `FIREBASE_ENABLED` flag and don't include `GoogleService-Info.plist`.
