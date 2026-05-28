using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShieldOps.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1111111-1111-1111-1111-111111111111",
                column: "ConcurrencyStamp",
                value: "978b2858-43a9-434e-a525-a3434cf276c0");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c2222222-2222-2222-2222-222222222222",
                column: "ConcurrencyStamp",
                value: "932e515c-e313-40ad-8384-fc198728bd2d");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0000-000000000000",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "e981b64d-e0d7-4943-866e-96045a06cb26", "AQAAAAIAAYagAAAAEP7uzvBs2/N8lby0n3llW9B53ORHVpMjYuaBPMq8/PiRtf4F6sYRzl5208pcDxlCJg==" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsActive",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1111111-1111-1111-1111-111111111111",
                column: "ConcurrencyStamp",
                value: "492d5eb8-106d-4fd6-8ab1-a21c89317e3f");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "c2222222-2222-2222-2222-222222222222",
                column: "ConcurrencyStamp",
                value: "b39147ae-8c59-454d-bbe8-79af4b62e368");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0000-000000000000",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "08718389-6475-43e0-8fd1-315345ca532d", "AQAAAAIAAYagAAAAEPo1tkrp0+Dmnjq6W8MS0JzZvs6lnty0sZkkuJaIrMqcN6AFLsNw8J/QQi7UgukvSQ==" });
        }
    }
}
