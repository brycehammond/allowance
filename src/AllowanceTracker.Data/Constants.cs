namespace AllowanceTracker.Data;

/// <summary>
/// Application-wide constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Well-known system user ID for automated/scheduled operations (e.g., weekly allowance).
    /// This user must exist in the AspNetUsers table with the name "Earn & Learn".
    /// </summary>
    public static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");
}
