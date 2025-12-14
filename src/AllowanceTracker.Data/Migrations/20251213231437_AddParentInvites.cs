using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllowanceTracker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParentInvites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParentInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvitedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsExistingUser = table.Column<bool>(type: "bit", nullable: false),
                    ExistingUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParentInvites_AspNetUsers_ExistingUserId",
                        column: x => x.ExistingUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ParentInvites_AspNetUsers_InvitedById",
                        column: x => x.InvitedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParentInvites_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParentInvites_ExistingUserId",
                table: "ParentInvites",
                column: "ExistingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentInvites_ExpiresAt",
                table: "ParentInvites",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ParentInvites_FamilyId",
                table: "ParentInvites",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentInvites_InvitedById",
                table: "ParentInvites",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_ParentInvites_InvitedEmail_FamilyId",
                table: "ParentInvites",
                columns: new[] { "InvitedEmail", "FamilyId" });

            migrationBuilder.CreateIndex(
                name: "IX_ParentInvites_Status",
                table: "ParentInvites",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ParentInvites_Token",
                table: "ParentInvites",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParentInvites");
        }
    }
}
