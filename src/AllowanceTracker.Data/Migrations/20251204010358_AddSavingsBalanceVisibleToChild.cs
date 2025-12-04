using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllowanceTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsBalanceVisibleToChild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SavingsBalanceVisibleToChild",
                table: "Children",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SavingsBalanceVisibleToChild",
                table: "Children");
        }
    }
}
