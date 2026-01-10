using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AllowanceTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AchievementBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AvailablePoints",
                table: "Children",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EquippedAvatarUrl",
                table: "Children",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquippedTheme",
                table: "Children",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquippedTitle",
                table: "Children",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSavingDate",
                table: "Children",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SavingStreak",
                table: "Children",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPoints",
                table: "Children",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Badges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Rarity = table.Column<int>(type: "int", nullable: false),
                    PointsValue = table.Column<int>(type: "int", nullable: false),
                    CriteriaType = table.Column<int>(type: "int", nullable: false),
                    CriteriaConfig = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsSecret = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PreviewUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PointsCost = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BadgeProgressRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentProgress = table.Column<int>(type: "int", nullable: false),
                    TargetProgress = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeProgressRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BadgeProgressRecords_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BadgeProgressRecords_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChildBadges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDisplayed = table.Column<bool>(type: "bit", nullable: false),
                    IsNew = table.Column<bool>(type: "bit", nullable: false),
                    EarnedContext = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildBadges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildBadges_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildBadges_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChildRewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RewardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsEquipped = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChildRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChildRewards_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChildRewards_Rewards_RewardId",
                        column: x => x.RewardId,
                        principalTable: "Rewards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "Category", "Code", "CreatedAt", "CriteriaConfig", "CriteriaType", "Description", "IconUrl", "IsActive", "IsSecret", "Name", "PointsValue", "Rarity", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("0ba7a61e-8018-dcd2-f80a-809d89f74604"), 3, "GOAL_SETTER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(2840), "{\"ActionType\":\"first_goal_created\",\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[3]}", 1, "Created your first savings goal", "/badges/goal-setter.png", true, false, "Goal Setter", 10, 1, 8 },
                    { new Guid("1257ae0c-1497-74cc-7a46-4f1d077c06a4"), 5, "UNSTOPPABLE", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4140), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":26,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"saving_streak\",\"Triggers\":[9]}", 4, "Saved for 26 weeks in a row", "/badges/unstoppable.png", true, false, "Unstoppable", 100, 4, 21 },
                    { new Guid("177b728d-d385-7955-e007-f6bf21ff732d"), 4, "HELPER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(3640), "{\"ActionType\":\"first_task_completed\",\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[5]}", 1, "Completed your first task", "/badges/helper.png", true, false, "Helper", 10, 1, 13 },
                    { new Guid("3e795237-3ba8-e026-b58b-e35fbe16f6bf"), 1, "FIRST_SAVER", new DateTime(2026, 1, 10, 18, 52, 34, 785, DateTimeKind.Utc).AddTicks(9980), "{\"ActionType\":\"first_savings_deposit\",\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[2]}", 1, "Made your first deposit to savings", "/badges/first-saver.png", true, false, "First Saver", 10, 1, 0 },
                    { new Guid("4015d963-8ddf-e636-df08-50bfbbff6dd3"), 4, "PERFECT_RECORD", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4050), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":10,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"approved_task_streak\",\"Triggers\":[6]}", 4, "Had 10 tasks approved in a row", "/badges/perfect-record.png", true, false, "Perfect Record", 40, 3, 17 },
                    { new Guid("45c96479-6e6f-79bd-fc9f-c1d3c658810e"), 1, "MONEY_STACKER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(850), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":50,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"total_saved\",\"Triggers\":[2]}", 3, "Saved $50 total", "/badges/money-stacker.png", true, false, "Money Stacker", 25, 2, 2 },
                    { new Guid("46c52b4d-7df4-0896-2f0a-d25ae02e5e9f"), 6, "FIFTY_CLUB", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4250), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":50,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"current_balance\",\"Triggers\":[8]}", 3, "Reached $50 balance", "/badges/fifty-club.png", true, false, "Fifty Club", 25, 2, 25 },
                    { new Guid("4866d3c4-bbc8-4d9b-afd9-0d9cc99ac34f"), 2, "TRANSACTION_TRACKER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4380), "{\"ActionType\":null,\"CountTarget\":200,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"transaction_count\",\"Triggers\":[1]}", 2, "Tracked 200 transactions", "/badges/transaction-tracker.png", true, false, "Transaction Tracker", 50, 3, 31 },
                    { new Guid("4b15c8dc-5ca7-d7dd-fceb-6cb0fea7e497"), 3, "DREAM_ACHIEVER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(3050), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":5,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[4]}", 6, "Completed 5 savings goals", "/badges/dream-achiever.png", true, false, "Dream Achiever", 50, 3, 10 },
                    { new Guid("4ea2615b-86b3-54bb-4ae0-ac614fbf0246"), 5, "STREAK_STARTER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4080), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":2,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"saving_streak\",\"Triggers\":[9]}", 4, "Saved for 2 weeks in a row", "/badges/streak-starter.png", true, false, "Streak Starter", 15, 1, 18 },
                    { new Guid("5452e4f6-3c2d-b7ef-aa46-4c6343c2c03c"), 7, "BIRTHDAY_BONUS", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4440), "{\"ActionType\":\"birthday_gift\",\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[1]}", 1, "Received a gift on your birthday", "/badges/birthday-bonus.png", true, true, "Birthday Bonus", 25, 2, 33 },
                    { new Guid("61a2ef53-8f21-1b35-9845-051e329b2974"), 1, "SAVINGS_CHAMPION", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(910), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":500,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"total_saved\",\"Triggers\":[2]}", 3, "Saved $500 total", "/badges/savings-champion.png", true, false, "Savings Champion", 100, 4, 4 },
                    { new Guid("691b08a9-2278-4791-ff01-3309151236c2"), 6, "HIGH_ROLLER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4300), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":500,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"current_balance\",\"Triggers\":[8]}", 3, "Reached $500 balance", "/badges/high-roller.png", true, false, "High Roller", 100, 4, 27 },
                    { new Guid("6d2551f8-2aff-2588-ad4a-f615111b6a4f"), 7, "WELCOME", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4420), "{\"ActionType\":\"account_created\",\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[11]}", 1, "Joined the app", "/badges/welcome.png", true, false, "Welcome", 5, 1, 32 },
                    { new Guid("7b7830f5-684f-9390-385d-d262a49c61be"), 4, "CHORE_CHAMPION", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(3840), "{\"ActionType\":null,\"CountTarget\":50,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"task_count\",\"Triggers\":[6]}", 2, "Completed 50 tasks", "/badges/chore-champion.png", true, false, "Chore Champion", 50, 3, 15 },
                    { new Guid("8a6052ee-1886-3bdd-dee8-8ba0f0c70564"), 2, "SMART_SPENDER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4360), "{\"ActionType\":null,\"CountTarget\":50,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"transaction_count\",\"Triggers\":[1]}", 2, "Tracked 50 transactions", "/badges/smart-spender.png", true, false, "Smart Spender", 25, 2, 30 },
                    { new Guid("8e8a432c-1d3e-8304-8cdf-22dd17b222f4"), 1, "SAVINGS_STAR", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(880), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":100,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"total_saved\",\"Triggers\":[2]}", 3, "Saved $100 total", "/badges/savings-star.png", true, false, "Savings Star", 50, 3, 3 },
                    { new Guid("94f6c5a0-d249-8cc9-010d-ceb5141eec49"), 2, "BUDGET_BOSS", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4340), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":4,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"budget_streak\",\"Triggers\":[10]}", 4, "Stayed under budget for 4 weeks", "/badges/budget-boss.png", true, false, "Budget Boss", 50, 3, 29 },
                    { new Guid("96f3ee74-8ec5-7bf7-5a08-533aadd2852b"), 4, "HARD_WORKER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(3820), "{\"ActionType\":null,\"CountTarget\":10,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"task_count\",\"Triggers\":[6]}", 2, "Completed 10 tasks", "/badges/hard-worker.png", true, false, "Hard Worker", 20, 1, 14 },
                    { new Guid("a24ac38f-3e99-b0bb-3edf-82fbd9365b59"), 1, "EARLY_BIRD", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(1120), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":\"same_day_as_allowance\",\"MeasureField\":null,\"Triggers\":[2]}", 7, "Saved on the same day as allowance", "/badges/early-bird.png", true, false, "Early Bird", 20, 2, 5 },
                    { new Guid("a435db6e-f748-90d6-8e6c-378fe26da318"), 3, "GOAL_CRUSHER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(3020), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":1,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[4]}", 6, "Completed your first savings goal", "/badges/goal-crusher.png", true, false, "Goal Crusher", 20, 1, 9 },
                    { new Guid("aa53f3c1-66ce-1ff2-8518-b5b087ed579f"), 3, "WISHLIST_WINNER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(3600), "{\"ActionType\":\"first_wishlist_purchase\",\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[12]}", 1, "Purchased an item from your wishlist", "/badges/wishlist-winner.png", true, false, "Wishlist Winner", 15, 1, 12 },
                    { new Guid("aa7ae5ae-f2cc-2b56-84cc-001ef1404e63"), 1, "SUPER_SAVER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(2490), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":50,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"monthly_savings_rate\",\"Triggers\":[9]}", 5, "Saved 50% of allowance in a month", "/badges/super-saver.png", true, false, "Super Saver", 40, 3, 6 },
                    { new Guid("aac58817-2b3b-8119-bde6-45455493ddfa"), 2, "BUDGET_AWARE", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4320), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":1,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"budget_streak\",\"Triggers\":[10]}", 4, "Stayed under budget for a week", "/badges/budget-aware.png", true, false, "Budget Aware", 15, 1, 28 },
                    { new Guid("ae843773-43db-11cd-8bf0-dec76b9751fd"), 1, "FRUGAL_MASTER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(2530), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":75,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"monthly_savings_rate\",\"Triggers\":[9]}", 5, "Saved 75% of allowance in a month", "/badges/frugal-master.png", true, false, "Frugal Master", 75, 4, 7 },
                    { new Guid("b419f5e0-33b6-2aa3-661e-c507eeedb646"), 5, "CONSISTENCY_KING", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4100), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":4,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"saving_streak\",\"Triggers\":[9]}", 4, "Saved for 4 weeks in a row", "/badges/consistency-king.png", true, false, "Consistency King", 30, 2, 19 },
                    { new Guid("b80f47ec-6c73-4fdb-1d5e-f83d7fb36910"), 1, "PENNY_PINCHER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(740), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":10,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"total_saved\",\"Triggers\":[2]}", 3, "Saved $10 total", "/badges/penny-pincher.png", true, false, "Penny Pincher", 15, 1, 1 },
                    { new Guid("d69f88ad-827f-ac88-4562-8af20c52e364"), 5, "STREAK_MASTER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4120), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":10,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"saving_streak\",\"Triggers\":[9]}", 4, "Saved for 10 weeks in a row", "/badges/streak-master.png", true, false, "Streak Master", 60, 3, 20 },
                    { new Guid("d6eba9ac-727b-d6ff-8208-e156de521e77"), 3, "GOAL_MACHINE", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(3090), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":10,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[4]}", 6, "Completed 10 savings goals", "/badges/goal-machine.png", true, false, "Goal Machine", 100, 4, 11 },
                    { new Guid("d8e575ed-511d-f5a5-d4f4-8be100b2a1ba"), 6, "FIRST_PURCHASE", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4210), "{\"ActionType\":\"first_transaction\",\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[1]}", 1, "Made your first transaction", "/badges/first-purchase.png", true, false, "First Purchase", 5, 1, 23 },
                    { new Guid("db6341ec-c478-db02-1e0b-a9121471d543"), 6, "DOUBLE_DIGITS", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4230), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":10,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"current_balance\",\"Triggers\":[8]}", 3, "Reached $10 balance", "/badges/double-digits.png", true, false, "Double Digits", 10, 1, 24 },
                    { new Guid("dd4bf189-e627-2b4e-e733-49ebbddc8f78"), 5, "LEGENDARY_STREAK", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4180), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":52,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"saving_streak\",\"Triggers\":[9]}", 4, "Saved for 52 weeks in a row", "/badges/legendary-streak.png", true, false, "Legendary Streak", 200, 5, 22 },
                    { new Guid("e1c4462b-6192-c704-338d-a6408ffae517"), 4, "TASK_MASTER", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(3870), "{\"ActionType\":null,\"CountTarget\":100,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"task_count\",\"Triggers\":[6]}", 2, "Completed 100 tasks", "/badges/task-master.png", true, false, "Task Master", 100, 4, 16 },
                    { new Guid("e5e42864-3a77-c5d1-097d-29c30b065035"), 6, "CENTURY_CLUB", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4270), "{\"ActionType\":null,\"CountTarget\":null,\"AmountTarget\":100,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":\"current_balance\",\"Triggers\":[8]}", 3, "Reached $100 balance", "/badges/century-club.png", true, false, "Century Club", 50, 3, 26 },
                    { new Guid("f62e4765-328a-8d50-6ac6-2f2a2be9ad74"), 7, "FAMILY_FIRST", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4480), "{\"ActionType\":\"family_goal_participant\",\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[3]}", 1, "Part of a family savings goal", "/badges/family-first.png", true, false, "Family First", 40, 3, 35 },
                    { new Guid("feaeb883-b9d3-4026-573c-18a75502f253"), 7, "GENEROUS_HEART", new DateTime(2026, 1, 10, 18, 52, 34, 787, DateTimeKind.Utc).AddTicks(4460), "{\"ActionType\":\"sibling_transfer\",\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[1]}", 1, "Gave money to a sibling", "/badges/generous-heart.png", true, false, "Generous Heart", 40, 3, 34 }
                });

            migrationBuilder.InsertData(
                table: "Rewards",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "PointsCost", "PreviewUrl", "SortOrder", "Type", "Value" },
                values: new object[,]
                {
                    { new Guid("064bd1e7-1056-023b-21fd-7c2bb12520c7"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(540), "The 'Money Expert' title", true, "Money Expert", 100, null, 12, 3, "Money Expert" },
                    { new Guid("10318481-c7a3-ecb9-7588-1936fd291dac"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(670), "A silver profile frame", true, "Silver Frame", 60, "/previews/frame-silver.png", 16, 4, "frame-silver" },
                    { new Guid("2c1d8188-ba97-adb6-da74-8664e1a05224"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(540), "The 'Budget Master' title", true, "Budget Master", 50, null, 11, 3, "Budget Master" },
                    { new Guid("3f67f6d4-dd76-c4ae-dc64-6e7975f1f627"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(530), "The 'Saver' title", true, "Saver", 25, null, 10, 3, "Saver" },
                    { new Guid("49939a7c-a060-d94f-bcdc-26ef9e7673ff"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(160), "A stylish cat avatar", true, "Cool Cat", 25, "/previews/cool-cat.png", 0, 1, "avatars/cool-cat.png" },
                    { new Guid("4a2c0201-f090-8b1c-8d61-5cea12ddcab6"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(340), "A professional piggy bank", true, "Piggy Pro", 75, "/previews/piggy-pro.png", 3, 1, "avatars/piggy-pro.png" },
                    { new Guid("8ed2e83e-b578-f6f8-b4c5-9e3d720816bf"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(500), "A warm sunset theme", true, "Sunset Orange", 75, "/previews/theme-sunset.png", 7, 2, "theme-sunset" },
                    { new Guid("8f09b25c-3e45-6e51-d801-1ab3668a6457"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(510), "A cosmic galaxy theme", true, "Galaxy Purple", 100, "/previews/theme-galaxy.png", 8, 2, "theme-galaxy" },
                    { new Guid("910aca51-0e61-4939-dbf6-7a6359276e64"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(300), "A shining star avatar", true, "Super Star", 50, "/previews/super-star.png", 1, 1, "avatars/super-star.png" },
                    { new Guid("954ac8be-7c0e-857f-b623-a4e849586c66"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(660), "A bronze profile frame", true, "Bronze Frame", 30, "/previews/frame-bronze.png", 15, 4, "frame-bronze" },
                    { new Guid("a2d5fdd1-678c-db5e-ef70-3eac3797d750"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(490), "A refreshing forest theme", true, "Forest Green", 50, "/previews/theme-forest.png", 6, 2, "theme-forest" },
                    { new Guid("aa928716-6329-dac8-9deb-0d06ffa0f6ec"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(680), "A gold profile frame", true, "Gold Frame", 100, "/previews/frame-gold.png", 17, 4, "frame-gold" },
                    { new Guid("b93d4812-7235-b5bb-394c-c8ae90717f54"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(350), "A coin collector character", true, "Coin Collector", 150, "/previews/coin-collector.png", 4, 1, "avatars/coin-collector.png" },
                    { new Guid("c4d90196-6440-b7c2-725b-6f320f61df1e"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(550), "The 'Financial Wizard' title", true, "Financial Wizard", 150, null, 13, 3, "Financial Wizard" },
                    { new Guid("c676a7fb-4d78-cd7c-15ad-d4c5c2a47d81"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(680), "A diamond profile frame", true, "Diamond Frame", 200, "/previews/frame-diamond.png", 18, 4, "frame-diamond" },
                    { new Guid("c9439d0e-867d-7e7a-d3d9-c4bfa39dbf68"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(330), "A dragon guarding treasure", true, "Money Dragon", 100, "/previews/money-dragon.png", 2, 1, "avatars/money-dragon.png" },
                    { new Guid("f4a91b79-f29d-d613-9279-2c0bdf98eacb"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(520), "A premium gold theme", true, "Golden Luxury", 200, "/previews/theme-gold.png", 9, 2, "theme-gold" },
                    { new Guid("fd7705fb-e17b-9df1-0ac3-79b4556523d0"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(480), "A calming ocean theme", true, "Ocean Blue", 50, "/previews/theme-ocean.png", 5, 2, "theme-ocean" },
                    { new Guid("fe6ed685-9741-0e94-a661-c9eb6db05165"), new DateTime(2026, 1, 10, 18, 52, 34, 788, DateTimeKind.Utc).AddTicks(560), "The 'Legendary Investor' title", true, "Legendary Investor", 300, null, 14, 3, "Legendary Investor" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeProgressRecords_BadgeId",
                table: "BadgeProgressRecords",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeProgressRecords_ChildId_BadgeId",
                table: "BadgeProgressRecords",
                columns: new[] { "ChildId", "BadgeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Badges_Category",
                table: "Badges",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Badges_Code",
                table: "Badges",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Badges_IsActive",
                table: "Badges",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Badges_Rarity",
                table: "Badges",
                column: "Rarity");

            migrationBuilder.CreateIndex(
                name: "IX_ChildBadges_BadgeId",
                table: "ChildBadges",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChildBadges_ChildId_BadgeId",
                table: "ChildBadges",
                columns: new[] { "ChildId", "BadgeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildBadges_EarnedAt",
                table: "ChildBadges",
                column: "EarnedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChildBadges_IsNew",
                table: "ChildBadges",
                column: "IsNew");

            migrationBuilder.CreateIndex(
                name: "IX_ChildRewards_ChildId_RewardId",
                table: "ChildRewards",
                columns: new[] { "ChildId", "RewardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChildRewards_IsEquipped",
                table: "ChildRewards",
                column: "IsEquipped");

            migrationBuilder.CreateIndex(
                name: "IX_ChildRewards_RewardId",
                table: "ChildRewards",
                column: "RewardId");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_IsActive",
                table: "Rewards",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_PointsCost",
                table: "Rewards",
                column: "PointsCost");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_Type",
                table: "Rewards",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BadgeProgressRecords");

            migrationBuilder.DropTable(
                name: "ChildBadges");

            migrationBuilder.DropTable(
                name: "ChildRewards");

            migrationBuilder.DropTable(
                name: "Badges");

            migrationBuilder.DropTable(
                name: "Rewards");

            migrationBuilder.DropColumn(
                name: "AvailablePoints",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "EquippedAvatarUrl",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "EquippedTheme",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "EquippedTitle",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "LastSavingDate",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "SavingStreak",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "TotalPoints",
                table: "Children");
        }
    }
}
