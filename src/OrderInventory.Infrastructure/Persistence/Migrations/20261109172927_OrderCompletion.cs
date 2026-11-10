using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_State",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "CompletedBy",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CompletedBy",
                table: "Orders",
                column: "CompletedBy");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_State",
                table: "Orders",
                sql: "([Status] = 1 AND [CompletedAt] IS NULL AND [CompletedBy] IS NULL AND [CancelledAt] IS NULL) OR ([Status] = 2 AND [CompletedAt] IS NOT NULL AND [CompletedBy] IS NOT NULL AND [CancelledAt] IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_UserAccounts_CompletedBy",
                table: "Orders",
                column: "CompletedBy",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_UserAccounts_CompletedBy",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CompletedBy",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Orders_State",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CompletedBy",
                table: "Orders");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Orders_State",
                table: "Orders",
                sql: "[Status] = 1 AND [CompletedAt] IS NULL AND [CancelledAt] IS NULL");
        }
    }
}
