using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WifiLocator.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AddressEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    Road = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddressEntity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WifiEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ssid = table.Column<string>(type: "text", nullable: false),
                    Bssid = table.Column<string>(type: "text", nullable: false),
                    ApproximatedLatitude = table.Column<double>(type: "double precision", nullable: true),
                    ApproximatedLongitude = table.Column<double>(type: "double precision", nullable: true),
                    Encryption = table.Column<string>(type: "text", nullable: false),
                    UncertaintyRadius = table.Column<double>(type: "double precision", nullable: true),
                    AddressId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WifiEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WifiEntity_AddressEntity_AddressId",
                        column: x => x.AddressId,
                        principalTable: "AddressEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LocationEntity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Altitude = table.Column<int>(type: "integer", nullable: false),
                    Accuracy = table.Column<double>(type: "double precision", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SignaldBm = table.Column<double>(type: "double precision", nullable: false),
                    FrequencyMHz = table.Column<int>(type: "integer", nullable: false),
                    EncryptionValue = table.Column<string>(type: "text", nullable: false),
                    UsedForApproximation = table.Column<bool>(type: "boolean", nullable: false),
                    WifiId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationEntity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationEntity_WifiEntity_WifiId",
                        column: x => x.WifiId,
                        principalTable: "WifiEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationEntity_WifiId",
                table: "LocationEntity",
                column: "WifiId");

            migrationBuilder.CreateIndex(
                name: "IX_WifiEntity_AddressId",
                table: "WifiEntity",
                column: "AddressId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationEntity");

            migrationBuilder.DropTable(
                name: "WifiEntity");

            migrationBuilder.DropTable(
                name: "AddressEntity");
        }
    }
}
