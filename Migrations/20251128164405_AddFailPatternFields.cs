using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MailUptime.Migrations
{
    /// <inheritdoc />
    public partial class AddFailPatternFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FailPatternMatched",
                table: "MailCheckRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastFailedSubject",
                table: "MailCheckRecords",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailPatternMatched",
                table: "MailCheckRecords");

            migrationBuilder.DropColumn(
                name: "LastFailedSubject",
                table: "MailCheckRecords");
        }
    }
}
