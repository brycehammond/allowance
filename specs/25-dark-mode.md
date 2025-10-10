# Dark Mode Support Specification

## Overview

This specification implements comprehensive dark theme support across the entire Allowance Tracker application. The goal is to provide a comfortable viewing experience in low-light environments, reduce eye strain, and offer user preference customization with automatic system detection.

## Goals

1. **System Integration**: Detect and respect OS-level dark mode preference
2. **User Control**: Toggle between light/dark themes manually
3. **Persistence**: Remember user preference across sessions
4. **Smooth Transitions**: Animated theme switching without jarring changes
5. **Chart Adaptation**: Ensure charts remain readable in dark mode
6. **Image Handling**: Adapt logos and images for dark backgrounds
7. **TDD Approach**: 8 comprehensive tests

## Technology Stack

- **CSS**: Custom properties (CSS variables) for theming
- **Storage**: localStorage for preference persistence
- **Detection**: `prefers-color-scheme` media query
- **Charts**: ApexCharts theme customization
- **Testing**: bUnit + Playwright for E2E

---

## Phase 1: CSS Custom Properties

### 1.1 Theme Variables (wwwroot/css/themes.css)

```css
/* ==========================================================================
   Theme Variables - Light & Dark Mode
   ========================================================================== */

:root {
    /* --- Color Palette --- */
    --color-primary: #667eea;
    --color-primary-dark: #5a67d8;
    --color-primary-light: #7c3aed;
    --color-secondary: #764ba2;
    --color-success: #10b981;
    --color-danger: #ef4444;
    --color-warning: #f59e0b;
    --color-info: #3b82f6;

    /* --- Backgrounds --- */
    --bg-primary: #ffffff;
    --bg-secondary: #f8f9fa;
    --bg-tertiary: #e5e7eb;
    --bg-elevated: #ffffff;
    --bg-overlay: rgba(0, 0, 0, 0.5);

    /* --- Text Colors --- */
    --text-primary: #1f2937;
    --text-secondary: #6b7280;
    --text-tertiary: #9ca3af;
    --text-inverse: #ffffff;
    --text-on-primary: #ffffff;

    /* --- Borders --- */
    --border-color: #e5e7eb;
    --border-color-hover: #d1d5db;
    --border-color-focus: #667eea;

    /* --- Shadows --- */
    --shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
    --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
    --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
    --shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.1);

    /* --- Chart Colors --- */
    --chart-income: #10b981;
    --chart-spending: #ef4444;
    --chart-balance: #3b82f6;
    --chart-savings: #8b5cf6;
    --chart-grid: #e5e7eb;
    --chart-text: #374151;
    --chart-background: #ffffff;

    /* --- Component Specific --- */
    --card-bg: #ffffff;
    --card-border: #e5e7eb;
    --input-bg: #ffffff;
    --input-border: #d1d5db;
    --input-text: #1f2937;
    --button-bg: #667eea;
    --button-text: #ffffff;
    --button-hover-bg: #5a67d8;

    /* --- Gradients --- */
    --gradient-primary: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    --gradient-success: linear-gradient(135deg, #10b981 0%, #059669 100%);
    --gradient-info: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
}

/* ==========================================================================
   Dark Mode Variables
   ========================================================================== */

[data-theme="dark"] {
    /* --- Backgrounds --- */
    --bg-primary: #111827;
    --bg-secondary: #1f2937;
    --bg-tertiary: #374151;
    --bg-elevated: #1f2937;
    --bg-overlay: rgba(0, 0, 0, 0.7);

    /* --- Text Colors --- */
    --text-primary: #f9fafb;
    --text-secondary: #d1d5db;
    --text-tertiary: #9ca3af;
    --text-inverse: #1f2937;
    --text-on-primary: #ffffff;

    /* --- Borders --- */
    --border-color: #374151;
    --border-color-hover: #4b5563;
    --border-color-focus: #7c3aed;

    /* --- Shadows (darker in dark mode) --- */
    --shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.3);
    --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.4);
    --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.5);
    --shadow-xl: 0 20px 25px -5px rgba(0, 0, 0, 0.6);

    /* --- Chart Colors (adjusted for dark mode) --- */
    --chart-income: #34d399;
    --chart-spending: #f87171;
    --chart-balance: #60a5fa;
    --chart-savings: #a78bfa;
    --chart-grid: #374151;
    --chart-text: #e5e7eb;
    --chart-background: #1f2937;

    /* --- Component Specific --- */
    --card-bg: #1f2937;
    --card-border: #374151;
    --input-bg: #374151;
    --input-border: #4b5563;
    --input-text: #f9fafb;
    --button-bg: #7c3aed;
    --button-text: #ffffff;
    --button-hover-bg: #6d28d9;

    /* --- Gradients (adjusted) --- */
    --gradient-primary: linear-gradient(135deg, #7c3aed 0%, #8b5cf6 100%);
    --gradient-success: linear-gradient(135deg, #10b981 0%, #34d399 100%);
    --gradient-info: linear-gradient(135deg, #3b82f6 0%, #60a5fa 100%);
}

/* ==========================================================================
   Auto Dark Mode (System Preference)
   ========================================================================== */

@media (prefers-color-scheme: dark) {
    :root:not([data-theme="light"]) {
        /* Copy all dark mode variables here */
        --bg-primary: #111827;
        --bg-secondary: #1f2937;
        --bg-tertiary: #374151;
        --bg-elevated: #1f2937;
        --bg-overlay: rgba(0, 0, 0, 0.7);
        --text-primary: #f9fafb;
        --text-secondary: #d1d5db;
        --text-tertiary: #9ca3af;
        --text-inverse: #1f2937;
        --border-color: #374151;
        --border-color-hover: #4b5563;
        --shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.3);
        --shadow-md: 0 4px 6px -1px rgba(0, 0, 0, 0.4);
        --shadow-lg: 0 10px 15px -3px rgba(0, 0, 0, 0.5);
        --chart-income: #34d399;
        --chart-spending: #f87171;
        --chart-balance: #60a5fa;
        --chart-grid: #374151;
        --chart-text: #e5e7eb;
        --chart-background: #1f2937;
        --card-bg: #1f2937;
        --card-border: #374151;
        --input-bg: #374151;
        --input-border: #4b5563;
        --input-text: #f9fafb;
    }
}

/* ==========================================================================
   Smooth Theme Transitions
   ========================================================================== */

body {
    background-color: var(--bg-primary);
    color: var(--text-primary);
    transition: background-color 0.3s ease, color 0.3s ease;
}

.card,
.btn,
input,
select,
textarea,
.modal,
.navbar {
    transition: background-color 0.3s ease,
                color 0.3s ease,
                border-color 0.3s ease;
}

/* Disable transitions during initial load */
.no-transition,
.no-transition * {
    transition: none !important;
}
```

### 1.2 Apply Variables to Components (wwwroot/css/site.css)

```css
/* Apply theme variables to all components */

body {
    background-color: var(--bg-primary);
    color: var(--text-primary);
}

.card {
    background-color: var(--card-bg);
    border-color: var(--card-border);
    box-shadow: var(--shadow-md);
}

.btn-primary {
    background-color: var(--button-bg);
    color: var(--button-text);
    border-color: var(--button-bg);
}

.btn-primary:hover {
    background-color: var(--button-hover-bg);
    border-color: var(--button-hover-bg);
}

input,
select,
textarea {
    background-color: var(--input-bg);
    color: var(--input-text);
    border-color: var(--input-border);
}

input:focus,
select:focus,
textarea:focus {
    border-color: var(--border-color-focus);
    box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.1);
}

.text-secondary {
    color: var(--text-secondary) !important;
}

.text-muted {
    color: var(--text-tertiary) !important;
}

.border {
    border-color: var(--border-color) !important;
}

/* Links */
a {
    color: var(--color-primary);
}

a:hover {
    color: var(--color-primary-dark);
}

/* Tables */
.table {
    color: var(--text-primary);
}

.table-striped tbody tr:nth-of-type(odd) {
    background-color: var(--bg-secondary);
}

/* Modals */
.modal-content {
    background-color: var(--card-bg);
    color: var(--text-primary);
}

.modal-header {
    border-bottom-color: var(--border-color);
}

.modal-footer {
    border-top-color: var(--border-color);
}
```

---

## Phase 2: Theme Toggle Component

### 2.1 ThemeToggle.razor Component

```razor
<div class="theme-toggle">
    <button class="theme-toggle-button"
            @onclick="ToggleTheme"
            aria-label="@GetAriaLabel()"
            title="@GetTitle()">
        <span class="theme-icon">@GetThemeIcon()</span>
        <span class="theme-label">@GetThemeLabel()</span>
    </button>
</div>

@code {
    [Parameter] public EventCallback<string> OnThemeChanged { get; set; }

    private string CurrentTheme { get; set; } = "auto";

    protected override async Task OnInitializedAsync()
    {
        // Load saved theme preference
        CurrentTheme = await ThemeService.GetThemeAsync();
        await ApplyTheme(CurrentTheme);
    }

    private async Task ToggleTheme()
    {
        // Cycle through: auto → light → dark → auto
        CurrentTheme = CurrentTheme switch
        {
            "auto" => "light",
            "light" => "dark",
            "dark" => "auto",
            _ => "auto"
        };

        await ApplyTheme(CurrentTheme);
        await ThemeService.SaveThemeAsync(CurrentTheme);
        await OnThemeChanged.InvokeAsync(CurrentTheme);
    }

    private async Task ApplyTheme(string theme)
    {
        await JS.InvokeVoidAsync("applyTheme", theme);
    }

    private string GetThemeIcon()
    {
        return CurrentTheme switch
        {
            "light" => "☀️",
            "dark" => "🌙",
            "auto" => "🔄",
            _ => "🔄"
        };
    }

    private string GetThemeLabel()
    {
        return CurrentTheme switch
        {
            "light" => "Light",
            "dark" => "Dark",
            "auto" => "Auto",
            _ => "Auto"
        };
    }

    private string GetAriaLabel()
    {
        return $"Switch to {GetNextTheme()} mode";
    }

    private string GetTitle()
    {
        return $"Current: {GetThemeLabel()}. Click to switch.";
    }

    private string GetNextTheme()
    {
        return CurrentTheme switch
        {
            "auto" => "light",
            "light" => "dark",
            "dark" => "auto",
            _ => "auto"
        };
    }
}
```

**CSS** (`ThemeToggle.razor.css`):

```css
.theme-toggle {
    display: inline-block;
}

.theme-toggle-button {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 1rem;
    background-color: var(--bg-secondary);
    border: 1px solid var(--border-color);
    border-radius: 8px;
    color: var(--text-primary);
    cursor: pointer;
    transition: all 0.3s ease;
    min-height: 44px;
    min-width: 44px;
}

.theme-toggle-button:hover {
    background-color: var(--bg-tertiary);
    border-color: var(--border-color-hover);
}

.theme-toggle-button:focus {
    outline: none;
    border-color: var(--border-color-focus);
    box-shadow: 0 0 0 3px rgba(124, 58, 237, 0.2);
}

.theme-icon {
    font-size: 1.25rem;
    line-height: 1;
}

.theme-label {
    font-weight: 600;
    font-size: 0.875rem;
}

@media (max-width: 768px) {
    .theme-label {
        display: none;
    }
}
```

### 2.2 IThemeService Interface

```csharp
namespace AllowanceTracker.Services;

public interface IThemeService
{
    /// <summary>
    /// Get current theme preference
    /// </summary>
    Task<string> GetThemeAsync();

    /// <summary>
    /// Save theme preference to localStorage
    /// </summary>
    Task SaveThemeAsync(string theme);

    /// <summary>
    /// Detect system theme preference
    /// </summary>
    Task<bool> IsSystemDarkModeAsync();
}
```

### 2.3 ThemeService Implementation

```csharp
using Microsoft.JSInterop;

namespace AllowanceTracker.Services;

public class ThemeService : IThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private const string THEME_KEY = "allowancetracker_theme";

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetThemeAsync()
    {
        try
        {
            var theme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", THEME_KEY);
            return theme ?? "auto"; // Default to auto
        }
        catch
        {
            return "auto";
        }
    }

    public async Task SaveThemeAsync(string theme)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", THEME_KEY, theme);
    }

    public async Task<bool> IsSystemDarkModeAsync()
    {
        return await _jsRuntime.InvokeAsync<bool>("matchMedia", "(prefers-color-scheme: dark)");
    }
}
```

### 2.4 JavaScript Helper (wwwroot/js/theme.js)

```javascript
// Theme Management Functions

function applyTheme(theme) {
    const root = document.documentElement;

    // Remove existing theme
    root.removeAttribute('data-theme');

    if (theme === 'light') {
        root.setAttribute('data-theme', 'light');
    } else if (theme === 'dark') {
        root.setAttribute('data-theme', 'dark');
    }
    // If 'auto', no attribute is set, CSS media query handles it

    // Remove no-transition class after theme is applied
    setTimeout(() => {
        document.body.classList.remove('no-transition');
    }, 100);
}

function getSystemTheme() {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function watchSystemTheme(callback) {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    mediaQuery.addEventListener('change', (e) => {
        const newTheme = e.matches ? 'dark' : 'light';
        callback(newTheme);
    });
}

// Initialize theme on page load
(function initTheme() {
    // Prevent flash of unstyled content
    document.body.classList.add('no-transition');

    const savedTheme = localStorage.getItem('allowancetracker_theme') || 'auto';
    applyTheme(savedTheme);
})();
```

---

## Phase 3: Chart Theme Adaptation

### 3.1 Update ApexCharts Configuration

```csharp
// In chart components, update chart options based on theme

private ApexChartOptions<DataPoint> GetChartOptions()
{
    var isDarkMode = IsCurrentlyDarkMode();

    return new ApexChartOptions<DataPoint>
    {
        Chart = new Chart
        {
            Background = isDarkMode ? "#1f2937" : "#ffffff",
            ForeColor = isDarkMode ? "#e5e7eb" : "#374151"
        },
        Theme = new Theme
        {
            Mode = isDarkMode ? Mode.Dark : Mode.Light
        },
        Grid = new Grid
        {
            BorderColor = isDarkMode ? "#374151" : "#e5e7eb"
        },
        Xaxis = new XAxis
        {
            Labels = new XAxisLabels
            {
                Style = new AxisLabelStyle
                {
                    Colors = isDarkMode ? "#d1d5db" : "#6b7280"
                }
            }
        },
        Yaxis = new List<YAxis>
        {
            new YAxis
            {
                Labels = new YAxisLabels
                {
                    Style = new AxisLabelStyle
                    {
                        Colors = isDarkMode ? "#d1d5db" : "#6b7280"
                    }
                }
            }
        },
        Tooltip = new Tooltip
        {
            Theme = isDarkMode ? "dark" : "light"
        },
        Colors = new List<string>
        {
            isDarkMode ? "#34d399" : "#10b981", // Income
            isDarkMode ? "#f87171" : "#ef4444", // Spending
            isDarkMode ? "#60a5fa" : "#3b82f6"  // Balance
        }
    };
}

private bool IsCurrentlyDarkMode()
{
    // Check data-theme attribute or system preference
    var theme = GetCurrentTheme();
    if (theme == "dark") return true;
    if (theme == "light") return false;

    // Auto: check system preference
    return IsSystemDarkMode();
}
```

### 3.2 Chart Theme Service

```csharp
namespace AllowanceTracker.Services;

public interface IChartThemeService
{
    /// <summary>
    /// Get chart colors based on current theme
    /// </summary>
    ChartThemeColors GetColors();

    /// <summary>
    /// Get ApexCharts theme mode
    /// </summary>
    Mode GetChartMode();

    /// <summary>
    /// Subscribe to theme changes
    /// </summary>
    event EventHandler<string>? ThemeChanged;
}

public record ChartThemeColors(
    string Income,
    string Spending,
    string Balance,
    string Savings,
    string Grid,
    string Text,
    string Background);

public class ChartThemeService : IChartThemeService
{
    private readonly IThemeService _themeService;
    public event EventHandler<string>? ThemeChanged;

    public ChartThemeService(IThemeService themeService)
    {
        _themeService = themeService;
    }

    public ChartThemeColors GetColors()
    {
        var isDark = IsDarkMode();

        return new ChartThemeColors(
            Income: isDark ? "#34d399" : "#10b981",
            Spending: isDark ? "#f87171" : "#ef4444",
            Balance: isDark ? "#60a5fa" : "#3b82f6",
            Savings: isDark ? "#a78bfa" : "#8b5cf6",
            Grid: isDark ? "#374151" : "#e5e7eb",
            Text: isDark ? "#e5e7eb" : "#374151",
            Background: isDark ? "#1f2937" : "#ffffff"
        );
    }

    public Mode GetChartMode()
    {
        return IsDarkMode() ? Mode.Dark : Mode.Light;
    }

    public void NotifyThemeChanged(string newTheme)
    {
        ThemeChanged?.Invoke(this, newTheme);
    }

    private bool IsDarkMode()
    {
        var theme = _themeService.GetThemeAsync().Result;
        return theme == "dark";
    }
}
```

---

## Phase 4: Image & Logo Adaptation

### 4.1 SVG Logo with Theme Support

```html
<!-- Logo that adapts to theme -->
<svg class="logo-svg" viewBox="0 0 100 100">
    <style>
        .logo-primary { fill: var(--color-primary); }
        .logo-secondary { fill: var(--color-secondary); }
        .logo-text { fill: var(--text-primary); }
    </style>
    <!-- SVG paths here with theme-aware classes -->
    <path class="logo-primary" d="..." />
    <path class="logo-secondary" d="..." />
    <text class="logo-text" x="50" y="50">AT</text>
</svg>
```

### 4.2 Image Inversion for Icons

```css
/* Invert icons in dark mode */
[data-theme="dark"] .icon-image {
    filter: invert(1) hue-rotate(180deg);
}

/* Specific exclusions */
[data-theme="dark"] .icon-image.no-invert {
    filter: none;
}

/* Adjust opacity for better visibility */
[data-theme="dark"] img {
    opacity: 0.9;
}

[data-theme="dark"] img:hover {
    opacity: 1;
}
```

---

## Phase 5: Testing Strategy

### 5.1 Unit Tests (8 Tests Total)

```csharp
namespace AllowanceTracker.Tests.Services;

public class ThemeServiceTests
{
    private readonly Mock<IJSRuntime> _mockJS;
    private readonly ThemeService _themeService;

    public ThemeServiceTests()
    {
        _mockJS = new Mock<IJSRuntime>();
        _themeService = new ThemeService(_mockJS.Object);
    }

    [Fact]
    public async Task GetThemeAsync_NoSavedPreference_ReturnsAuto()
    {
        // Arrange
        _mockJS.Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync((string)null!);

        // Act
        var theme = await _themeService.GetThemeAsync();

        // Assert
        theme.Should().Be("auto");
    }

    [Fact]
    public async Task GetThemeAsync_SavedDark_ReturnsDark()
    {
        // Arrange
        _mockJS.Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync("dark");

        // Act
        var theme = await _themeService.GetThemeAsync();

        // Assert
        theme.Should().Be("dark");
    }

    [Fact]
    public async Task SaveThemeAsync_ValidTheme_CallsLocalStorage()
    {
        // Arrange
        var theme = "dark";

        // Act
        await _themeService.SaveThemeAsync(theme);

        // Assert
        _mockJS.Verify(x => x.InvokeVoidAsync(
            "localStorage.setItem",
            It.Is<object[]>(args =>
                args[0].ToString() == "allowancetracker_theme" &&
                args[1].ToString() == "dark")),
            Times.Once);
    }

    [Fact]
    public async Task IsSystemDarkModeAsync_DarkMode_ReturnsTrue()
    {
        // Arrange
        _mockJS.Setup(x => x.InvokeAsync<bool>("matchMedia", It.IsAny<object[]>()))
            .ReturnsAsync(true);

        // Act
        var isDark = await _themeService.IsSystemDarkModeAsync();

        // Assert
        isDark.Should().BeTrue();
    }
}

public class ThemeToggleTests : TestContext
{
    [Fact]
    public void ThemeToggle_InitialRender_ShowsCurrentTheme()
    {
        // Arrange
        var mockThemeService = new Mock<IThemeService>();
        mockThemeService.Setup(x => x.GetThemeAsync()).ReturnsAsync("light");
        Services.AddSingleton(mockThemeService.Object);

        // Act
        var component = RenderComponent<ThemeToggle>();

        // Assert
        component.Find(".theme-label").TextContent.Should().Be("Light");
        component.Find(".theme-icon").TextContent.Should().Be("☀️");
    }

    [Fact]
    public async Task ThemeToggle_ClickButton_CyclesThroughThemes()
    {
        // Arrange
        var mockThemeService = new Mock<IThemeService>();
        mockThemeService.Setup(x => x.GetThemeAsync()).ReturnsAsync("auto");
        Services.AddSingleton(mockThemeService.Object);

        var component = RenderComponent<ThemeToggle>();

        // Act - First click: auto → light
        component.Find(".theme-toggle-button").Click();

        // Assert
        mockThemeService.Verify(x => x.SaveThemeAsync("light"), Times.Once);
    }

    [Fact]
    public void ThemeToggle_HasAccessibleLabel()
    {
        // Test ARIA attributes
    }

    [Fact]
    public void ThemeToggle_MeetsTouchTargetSize()
    {
        // Test minimum 44x44px size
    }
}

public class ChartThemeServiceTests
{
    [Fact]
    public void GetColors_LightMode_ReturnsLightColors()
    {
        // Arrange
        var mockThemeService = new Mock<IThemeService>();
        mockThemeService.Setup(x => x.GetThemeAsync()).ReturnsAsync("light");
        var chartThemeService = new ChartThemeService(mockThemeService.Object);

        // Act
        var colors = chartThemeService.GetColors();

        // Assert
        colors.Income.Should().Be("#10b981");
        colors.Spending.Should().Be("#ef4444");
        colors.Background.Should().Be("#ffffff");
    }

    [Fact]
    public void GetColors_DarkMode_ReturnsDarkColors()
    {
        // Arrange
        var mockThemeService = new Mock<IThemeService>();
        mockThemeService.Setup(x => x.GetThemeAsync()).ReturnsAsync("dark");
        var chartThemeService = new ChartThemeService(mockThemeService.Object);

        // Act
        var colors = chartThemeService.GetColors();

        // Assert
        colors.Income.Should().Be("#34d399");
        colors.Spending.Should().Be("#f87171");
        colors.Background.Should().Be("#1f2937");
    }
}
```

---

## Phase 6: Integration & Registration

### 6.1 Update Program.cs

```csharp
// Register theme service
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IChartThemeService, ChartThemeService>();
```

### 6.2 Update MainLayout.razor

```razor
@inherits LayoutComponentBase
@inject IThemeService ThemeService

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <main>
        <div class="top-row px-4">
            <!-- Add theme toggle to header -->
            <ThemeToggle OnThemeChanged="@HandleThemeChanged" />
            <a href="https://docs.microsoft.com/aspnet/" target="_blank">About</a>
        </div>

        <article class="content px-4">
            @Body
        </article>
    </main>
</div>

@code {
    private async Task HandleThemeChanged(string newTheme)
    {
        // Notify chart components or other theme-aware components
        await InvokeAsync(StateHasChanged);
    }
}
```

---

## Success Metrics

### Technical
- ✅ All 8 tests passing
- ✅ Theme persists across sessions
- ✅ System theme detected correctly
- ✅ Smooth transitions without flicker
- ✅ Charts readable in both modes
- ✅ No FOUC (Flash of Unstyled Content)
- ✅ Accessibility maintained in both themes

### User Experience
- ✅ Theme toggle is easily discoverable
- ✅ Current theme is clearly indicated
- ✅ Transitions are smooth and pleasant
- ✅ All content remains readable
- ✅ Images and icons look good in both modes
- ✅ Charts are visually appealing in dark mode
- ✅ Preference is remembered

---

## Browser Compatibility

- Chrome 76+ (CSS variables, prefers-color-scheme)
- Firefox 67+
- Safari 12.1+
- Edge 79+
- Mobile browsers: iOS 12.2+, Android 76+

---

## Performance Considerations

- CSS variables have minimal performance impact
- localStorage reads/writes are async
- Theme transitions use CSS, hardware-accelerated
- No JavaScript required for initial theme detection
- Lazy load theme preference from localStorage

---

## Future Enhancements

1. **Custom Color Schemes**: Let users pick accent colors
2. **Scheduled Theme**: Auto-switch at sunset/sunrise
3. **High Contrast Mode**: Enhanced accessibility option
4. **Reading Mode**: Simplified, distraction-free view
5. **Theme Presets**: Multiple color palettes (Ocean, Forest, etc.)

---

**Total Implementation Time**: 1 week
**Priority**: Medium - Nice to have, improves UX
**Dependencies**: None - can be implemented independently
