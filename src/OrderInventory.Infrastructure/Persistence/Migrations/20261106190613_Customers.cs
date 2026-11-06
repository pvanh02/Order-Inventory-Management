using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Customers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ChangedFields = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAuditEntries", x => x.Id);
                    table.CheckConstraint("CK_CustomerAuditEntries_Action", "[Action] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_CustomerAuditEntries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerAuditEntries_UserAccounts_ActorId",
                        column: x => x.ActorId,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAuditEntries_ActorId",
                table: "CustomerAuditEntries",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAuditEntries_CustomerId_OccurredAt_Id",
                table: "CustomerAuditEntries",
                columns: new[] { "CustomerId", "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsDeleted_Name_Id",
                table: "Customers",
                columns: new[] { "IsDeleted", "Name", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerAuditEntries");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
