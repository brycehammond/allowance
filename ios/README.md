# Allowance Tracker - iOS App

Native iOS application built with SwiftUI for iOS 17.0+

## Project Structure

```
ios/AllowanceTracker/
├── App/
│   ├── AllowanceTrackerApp.swift
│   └── AppState.swift
├── Models/
│   ├── User.swift ✅
│   ├── Child.swift
│   ├── Transaction.swift
│   ├── WishListItem.swift
│   └── Analytics/
│       ├── BalancePoint.swift
│       ├── IncomeSpending.swift
│       └── MonthlyComparison.swift
├── Services/
│   ├── Network/
│   │   ├── APIService.swift
│   │   ├── Endpoints.swift
│   │   └── APIError.swift
│   ├── KeychainService.swift
│   └── CacheService.swift
├── ViewModels/
│   ├── AuthViewModel.swift
│   ├── DashboardViewModel.swift
│   ├── TransactionViewModel.swift
│   ├── WishListViewModel.swift
│   └── AnalyticsViewModel.swift
├── Views/
│   ├── Auth/
│   │   ├── LoginView.swift
│   │   └── RegisterView.swift
│   ├── Dashboard/
│   │   ├── DashboardView.swift
│   │   ├── ChildCardView.swift
│   │   └── QuickTransactionSheet.swift
│   ├── Transactions/
│   │   ├── TransactionListView.swift
│   │   └── TransactionRow.swift
│   ├── WishList/
│   │   ├── WishListView.swift
│   │   └── WishListItemRow.swift
│   └── Analytics/
│       └── AnalyticsView.swift
└── Utilities/
    ├── Constants.swift ✅
    └── Extensions.swift ✅
```

## Implementation Status

### ✅ Completed
- Project structure
- Constants and utilities
- User model with authentication DTOs

### 🚧 In Progress
- Remaining models
- Service layer
- ViewModels
- Views

## Next Steps

The iOS app foundation is set up. To complete the implementation:

1. **Models**: Create Child, Transaction, WishListItem, and Analytics models
2. **Services**: Implement APIService, KeychainService, and Endpoints
3. **ViewModels**: Build MVVM layer for all features
4. **Views**: Create SwiftUI views for auth, dashboard, transactions, wishlist, and analytics
5. **SignalR**: Add real-time updates support
6. **Testing**: Unit and UI tests
7. **App Store**: Privacy manifest, icons, and submission

## API Integration

The iOS app connects to the .NET backend at:
- **Development**: `http://localhost:5000`
- **Production**: TBD

All API endpoints from `/api/v1/*` are available and tested.

## Running the App

1. Open `AllowanceTracker.xcodeproj` in Xcode
2. Ensure backend is running on `localhost:5000`
3. Build and run on iOS Simulator (iOS 17.0+)

## Dependencies

Required Swift Packages:
- SignalR-Swift for real-time updates

Add via Xcode: File → Add Package Dependencies
