using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireWire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Industry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ScrapedContext = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobLeads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RawDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SalaryRange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobLeads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobLeads_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobLeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MessageBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequiresFollowUp = table.Column<bool>(type: "bit", nullable: false),
                    FollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Communications_JobLeads_JobLeadId",
                        column: x => x.JobLeadId,
                        principalTable: "JobLeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobLeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DynamicCvPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CheatSheetData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewAssets_JobLeads_JobLeadId",
                        column: x => x.JobLeadId,
                        principalTable: "JobLeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OE_Configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobLeadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AiMatchScore = table.Column<double>(type: "float", nullable: true),
                    IsAsync = table.Column<bool>(type: "bit", nullable: false),
                    DetectedRedFlags = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OE_Configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OE_Configs_JobLeads_JobLeadId",
                        column: x => x.JobLeadId,
                        principalTable: "JobLeads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Communications_JobLeadId",
                table: "Communications",
                column: "JobLeadId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssets_JobLeadId",
                table: "InterviewAssets",
                column: "JobLeadId");

            migrationBuilder.CreateIndex(
                name: "IX_JobLeads_CompanyId",
                table: "JobLeads",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_OE_Configs_JobLeadId",
                table: "OE_Configs",
                column: "JobLeadId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Communications");

            migrationBuilder.DropTable(
                name: "InterviewAssets");

            migrationBuilder.DropTable(
                name: "OE_Configs");

            migrationBuilder.DropTable(
                name: "JobLeads");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
