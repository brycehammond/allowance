using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllowanceTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowanceAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowancePaused",
                table: "Children",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AllowancePausedReason",
                table: "Children",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AllowanceAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    OldAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    NewAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdjustedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowanceAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllowanceAdjustments_AspNetUsers_AdjustedById",
                        column: x => x.AdjustedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AllowanceAdjustments_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllowanceAdjustments_AdjustedById",
                table: "AllowanceAdjustments",
                column: "AdjustedById");

            migrationBuilder.CreateIndex(
                name: "IX_AllowanceAdjustments_AdjustmentType",
                table: "AllowanceAdjustments",
                column: "AdjustmentType");

            migrationBuilder.CreateIndex(
                name: "IX_AllowanceAdjustments_ChildId",
                table: "AllowanceAdjustments",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_AllowanceAdjustments_CreatedAt",
                table: "AllowanceAdjustments",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllowanceAdjustments");

            migrationBuilder.DropColumn(
                name: "AllowancePaused",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "AllowancePausedReason",
                table: "Children");
        }
    }
}
