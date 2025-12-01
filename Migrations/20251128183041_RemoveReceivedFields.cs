using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MailUptime.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReceivedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MailArrived",
                table: "MailCheckRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MailArrived",
                table: "MailCheckRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
