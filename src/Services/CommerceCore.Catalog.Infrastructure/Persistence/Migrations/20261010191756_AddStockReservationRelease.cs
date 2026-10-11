using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceCore.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReservationRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedAtUtc",
                table: "StockReservations",
                type: "datetime2(3)",
                precision: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReleasedAtUtc",
                table: "StockReservations");
        }
    }
}
