using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FacilityApp.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumableRestockLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consumable_restock_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumableTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    RestockedById = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consumable_restock_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_consumable_restock_logs_AspNetUsers_RestockedById",
                        column: x => x.RestockedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_consumable_restock_logs_consumable_types_ConsumableTypeId",
                        column: x => x.ConsumableTypeId,
                        principalTable: "consumable_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_consumable_restock_logs_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_consumable_restock_logs_ConsumableTypeId",
                table: "consumable_restock_logs",
                column: "ConsumableTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_consumable_restock_logs_RestockedById",
                table: "consumable_restock_logs",
                column: "RestockedById");

            migrationBuilder.CreateIndex(
                name: "IX_consumable_restock_logs_TenantId",
                table: "consumable_restock_logs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consumable_restock_logs");
        }
    }
}
