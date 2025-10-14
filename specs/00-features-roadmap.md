# Allowance Tracker - Features Roadmap

## Overview

This document provides a comprehensive roadmap for all planned features beyond the MVP. Each feature is designed to enhance the financial education experience for families while maintaining the TDD approach and clean architecture established in the core application.

## Current Status (MVP Complete ✅)

### Phase 1-4: Core Functionality
- ✅ ASP.NET Core 8.0 + Blazor Server
- ✅ PostgreSQL with Entity Framework Core
- ✅ ASP.NET Core Identity + JWT Authentication
- ✅ Transaction Management with Balance Tracking
- ✅ Weekly Allowance with Background Jobs
- ✅ SignalR Real-Time Updates
- ✅ REST API for Mobile Access
- ✅ Basic Blazor UI with Authentication
- ✅ Family & Child Management
- ✅ **94 Tests Passing** (>90% coverage)

### Enhancement 1-4: Essential Features
- ✅ JWT Authentication & API Controllers (15 tests)
- ✅ Advanced Blazor Components (13 tests)
- ✅ Real-Time SignalR Updates
- ✅ Docker & Deployment Configuration
- ✅ CI/CD Pipelines (Azure Pipelines)
- ✅ Complete Authentication Flow
- ✅ Full REST API Coverage

### Enhancement 5: Charts & Analytics (In Progress)
- ✅ Foundation & DTOs created
- ✅ NuGet packages added (ApexCharts, Radzen, QuestPDF, CsvHelper)
- ⏳ TransactionAnalyticsService (12 tests planned)
- ⏳ Chart Components (18 tests planned)
- ⏳ Dashboard Integration

---

## Feature Categories

### 🎓 Financial Education
Features that teach children about money management, savings, and financial responsibility.

### 🎮 Engagement & Motivation
Gamification features that keep children engaged and motivated to save.

### 👨‍👩‍👧‍👦 Family Management
Tools for parents to manage, monitor, and guide their children's financial journey.

### 📊 Analytics & Insights
Data visualization and reporting to understand financial patterns and progress.

### 🔐 Security & Trust
Features that ensure data safety and build trust in the platform.

### 🎨 User Experience
UI/UX improvements for better usability and accessibility.

---

## Priority Matrix

### P0: Essential Post-MVP Features (Next 4 Weeks)
These features significantly enhance the core value proposition.

| Feature | Spec | Category | Effort | Impact | Dependencies |
|---------|------|----------|--------|--------|--------------|
| Complete Charts & Analytics | 11 | 📊 Analytics | M | High | None |
| Transaction Categories | 12 | 🎓 Education | S | High | None |
| Chores/Tasks System | 13 | 🎮 Engagement | L | Very High | Categories |
| Enhanced Savings Goals | 14 | 🎓 Education | M | High | None |

**Total: 1 Small + 2 Medium + 1 Large = ~6 weeks**

### P1: High-Value Features (Weeks 5-12)
Features that significantly improve engagement and retention.

| Feature | Spec | Category | Effort | Impact | Dependencies |
|---------|------|----------|--------|--------|--------------|
| Interest Simulation | 15 | 🎓 Education | M | Medium | None |
| Transaction Approval | 16 | 👨‍👩‍👧‍👦 Family | M | High | Notifications |
| Achievements & Badges | 17 | 🎮 Engagement | L | Very High | Categories, Chores |
| Savings Streaks | 18 | 🎮 Engagement | S | Medium | None |
| Family Overview Dashboard | 19 | 👨‍👩‍👧‍👦 Family | M | High | Charts |
| Reports & Exports | 20 | 📊 Analytics | M | Medium | Charts |

**Total: 1 Small + 4 Medium + 1 Large = ~8 weeks**

### P2: Quality of Life Features (Weeks 13-20)
Important but not critical features.

| Feature | Spec | Category | Effort | Impact | Dependencies |
|---------|------|----------|--------|--------|--------------|
| Smart Notifications | 21 | 🎨 UX | M | Medium | None |
| QR Code Transactions | 22 | 👨‍👩‍👧‍👦 Family | S | Medium | None |
| Photo Attachments | 23 | 📊 Analytics | M | Low | Blob Storage |
| Child-Friendly Dashboard | 24 | 🎨 UX | L | High | Charts |
| Dark Mode | 25 | 🎨 UX | S | Medium | None |
| Transaction Search/Filters | 26 | 🎨 UX | M | Medium | None |

**Total: 2 Small + 4 Medium + 1 Large = ~8 weeks**

### P3: Enterprise & Security Features (Weeks 21-24)
Advanced features for security-conscious families.

| Feature | Spec | Category | Effort | Impact | Dependencies |
|---------|------|----------|--------|--------|--------------|
| Two-Factor Authentication | 27 | 🔐 Security | M | Medium | None |
| Activity Audit Log | 28 | 🔐 Security | S | Low | None |

**Total: 1 Small + 1 Medium = ~2 weeks**

**Effort Key**: S = Small (1-2 weeks), M = Medium (2-3 weeks), L = Large (4-6 weeks)

---

## Detailed Feature Breakdown

### Spec 12: Transaction Categories & Budgeting 📊
**Priority: P0 | Effort: Small (1-2 weeks) | Tests: 25**

**What**: Categorize all transactions (Toys, Snacks, Games, Clothes, Savings, Charity, etc.) and set budget limits per category.

**Value**: Teaches children to budget and track spending patterns.

**Key Components**:
- `TransactionCategory` enum with 10+ categories
- Category-based analytics and pie charts
- Parent-set budget limits per category
- Budget warning notifications
- Category trends over time

**Database Changes**:
- Add `Category` column to `Transaction` table
- New `CategoryBudget` table for limits

---

### Spec 13: Chores/Tasks System 🧹
**Priority: P0 | Effort: Large (4-6 weeks) | Tests: 45**

**What**: Parents create chores with reward amounts. Children complete them and request approval. Upon approval, automatic payment.

**Value**: Teaches work ethic and earning through effort (not just allowance).

**Key Components**:
- `Chore`, `ChoreTemplate` models
- Recurring chores (daily/weekly)
- Approval workflow with notifications
- Chore completion history
- Performance statistics
- Photo proof of completion

**Database Changes**:
- New `Chores` table
- New `ChoreCompletions` table
- New `ChoreTemplates` table for recurring chores

---

### Spec 14: Enhanced Savings Goals with Milestones 🎯
**Priority: P0 | Effort: Medium (2-3 weeks) | Tests: 30**

**What**: Enhance existing WishListItem with progress milestones, auto-save features, and goal tracking.

**Value**: Motivates saving with visual progress and celebrations.

**Key Components**:
- Milestone celebrations (25%, 50%, 75%, 100%)
- Auto-transfer X% of allowance to goal
- Multiple savings "buckets"
- Goal deadline tracking
- Achievement unlocks on goal completion
- Goal history and completed goals gallery

**Database Changes**:
- Add `AutoSavePercentage`, `TargetDate`, `CompletedDate` to `WishListItem`
- New `GoalMilestone` table for milestone events

---

### Spec 15: Interest Simulation 💰
**Priority: P1 | Effort: Medium (2-3 weeks) | Tests: 20**

**What**: Teach compound interest by simulating a savings account with weekly/monthly interest payments.

**Value**: Teaches one of the most powerful financial concepts early.

**Key Components**:
- Parent-configurable interest rate (5-10%)
- Weekly or monthly interest payments
- Interest transaction history
- Visual compound interest chart
- "What if" calculator (see future balance)
- Interest earned badges

**Database Changes**:
- New `SavingsAccount` table per child
- Track `InterestRate`, `Frequency`, `LastPaymentDate`

---

### Spec 16: Transaction Approval Workflow ✅
**Priority: P1 | Effort: Medium (2-3 weeks) | Tests: 25**

**What**: Children request spending transactions. Parents approve/reject with comments. Funds deducted only on approval.

**Value**: Teaches asking permission and decision-making consequences.

**Key Components**:
- `TransactionRequest` model with status
- Real-time approval notifications (SignalR)
- Parent approval/rejection with comments
- Request history and statistics
- Spending request categories
- Automatic approval rules (< $5)

**Database Changes**:
- New `TransactionRequests` table
- Add `ApprovedById`, `ApprovedAt` to `Transaction`

---

### Spec 17: Achievements & Badges 🏆
**Priority: P1 | Effort: Large (4-6 weeks) | Tests: 50**

**What**: Unlock achievements and badges for financial milestones (First $100, Super Saver streak, Goal Crusher, etc.)

**Value**: Gamifies saving and provides positive reinforcement.

**Key Components**:
- 30+ predefined achievements
- Badge collection gallery
- Achievement notifications with animations
- Rarity levels (Common, Rare, Epic, Legendary)
- Share achievements with family
- Leaderboard (optional, within family)

**Database Changes**:
- New `Achievements` table (predefined)
- New `ChildAchievements` table (earned badges)
- Achievement trigger system

---

### Spec 18: Savings Streaks 🔥
**Priority: P1 | Effort: Small (1-2 weeks) | Tests: 15**

**What**: Track consecutive weeks of positive net savings (income > spending). Visual streak counter with bonus rewards.

**Value**: Encourages consistent saving behavior.

**Key Components**:
- Streak calculation service
- Visual streak display (🔥 7 week streak!)
- Longest streak history
- Streak broken notifications
- Streak bonus rewards (e.g., 10% bonus at 10 weeks)
- Streak achievements

**Database Changes**:
- Add `CurrentStreak`, `LongestStreak`, `LastStreakDate` to `Child`

---

### Spec 19: Family Overview Dashboard 👨‍👩‍👧‍👦
**Priority: P1 | Effort: Medium (2-3 weeks) | Tests: 20**

**What**: Parent-focused dashboard showing family-wide statistics, comparisons, and insights.

**Value**: Gives parents holistic view of family finances.

**Key Components**:
- Total family balance across all children
- This week's allowance payments scheduled
- Pending approvals (chores, transaction requests)
- Recent activity feed across all children
- Comparison charts (which child saves most?)
- Family financial health score
- Export family report

**Database Changes**:
- Views/aggregations, no new tables needed

---

### Spec 20: Reports & Exports (PDF/CSV) 📄
**Priority: P1 | Effort: Medium (2-3 weeks) | Tests: 18**

**What**: Generate professional PDF reports and CSV exports for record-keeping and tax purposes.

**Value**: Professional record-keeping and tax documentation.

**Key Components**:
- Monthly/quarterly/annual PDF reports with charts
- CSV export of all transactions
- Email delivery of reports
- Customizable report templates
- Year-end tax summary
- Print-optimized layouts

**Technology**:
- QuestPDF for PDF generation (already added)
- CsvHelper for CSV exports (already added)

---

### Spec 21: Smart Notifications System 🔔
**Priority: P2 | Effort: Medium (2-3 weeks) | Tests: 30**

**What**: Comprehensive notification system via email, SMS, and in-app notifications.

**Value**: Keeps users engaged and informed.

**Key Components**:
- Email notifications (SendGrid/SMTP)
- In-app notification center
- Notification preferences per user
- Notification types:
  - Transaction created
  - Allowance paid
  - Goal milestone reached
  - Low balance warning
  - Approval requests
  - Achievement unlocked
  - Weekly summary email

**Database Changes**:
- New `Notifications` table
- New `NotificationPreferences` table

---

### Spec 22: QR Code Transactions 📱
**Priority: P2 | Effort: Small (1-2 weeks) | Tests: 12**

**What**: Generate QR codes for children. Parents scan to create instant transactions ("point of sale").

**Value**: Fun, modern way to handle transactions, teaches real-world payment concepts.

**Key Components**:
- QR code generation per child (QRCoder library)
- Mobile-optimized scanner page
- Quick payment form after scan
- QR code display on child dashboard
- Transaction history from QR payments
- Regenerate QR code security

**Technology**:
- QRCoder NuGet package (free)
- Built-in camera API for scanning

---

### Spec 23: Photo Attachments for Transactions 📸
**Priority: P2 | Effort: Medium (2-3 weeks) | Tests: 15**

**What**: Attach receipt photos to transactions for record-keeping and proof.

**Value**: Teaches documentation and accountability.

**Key Components**:
- Upload receipt photos
- Azure Blob Storage integration (or local file storage)
- Photo gallery per child
- Thumbnail previews in transaction list
- Image compression and optimization
- Photo required for transactions > $20 (optional rule)

**Database Changes**:
- Add `ReceiptPhotoUrl` to `Transaction`
- Add `PhotoStorageProvider` enum (Azure/Local)

**Technology**:
- Azure.Storage.Blobs (or local file system)
- Image resizing library

---

### Spec 24: Child-Friendly Dashboard 🧒
**Priority: P2 | Effort: Large (4-6 weeks) | Tests: 35**

**What**: Completely redesigned child-focused dashboard with large visuals, simple language, and fun animations.

**Value**: Makes the app accessible and engaging for younger children.

**Key Components**:
- Large, colorful balance display
- Animated progress bars for goals
- "Can I afford this?" calculator widget
- Simple transaction history (icons instead of text)
- Age-appropriate language
- Fun illustrations and mascots
- Gamified navigation
- Voice-over support (accessibility)

**Design**:
- Mobile-first design
- Large touch targets (min 44x44px)
- High contrast colors
- Minimal text, maximum visuals

---

### Spec 25: Dark Mode Theme 🌙
**Priority: P2 | Effort: Small (1-2 weeks) | Tests: 8**

**What**: Dark theme toggle with user preference persistence.

**Value**: Better UX for evening use, modern aesthetic preference.

**Key Components**:
- CSS custom properties for theming
- Theme toggle component
- User theme preference storage
- System theme detection (prefers-color-scheme)
- Chart theme adaptation
- Image/icon inversions for dark mode

**Implementation**:
- CSS variables throughout
- Local storage for preference
- No new database tables needed

---

### Spec 26: Advanced Transaction Search & Filters 🔍
**Priority: P2 | Effort: Medium (2-3 weeks) | Tests: 22**

**What**: Powerful search and filtering for transaction history.

**Value**: Easy to find specific transactions for budgeting and analysis.

**Key Components**:
- Full-text search on descriptions
- Filter by:
  - Date range (with date picker)
  - Category
  - Transaction type (Credit/Debit)
  - Amount range
  - Approval status
- Sort by date, amount, category
- Save common filters as "views"
- Export filtered results to CSV
- Search suggestions/autocomplete

**Database Changes**:
- Add full-text search index on `Description`
- New `SavedFilters` table for custom views

---

### Spec 27: Two-Factor Authentication 🔐
**Priority: P3 | Effort: Medium (2-3 weeks) | Tests: 20**

**What**: Add 2FA for parent accounts using TOTP (Google Authenticator, Authy, etc.)

**Value**: Enhanced security for financial data.

**Key Components**:
- TOTP implementation (AspNetCore.Identity.Otp)
- QR code setup for authenticator apps
- Backup codes generation (10 codes)
- Recovery email option
- Enforce 2FA for parents (optional setting)
- Trust device for 30 days option
- 2FA required for sensitive operations

**Database Changes**:
- Add `TwoFactorEnabled`, `TwoFactorSecret` to `ApplicationUser` (already in Identity)
- New `RecoveryCodes` table

---

### Spec 28: Activity Audit Log 📋
**Priority: P3 | Effort: Small (1-2 weeks) | Tests: 15**

**What**: Comprehensive audit log of all system actions for security and accountability.

**Value**: Trust, transparency, and security for families.

**Key Components**:
- Log all critical actions:
  - User login/logout
  - Transaction created/modified
  - Allowance updated
  - Child added/removed
  - Settings changed
- View audit log (parents only)
- Filter by user, action type, date
- Export audit log
- Retention policy (keep 1 year)
- IP address and device tracking

**Database Changes**:
- New `AuditLog` table
- Automatic logging middleware

---

## Implementation Strategy

### Test-Driven Development (TDD)
All features follow strict TDD:
1. **RED**: Write failing tests first
2. **GREEN**: Implement minimum code to pass
3. **REFACTOR**: Improve code while keeping tests green

### Estimated Test Counts by Feature
| Feature | Service Tests | Component Tests | Integration Tests | Total |
|---------|--------------|-----------------|-------------------|-------|
| Categories | 12 | 10 | 3 | 25 |
| Chores | 20 | 20 | 5 | 45 |
| Savings Goals | 15 | 12 | 3 | 30 |
| Interest | 10 | 8 | 2 | 20 |
| Approvals | 12 | 10 | 3 | 25 |
| Achievements | 25 | 20 | 5 | 50 |
| Streaks | 8 | 5 | 2 | 15 |
| Family Dashboard | 8 | 10 | 2 | 20 |
| Reports | 10 | 5 | 3 | 18 |
| Notifications | 15 | 12 | 3 | 30 |
| QR Codes | 6 | 5 | 1 | 12 |
| Photos | 8 | 5 | 2 | 15 |
| Child Dashboard | 15 | 18 | 2 | 35 |
| Dark Mode | 2 | 6 | 0 | 8 |
| Search | 10 | 10 | 2 | 22 |
| 2FA | 12 | 6 | 2 | 20 |
| Audit Log | 8 | 5 | 2 | 15 |
| **TOTAL** | **196** | **167** | **42** | **405** |

**Grand Total: 405 tests** (plus existing 94 = **499 tests**)

---

## Success Metrics

### Technical Metrics
- **Test Coverage**: Maintain >90% code coverage
- **Performance**: All pages load in <500ms
- **Real-time**: SignalR updates within 1 second
- **Zero Critical Bugs**: No P0 bugs in production
- **Build Time**: CI/CD pipeline completes in <5 minutes

### User Engagement Metrics
- **Active Families**: 80% of families use app weekly
- **Transaction Volume**: Average 10+ transactions per child/week
- **Goal Completion**: 60% of savings goals reached
- **Chore Completion**: 70% of assigned chores completed
- **Feature Adoption**: 50%+ use new features within 30 days

### Business Metrics
- **User Satisfaction**: 4.5+ star rating
- **Retention**: 80% 30-day retention
- **Support Tickets**: <5% of users require support
- **Performance**: 99.9% uptime

---

## Risk Assessment

### Technical Risks
| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Performance degradation with scale | Medium | High | Implement caching, database indexing, pagination |
| SignalR connection issues | Medium | Medium | Automatic reconnection, fallback to polling |
| Photo storage costs | Low | Medium | Implement size limits, compression, cleanup policy |
| Complex query performance | Medium | High | Database query optimization, read replicas |

### Product Risks
| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Feature complexity overwhelming users | High | High | Progressive disclosure, onboarding flow |
| Gamification backfiring | Low | Medium | Optional features, parental controls |
| Privacy concerns with photos | Medium | High | Clear privacy policy, encryption, parental approval |
| Chore system creating conflict | Medium | Medium | Clear guidelines, dispute resolution flow |

---

## Dependencies & Prerequisites

### Infrastructure
- ✅ PostgreSQL database (already setup)
- ✅ SignalR hub (already setup)
- ✅ JWT authentication (already setup)
- ⏳ Azure Blob Storage (for photos) OR local file system
- ⏳ Email service (SendGrid/SMTP for notifications)
- ⏳ SMS service (optional, Twilio for 2FA)

### NuGet Packages
- ✅ Blazor-ApexCharts (charts)
- ✅ Radzen.Blazor (UI components)
- ✅ QuestPDF (PDF reports)
- ✅ CsvHelper (CSV exports)
- ⏳ QRCoder (QR code generation)
- ⏳ AspNetCore.Identity.Otp (2FA)
- ⏳ Azure.Storage.Blobs (optional, photo storage)
- ⏳ SixLabors.ImageSharp (image processing)

---

## Release Strategy

### Version Naming
- **v1.0**: MVP (current - 94 tests)
- **v1.1**: Charts & Categories (P0 priority)
- **v1.2**: Chores & Goals (P0 priority)
- **v1.3**: Gamification (P1 priority)
- **v1.4**: Family Features (P1 priority)
- **v2.0**: Major UX overhaul (P2 priority)
- **v2.1**: Enterprise Security (P3 priority)

### Release Cadence
- **Sprint Duration**: 2 weeks
- **Release Frequency**: Every 2 sprints (monthly)
- **Hotfix Window**: 24 hours for critical bugs
- **Feature Flags**: Use for gradual rollout

---

## Documentation Plan

Each spec includes:
1. **Overview**: What and why
2. **Database Schema**: EF Core entities and migrations
3. **API Specification**: REST endpoints with examples
4. **Service Layer**: Business logic with interfaces
5. **Blazor Components**: UI implementation
6. **Test Cases**: Comprehensive test plans with TDD approach
7. **Performance**: Optimization strategies
8. **Accessibility**: WCAG compliance
9. **Security**: Threat modeling and mitigations

---

## Getting Started

### For Developers
1. Read `CLAUDE.md` for project context and TDD workflow
2. Review this roadmap to understand priorities
3. Choose a feature from P0 list
4. Read detailed spec (e.g., `specs/12-categories.md`)
5. Follow TDD: Write tests first!
6. Submit PR with all tests passing

### For Product Managers
1. Review priority matrix for current focus
2. Validate feature value propositions
3. Provide user feedback for prioritization adjustments
4. Track success metrics for released features

### For Designers
1. Review detailed specs for UI requirements
2. Create mockups for P0 features first
3. Follow established design system
4. Consider mobile-first approach
5. Accessibility is mandatory (WCAG AA)

---

## Conclusion

This roadmap represents approximately **6 months of development work** with a team of 2-3 developers following strict TDD methodology. Each feature is designed to be:

- **Valuable**: Solves real user problems
- **Testable**: Comprehensive test coverage
- **Maintainable**: Clean architecture, SOLID principles
- **Scalable**: Performance-conscious design
- **Secure**: Security-first approach

The priority matrix ensures we build the most impactful features first while maintaining code quality and test coverage throughout.

**Next Steps**: Begin with **Spec 12: Transaction Categories & Budgeting** as the first P0 feature after completing Charts & Analytics!

---

**Document Version**: 1.0
**Last Updated**: 2025-10-09
**Status**: Living Document (update as priorities shift)
