using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllowanceTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedSystemUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL to handle case where user already exists (idempotent)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = '00000000-0000-0000-0000-000000000001')
                BEGIN
                    INSERT INTO AspNetUsers (Id, AccessFailedCount, ConcurrencyStamp, Email, EmailConfirmed, FamilyId, FirstName, LastName, LockoutEnabled, LockoutEnd, NormalizedEmail, NormalizedUserName, PasswordHash, PhoneNumber, PhoneNumberConfirmed, Role, SecurityStamp, TwoFactorEnabled, UserName)
                    VALUES ('00000000-0000-0000-0000-000000000001', 0, 'SYSTEM-CONCURRENCY-STAMP', 'system@allowancetracker.local', 1, NULL, 'System', 'Automated', 0, NULL, 'SYSTEM@ALLOWANCETRACKER.LOCAL', 'SYSTEM@ALLOWANCETRACKER.LOCAL', NULL, NULL, 0, 0, 'SYSTEM-SECURITY-STAMP', 0, 'system@allowancetracker.local')
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));
        }
    }
}
