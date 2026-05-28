using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShieldOps.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderReturnPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminReturnNotes",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnRequestedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnState",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminReturnNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReturnRequestedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReturnState",
                table: "Orders");

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
        }
    }
}
