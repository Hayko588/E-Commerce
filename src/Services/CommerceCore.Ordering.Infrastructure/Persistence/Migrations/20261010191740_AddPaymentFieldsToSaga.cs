using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommerceCore.Ordering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFieldsToSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "OrderSagas",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentToken",
                table: "OrderSagas",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "OrderSagas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "OrderSagas");

            migrationBuilder.DropColumn(
                name: "PaymentToken",
                table: "OrderSagas");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "OrderSagas");
        }
    }
}
