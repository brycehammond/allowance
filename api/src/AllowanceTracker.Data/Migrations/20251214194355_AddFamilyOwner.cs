using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllowanceTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add OwnerId as nullable first
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Families",
                type: "uniqueidentifier",
                nullable: true);

            // Step 2: Populate OwnerId with the first parent in each family
            // (the parent with the earliest Id, which approximates creation order)
            migrationBuilder.Sql(@"
                UPDATE f
                SET f.OwnerId = (
                    SELECT TOP 1 u.Id
                    FROM AspNetUsers u
                    WHERE u.FamilyId = f.Id
                      AND u.Role = 0  -- Parent role
                    ORDER BY u.Id
                )
                FROM Families f
                WHERE EXISTS (
                    SELECT 1 FROM AspNetUsers u2
                    WHERE u2.FamilyId = f.Id AND u2.Role = 0
                )
            ");

            // Step 3: Make OwnerId non-nullable (after data is populated)
            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Families",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Step 4: Create index
            migrationBuilder.CreateIndex(
                name: "IX_Families_OwnerId",
                table: "Families",
                column: "OwnerId");

            // Step 5: Add foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_Families_AspNetUsers_OwnerId",
                table: "Families",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Families_AspNetUsers_OwnerId",
                table: "Families");

            migrationBuilder.DropIndex(
                name: "IX_Families_OwnerId",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Families");
        }
    }
}
