# Child-Friendly Dashboard Specification

## Overview

This specification redesigns the dashboard interface with a kid-focused UI that makes financial concepts fun, accessible, and engaging for children ages 6-16. The goal is to create an age-appropriate interface that teaches money management through visual, interactive, and gamified elements.

## Goals

1. **Visual Learning**: Large, animated displays that make numbers easy to understand
2. **Instant Gratification**: "Can I afford this?" calculator for immediate decision-making
3. **Simplified Navigation**: Icon-based, intuitive navigation for young users
4. **Accessibility**: High contrast, large touch targets, screen reader support
5. **Engagement**: Fun illustrations, mascots, and progress bars to maintain interest
6. **Mobile-First**: Optimized for tablets and phones with large, touchable elements
7. **TDD Approach**: 35 comprehensive tests

## Technology Stack

- **Frontend**: Blazor Server with component-based architecture
- **UI Library**: Radzen.Blazor for child-friendly components
- **Icons**: Open Iconic + custom SVG illustrations
- **Animations**: CSS animations and transitions
- **Charts**: ApexCharts.Blazor for visual progress
- **Testing**: bUnit for component testing
- **Accessibility**: ARIA labels, semantic HTML, keyboard navigation

---

## Phase 1: Child Dashboard Page

### 1.1 ChildDashboard.razor Page

**Location**: `/Pages/ChildDashboard.razor`

**Route**: `/child/dashboard`

**Authorization**: `[Authorize(Roles = "Child")]`

**Features**:
- Large animated balance display at top
- "Can I afford this?" calculator widget
- Icon-based transaction history (last 10)
- Simplified navigation bar
- Savings goals progress section
- Achievement badges display
- Fun mascot/avatar character

**Page Structure**:

```razor
@page "/child/dashboard"
@attribute [Authorize(Roles = "Child")]
@inject ICurrentUserService CurrentUserService
@inject IChildManagementService ChildService
@inject ISavingsGoalService SavingsGoalService
@inject ITransactionService TransactionService

<PageTitle>My Money Dashboard</PageTitle>

<div class="child-dashboard">
    <!-- Fun Header with Mascot -->
    <div class="dashboard-header">
        <div class="mascot-welcome">
            <img src="/images/money-mascot.svg" alt="Piggy the Money Pal" class="mascot-avatar" />
            <div class="welcome-message">
                <h1>Hey @ChildName! 👋</h1>
                <p class="tagline">Let's check your money!</p>
            </div>
        </div>
    </div>

    <!-- Large Balance Display -->
    <BalanceCardLarge
        Balance="@CurrentBalance"
        WeeklyAllowance="@WeeklyAllowance"
        NextAllowanceDate="@NextAllowanceDate" />

    <!-- Can I Afford This? Calculator -->
    <AffordabilityCalculator
        CurrentBalance="@CurrentBalance"
        SavingsGoals="@SavingsGoals" />

    <!-- Savings Goals Progress -->
    @if (SavingsGoals.Any())
    {
        <div class="savings-goals-section">
            <h2 class="section-title">
                <span class="icon">🎯</span>
                My Savings Goals
            </h2>
            <div class="goals-grid">
                @foreach (var goal in SavingsGoals.Take(3))
                {
                    <SavingsGoalCardChild Goal="@goal" />
                }
            </div>
            @if (SavingsGoals.Count > 3)
            {
                <a href="/child/savings-goals" class="btn-view-all">
                    See All Goals →
                </a>
            }
        </div>
    }
    else
    {
        <div class="empty-goals-cta">
            <img src="/images/goal-illustration.svg" alt="Set a goal" class="empty-illustration" />
            <h3>No savings goals yet!</h3>
            <p>What would you like to save for?</p>
            <a href="/child/savings-goals/create" class="btn btn-primary btn-lg">
                <span class="icon">➕</span>
                Create My First Goal
            </a>
        </div>
    }

    <!-- Recent Activity - Icon Based -->
    <div class="recent-activity-section">
        <h2 class="section-title">
            <span class="icon">📊</span>
            Recent Activity
        </h2>
        <TransactionHistoryChild
            Transactions="@RecentTransactions"
            MaxItems="10" />
    </div>

    <!-- Achievement Badges -->
    @if (Achievements.Any())
    {
        <div class="achievements-section">
            <h2 class="section-title">
                <span class="icon">🏆</span>
                My Achievements
            </h2>
            <AchievementsBadgeList Achievements="@Achievements" />
        </div>
    }

    <!-- Fun Facts / Tips -->
    <div class="money-tip-card">
        <div class="tip-icon">💡</div>
        <div class="tip-content">
            <h4>Money Tip of the Day</h4>
            <p>@DailyMoneyTip</p>
        </div>
    </div>
</div>

@code {
    private string ChildName = "";
    private decimal CurrentBalance = 0;
    private decimal WeeklyAllowance = 0;
    private DateTime? NextAllowanceDate;
    private List<WishListItem> SavingsGoals = new();
    private List<Transaction> RecentTransactions = new();
    private List<Achievement> Achievements = new();
    private string DailyMoneyTip = "";

    protected override async Task OnInitializedAsync()
    {
        var currentUser = await CurrentUserService.GetCurrentUserAsync();
        var child = await ChildService.GetChildByUserIdAsync(currentUser.Id);

        ChildName = currentUser.FirstName;
        CurrentBalance = child.CurrentBalance;
        WeeklyAllowance = child.WeeklyAllowance;
        NextAllowanceDate = CalculateNextAllowanceDate(child.LastAllowanceDate);

        SavingsGoals = await SavingsGoalService.GetActiveGoalsAsync(child.Id);
        RecentTransactions = await TransactionService.GetRecentTransactionsAsync(child.Id, 10);

        // TODO: Load achievements when achievement system is implemented
        Achievements = new List<Achievement>();

        DailyMoneyTip = GetRandomMoneyTip();
    }

    private DateTime? CalculateNextAllowanceDate(DateTime? lastAllowanceDate)
    {
        if (!lastAllowanceDate.HasValue)
            return DateTime.Now.AddDays(7 - (int)DateTime.Now.DayOfWeek); // Next Sunday

        return lastAllowanceDate.Value.AddDays(7);
    }

    private string GetRandomMoneyTip()
    {
        var tips = new[]
        {
            "Save a little bit from every dollar you get!",
            "The best time to save money is NOW!",
            "Setting goals makes saving fun!",
            "Every penny counts toward your dreams!",
            "Patience pays off - keep saving!",
            "You're doing great with your money!",
            "Smart savers become successful adults!",
            "Think before you spend - do you really need it?"
        };

        var random = new Random();
        return tips[random.Next(tips.Length)];
    }
}
```

### 1.2 CSS Styling (ChildDashboard.razor.css)

```css
.child-dashboard {
    padding: 1rem;
    max-width: 1200px;
    margin: 0 auto;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    min-height: 100vh;
}

.dashboard-header {
    background: white;
    border-radius: 20px;
    padding: 1.5rem;
    margin-bottom: 1.5rem;
    box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
}

.mascot-welcome {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.mascot-avatar {
    width: 80px;
    height: 80px;
    animation: bounce 2s infinite;
}

@keyframes bounce {
    0%, 100% { transform: translateY(0); }
    50% { transform: translateY(-10px); }
}

.welcome-message h1 {
    font-size: 2rem;
    color: #667eea;
    margin: 0;
    font-weight: 700;
}

.welcome-message .tagline {
    font-size: 1.1rem;
    color: #6c757d;
    margin: 0.25rem 0 0 0;
}

.section-title {
    font-size: 1.5rem;
    font-weight: 700;
    color: white;
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 1rem;
}

.section-title .icon {
    font-size: 1.8rem;
}

.savings-goals-section,
.recent-activity-section,
.achievements-section {
    background: white;
    border-radius: 20px;
    padding: 1.5rem;
    margin-bottom: 1.5rem;
    box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
}

.goals-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 1rem;
    margin-bottom: 1rem;
}

.btn-view-all {
    display: block;
    text-align: center;
    padding: 0.75rem;
    background: #f8f9fa;
    border-radius: 10px;
    color: #667eea;
    text-decoration: none;
    font-weight: 600;
    transition: all 0.3s ease;
}

.btn-view-all:hover {
    background: #667eea;
    color: white;
    transform: translateY(-2px);
}

.empty-goals-cta {
    background: white;
    border-radius: 20px;
    padding: 3rem 2rem;
    text-align: center;
    margin-bottom: 1.5rem;
    box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
}

.empty-illustration {
    width: 200px;
    height: 200px;
    margin-bottom: 1rem;
}

.empty-goals-cta h3 {
    font-size: 1.5rem;
    color: #333;
    margin-bottom: 0.5rem;
}

.empty-goals-cta p {
    font-size: 1.1rem;
    color: #6c757d;
    margin-bottom: 1.5rem;
}

.btn-primary.btn-lg {
    padding: 1rem 2rem;
    font-size: 1.2rem;
    border-radius: 15px;
    min-height: 44px;
    min-width: 44px; /* WCAG touch target minimum */
}

.money-tip-card {
    background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
    border-radius: 20px;
    padding: 1.5rem;
    color: white;
    display: flex;
    align-items: center;
    gap: 1rem;
    box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
}

.tip-icon {
    font-size: 3rem;
    flex-shrink: 0;
}

.tip-content h4 {
    font-size: 1.2rem;
    margin: 0 0 0.5rem 0;
}

.tip-content p {
    font-size: 1rem;
    margin: 0;
    opacity: 0.95;
}

/* Mobile Responsiveness */
@media (max-width: 768px) {
    .child-dashboard {
        padding: 0.5rem;
    }

    .dashboard-header {
        padding: 1rem;
    }

    .mascot-avatar {
        width: 60px;
        height: 60px;
    }

    .welcome-message h1 {
        font-size: 1.5rem;
    }

    .welcome-message .tagline {
        font-size: 0.95rem;
    }

    .section-title {
        font-size: 1.25rem;
    }

    .goals-grid {
        grid-template-columns: 1fr;
    }

    .money-tip-card {
        flex-direction: column;
        text-align: center;
    }
}

/* High Contrast Mode Support */
@media (prefers-contrast: high) {
    .child-dashboard {
        background: #000;
    }

    .section-title {
        color: #fff;
        text-shadow: 0 0 4px rgba(0, 0, 0, 0.8);
    }

    .dashboard-header,
    .savings-goals-section,
    .recent-activity-section,
    .achievements-section,
    .empty-goals-cta {
        border: 2px solid #fff;
    }
}

/* Reduced Motion Support */
@media (prefers-reduced-motion: reduce) {
    .mascot-avatar {
        animation: none;
    }

    .btn-view-all {
        transition: none;
    }

    .btn-view-all:hover {
        transform: none;
    }
}
```

---

## Phase 2: Key Components

### 2.1 BalanceCardLarge Component

**Purpose**: Display current balance with large, animated numbers

```razor
@using System.Globalization

<div class="balance-card-large">
    <div class="balance-container">
        <div class="balance-label">
            <span class="icon">💰</span>
            My Money
        </div>
        <div class="balance-amount" role="region" aria-label="Current balance">
            <span class="currency-symbol">$</span>
            <span class="balance-value">@FormattedBalance</span>
        </div>
        <div class="balance-change">
            @if (BalanceChange > 0)
            {
                <span class="change-positive">
                    <span class="icon">↗️</span> +@BalanceChange.ToString("C") this week
                </span>
            }
            else if (BalanceChange < 0)
            {
                <span class="change-negative">
                    <span class="icon">↘️</span> @BalanceChange.ToString("C") this week
                </span>
            }
            else
            {
                <span class="change-neutral">
                    No changes this week
                </span>
            }
        </div>
    </div>

    @if (WeeklyAllowance > 0 && NextAllowanceDate.HasValue)
    {
        <div class="next-allowance">
            <div class="allowance-icon">🗓️</div>
            <div class="allowance-info">
                <div class="allowance-label">Next Allowance</div>
                <div class="allowance-amount">@WeeklyAllowance.ToString("C")</div>
                <div class="allowance-date">@GetFriendlyDate(NextAllowanceDate.Value)</div>
            </div>
            <div class="countdown">
                <div class="countdown-days">@GetDaysUntil(NextAllowanceDate.Value)</div>
                <div class="countdown-label">days</div>
            </div>
        </div>
    }
</div>

@code {
    [Parameter] public decimal Balance { get; set; }
    [Parameter] public decimal WeeklyAllowance { get; set; }
    [Parameter] public DateTime? NextAllowanceDate { get; set; }
    [Parameter] public decimal BalanceChange { get; set; } = 0;

    private string FormattedBalance => Balance.ToString("N2", CultureInfo.CurrentCulture);

    private string GetFriendlyDate(DateTime date)
    {
        var days = (date.Date - DateTime.Now.Date).Days;
        if (days == 0) return "Today!";
        if (days == 1) return "Tomorrow";
        if (days <= 7) return date.ToString("dddd"); // Day name
        return date.ToString("MMM dd");
    }

    private int GetDaysUntil(DateTime date)
    {
        return Math.Max(0, (date.Date - DateTime.Now.Date).Days);
    }
}
```

**CSS** (`BalanceCardLarge.razor.css`):

```css
.balance-card-large {
    background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
    border-radius: 20px;
    padding: 2rem;
    margin-bottom: 1.5rem;
    color: white;
    box-shadow: 0 8px 30px rgba(0, 0, 0, 0.15);
}

.balance-container {
    text-align: center;
    margin-bottom: 1.5rem;
}

.balance-label {
    font-size: 1.2rem;
    font-weight: 600;
    margin-bottom: 0.5rem;
    opacity: 0.95;
}

.balance-label .icon {
    font-size: 1.5rem;
    margin-right: 0.5rem;
}

.balance-amount {
    display: flex;
    justify-content: center;
    align-items: baseline;
    gap: 0.25rem;
    margin: 1rem 0;
}

.currency-symbol {
    font-size: 2.5rem;
    font-weight: 700;
}

.balance-value {
    font-size: 4rem;
    font-weight: 900;
    line-height: 1;
    animation: countUp 0.5s ease-out;
}

@keyframes countUp {
    from {
        opacity: 0;
        transform: translateY(20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.balance-change {
    font-size: 1rem;
    font-weight: 500;
}

.change-positive {
    color: #4ade80;
}

.change-negative {
    color: #fb7185;
}

.change-neutral {
    opacity: 0.8;
}

.next-allowance {
    background: rgba(255, 255, 255, 0.2);
    backdrop-filter: blur(10px);
    border-radius: 15px;
    padding: 1rem;
    display: flex;
    align-items: center;
    gap: 1rem;
}

.allowance-icon {
    font-size: 2.5rem;
    flex-shrink: 0;
}

.allowance-info {
    flex: 1;
}

.allowance-label {
    font-size: 0.9rem;
    opacity: 0.9;
    margin-bottom: 0.25rem;
}

.allowance-amount {
    font-size: 1.5rem;
    font-weight: 700;
}

.allowance-date {
    font-size: 0.95rem;
    opacity: 0.85;
    margin-top: 0.25rem;
}

.countdown {
    text-align: center;
    background: rgba(255, 255, 255, 0.3);
    border-radius: 10px;
    padding: 0.75rem 1rem;
}

.countdown-days {
    font-size: 2rem;
    font-weight: 900;
    line-height: 1;
}

.countdown-label {
    font-size: 0.8rem;
    opacity: 0.9;
    margin-top: 0.25rem;
}

@media (max-width: 768px) {
    .balance-card-large {
        padding: 1.5rem;
    }

    .balance-value {
        font-size: 3rem;
    }

    .currency-symbol {
        font-size: 2rem;
    }

    .next-allowance {
        flex-direction: column;
        text-align: center;
    }

    .allowance-info {
        text-align: center;
    }
}
```

### 2.2 AffordabilityCalculator Component

**Purpose**: "Can I afford this?" interactive calculator

```razor
<div class="affordability-calculator">
    <div class="calculator-header">
        <h3>
            <span class="icon">🤔</span>
            Can I Afford This?
        </h3>
        <p class="calculator-subtitle">Type in a price to check!</p>
    </div>

    <div class="calculator-input-container">
        <div class="input-wrapper">
            <span class="currency-prefix">$</span>
            <input type="number"
                   class="calculator-input"
                   placeholder="0.00"
                   min="0"
                   step="0.01"
                   @bind="ItemPrice"
                   @bind:event="oninput"
                   aria-label="Item price" />
        </div>
    </div>

    @if (ItemPrice > 0)
    {
        <div class="affordability-result @GetResultClass()">
            <div class="result-icon">@GetResultIcon()</div>
            <div class="result-content">
                <div class="result-message">@GetResultMessage()</div>
                @if (CanAfford)
                {
                    <div class="result-detail success">
                        You'll have <strong>@RemainingBalance.ToString("C")</strong> left!
                    </div>
                }
                else
                {
                    <div class="result-detail warning">
                        You need <strong>@AmountNeeded.ToString("C")</strong> more.
                        @if (WeeksToSave > 0 && WeeksToSave < 100)
                        {
                            <div class="savings-estimate">
                                Save for <strong>@WeeksToSave weeks</strong> to afford it!
                            </div>
                        }
                    </div>
                }

                @if (HasSavingsGoals)
                {
                    <div class="savings-warning">
                        <span class="icon">⚠️</span>
                        Remember: You have <strong>@TotalSavingsGoals.ToString("C")</strong> in savings goals!
                    </div>
                }
            </div>
        </div>

        <!-- Visual Progress Bar -->
        <div class="affordability-bar">
            <div class="bar-fill" style="width: @GetProgressPercent()%"></div>
            <div class="bar-marker" style="left: @GetItemPricePercent()%">
                <span class="marker-label">Item Price</span>
            </div>
        </div>
    }
</div>

@code {
    [Parameter] public decimal CurrentBalance { get; set; }
    [Parameter] public decimal WeeklyAllowance { get; set; }
    [Parameter] public List<WishListItem> SavingsGoals { get; set; } = new();

    private decimal ItemPrice { get; set; } = 0;

    private bool CanAfford => CurrentBalance >= ItemPrice;
    private decimal RemainingBalance => CurrentBalance - ItemPrice;
    private decimal AmountNeeded => ItemPrice - CurrentBalance;
    private int WeeksToSave => WeeklyAllowance > 0
        ? (int)Math.Ceiling(AmountNeeded / WeeklyAllowance)
        : 0;
    private bool HasSavingsGoals => SavingsGoals.Any(g => g.IsActive);
    private decimal TotalSavingsGoals => SavingsGoals
        .Where(g => g.IsActive)
        .Sum(g => g.CurrentAmount);

    private string GetResultClass()
    {
        return CanAfford ? "result-success" : "result-warning";
    }

    private string GetResultIcon()
    {
        return CanAfford ? "✅" : "❌";
    }

    private string GetResultMessage()
    {
        if (CanAfford)
        {
            if (RemainingBalance < 5)
                return "You can afford it, but you'll have almost nothing left!";
            return "Yes! You can afford it!";
        }
        else
        {
            return "Not yet, but keep saving!";
        }
    }

    private decimal GetProgressPercent()
    {
        if (ItemPrice == 0) return 0;
        var percent = (CurrentBalance / ItemPrice) * 100;
        return Math.Min(percent, 100);
    }

    private decimal GetItemPricePercent()
    {
        var maxValue = Math.Max(CurrentBalance, ItemPrice);
        if (maxValue == 0) return 0;
        return Math.Min((ItemPrice / maxValue) * 100, 100);
    }
}
```

**CSS** (`AffordabilityCalculator.razor.css`):

```css
.affordability-calculator {
    background: white;
    border-radius: 20px;
    padding: 1.5rem;
    margin-bottom: 1.5rem;
    box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
}

.calculator-header {
    text-align: center;
    margin-bottom: 1.5rem;
}

.calculator-header h3 {
    font-size: 1.5rem;
    color: #333;
    margin: 0 0 0.5rem 0;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
}

.calculator-header .icon {
    font-size: 1.8rem;
}

.calculator-subtitle {
    color: #6c757d;
    margin: 0;
}

.calculator-input-container {
    margin-bottom: 1.5rem;
}

.input-wrapper {
    position: relative;
    max-width: 400px;
    margin: 0 auto;
}

.currency-prefix {
    position: absolute;
    left: 1.5rem;
    top: 50%;
    transform: translateY(-50%);
    font-size: 2rem;
    font-weight: 700;
    color: #667eea;
    pointer-events: none;
}

.calculator-input {
    width: 100%;
    padding: 1rem 1rem 1rem 3rem;
    font-size: 2rem;
    font-weight: 700;
    border: 3px solid #667eea;
    border-radius: 15px;
    text-align: center;
    min-height: 60px; /* Touch target */
    transition: all 0.3s ease;
}

.calculator-input:focus {
    outline: none;
    border-color: #764ba2;
    box-shadow: 0 0 0 4px rgba(102, 126, 234, 0.2);
}

.calculator-input::placeholder {
    color: #ccc;
}

.affordability-result {
    padding: 1.5rem;
    border-radius: 15px;
    margin-bottom: 1rem;
    display: flex;
    gap: 1rem;
    align-items: start;
}

.result-success {
    background: #d1fae5;
    border: 2px solid #10b981;
}

.result-warning {
    background: #fed7aa;
    border: 2px solid #f59e0b;
}

.result-icon {
    font-size: 3rem;
    flex-shrink: 0;
}

.result-content {
    flex: 1;
}

.result-message {
    font-size: 1.3rem;
    font-weight: 700;
    color: #333;
    margin-bottom: 0.5rem;
}

.result-detail {
    font-size: 1rem;
    margin-top: 0.5rem;
}

.result-detail.success {
    color: #059669;
}

.result-detail.warning {
    color: #d97706;
}

.savings-estimate {
    margin-top: 0.5rem;
    padding: 0.75rem;
    background: rgba(255, 255, 255, 0.7);
    border-radius: 8px;
    font-weight: 600;
}

.savings-warning {
    margin-top: 1rem;
    padding: 0.75rem;
    background: rgba(239, 68, 68, 0.1);
    border-radius: 8px;
    color: #dc2626;
    font-size: 0.95rem;
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.affordability-bar {
    position: relative;
    height: 40px;
    background: #e5e7eb;
    border-radius: 20px;
    overflow: hidden;
}

.bar-fill {
    height: 100%;
    background: linear-gradient(90deg, #10b981 0%, #059669 100%);
    border-radius: 20px;
    transition: width 0.5s ease;
}

.bar-marker {
    position: absolute;
    top: -30px;
    transform: translateX(-50%);
}

.marker-label {
    background: #667eea;
    color: white;
    padding: 0.25rem 0.75rem;
    border-radius: 8px;
    font-size: 0.8rem;
    font-weight: 600;
    white-space: nowrap;
}

.marker-label::after {
    content: '';
    position: absolute;
    bottom: -6px;
    left: 50%;
    transform: translateX(-50%);
    width: 0;
    height: 0;
    border-left: 6px solid transparent;
    border-right: 6px solid transparent;
    border-top: 6px solid #667eea;
}

@media (max-width: 768px) {
    .calculator-input {
        font-size: 1.75rem;
    }

    .currency-prefix {
        font-size: 1.5rem;
        left: 1rem;
    }

    .result-message {
        font-size: 1.1rem;
    }
}
```

### 2.3 TransactionHistoryChild Component

**Purpose**: Icon-based, simplified transaction history for kids

```razor
<div class="transaction-history-child">
    @if (Transactions.Any())
    {
        <div class="transaction-list">
            @foreach (var transaction in Transactions.Take(MaxItems))
            {
                <div class="transaction-item @GetTransactionClass(transaction)">
                    <div class="transaction-icon">
                        @GetTransactionIcon(transaction)
                    </div>
                    <div class="transaction-details">
                        <div class="transaction-description">
                            @transaction.Description
                        </div>
                        <div class="transaction-date">
                            @GetFriendlyDate(transaction.CreatedAt)
                        </div>
                    </div>
                    <div class="transaction-amount @GetAmountClass(transaction)">
                        @GetAmountDisplay(transaction)
                    </div>
                </div>
            }
        </div>

        @if (Transactions.Count > MaxItems)
        {
            <a href="/child/transactions" class="btn-view-more">
                View All Transactions →
            </a>
        }
    }
    else
    {
        <div class="empty-transactions">
            <div class="empty-icon">📝</div>
            <p>No transactions yet!</p>
            <small>Your money activity will show up here.</small>
        </div>
    }
</div>

@code {
    [Parameter] public List<Transaction> Transactions { get; set; } = new();
    [Parameter] public int MaxItems { get; set; } = 10;

    private string GetTransactionClass(Transaction transaction)
    {
        return transaction.Type == TransactionType.Credit
            ? "transaction-credit"
            : "transaction-debit";
    }

    private string GetTransactionIcon(Transaction transaction)
    {
        // Determine icon based on description keywords
        var desc = transaction.Description.ToLower();

        if (desc.Contains("allowance")) return "💵";
        if (desc.Contains("chore") || desc.Contains("task")) return "🧹";
        if (desc.Contains("bonus") || desc.Contains("reward")) return "🎁";
        if (desc.Contains("birthday") || desc.Contains("gift")) return "🎂";
        if (desc.Contains("toy") || desc.Contains("game")) return "🎮";
        if (desc.Contains("book")) return "📚";
        if (desc.Contains("candy") || desc.Contains("snack")) return "🍬";
        if (desc.Contains("save") || desc.Contains("goal")) return "🎯";

        return transaction.Type == TransactionType.Credit ? "➕" : "➖";
    }

    private string GetAmountClass(Transaction transaction)
    {
        return transaction.Type == TransactionType.Credit
            ? "amount-positive"
            : "amount-negative";
    }

    private string GetAmountDisplay(Transaction transaction)
    {
        var sign = transaction.Type == TransactionType.Credit ? "+" : "-";
        return $"{sign}{transaction.Amount:C}";
    }

    private string GetFriendlyDate(DateTime date)
    {
        var days = (DateTime.Now.Date - date.Date).Days;

        if (days == 0) return "Today";
        if (days == 1) return "Yesterday";
        if (days < 7) return $"{days} days ago";
        if (days < 30) return $"{days / 7} weeks ago";

        return date.ToString("MMM dd");
    }
}
```

**CSS** (`TransactionHistoryChild.razor.css`):

```css
.transaction-history-child {
    /* Inherits styles from parent container */
}

.transaction-list {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
}

.transaction-item {
    display: flex;
    align-items: center;
    gap: 1rem;
    padding: 1rem;
    border-radius: 12px;
    background: #f8f9fa;
    transition: all 0.2s ease;
}

.transaction-item:hover {
    transform: translateX(4px);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.transaction-credit {
    border-left: 4px solid #10b981;
}

.transaction-debit {
    border-left: 4px solid #f59e0b;
}

.transaction-icon {
    font-size: 2rem;
    flex-shrink: 0;
    width: 50px;
    height: 50px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: white;
    border-radius: 10px;
}

.transaction-details {
    flex: 1;
    min-width: 0; /* Allow text truncation */
}

.transaction-description {
    font-size: 1rem;
    font-weight: 600;
    color: #333;
    margin-bottom: 0.25rem;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.transaction-date {
    font-size: 0.85rem;
    color: #6c757d;
}

.transaction-amount {
    font-size: 1.25rem;
    font-weight: 700;
    flex-shrink: 0;
}

.amount-positive {
    color: #10b981;
}

.amount-negative {
    color: #f59e0b;
}

.btn-view-more {
    display: block;
    margin-top: 1rem;
    padding: 0.75rem;
    text-align: center;
    background: #f8f9fa;
    border-radius: 10px;
    color: #667eea;
    text-decoration: none;
    font-weight: 600;
    transition: all 0.3s ease;
    min-height: 44px; /* Touch target */
}

.btn-view-more:hover {
    background: #667eea;
    color: white;
}

.empty-transactions {
    text-align: center;
    padding: 3rem 1rem;
    color: #6c757d;
}

.empty-icon {
    font-size: 4rem;
    margin-bottom: 1rem;
    opacity: 0.5;
}

.empty-transactions p {
    font-size: 1.1rem;
    font-weight: 600;
    margin-bottom: 0.5rem;
}

.empty-transactions small {
    font-size: 0.9rem;
}

@media (max-width: 768px) {
    .transaction-item {
        padding: 0.75rem;
    }

    .transaction-icon {
        width: 44px;
        height: 44px;
        font-size: 1.5rem;
    }

    .transaction-description {
        font-size: 0.95rem;
    }

    .transaction-amount {
        font-size: 1.1rem;
    }
}
```

---

## Phase 3: Navigation & Layout

### 3.1 ChildNavigation Component

**Purpose**: Simplified, icon-based navigation for children

```razor
<nav class="child-navigation" role="navigation" aria-label="Main navigation">
    <NavLink href="/child/dashboard" class="nav-item" Match="NavLinkMatch.All">
        <div class="nav-icon">🏠</div>
        <div class="nav-label">Home</div>
    </NavLink>

    <NavLink href="/child/savings-goals" class="nav-item">
        <div class="nav-icon">🎯</div>
        <div class="nav-label">Goals</div>
    </NavLink>

    <NavLink href="/child/transactions" class="nav-item">
        <div class="nav-icon">📊</div>
        <div class="nav-label">History</div>
    </NavLink>

    <NavLink href="/child/profile" class="nav-item">
        <div class="nav-icon">👤</div>
        <div class="nav-label">Me</div>
    </NavLink>
</nav>
```

**CSS** (`ChildNavigation.razor.css`):

```css
.child-navigation {
    display: flex;
    justify-content: space-around;
    align-items: center;
    background: white;
    border-radius: 20px 20px 0 0;
    padding: 0.75rem 0.5rem;
    box-shadow: 0 -4px 15px rgba(0, 0, 0, 0.1);
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    z-index: 1000;
}

.nav-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 0.75rem 1rem;
    border-radius: 12px;
    text-decoration: none;
    color: #6c757d;
    transition: all 0.3s ease;
    min-width: 70px;
    min-height: 70px; /* Touch target */
}

.nav-item:hover {
    background: #f8f9fa;
    color: #667eea;
}

.nav-item.active {
    background: #667eea;
    color: white;
}

.nav-icon {
    font-size: 1.75rem;
    margin-bottom: 0.25rem;
}

.nav-label {
    font-size: 0.75rem;
    font-weight: 600;
    text-align: center;
}

@media (min-width: 769px) {
    .child-navigation {
        position: static;
        border-radius: 20px;
        margin-bottom: 1.5rem;
    }
}

@media (max-width: 768px) {
    .nav-item {
        min-width: 60px;
        min-height: 60px;
        padding: 0.5rem 0.75rem;
    }

    .nav-icon {
        font-size: 1.5rem;
    }

    .nav-label {
        font-size: 0.7rem;
    }
}
```

---

## Phase 4: Testing Strategy

### 4.1 Component Tests (35 Tests Total)

**ChildDashboard Tests (8 tests)**:
```csharp
namespace AllowanceTracker.Tests.Components;

public class ChildDashboardTests : TestContext
{
    [Fact]
    public void ChildDashboard_RendersWelcomeMessage()
    {
        // Arrange
        var child = CreateTestChild("Alice", balance: 50m);
        Services.AddSingleton(CreateMockCurrentUserService(child));
        Services.AddSingleton(CreateMockChildService(child));

        // Act
        var component = RenderComponent<ChildDashboard>();

        // Assert
        component.Find(".welcome-message h1").TextContent
            .Should().Contain("Hey Alice!");
    }

    [Fact]
    public void ChildDashboard_DisplaysBalanceCard()
    {
        // Test balance card is rendered
    }

    [Fact]
    public void ChildDashboard_ShowsAffordabilityCalculator()
    {
        // Test calculator widget is present
    }

    [Fact]
    public void ChildDashboard_DisplaysSavingsGoals_WhenPresent()
    {
        // Test goals section renders when child has goals
    }

    [Fact]
    public void ChildDashboard_ShowsEmptyGoalsCTA_WhenNoGoals()
    {
        // Test empty state with create goal button
    }

    [Fact]
    public void ChildDashboard_DisplaysRecentTransactions()
    {
        // Test transaction history component
    }

    [Fact]
    public void ChildDashboard_ShowsMoneyTip()
    {
        // Test daily tip is displayed
    }

    [Fact]
    public void ChildDashboard_LoadsDataOnInitialization()
    {
        // Test that all data services are called
    }
}
```

**BalanceCardLarge Tests (7 tests)**:
```csharp
[Fact] BalanceCardLarge_DisplaysFormattedBalance
[Fact] BalanceCardLarge_ShowsPositiveChange_WhenBalanceIncreased
[Fact] BalanceCardLarge_ShowsNegativeChange_WhenBalanceDecreased
[Fact] BalanceCardLarge_ShowsNextAllowanceInfo_WhenAllowanceSet
[Fact] BalanceCardLarge_CalculatesCountdownDays
[Fact] BalanceCardLarge_ShowsFriendlyDateFormat
[Fact] BalanceCardLarge_AnimatesBalanceChange
```

**AffordabilityCalculator Tests (10 tests)**:
```csharp
[Fact] AffordabilityCalculator_ShowsSuccessMessage_WhenCanAfford
[Fact] AffordabilityCalculator_ShowsWarningMessage_WhenCannotAfford
[Fact] AffordabilityCalculator_CalculatesRemainingBalance
[Fact] AffordabilityCalculator_CalculatesAmountNeeded
[Fact] AffordabilityCalculator_EstimatesWeeksToSave
[Fact] AffordabilityCalculator_ShowsSavingsGoalsWarning_WhenGoalsExist
[Fact] AffordabilityCalculator_UpdatesRealtime_OnInputChange
[Fact] AffordabilityCalculator_ShowsProgressBar
[Fact] AffordabilityCalculator_HandlesZeroBalance
[Fact] AffordabilityCalculator_HandlesNegativeInput
```

**TransactionHistoryChild Tests (6 tests)**:
```csharp
[Fact] TransactionHistoryChild_RendersTransactionList
[Fact] TransactionHistoryChild_ShowsCorrectIcons_ForTransactionTypes
[Fact] TransactionHistoryChild_DisplaysFriendlyDates
[Fact] TransactionHistoryChild_LimitsToMaxItems
[Fact] TransactionHistoryChild_ShowsViewAllButton_WhenMoreTransactions
[Fact] TransactionHistoryChild_ShowsEmptyState_WhenNoTransactions
```

**ChildNavigation Tests (4 tests)**:
```csharp
[Fact] ChildNavigation_RendersAllNavigationItems
[Fact] ChildNavigation_HighlightsActiveRoute
[Fact] ChildNavigation_HasMinimumTouchTargetSize
[Fact] ChildNavigation_MeetsAccessibilityStandards
```

---

## Phase 5: Accessibility Features

### 5.1 ARIA Labels & Semantic HTML

All components include:
- `role` attributes for screen readers
- `aria-label` for interactive elements
- Semantic HTML (`<nav>`, `<section>`, `<button>`)
- Proper heading hierarchy (`<h1>` → `<h2>` → `<h3>`)

### 5.2 Keyboard Navigation

- All interactive elements accessible via Tab key
- Enter/Space to activate buttons
- Focus indicators visible on all elements
- Skip navigation link for screen readers

### 5.3 Touch Targets

All buttons and interactive elements:
- Minimum 44x44px touch target (WCAG 2.1 Level AAA)
- Adequate spacing between elements
- Clear visual feedback on touch/hover

### 5.4 Color Contrast

- WCAG AA compliance (4.5:1 for normal text)
- High contrast mode support via media query
- Not relying solely on color for information

### 5.5 Reduced Motion

Respects `prefers-reduced-motion` media query:
- Disables animations
- Removes transitions
- Maintains functionality

---

## Success Metrics

### Technical
- ✅ All 35 tests passing
- ✅ Page loads in < 1 second
- ✅ Lighthouse accessibility score > 95
- ✅ All touch targets ≥ 44x44px
- ✅ WCAG AA color contrast met
- ✅ Keyboard navigation fully functional
- ✅ Screen reader compatible

### User Experience
- ✅ Child can understand their balance at a glance
- ✅ Affordability calculator provides instant feedback
- ✅ Navigation is intuitive without text labels
- ✅ Transactions are visually scannable
- ✅ Fun elements engage without distracting
- ✅ Mobile interface feels natural
- ✅ Loading states prevent confusion

---

## Future Enhancements

1. **Avatar Customization**: Let children choose/customize their mascot
2. **Sound Effects**: Optional audio feedback for actions (with parental control)
3. **Animated Tutorials**: Interactive walkthrough for first-time users
4. **Voice Commands**: "How much money do I have?"
5. **Dark Mode**: Child-friendly dark theme
6. **Multiple Languages**: Internationalization support
7. **Parental Controls**: Toggle between simple/advanced views by age
8. **Reward Animations**: Confetti/celebration effects on goal completion

---

**Total Implementation Time**: 2-3 weeks following TDD methodology
**Priority**: High - Critical for child engagement and usability
