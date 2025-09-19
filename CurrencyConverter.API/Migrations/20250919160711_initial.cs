using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CurrencyConverter.API.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KeyValue = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RequestsPerHour = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Code = table.Column<string>(type: "TEXT", fixedLength: true, maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeyUsages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApiKeyId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequestTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false),
                    Endpoint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeyUsages_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BaseCurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    TargetCurrencyCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsHistorical = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_BaseCurrencyCode",
                        column: x => x.BaseCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_TargetCurrencyCode",
                        column: x => x.TargetCurrencyCode,
                        principalTable: "Currencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ApiKeys",
                columns: new[] { "Id", "CreatedAt", "ExpiresAt", "IsActive", "KeyValue", "Name", "RequestsPerHour" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 19, 16, 7, 10, 545, DateTimeKind.Utc).AddTicks(7240), null, true, "demo-key-123456789", "Demo API Key", 1000 },
                    { 2, new DateTime(2025, 9, 19, 16, 7, 10, 545, DateTimeKind.Utc).AddTicks(7250), null, true, "premium-key-987654321", "Premium API Key", 5000 }
                });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Code", "CreatedAt", "IsActive", "Name", "Symbol" },
                values: new object[,]
                {
                    { "AUD", new DateTime(2025, 9, 19, 16, 7, 10, 545, DateTimeKind.Utc).AddTicks(6740), true, "Australian Dollar", "A$" },
                    { "CAD", new DateTime(2025, 9, 19, 16, 7, 10, 545, DateTimeKind.Utc).AddTicks(6740), true, "Canadian Dollar", "C$" },
                    { "CHF", new DateTime(2025, 9, 19, 16, 7, 10, 545, DateTimeKind.Utc).AddTicks(6850), true, "Swiss Franc", "CHF" },
                    { "CNY", new DateTime(2025, 9, 19, 16, 7, 10, 545, DateTimeKind.Utc).AddTicks(6850), true, "Chinese Yuan", "¥" },
                    { "EUR", new DateTime(2025, 9, 19, 16, 7, 10, 545, DateTimeKind.Utc).AddTicks(6730), true, "Euro", "€" },
                    { "GBP", new DateTime(2025, 9, 19, 16, 7, 10, 545, DateTimeKind.Utc).AddTicks(6730), true, "British Pound", "£" },
                    { "JPY", new DateTime(2025, 9, 19, 16, 7, 10, 545, DateTimeKind.Utc).AddTicks(6730), true, "Japanese Yen", "¥" },
                    { "USD", new DateTime(2025, 9, 19, 16, 7, 10, 545, DateTimeKind.Utc).AddTicks(6720), true, "US Dollar", "$" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_ExpiresAt",
                table: "ApiKeys",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_IsActive",
                table: "ApiKeys",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyValue",
                table: "ApiKeys",
                column: "KeyValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyUsages_ApiKeyId_RequestTime",
                table: "ApiKeyUsages",
                columns: new[] { "ApiKeyId", "RequestTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyUsages_RequestTime",
                table: "ApiKeyUsages",
                column: "RequestTime");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_IsActive",
                table: "Currencies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_BaseCurrencyCode_TargetCurrencyCode_Date",
                table: "ExchangeRates",
                columns: new[] { "BaseCurrencyCode", "TargetCurrencyCode", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_BaseCurrencyCode_TargetCurrencyCode_IsHistorical",
                table: "ExchangeRates",
                columns: new[] { "BaseCurrencyCode", "TargetCurrencyCode", "IsHistorical" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_Date",
                table: "ExchangeRates",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_IsHistorical",
                table: "ExchangeRates",
                column: "IsHistorical");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_TargetCurrencyCode",
                table: "ExchangeRates",
                column: "TargetCurrencyCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeyUsages");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "Currencies");
        }
    }
}
