namespace AllowanceTracker.Models;

/// <summary>
/// Controls what information is visible to gift givers on the portal
/// </summary>
public enum GiftLinkVisibility
{
    /// <summary>
    /// Only shows the child's name
    /// </summary>
    Minimal = 1,

    /// <summary>
    /// Shows child's name and their savings goals
    /// </summary>
    WithGoals = 2,

    /// <summary>
    /// Deprecated: Wish list feature removed. Behaves same as Minimal.
    /// Kept for backward compatibility with existing data.
    /// </summary>
    [Obsolete("Wish list feature removed. Use WithGoals or Minimal instead.")]
    WithWishList = 3,

    /// <summary>
    /// Shows child's name and savings goals (same as WithGoals, wish list removed)
    /// </summary>
    Full = 4
}

/// <summary>
/// Common occasions for gift giving
/// </summary>
public enum GiftOccasion
{
    Birthday = 1,
    Christmas = 2,
    Hanukkah = 3,
    Easter = 4,
    Graduation = 5,
    GoodGrades = 6,
    JustBecause = 7,
    Holiday = 8,
    Achievement = 9,
    Other = 10
}

/// <summary>
/// Status of a gift submission
/// </summary>
public enum GiftStatus
{
    /// <summary>
    /// Gift has been submitted, awaiting parent approval
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Gift has been approved by parent and added to child's balance
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Gift was rejected by parent
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Gift expired without parent action
    /// </summary>
    Expired = 4
}
