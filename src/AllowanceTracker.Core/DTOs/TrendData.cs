namespace AllowanceTracker.DTOs;

/// <summary>
/// Trend direction indicator
/// </summary>
public enum TrendDirection
{
    Up,
    Down,
    Stable
}

/// <summary>
/// Trend analysis data with direction and percentage change
/// </summary>
public record TrendData(
    List<DataPoint> Points,
    TrendDirection Direction,
    decimal ChangePercent,
    string Description);

/// <summary>
/// Basic data point for charts
/// </summary>
public record DataPoint(
    DateTime Date,
    decimal Value);
