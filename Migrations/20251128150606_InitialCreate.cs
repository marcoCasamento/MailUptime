using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MailUptime.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailCheckRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MailboxIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    Day = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MailArrived = table.Column<bool>(type: "INTEGER", nullable: false),
                    PatternMatched = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastCheckTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastReceivedTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastMatchedSubject = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailCheckRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailCheckRecords_MailboxIdentifier_Day",
                table: "MailCheckRecords",
                columns: new[] { "MailboxIdentifier", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailCheckRecords");
        }
    }
}
