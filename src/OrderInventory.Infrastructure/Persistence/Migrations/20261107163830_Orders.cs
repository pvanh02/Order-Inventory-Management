using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Orders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryMovements_Type",
                table: "InventoryMovements");

            migrationBuilder.AddColumn<int>(
                name: "OrderLineId",
                table: "InventoryMovements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.CheckConstraint("CK_Orders_State", "[Status] = 1 AND [CompletedAt] IS NULL AND [CancelledAt] IS NULL");
                    table.CheckConstraint("CK_Orders_Total", "[Total] > 0");
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_UserAccounts_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SkuSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    LineTotal = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.UniqueConstraint("AK_OrderLines_Id_ProductId", x => new { x.Id, x.ProductId });
                    table.CheckConstraint("CK_OrderLines_Amounts", "[UnitPriceSnapshot] > 0 AND [LineTotal] > 0 AND CONVERT(decimal(38,0), [LineTotal]) = CONVERT(decimal(38,0), [UnitPriceSnapshot]) * [Quantity]");
                    table.CheckConstraint("CK_OrderLines_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_OrderLineId",
                table: "InventoryMovements",
                column: "OrderLineId",
                unique: true,
                filter: "[OrderLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_OrderLineId_ProductId",
                table: "InventoryMovements",
                columns: new[] { "OrderLineId", "ProductId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryMovements_Type",
                table: "InventoryMovements",
                sql: "([Type] = 1 AND [Delta] > 0 AND [OrderLineId] IS NULL) OR ([Type] = 2 AND LEN(LTRIM(RTRIM([Reason]))) > 0 AND [Reason] IS NOT NULL AND [OrderLineId] IS NULL) OR ([Type] = 3 AND [Delta] < 0 AND [OrderLineId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId_ProductId",
                table: "OrderLines",
                columns: new[] { "OrderId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_ProductId",
                table: "OrderLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt_Id",
                table: "Orders",
                columns: new[] { "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedBy",
                table: "Orders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryMovements_OrderLines_OrderLineId_ProductId",
                table: "InventoryMovements",
                columns: new[] { "OrderLineId", "ProductId" },
                principalTable: "OrderLines",
                principalColumns: new[] { "Id", "ProductId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryMovements_OrderLines_OrderLineId_ProductId",
                table: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "OrderLines");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_OrderLineId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_OrderLineId_ProductId",
                table: "InventoryMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryMovements_Type",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "OrderLineId",
                table: "InventoryMovements");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryMovements_Type",
                table: "InventoryMovements",
                sql: "([Type] = 1 AND [Delta] > 0) OR ([Type] = 2 AND LEN(LTRIM(RTRIM([Reason]))) > 0 AND [Reason] IS NOT NULL)");
        }
    }
}
