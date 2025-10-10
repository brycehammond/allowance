# Charts, Analytics & Visualization Specification

## Overview

This specification details the implementation of interactive charts, analytics dashboards, and data visualization features for the Allowance Tracker application. The goal is to provide meaningful financial insights through visual representations of transaction data, balances, and trends.

## Technology Stack

### Charting Library: ApexCharts.Blazor
- **Why**: Modern, interactive, MIT license, excellent Blazor Server support
- **Package**: `Blazor-ApexCharts`
- **Features**: Line charts, bar charts, pie charts, area charts, mixed charts
- **Customization**: Full theming support, animations, tooltips, legends

### Supporting Libraries
- **Radzen.Blazor**: Date range picker, UI components (free)
- **QuestPDF**: PDF report generation (free for OSS)
- **CsvHelper**: CSV export functionality (free)

## Architecture

### Service Layer
```
ITransactionAnalyticsService
├── Data Aggregation
├── Trend Calculation
├── Statistical Analysis
└── DTO Mapping

Chart Components
├── Base Chart Configuration
├── Data Binding
├── Real-Time Updates
└── User Interactions
```

### Data Flow
```
Transaction Data → AnalyticsService → DTOs → Chart Component → ApexCharts Rendering
                                    ↓
                              SignalR Updates → Auto-Refresh
```

---

## Phase 1: Transaction Analytics Service

### 1.1 Interface Definition

```csharp
public interface ITransactionAnalyticsService
{
    /// <summary>
    /// Get balance history for a child over a specified number of days
    /// </summary>
    Task<List<BalancePoint>> GetBalanceHistoryAsync(Guid childId, int days = 30);

    /// <summary>
    /// Get income vs spending summary for a date range
    /// </summary>
    Task<IncomeSpendingSummary> GetIncomeVsSpendingAsync(
        Guid childId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Get spending trend data for analysis
    /// </summary>
    Task<TrendData> GetSpendingTrendAsync(Guid childId, TimePeriod period);

    /// <summary>
    /// Calculate savings rate (money saved vs money earned)
    /// </summary>
    Task<decimal> GetSavingsRateAsync(Guid childId, TimePeriod period);

    /// <summary>
    /// Get monthly comparison data for the last N months
    /// </summary>
    Task<List<MonthlyComparison>> GetMonthlyComparisonAsync(Guid childId, int months = 6);

    /// <summary>
    /// Get transaction count by day for heatmap
    /// </summary>
    Task<Dictionary<DateTime, TransactionDayData>> GetTransactionHeatmapDataAsync(
        Guid childId,
        int days = 365);

    /// <summary>
    /// Get spending breakdown by transaction description patterns
    /// </summary>
    Task<List<CategoryBreakdown>> GetSpendingBreakdownAsync(
        Guid childId,
        DateTime? startDate = null,
        DateTime? endDate = null);
}
```

### 1.2 Data Transfer Objects

```csharp
// Balance history point
public record BalancePoint(
    DateTime Date,
    decimal Balance,
    string? TransactionDescription);

// Income vs spending summary
public record IncomeSpendingSummary(
    decimal TotalIncome,
    decimal TotalSpending,
    decimal NetSavings,
    int IncomeTransactionCount,
    int SpendingTransactionCount,
    decimal SavingsRate);

// Trend analysis
public enum TrendDirection { Up, Down, Stable }

public record TrendData(
    List<DataPoint> Points,
    TrendDirection Direction,
    decimal ChangePercent,
    string Description);

public record DataPoint(
    DateTime Date,
    decimal Value);

// Monthly comparison
public record MonthlyComparison(
    int Year,
    int Month,
    string MonthName,
    decimal Income,
    decimal Spending,
    decimal NetSavings,
    decimal EndingBalance);

// Time period enum
public enum TimePeriod
{
    Week = 7,
    Month = 30,
    Quarter = 90,
    Year = 365,
    AllTime = 0
}

// Transaction day data for heatmap
public record TransactionDayData(
    int TransactionCount,
    decimal TotalAmount,
    decimal NetChange);

// Category breakdown
public record CategoryBreakdown(
    string Category,
    decimal Amount,
    int TransactionCount,
    decimal Percentage);
```

### 1.3 Implementation Requirements

**GetBalanceHistoryAsync**:
- Query transactions for child in date range
- Create daily balance snapshots
- Fill gaps with previous balance (no transaction = same balance)
- Return in chronological order
- Include transaction description for tooltips

**GetIncomeVsSpendingAsync**:
- Aggregate Credits (income) vs Debits (spending)
- Calculate net savings: Income - Spending
- Calculate savings rate: (Net / Income) * 100
- Handle empty transaction list gracefully

**GetSpendingTrendAsync**:
- Calculate spending for each period (weekly, monthly)
- Determine trend direction (up, down, stable)
- Calculate percentage change from previous period
- Return user-friendly description

**GetSavingsRateAsync**:
- Total income vs total spending for period
- Return percentage: ((Income - Spending) / Income) * 100
- Handle division by zero (no income = 0% savings rate)

**GetMonthlyComparisonAsync**:
- Group transactions by month
- Calculate income, spending, balance for each month
- Return last N months in reverse chronological order

### 1.4 Test Cases (12 Tests)

```csharp
// BalanceHistory Tests
[Fact] GetBalanceHistory_WithTransactions_ReturnsCorrectPoints
[Fact] GetBalanceHistory_WithNoTransactions_ReturnsEmptyList
[Fact] GetBalanceHistory_FillsGapsWithPreviousBalance

// Income vs Spending Tests
[Fact] GetIncomeVsSpending_CalculatesCorrectly
[Fact] GetIncomeVsSpending_WithOnlyIncome_ReturnsCorrectSummary
[Fact] GetIncomeVsSpending_WithNoTransactions_ReturnsZeros

// Trend Tests
[Fact] GetSpendingTrend_UpwardTrend_ReturnsCorrectDirection
[Fact] GetSpendingTrend_DownwardTrend_ReturnsCorrectDirection
[Fact] GetSpendingTrend_StableTrend_ReturnsCorrectDirection

// Savings Rate Tests
[Fact] GetSavingsRate_CalculatesCorrectPercentage
[Fact] GetSavingsRate_NoIncome_ReturnsZero

// Monthly Comparison Tests
[Fact] GetMonthlyComparison_ReturnsLastSixMonths
```

---

## Phase 2: Chart Components

### 2.1 Balance History Chart

#### Component: BalanceHistoryChart.razor

**Purpose**: Display balance over time as an interactive line chart

**Features**:
- Smooth line chart showing balance progression
- Date range selector (7, 30, 90, 365 days)
- Interactive tooltips with transaction details
- Green fill for positive trend, red for declining
- Allowance payment markers (vertical lines)
- Zoom and pan capabilities
- Loading state

**Props**:
```csharp
[Parameter] public Guid ChildId { get; set; }
[Parameter] public int DefaultDays { get; set; } = 30;
[Parameter] public bool ShowControls { get; set; } = true;
```

**ApexCharts Configuration**:
```csharp
var options = new ApexChartOptions<BalancePoint>
{
    Chart = new Chart
    {
        Type = ChartType.Area,
        Toolbar = new Toolbar { Show = true },
        Animations = new Animations { Enabled = true }
    },
    Stroke = new Stroke
    {
        Curve = Curve.Smooth,
        Width = 3
    },
    Fill = new Fill
    {
        Type = new List<FillType> { FillType.Gradient },
        Gradient = new FillGradient
        {
            ShadeIntensity = 1,
            OpacityFrom = 0.7,
            OpacityTo = 0.3,
            Stops = new List<double> { 0, 100 }
        }
    },
    Xaxis = new XAxis
    {
        Type = XAxisType.Datetime,
        Labels = new XAxisLabels
        {
            Format = "MMM dd"
        }
    },
    Yaxis = new List<YAxis>
    {
        new YAxis
        {
            Labels = new YAxisLabels
            {
                Formatter = "function(value) { return '$' + value.toFixed(2); }"
            }
        }
    },
    Tooltip = new Tooltip
    {
        Enabled = true,
        X = new TooltipX { Format = "MMM dd, yyyy" }
    },
    Colors = new List<string> { "#10B981" } // Green
};
```

**Component Structure**:
```razor
@using Blazor-ApexCharts
@inject ITransactionAnalyticsService AnalyticsService

<div class="balance-history-chart">
    <div class="chart-header">
        <h4>Balance History</h4>
        @if (ShowControls)
        {
            <div class="btn-group btn-group-sm">
                <button @onclick="() => LoadData(7)">7D</button>
                <button @onclick="() => LoadData(30)">30D</button>
                <button @onclick="() => LoadData(90)">90D</button>
                <button @onclick="() => LoadData(365)">1Y</button>
            </div>
        }
    </div>

    @if (IsLoading)
    {
        <div class="chart-loading">
            <span class="spinner-border"></span>
        </div>
    }
    else if (Data.Any())
    {
        <ApexChart TItem="BalancePoint"
                   Options="@chartOptions"
                   Height="350">
            <ApexPointSeries TItem="BalancePoint"
                            Items="@Data"
                            Name="Balance"
                            SeriesType="SeriesType.Area"
                            XValue="@(e => e.Date)"
                            YValue="@(e => (decimal?)e.Balance)" />
        </ApexChart>
    }
    else
    {
        <div class="chart-empty">
            <p>No transaction history yet.</p>
        </div>
    }
</div>

@code {
    // Component code...
}
```

#### Tests (6 bUnit Tests)

```csharp
[Fact] BalanceHistoryChart_RendersCorrectly
[Fact] BalanceHistoryChart_LoadsDataOnInitialized
[Fact] BalanceHistoryChart_DateRangeButtons_UpdateChart
[Fact] BalanceHistoryChart_EmptyData_ShowsEmptyState
[Fact] BalanceHistoryChart_Loading_ShowsSpinner
[Fact] BalanceHistoryChart_RealTimeUpdate_RefreshesData
```

---

### 2.2 Income vs Spending Chart

#### Component: IncomeSpendingChart.razor

**Purpose**: Compare income (credits) vs spending (debits) over time

**Features**:
- Grouped bar chart (income vs spending side-by-side)
- Period selector (weekly, monthly, yearly)
- Net savings indicator line
- Color-coded bars (green = income, red = spending)
- Percentage breakdown
- Export to CSV option

**Props**:
```csharp
[Parameter] public Guid ChildId { get; set; }
[Parameter] public TimePeriod DefaultPeriod { get; set; } = TimePeriod.Month;
```

**ApexCharts Configuration**:
```csharp
var options = new ApexChartOptions<MonthlyComparison>
{
    Chart = new Chart
    {
        Type = ChartType.Bar,
        Stacked = false
    },
    PlotOptions = new PlotOptions
    {
        Bar = new PlotOptionsBar
        {
            Horizontal = false,
            ColumnWidth = "55%"
        }
    },
    DataLabels = new DataLabels
    {
        Enabled = false
    },
    Stroke = new Stroke
    {
        Show = true,
        Width = 2,
        Colors = new List<string> { "transparent" }
    },
    Xaxis = new XAxis
    {
        Categories = Data.Select(d => d.MonthName).ToList()
    },
    Yaxis = new List<YAxis>
    {
        new YAxis
        {
            Title = new AxisTitle { Text = "Amount ($)" }
        }
    },
    Fill = new Fill
    {
        Opacity = 1
    },
    Tooltip = new Tooltip
    {
        Y = new TooltipY
        {
            Formatter = "function(value) { return '$' + value.toFixed(2); }"
        }
    },
    Colors = new List<string> { "#10B981", "#EF4444", "#3B82F6" } // Green, Red, Blue
};
```

**Series Data**:
```csharp
// Income series
new ApexPointSeries<MonthlyComparison>
{
    Name = "Income",
    Items = Data,
    XValue = d => d.MonthName,
    YValue = d => (decimal?)d.Income
}

// Spending series
new ApexPointSeries<MonthlyComparison>
{
    Name = "Spending",
    Items = Data,
    XValue = d => d.MonthName,
    YValue = d => (decimal?)d.Spending
}

// Net Savings line
new ApexPointSeries<MonthlyComparison>
{
    Name = "Net Savings",
    SeriesType = SeriesType.Line,
    Items = Data,
    XValue = d => d.MonthName,
    YValue = d => (decimal?)d.NetSavings
}
```

#### Tests (5 bUnit Tests)

```csharp
[Fact] IncomeSpendingChart_RendersCorrectly
[Fact] IncomeSpendingChart_ShowsThreeSeries
[Fact] IncomeSpendingChart_PeriodSelector_UpdatesData
[Fact] IncomeSpendingChart_ExportButton_DownloadsCSV
[Fact] IncomeSpendingChart_EmptyState_DisplaysMessage
```

---

### 2.3 Savings Progress Widget

#### Component: SavingsProgressWidget.razor

**Purpose**: Show savings progress toward wish list goals with visual indicators

**Features**:
- Circular progress gauge for overall savings
- Linear progress bars for each wish list item
- Percentage saved
- Estimated time to reach goal
- Mini sparkline showing recent balance trend
- "Add to Wish List" CTA if empty

**Props**:
```csharp
[Parameter] public Guid ChildId { get; set; }
[Parameter] public bool ShowSparkline { get; set; } = true;
```

**Layout**:
```razor
<div class="savings-progress-widget">
    <!-- Overall Balance Gauge -->
    <div class="overall-savings">
        <ApexChart TItem="DataPoint"
                   Options="@gaugeOptions"
                   Height="200">
            <ApexPointSeries TItem="DataPoint"
                            Items="@balanceData"
                            SeriesType="SeriesType.RadialBar"
                            Name="Savings" />
        </ApexChart>
        <div class="balance-amount">
            <h3>@CurrentBalance.ToString("C")</h3>
            <small>Current Balance</small>
        </div>
    </div>

    <!-- Wish List Progress Bars -->
    <div class="wish-list-progress">
        @if (WishListItems.Any())
        {
            foreach (var item in WishListItems)
            {
                <div class="wish-item-progress">
                    <div class="wish-item-header">
                        <span class="wish-item-name">@item.Name</span>
                        <span class="wish-item-price">@item.Price.ToString("C")</span>
                    </div>
                    <div class="progress" style="height: 24px;">
                        <div class="progress-bar @GetProgressClass(item)"
                             style="width: @GetProgressPercent(item)%">
                            @GetProgressPercent(item)%
                        </div>
                    </div>
                    <small class="text-muted">
                        @GetTimeToGoal(item)
                    </small>
                </div>
            }
        }
        else
        {
            <div class="empty-wish-list">
                <p>No savings goals yet!</p>
                <a href="/wishlist/add" class="btn btn-sm btn-primary">
                    Add Wish List Item
                </a>
            </div>
        }
    </div>

    <!-- Recent Trend Sparkline -->
    @if (ShowSparkline && balanceTrend.Any())
    {
        <div class="balance-trend-sparkline">
            <small class="text-muted">Last 7 Days</small>
            <ApexChart TItem="DataPoint"
                       Options="@sparklineOptions"
                       Height="60">
                <ApexPointSeries TItem="DataPoint"
                                Items="@balanceTrend"
                                SeriesType="SeriesType.Line"
                                Name="Balance"
                                XValue="@(e => e.Date)"
                                YValue="@(e => (decimal?)e.Value)" />
            </ApexChart>
        </div>
    }
</div>
```

**Helper Methods**:
```csharp
private decimal GetProgressPercent(WishListItem item)
{
    if (item.Price == 0) return 0;
    var percent = (CurrentBalance / item.Price) * 100;
    return Math.Min(percent, 100); // Cap at 100%
}

private string GetProgressClass(WishListItem item)
{
    var percent = GetProgressPercent(item);
    if (percent >= 100) return "bg-success";
    if (percent >= 75) return "bg-info";
    if (percent >= 50) return "bg-warning";
    return "bg-danger";
}

private string GetTimeToGoal(WishListItem item)
{
    var remaining = item.Price - CurrentBalance;
    if (remaining <= 0) return "Goal reached! 🎉";

    if (WeeklyAllowance == 0) return "Set weekly allowance to estimate";

    var weeksNeeded = Math.Ceiling(remaining / WeeklyAllowance);
    return $"~{weeksNeeded} weeks at current allowance";
}
```

#### Tests (7 bUnit Tests)

```csharp
[Fact] SavingsProgressWidget_RendersGauge
[Fact] SavingsProgressWidget_DisplaysWishListItems
[Fact] SavingsProgressWidget_CalculatesProgressPercent
[Fact] SavingsProgressWidget_ShowsTimeToGoal
[Fact] SavingsProgressWidget_GoalReached_ShowsCelebration
[Fact] SavingsProgressWidget_EmptyWishList_ShowsCTA
[Fact] SavingsProgressWidget_Sparkline_DisplaysTrend
```

---

## Phase 3: Dashboard Integration

### 3.1 Enhanced Child Dashboard

**Location**: `/dashboard` (existing page)

**Layout Update**:
```razor
@page "/dashboard"
@attribute [Authorize]
@inject IFamilyService FamilyService
@inject ITransactionAnalyticsService AnalyticsService

<PageTitle>Dashboard</PageTitle>

<div class="container-fluid">
    <div class="row mb-4">
        <div class="col-md-12">
            <h1>Family Dashboard</h1>
            <div class="float-end">
                <a href="/children/create" class="btn btn-primary">
                    <span class="oi oi-plus"></span> Add Child
                </a>
            </div>
        </div>
    </div>

    @if (Loading)
    {
        <div class="text-center">
            <div class="spinner-border" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    }
    else if (Children.Any())
    {
        <div class="row">
            @foreach (var child in Children)
            {
                <!-- Child Card with Charts -->
                <div class="col-lg-6 mb-4">
                    <div class="card">
                        <div class="card-header">
                            <h4 class="mb-0">@child.FirstName @child.LastName</h4>
                        </div>
                        <div class="card-body">
                            <!-- Existing ChildCard -->
                            <ChildCard Child="@child" OnTransactionAdded="@RefreshData" />

                            <!-- NEW: Charts Accordion -->
                            <div class="accordion mt-3" id="charts-@child.Id">
                                <!-- Balance History -->
                                <div class="accordion-item">
                                    <h2 class="accordion-header">
                                        <button class="accordion-button collapsed"
                                                type="button"
                                                data-bs-toggle="collapse"
                                                data-bs-target="#balance-@child.Id">
                                            📈 Balance History
                                        </button>
                                    </h2>
                                    <div id="balance-@child.Id"
                                         class="accordion-collapse collapse"
                                         data-bs-parent="#charts-@child.Id">
                                        <div class="accordion-body">
                                            <BalanceHistoryChart ChildId="@child.Id" />
                                        </div>
                                    </div>
                                </div>

                                <!-- Income vs Spending -->
                                <div class="accordion-item">
                                    <h2 class="accordion-header">
                                        <button class="accordion-button collapsed"
                                                type="button"
                                                data-bs-toggle="collapse"
                                                data-bs-target="#income-@child.Id">
                                            💰 Income vs Spending
                                        </button>
                                    </h2>
                                    <div id="income-@child.Id"
                                         class="accordion-collapse collapse"
                                         data-bs-parent="#charts-@child.Id">
                                        <div class="accordion-body">
                                            <IncomeSpendingChart ChildId="@child.Id" />
                                        </div>
                                    </div>
                                </div>

                                <!-- Savings Progress -->
                                <div class="accordion-item">
                                    <h2 class="accordion-header">
                                        <button class="accordion-button collapsed"
                                                type="button"
                                                data-bs-toggle="collapse"
                                                data-bs-target="#savings-@child.Id">
                                            🎯 Savings Goals
                                        </button>
                                    </h2>
                                    <div id="savings-@child.Id"
                                         class="accordion-collapse collapse"
                                         data-bs-parent="#charts-@child.Id">
                                        <div class="accordion-body">
                                            <SavingsProgressWidget ChildId="@child.Id" />
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            }
        </div>
    }
    else
    {
        <div class="alert alert-info">
            <p>No children found. Add a child to get started.</p>
        </div>
    }
</div>
```

---

## Real-Time Updates

### SignalR Integration

All charts should automatically refresh when:
- New transaction created
- Allowance paid
- Wish list item updated

**Implementation**:
```csharp
// In chart components
protected override async Task OnInitializedAsync()
{
    _hubConnection = new HubConnectionBuilder()
        .WithUrl(Navigation.ToAbsoluteUri("/familyhub"))
        .WithAutomaticReconnect()
        .Build();

    _hubConnection.On<Guid>("TransactionCreated", async (childId) =>
    {
        if (childId == ChildId)
        {
            await RefreshChartData();
            await InvokeAsync(StateHasChanged);
        }
    });

    await _hubConnection.StartAsync();
    await LoadData();
}
```

---

## Styling & Theming

### CSS Variables

```css
:root {
    --chart-income-color: #10B981;
    --chart-spending-color: #EF4444;
    --chart-balance-color: #3B82F6;
    --chart-savings-color: #8B5CF6;
    --chart-grid-color: #E5E7EB;
    --chart-text-color: #374151;
}

[data-theme="dark"] {
    --chart-grid-color: #374151;
    --chart-text-color: #E5E7EB;
}
```

### Chart Wrapper Styles

```css
.balance-history-chart,
.income-spending-chart,
.savings-progress-widget {
    padding: 1rem;
    background: white;
    border-radius: 8px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.chart-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1rem;
}

.chart-loading {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 300px;
}

.chart-empty {
    text-align: center;
    padding: 2rem;
    color: #6B7280;
}
```

---

## Performance Considerations

### Data Caching
- Cache chart data for 1 minute
- Invalidate cache on transaction creation
- Use `IMemoryCache` for service results

### Lazy Loading
- Charts load on accordion expand
- Prevent loading all charts on page load
- Implement skeleton loaders

### Optimization
- Limit data points (max 365 for line charts)
- Aggregate data for long time periods
- Use database indexes on CreatedAt column

---

## Accessibility

### ARIA Labels
```html
<div role="img" aria-label="Balance history chart showing balance over last 30 days">
    <ApexChart ... />
</div>
```

### Keyboard Navigation
- Tab through chart controls
- Enter/Space to toggle date ranges
- Focus indicators on interactive elements

### Screen Readers
- Provide text alternatives for visual data
- Summary statistics above charts
- Table view toggle option

---

## Testing Strategy

### Service Tests (12 tests)
- TransactionAnalyticsService: Data aggregation accuracy
- Edge cases: empty data, single transaction, leap years
- Performance: Large dataset handling

### Component Tests (18 tests)
- bUnit: Rendering, props, events
- User interactions: Date range selection, filters
- Real-time updates: SignalR message handling
- Loading states and error handling

### Integration Tests (5 tests)
- End-to-end chart rendering with real database
- SignalR broadcast triggers chart update
- Multiple charts on same page

---

## Future Enhancements

### Advanced Analytics
- Spending prediction based on trends
- Anomaly detection (unusual spending)
- Comparative analysis (vs siblings, vs age group)

### Additional Chart Types
- Pie chart for spending categories (when implemented)
- Heatmap calendar for transaction activity
- Radar chart for financial health score

### Interactivity
- Click data point to see transactions
- Drag to zoom on line charts
- Export chart as PNG image

---

## Success Metrics

### Technical
- All 30 tests passing (12 service + 18 component)
- Charts load in <500ms
- Real-time updates within 1 second
- 0 console errors

### User Experience
- Charts render smoothly on mobile
- Tooltips show helpful information
- Color choices accessible (WCAG AA)
- Loading states prevent confusion

---

## Documentation

### User Documentation
- Help tooltips on charts
- "About this chart" modals
- Sample data examples

### Developer Documentation
- Chart component API reference
- Service method documentation
- Integration guide for new charts

---

**Ready to implement!** This spec provides the blueprint for Phase 1-2 of the charts and analytics feature. Next steps:
1. Add NuGet packages
2. Implement TransactionAnalyticsService with TDD
3. Build chart components
4. Integrate into dashboard
5. Write comprehensive tests
6. Document and commit! 🚀
