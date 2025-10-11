using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllowanceTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SavingsAccountEnabled",
                table: "Children",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SavingsBalance",
                table: "Children",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SavingsTransferAmount",
                table: "Children",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SavingsTransferPercentage",
                table: "Children",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SavingsTransferType",
                table: "Children",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SavingsTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsAutomatic = table.Column<bool>(type: "boolean", nullable: false),
                    SourceAllowanceTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavingsTransactions_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavingsTransactions_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavingsTransactions_ChildId",
                table: "SavingsTransactions",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsTransactions_CreatedAt",
                table: "SavingsTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsTransactions_CreatedById",
                table: "SavingsTransactions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsTransactions_Type",
                table: "SavingsTransactions",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavingsTransactions");

            migrationBuilder.DropColumn(
                name: "SavingsAccountEnabled",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "SavingsBalance",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "SavingsTransferAmount",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "SavingsTransferPercentage",
                table: "Children");

            migrationBuilder.DropColumn(
                name: "SavingsTransferType",
                table: "Children");
        }
    }
}
