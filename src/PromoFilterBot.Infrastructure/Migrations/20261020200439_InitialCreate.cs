using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PromoFilterBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "channel",
                schema: "public",
                columns: table => new
                {
                    channel_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel_telegram_id = table.Column<long>(type: "bigint", nullable: true),
                    channel_access_hash = table.Column<long>(type: "bigint", nullable: true),
                    channel_username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    channel_title = table.Column<string>(type: "text", nullable: true),
                    channel_status = table.Column<int>(type: "integer", nullable: false),
                    channel_status_reason = table.Column<string>(type: "text", nullable: true),
                    channel_joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_creation_user = table.Column<string>(type: "text", nullable: false),
                    row_creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_update_user = table.Column<string>(type: "text", nullable: false),
                    row_update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel", x => x.channel_id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                schema: "public",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_telegram_chat_id = table.Column<long>(type: "bigint", nullable: false),
                    user_name = table.Column<string>(type: "text", nullable: false),
                    user_paused_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_creation_user = table.Column<string>(type: "text", nullable: false),
                    row_creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_update_user = table.Column<string>(type: "text", nullable: false),
                    row_update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "channel_message",
                schema: "public",
                columns: table => new
                {
                    channel_message_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel_id = table.Column<int>(type: "integer", nullable: false),
                    channel_message_telegram_id = table.Column<int>(type: "integer", nullable: false),
                    channel_message_text = table.Column<string>(type: "text", nullable: false),
                    channel_message_html = table.Column<string>(type: "text", nullable: true),
                    channel_message_link = table.Column<string>(type: "text", nullable: true),
                    channel_message_detected_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    channel_message_product_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    channel_message_store = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    channel_message_coupon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    channel_message_received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_creation_user = table.Column<string>(type: "text", nullable: false),
                    row_creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_update_user = table.Column<string>(type: "text", nullable: false),
                    row_update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_message", x => x.channel_message_id);
                    table.ForeignKey(
                        name: "FK_channel_message_channel_channel_id",
                        column: x => x.channel_id,
                        principalSchema: "public",
                        principalTable: "channel",
                        principalColumn: "channel_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alert",
                schema: "public",
                columns: table => new
                {
                    alert_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    alert_original_text = table.Column<string>(type: "text", nullable: false),
                    alert_normalized_terms = table.Column<string>(type: "text", nullable: false),
                    alert_excluded_terms = table.Column<string>(type: "text", nullable: false),
                    alert_max_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    alert_is_active = table.Column<bool>(type: "boolean", nullable: false),
                    row_creation_user = table.Column<string>(type: "text", nullable: false),
                    row_creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_update_user = table.Column<string>(type: "text", nullable: false),
                    row_update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert", x => x.alert_id);
                    table.ForeignKey(
                        name: "FK_alert_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversation_state",
                schema: "public",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    conversation_state_step = table.Column<int>(type: "integer", nullable: false),
                    conversation_state_data_json = table.Column<string>(type: "jsonb", nullable: true),
                    row_creation_user = table.Column<string>(type: "text", nullable: false),
                    row_creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_update_user = table.Column<string>(type: "text", nullable: false),
                    row_update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_state", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_conversation_state_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                schema: "public",
                columns: table => new
                {
                    notification_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    alert_id = table.Column<int>(type: "integer", nullable: false),
                    channel_message_id = table.Column<long>(type: "bigint", nullable: false),
                    notification_product_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    notification_store = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    notification_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    notification_coupon = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    notification_offer_date = table.Column<DateOnly>(type: "date", nullable: false),
                    notification_status = table.Column<int>(type: "integer", nullable: false),
                    notification_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notification_feedback = table.Column<int>(type: "integer", nullable: true),
                    row_creation_user = table.Column<string>(type: "text", nullable: false),
                    row_creation_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_update_user = table.Column<string>(type: "text", nullable: false),
                    row_update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    row_is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification", x => x.notification_id);
                    table.ForeignKey(
                        name: "FK_notification_alert_alert_id",
                        column: x => x.alert_id,
                        principalSchema: "public",
                        principalTable: "alert",
                        principalColumn: "alert_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_channel_message_channel_message_id",
                        column: x => x.channel_message_id,
                        principalSchema: "public",
                        principalTable: "channel_message",
                        principalColumn: "channel_message_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_alert_is_active",
                schema: "public",
                table: "alert",
                column: "alert_is_active");

            migrationBuilder.CreateIndex(
                name: "IX_alert_user_id",
                schema: "public",
                table: "alert",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_channel_channel_status",
                schema: "public",
                table: "channel",
                column: "channel_status");

            migrationBuilder.CreateIndex(
                name: "IX_channel_channel_telegram_id",
                schema: "public",
                table: "channel",
                column: "channel_telegram_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_channel_channel_username",
                schema: "public",
                table: "channel",
                column: "channel_username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_channel_message_channel_id_channel_message_telegram_id",
                schema: "public",
                table: "channel_message",
                columns: new[] { "channel_id", "channel_message_telegram_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_channel_message_channel_message_product_key",
                schema: "public",
                table: "channel_message",
                column: "channel_message_product_key");

            migrationBuilder.CreateIndex(
                name: "IX_channel_message_channel_message_received_at",
                schema: "public",
                table: "channel_message",
                column: "channel_message_received_at");

            migrationBuilder.CreateIndex(
                name: "IX_notification_alert_id",
                schema: "public",
                table: "notification",
                column: "alert_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_channel_message_id",
                schema: "public",
                table: "notification",
                column: "channel_message_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_notification_status",
                schema: "public",
                table: "notification",
                column: "notification_status");

            migrationBuilder.CreateIndex(
                name: "IX_notification_user_offer_date",
                schema: "public",
                table: "notification",
                columns: new[] { "user_id", "notification_product_key", "notification_store", "notification_price", "notification_coupon", "notification_offer_date" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_user_user_telegram_chat_id",
                schema: "public",
                table: "user",
                column: "user_telegram_chat_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_state",
                schema: "public");

            migrationBuilder.DropTable(
                name: "notification",
                schema: "public");

            migrationBuilder.DropTable(
                name: "alert",
                schema: "public");

            migrationBuilder.DropTable(
                name: "channel_message",
                schema: "public");

            migrationBuilder.DropTable(
                name: "user",
                schema: "public");

            migrationBuilder.DropTable(
                name: "channel",
                schema: "public");
        }
    }
}
