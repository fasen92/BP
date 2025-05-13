using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WifiLocator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChannelAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Channel",
                table: "WifiEntity",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Channel",
                table: "WifiEntity");
        }
    }
}
