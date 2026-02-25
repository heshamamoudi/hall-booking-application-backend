using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HallApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialAuditLogAndGdprDataRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AnonymizedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DataRetentionPolicy",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymized",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DataRetentionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RequestType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ProcessedByUserId = table.Column<int>(type: "integer", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AffectedDataSummary = table.Column<string>(type: "text", nullable: true),
                    AffectedEntityCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataRetentionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataRetentionRequests_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DataRetentionRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataRetentionRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    OperationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BeforeState = table.Column<string>(type: "text", nullable: true),
                    AfterState = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_DataRetentionPolicy",
                table: "Users",
                column: "DataRetentionPolicy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsAnonymized",
                table: "Users",
                column: "IsAnonymized");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastActivityAt",
                table: "Users",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataRetentionRequests_ProcessedByUserId",
                table: "DataRetentionRequests",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataRetentionRequests_RequestedAt",
                table: "DataRetentionRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataRetentionRequests_RequestedByUserId",
                table: "DataRetentionRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataRetentionRequests_Status",
                table: "DataRetentionRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataRetentionRequests_UserId",
                table: "DataRetentionRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataRetentionRequests_UserId_Status",
                table: "DataRetentionRequests",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAuditLogs_CorrelationId",
                table: "FinancialAuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAuditLogs_EntityType_EntityId",
                table: "FinancialAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAuditLogs_OperationType",
                table: "FinancialAuditLogs",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAuditLogs_Timestamp",
                table: "FinancialAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAuditLogs_UserId",
                table: "FinancialAuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataRetentionRequests");

            migrationBuilder.DropTable(
                name: "FinancialAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Users_DataRetentionPolicy",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsAnonymized",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_LastActivityAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AnonymizedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DataRetentionPolicy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAnonymized",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "Users");
        }
    }
}
