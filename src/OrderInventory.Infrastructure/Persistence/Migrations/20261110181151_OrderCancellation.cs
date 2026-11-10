using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_State",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_OrderLineId",
                table: "InventoryMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryMovements_Type",
                table: "InventoryMovements");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Orders",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledBy",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CancelledBy",
                table: "Orders",
                column: "CancelledBy");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_State",
                table: "Orders",
                sql: "([Status] = 1 AND [CompletedAt] IS NULL AND [CompletedBy] IS NULL AND [CancelledAt] IS NULL AND [CancelledBy] IS NULL AND [CancellationReason] IS NULL) OR ([Status] = 2 AND [CompletedAt] IS NOT NULL AND [CompletedBy] IS NOT NULL AND [CancelledAt] IS NULL AND [CancelledBy] IS NULL AND [CancellationReason] IS NULL) OR ([Status] = 3 AND [CancelledAt] IS NOT NULL AND [CancelledBy] IS NOT NULL AND [CompletedAt] IS NULL AND [CompletedBy] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_OrderLineId_Type",
                table: "InventoryMovements",
                columns: new[] { "OrderLineId", "Type" },
                unique: true,
                filter: "[OrderLineId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryMovements_Type",
                table: "InventoryMovements",
                sql: "([Type] = 1 AND [Delta] > 0 AND [OrderLineId] IS NULL) OR ([Type] = 2 AND LEN(LTRIM(RTRIM([Reason]))) > 0 AND [Reason] IS NOT NULL AND [OrderLineId] IS NULL) OR ([Type] = 3 AND [Delta] < 0 AND [OrderLineId] IS NOT NULL) OR ([Type] = 4 AND [Delta] > 0 AND [OrderLineId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_UserAccounts_CancelledBy",
                table: "Orders",
                column: "CancelledBy",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_UserAccounts_CancelledBy",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CancelledBy",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_State",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_OrderLineId_Type",
                table: "InventoryMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InventoryMovements_Type",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "Orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_State",
                table: "Orders",
                sql: "([Status] = 1 AND [CompletedAt] IS NULL AND [CompletedBy] IS NULL AND [CancelledAt] IS NULL) OR ([Status] = 2 AND [CompletedAt] IS NOT NULL AND [CompletedBy] IS NOT NULL AND [CancelledAt] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_OrderLineId",
                table: "InventoryMovements",
                column: "OrderLineId",
                unique: true,
                filter: "[OrderLineId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InventoryMovements_Type",
                table: "InventoryMovements",
                sql: "([Type] = 1 AND [Delta] > 0 AND [OrderLineId] IS NULL) OR ([Type] = 2 AND LEN(LTRIM(RTRIM([Reason]))) > 0 AND [Reason] IS NOT NULL AND [OrderLineId] IS NULL) OR ([Type] = 3 AND [Delta] < 0 AND [OrderLineId] IS NOT NULL)");
        }
    }
}
