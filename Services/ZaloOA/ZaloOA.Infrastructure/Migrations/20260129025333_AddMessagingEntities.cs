using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZaloOA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZaloUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ZaloUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OAId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsFollower = table.Column<bool>(type: "boolean", nullable: false),
                    LastInteractionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FollowedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZaloUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ZaloConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OAAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZaloUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastMessagePreview = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnreadCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZaloConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZaloConversations_ZaloOAAccounts_OAAccountId",
                        column: x => x.OAAccountId,
                        principalTable: "ZaloOAAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ZaloConversations_ZaloUsers_ZaloUserId",
                        column: x => x.ZaloUserId,
                        principalTable: "ZaloUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ZaloMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ZaloMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttachmentName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZaloMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZaloMessages_ZaloConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ZaloConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZaloConversations_LastMessageAt",
                table: "ZaloConversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_ZaloConversations_OAAccountId",
                table: "ZaloConversations",
                column: "OAAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ZaloConversations_OAAccountId_ZaloUserId",
                table: "ZaloConversations",
                columns: new[] { "OAAccountId", "ZaloUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZaloConversations_ZaloUserId",
                table: "ZaloConversations",
                column: "ZaloUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ZaloMessages_ConversationId",
                table: "ZaloMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ZaloMessages_SentAt",
                table: "ZaloMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_ZaloMessages_ZaloMessageId",
                table: "ZaloMessages",
                column: "ZaloMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ZaloUsers_OAId",
                table: "ZaloUsers",
                column: "OAId");

            migrationBuilder.CreateIndex(
                name: "IX_ZaloUsers_ZaloUserId_OAId",
                table: "ZaloUsers",
                columns: new[] { "ZaloUserId", "OAId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZaloMessages");

            migrationBuilder.DropTable(
                name: "ZaloConversations");

            migrationBuilder.DropTable(
                name: "ZaloUsers");
        }
    }
}
