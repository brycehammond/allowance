using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllowanceTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReceiptImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptImagePath",
                table: "Transactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptImagePath",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
