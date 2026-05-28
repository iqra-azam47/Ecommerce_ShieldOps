using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShieldOps.Migrations
{
    /// <inheritdoc />
    public partial class SeedProductsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1111111-1111-1111-1111-111111111111",
                column: "ConcurrencyStamp",
                value: "e683217a-4765-4e22-9561-6322b7a0aff1");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c2222222-2222-2222-2222-222222222222",
                column: "ConcurrencyStamp",
                value: "81f50baa-a550-40dc-a099-eddda90e77fa");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0000-000000000000",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "aa0dbee2-9cc0-48cf-bdd1-02b45ffd999a", "AQAAAAIAAYagAAAAEE45NGuhle6mUBULI6QoFn+JC50P5VbkPpE37+sOlhsgjmSCg/FLLe1lp8gJaVAh8A==" });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CategoryId", "Description", "ImageUrl", "Name", "Price", "StockQuantity", "Type" },
                values: new object[,]
                {
                    { 1, 1, "Military-grade physical USB-C authentication key with biometrics.", "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=500", "Titanium MFA Security Key", 85.00m, 15, 0 },
                    { 2, 2, "Next-gen localized AES-256 software suite for full-disk database encryption.", "https://images.unsplash.com/photo-1563986768609-322da13575f3?w=500", "CryptoShield Enterprise Edition", 299.99m, 999, 1 },
                    { 3, 1, "NFC-enabled physical hardware token designed for zero-trust architectures.", "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=500", "YubiArmor Pro Token", 45.50m, 3, 0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1111111-1111-1111-1111-111111111111",
                column: "ConcurrencyStamp",
                value: "ce2a00b6-26b4-4934-88a6-dedf5d662641");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c2222222-2222-2222-2222-222222222222",
                column: "ConcurrencyStamp",
                value: "6bf714df-af92-49c6-93ff-2d671b95c805");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0000-000000000000",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "4ce8e838-3679-496e-92d8-cb73bd8d6cbd", "AQAAAAIAAYagAAAAEPj2yfo6216PAZ1gvz+65pCVuvTcW9yPx7fqLYnAAV7PRt6GDixoeUIx71S4aXAewQ==" });
        }
    }
}
