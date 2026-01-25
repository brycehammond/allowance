using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllowanceTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWishListItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WishListItems");

            migrationBuilder.DeleteData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("aa53f3c1-66ce-1ff2-8518-b5b087ed579f"));

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("1257ae0c-1497-74cc-7a46-4f1d077c06a4"),
                column: "SortOrder",
                value: 20);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("177b728d-d385-7955-e007-f6bf21ff732d"),
                column: "SortOrder",
                value: 12);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("4015d963-8ddf-e636-df08-50bfbbff6dd3"),
                column: "SortOrder",
                value: 16);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("46c52b4d-7df4-0896-2f0a-d25ae02e5e9f"),
                column: "SortOrder",
                value: 24);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("4866d3c4-bbc8-4d9b-afd9-0d9cc99ac34f"),
                column: "SortOrder",
                value: 30);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("4ea2615b-86b3-54bb-4ae0-ac614fbf0246"),
                column: "SortOrder",
                value: 17);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("5452e4f6-3c2d-b7ef-aa46-4c6343c2c03c"),
                column: "SortOrder",
                value: 32);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("691b08a9-2278-4791-ff01-3309151236c2"),
                column: "SortOrder",
                value: 26);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("6d2551f8-2aff-2588-ad4a-f615111b6a4f"),
                column: "SortOrder",
                value: 31);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("7b7830f5-684f-9390-385d-d262a49c61be"),
                column: "SortOrder",
                value: 14);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("8a6052ee-1886-3bdd-dee8-8ba0f0c70564"),
                column: "SortOrder",
                value: 29);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("94f6c5a0-d249-8cc9-010d-ceb5141eec49"),
                column: "SortOrder",
                value: 28);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("96f3ee74-8ec5-7bf7-5a08-533aadd2852b"),
                column: "SortOrder",
                value: 13);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("aac58817-2b3b-8119-bde6-45455493ddfa"),
                column: "SortOrder",
                value: 27);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("b419f5e0-33b6-2aa3-661e-c507eeedb646"),
                column: "SortOrder",
                value: 18);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("d69f88ad-827f-ac88-4562-8af20c52e364"),
                column: "SortOrder",
                value: 19);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("d8e575ed-511d-f5a5-d4f4-8be100b2a1ba"),
                column: "SortOrder",
                value: 22);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("db6341ec-c478-db02-1e0b-a9121471d543"),
                column: "SortOrder",
                value: 23);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("dd4bf189-e627-2b4e-e733-49ebbddc8f78"),
                column: "SortOrder",
                value: 21);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e1c4462b-6192-c704-338d-a6408ffae517"),
                column: "SortOrder",
                value: 15);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e5e42864-3a77-c5d1-097d-29c30b065035"),
                column: "SortOrder",
                value: 25);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("f62e4765-328a-8d50-6ac6-2f2a2be9ad74"),
                column: "SortOrder",
                value: 34);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("feaeb883-b9d3-4026-573c-18a75502f253"),
                column: "SortOrder",
                value: 33);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WishListItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPurchased = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WishListItems_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("1257ae0c-1497-74cc-7a46-4f1d077c06a4"),
                column: "SortOrder",
                value: 21);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("177b728d-d385-7955-e007-f6bf21ff732d"),
                column: "SortOrder",
                value: 13);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("4015d963-8ddf-e636-df08-50bfbbff6dd3"),
                column: "SortOrder",
                value: 17);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("46c52b4d-7df4-0896-2f0a-d25ae02e5e9f"),
                column: "SortOrder",
                value: 25);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("4866d3c4-bbc8-4d9b-afd9-0d9cc99ac34f"),
                column: "SortOrder",
                value: 31);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("4ea2615b-86b3-54bb-4ae0-ac614fbf0246"),
                column: "SortOrder",
                value: 18);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("5452e4f6-3c2d-b7ef-aa46-4c6343c2c03c"),
                column: "SortOrder",
                value: 33);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("691b08a9-2278-4791-ff01-3309151236c2"),
                column: "SortOrder",
                value: 27);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("6d2551f8-2aff-2588-ad4a-f615111b6a4f"),
                column: "SortOrder",
                value: 32);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("7b7830f5-684f-9390-385d-d262a49c61be"),
                column: "SortOrder",
                value: 15);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("8a6052ee-1886-3bdd-dee8-8ba0f0c70564"),
                column: "SortOrder",
                value: 30);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("94f6c5a0-d249-8cc9-010d-ceb5141eec49"),
                column: "SortOrder",
                value: 29);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("96f3ee74-8ec5-7bf7-5a08-533aadd2852b"),
                column: "SortOrder",
                value: 14);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("aac58817-2b3b-8119-bde6-45455493ddfa"),
                column: "SortOrder",
                value: 28);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("b419f5e0-33b6-2aa3-661e-c507eeedb646"),
                column: "SortOrder",
                value: 19);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("d69f88ad-827f-ac88-4562-8af20c52e364"),
                column: "SortOrder",
                value: 20);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("d8e575ed-511d-f5a5-d4f4-8be100b2a1ba"),
                column: "SortOrder",
                value: 23);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("db6341ec-c478-db02-1e0b-a9121471d543"),
                column: "SortOrder",
                value: 24);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("dd4bf189-e627-2b4e-e733-49ebbddc8f78"),
                column: "SortOrder",
                value: 22);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e1c4462b-6192-c704-338d-a6408ffae517"),
                column: "SortOrder",
                value: 16);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("e5e42864-3a77-c5d1-097d-29c30b065035"),
                column: "SortOrder",
                value: 26);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("f62e4765-328a-8d50-6ac6-2f2a2be9ad74"),
                column: "SortOrder",
                value: 35);

            migrationBuilder.UpdateData(
                table: "Badges",
                keyColumn: "Id",
                keyValue: new Guid("feaeb883-b9d3-4026-573c-18a75502f253"),
                column: "SortOrder",
                value: 34);

            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "Category", "Code", "CreatedAt", "CriteriaConfig", "CriteriaType", "Description", "IconUrl", "IsActive", "IsSecret", "Name", "PointsValue", "Rarity", "SortOrder" },
                values: new object[] { new Guid("aa53f3c1-66ce-1ff2-8518-b5b087ed579f"), 3, "WISHLIST_WINNER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "{\"ActionType\":\"first_wishlist_purchase\",\"CountTarget\":null,\"AmountTarget\":null,\"StreakTarget\":null,\"PercentageTarget\":null,\"GoalTarget\":null,\"TimeCondition\":null,\"MeasureField\":null,\"Triggers\":[12]}", 1, "Purchased an item from your wishlist", "/badges/wishlist-winner.png", true, false, "Wishlist Winner", 15, 1, 12 });

            migrationBuilder.CreateIndex(
                name: "IX_WishListItems_ChildId",
                table: "WishListItems",
                column: "ChildId");
        }
    }
}
