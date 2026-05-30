using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GesAchats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSetNullForPurchaseOrderNeedId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Needs_NeedId",
                table: "PurchaseOrders");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'reglements' AND column_name = 'Status') THEN
                        ALTER TABLE reglements RENAME COLUMN ""Status"" TO statut;
                    END IF;
                END $$;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Needs_NeedId",
                table: "PurchaseOrders",
                column: "NeedId",
                principalTable: "Needs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Needs_NeedId",
                table: "PurchaseOrders");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'reglements' AND column_name = 'statut') THEN
                        ALTER TABLE reglements RENAME COLUMN statut TO ""Status"";
                    END IF;
                END $$;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Needs_NeedId",
                table: "PurchaseOrders",
                column: "NeedId",
                principalTable: "Needs",
                principalColumn: "Id");
        }
    }
}
