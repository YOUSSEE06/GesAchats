using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GesAchats.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateComptableModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryNotes_PurchaseOrders_PurchaseOrderId",
                table: "DeliveryNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryNotes_Suppliers_SupplierId",
                table: "DeliveryNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryNotes_Users_ReceivedById",
                table: "DeliveryNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_DeliveryNotes_DeliveryNoteId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Suppliers_SupplierId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Users_CreatedById",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Invoices_InvoiceId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_CreatedById",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Products_ProductId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_Products_ProductId",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_ProductId",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_ProductId",
                table: "PurchaseOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Invoices",
                table: "Invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DeliveryNotes",
                table: "DeliveryNotes");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "UnitPriceHT",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "PurchaseOrders");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "reglements");

            migrationBuilder.RenameTable(
                name: "Invoices",
                newName: "factures");

            migrationBuilder.RenameTable(
                name: "DeliveryNotes",
                newName: "bons_livraison");

            migrationBuilder.RenameColumn(
                name: "UnitPriceTTC",
                table: "Quotations",
                newName: "TotalAmountHT");

            migrationBuilder.RenameColumn(
                name: "UnitPriceHT",
                table: "PurchaseOrders",
                newName: "TotalVAT");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "PurchaseOrders",
                newName: "TotalAmountTTC");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "reglements",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "reglements",
                newName: "date_maj");

            migrationBuilder.RenameColumn(
                name: "ReferenceNumber",
                table: "reglements",
                newName: "reference");

            migrationBuilder.RenameColumn(
                name: "PaymentMethod",
                table: "reglements",
                newName: "mode_paiement");

            migrationBuilder.RenameColumn(
                name: "PaymentDate",
                table: "reglements",
                newName: "date_paiement");

            migrationBuilder.RenameColumn(
                name: "InvoiceId",
                table: "reglements",
                newName: "facture_id");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "reglements",
                newName: "cree_par");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "reglements",
                newName: "date_creation");

            migrationBuilder.RenameColumn(
                name: "AmountPaid",
                table: "reglements",
                newName: "montant");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_InvoiceId",
                table: "reglements",
                newName: "IX_reglements_facture_id");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_CreatedById",
                table: "reglements",
                newName: "IX_reglements_cree_par");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "factures",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "factures",
                newName: "date_maj");

            migrationBuilder.RenameColumn(
                name: "TaxAmount",
                table: "factures",
                newName: "montant_tva");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "factures",
                newName: "fournisseur_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "factures",
                newName: "statut");

            migrationBuilder.RenameColumn(
                name: "InvoiceNumber",
                table: "factures",
                newName: "numero_facture");

            migrationBuilder.RenameColumn(
                name: "InvoiceDate",
                table: "factures",
                newName: "date_facture");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "factures",
                newName: "date_echeance");

            migrationBuilder.RenameColumn(
                name: "DeliveryNoteId",
                table: "factures",
                newName: "bl_id");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "factures",
                newName: "cree_par");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "factures",
                newName: "date_creation");

            migrationBuilder.RenameColumn(
                name: "AmountTTC",
                table: "factures",
                newName: "montant_ttc");

            migrationBuilder.RenameColumn(
                name: "AmountHT",
                table: "factures",
                newName: "montant_ht");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_SupplierId",
                table: "factures",
                newName: "IX_factures_fournisseur_id");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "factures",
                newName: "IX_factures_numero_facture");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_DeliveryNoteId",
                table: "factures",
                newName: "IX_factures_bl_id");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_CreatedById",
                table: "factures",
                newName: "IX_factures_cree_par");

            migrationBuilder.RenameColumn(
                name: "Observations",
                table: "bons_livraison",
                newName: "observations");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "bons_livraison",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "bons_livraison",
                newName: "fournisseur_id");

            migrationBuilder.RenameColumn(
                name: "ReceptionDate",
                table: "bons_livraison",
                newName: "date_reception");

            migrationBuilder.RenameColumn(
                name: "PurchaseOrderId",
                table: "bons_livraison",
                newName: "bc_id");

            migrationBuilder.RenameColumn(
                name: "DeliveryNumber",
                table: "bons_livraison",
                newName: "numero_bl");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryNotes_SupplierId",
                table: "bons_livraison",
                newName: "IX_bons_livraison_fournisseur_id");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryNotes_ReceivedById",
                table: "bons_livraison",
                newName: "IX_bons_livraison_ReceivedById");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryNotes_PurchaseOrderId",
                table: "bons_livraison",
                newName: "IX_bons_livraison_bc_id");

            migrationBuilder.RenameIndex(
                name: "IX_DeliveryNotes_DeliveryNumber",
                table: "bons_livraison",
                newName: "IX_bons_livraison_numero_bl");

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AverageDeliveryDelay",
                table: "Suppliers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentConditions",
                table: "Suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Rating",
                table: "Suppliers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeedId",
                table: "Quotations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "Quotations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseDate",
                table: "Quotations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeedId",
                table: "PurchaseOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentConditions",
                table: "PurchaseOrders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequestedDeliveryDelay",
                table: "PurchaseOrders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmountHT",
                table: "PurchaseOrders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyConsumption",
                table: "Products",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsNew",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPurchaseDate",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "reference",
                table: "reglements",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "montant",
                table: "reglements",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "banque",
                table: "reglements",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fichier_preuve",
                table: "reglements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fichier_recu",
                table: "reglements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fournisseur_id",
                table: "reglements",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "numero_reglement",
                table: "reglements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "observations",
                table: "reglements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type_fichier",
                table: "reglements",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "montant_tva",
                table: "factures",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "montant_ttc",
                table: "factures",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "montant_ht",
                table: "factures",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "bc_id",
                table: "factures",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "conformite",
                table: "factures",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "date_reception",
                table: "factures",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "justification_conformite",
                table: "factures",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "numero_facture_fournisseur",
                table: "factures",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "observations",
                table: "factures",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "taux_tva",
                table: "factures",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "fichier_pdf",
                table: "bons_livraison",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_reglements",
                table: "reglements",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_factures",
                table: "factures",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_bons_livraison",
                table: "bons_livraison",
                column: "id");

            migrationBuilder.CreateTable(
                name: "bl_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    bl_id = table.Column<int>(type: "integer", nullable: false),
                    produit_id = table.Column<int>(type: "integer", nullable: false),
                    quantite_commandee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantite_livree = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    prix_ht = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    prix_ttc = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    valide = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bl_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_bl_details_Products_produit_id",
                        column: x => x.produit_id,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_bl_details_bons_livraison_bl_id",
                        column: x => x.bl_id,
                        principalTable: "bons_livraison",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "facture_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    facture_id = table.Column<int>(type: "integer", nullable: false),
                    produit_id = table.Column<int>(type: "integer", nullable: false),
                    quantite = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    pu_ht = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_ht = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    taux_tva = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_ttc = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facture_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_facture_details_Products_produit_id",
                        column: x => x.produit_id,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_facture_details_factures_facture_id",
                        column: x => x.facture_id,
                        principalTable: "factures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Needs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroBesoin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    DesiredUrgencyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequiredDelayDays = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Justification = table.Column<string>(type: "text", nullable: true),
                    RequestedById = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidatedById = table.Column<int>(type: "integer", nullable: true),
                    DateTransmission = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateCompletion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MotifRejet = table.Column<string>(type: "text", nullable: true),
                    DateRejet = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    History = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Needs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Needs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Needs_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Needs_Users_ValidatedById",
                        column: x => x.ValidatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPriceHT = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPriceTTC = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderDetails_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuotationDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuotationId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitPriceHT = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPriceTTC = table.Column<decimal>(type: "numeric", nullable: false),
                    DeliveryDelayDays = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotationDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotationDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuotationDetails_Quotations_QuotationId",
                        column: x => x.QuotationId,
                        principalTable: "Quotations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NeedDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NeedId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPriceHT = table.Column<decimal>(type: "numeric", nullable: true),
                    IsNewProduct = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NeedDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NeedDetails_Needs_NeedId",
                        column: x => x.NeedId,
                        principalTable: "Needs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NeedDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_NeedId",
                table: "Quotations",
                column: "NeedId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_NeedId",
                table: "PurchaseOrders",
                column: "NeedId");

            migrationBuilder.CreateIndex(
                name: "IX_reglements_fournisseur_id",
                table: "reglements",
                column: "fournisseur_id");

            migrationBuilder.CreateIndex(
                name: "IX_reglements_numero_reglement",
                table: "reglements",
                column: "numero_reglement",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_factures_bc_id",
                table: "factures",
                column: "bc_id");

            migrationBuilder.CreateIndex(
                name: "IX_bl_details_bl_id",
                table: "bl_details",
                column: "bl_id");

            migrationBuilder.CreateIndex(
                name: "IX_bl_details_produit_id",
                table: "bl_details",
                column: "produit_id");

            migrationBuilder.CreateIndex(
                name: "IX_facture_details_facture_id",
                table: "facture_details",
                column: "facture_id");

            migrationBuilder.CreateIndex(
                name: "IX_facture_details_produit_id",
                table: "facture_details",
                column: "produit_id");

            migrationBuilder.CreateIndex(
                name: "IX_NeedDetails_NeedId",
                table: "NeedDetails",
                column: "NeedId");

            migrationBuilder.CreateIndex(
                name: "IX_NeedDetails_ProductId",
                table: "NeedDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Needs_ProductId",
                table: "Needs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Needs_RequestedById",
                table: "Needs",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_Needs_ValidatedById",
                table: "Needs",
                column: "ValidatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_ProductId",
                table: "PurchaseOrderDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderDetails_PurchaseOrderId",
                table: "PurchaseOrderDetails",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationDetails_ProductId",
                table: "QuotationDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QuotationDetails_QuotationId",
                table: "QuotationDetails",
                column: "QuotationId");

            migrationBuilder.AddForeignKey(
                name: "FK_bons_livraison_PurchaseOrders_bc_id",
                table: "bons_livraison",
                column: "bc_id",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_bons_livraison_Suppliers_fournisseur_id",
                table: "bons_livraison",
                column: "fournisseur_id",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bons_livraison_Users_ReceivedById",
                table: "bons_livraison",
                column: "ReceivedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_factures_PurchaseOrders_bc_id",
                table: "factures",
                column: "bc_id",
                principalTable: "PurchaseOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_factures_Suppliers_fournisseur_id",
                table: "factures",
                column: "fournisseur_id",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_factures_Users_cree_par",
                table: "factures",
                column: "cree_par",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_factures_bons_livraison_bl_id",
                table: "factures",
                column: "bl_id",
                principalTable: "bons_livraison",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Needs_NeedId",
                table: "PurchaseOrders",
                column: "NeedId",
                principalTable: "Needs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_Needs_NeedId",
                table: "Quotations",
                column: "NeedId",
                principalTable: "Needs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_reglements_Suppliers_fournisseur_id",
                table: "reglements",
                column: "fournisseur_id",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_reglements_Users_cree_par",
                table: "reglements",
                column: "cree_par",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_reglements_factures_facture_id",
                table: "reglements",
                column: "facture_id",
                principalTable: "factures",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bons_livraison_PurchaseOrders_bc_id",
                table: "bons_livraison");

            migrationBuilder.DropForeignKey(
                name: "FK_bons_livraison_Suppliers_fournisseur_id",
                table: "bons_livraison");

            migrationBuilder.DropForeignKey(
                name: "FK_bons_livraison_Users_ReceivedById",
                table: "bons_livraison");

            migrationBuilder.DropForeignKey(
                name: "FK_factures_PurchaseOrders_bc_id",
                table: "factures");

            migrationBuilder.DropForeignKey(
                name: "FK_factures_Suppliers_fournisseur_id",
                table: "factures");

            migrationBuilder.DropForeignKey(
                name: "FK_factures_Users_cree_par",
                table: "factures");

            migrationBuilder.DropForeignKey(
                name: "FK_factures_bons_livraison_bl_id",
                table: "factures");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Needs_NeedId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Quotations_Needs_NeedId",
                table: "Quotations");

            migrationBuilder.DropForeignKey(
                name: "FK_reglements_Suppliers_fournisseur_id",
                table: "reglements");

            migrationBuilder.DropForeignKey(
                name: "FK_reglements_Users_cree_par",
                table: "reglements");

            migrationBuilder.DropForeignKey(
                name: "FK_reglements_factures_facture_id",
                table: "reglements");

            migrationBuilder.DropTable(
                name: "bl_details");

            migrationBuilder.DropTable(
                name: "facture_details");

            migrationBuilder.DropTable(
                name: "NeedDetails");

            migrationBuilder.DropTable(
                name: "PurchaseOrderDetails");

            migrationBuilder.DropTable(
                name: "QuotationDetails");

            migrationBuilder.DropTable(
                name: "Needs");

            migrationBuilder.DropIndex(
                name: "IX_Quotations_NeedId",
                table: "Quotations");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_NeedId",
                table: "PurchaseOrders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_reglements",
                table: "reglements");

            migrationBuilder.DropIndex(
                name: "IX_reglements_fournisseur_id",
                table: "reglements");

            migrationBuilder.DropIndex(
                name: "IX_reglements_numero_reglement",
                table: "reglements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_factures",
                table: "factures");

            migrationBuilder.DropIndex(
                name: "IX_factures_bc_id",
                table: "factures");

            migrationBuilder.DropPrimaryKey(
                name: "PK_bons_livraison",
                table: "bons_livraison");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AverageDeliveryDelay",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "PaymentConditions",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "NeedId",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "Observations",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "ResponseDate",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "NeedId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PaymentConditions",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "RequestedDeliveryDelay",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TotalAmountHT",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DailyConsumption",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsNew",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LastPurchaseDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "banque",
                table: "reglements");

            migrationBuilder.DropColumn(
                name: "fichier_preuve",
                table: "reglements");

            migrationBuilder.DropColumn(
                name: "fichier_recu",
                table: "reglements");

            migrationBuilder.DropColumn(
                name: "fournisseur_id",
                table: "reglements");

            migrationBuilder.DropColumn(
                name: "numero_reglement",
                table: "reglements");

            migrationBuilder.DropColumn(
                name: "observations",
                table: "reglements");

            migrationBuilder.DropColumn(
                name: "type_fichier",
                table: "reglements");

            migrationBuilder.DropColumn(
                name: "bc_id",
                table: "factures");

            migrationBuilder.DropColumn(
                name: "conformite",
                table: "factures");

            migrationBuilder.DropColumn(
                name: "date_reception",
                table: "factures");

            migrationBuilder.DropColumn(
                name: "justification_conformite",
                table: "factures");

            migrationBuilder.DropColumn(
                name: "numero_facture_fournisseur",
                table: "factures");

            migrationBuilder.DropColumn(
                name: "observations",
                table: "factures");

            migrationBuilder.DropColumn(
                name: "taux_tva",
                table: "factures");

            migrationBuilder.DropColumn(
                name: "fichier_pdf",
                table: "bons_livraison");

            migrationBuilder.RenameTable(
                name: "reglements",
                newName: "Payments");

            migrationBuilder.RenameTable(
                name: "factures",
                newName: "Invoices");

            migrationBuilder.RenameTable(
                name: "bons_livraison",
                newName: "DeliveryNotes");

            migrationBuilder.RenameColumn(
                name: "TotalAmountHT",
                table: "Quotations",
                newName: "UnitPriceTTC");

            migrationBuilder.RenameColumn(
                name: "TotalVAT",
                table: "PurchaseOrders",
                newName: "UnitPriceHT");

            migrationBuilder.RenameColumn(
                name: "TotalAmountTTC",
                table: "PurchaseOrders",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Payments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "reference",
                table: "Payments",
                newName: "ReferenceNumber");

            migrationBuilder.RenameColumn(
                name: "montant",
                table: "Payments",
                newName: "AmountPaid");

            migrationBuilder.RenameColumn(
                name: "mode_paiement",
                table: "Payments",
                newName: "PaymentMethod");

            migrationBuilder.RenameColumn(
                name: "facture_id",
                table: "Payments",
                newName: "InvoiceId");

            migrationBuilder.RenameColumn(
                name: "date_paiement",
                table: "Payments",
                newName: "PaymentDate");

            migrationBuilder.RenameColumn(
                name: "date_maj",
                table: "Payments",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "date_creation",
                table: "Payments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "cree_par",
                table: "Payments",
                newName: "CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_reglements_facture_id",
                table: "Payments",
                newName: "IX_Payments_InvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_reglements_cree_par",
                table: "Payments",
                newName: "IX_Payments_CreatedById");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Invoices",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "statut",
                table: "Invoices",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "numero_facture",
                table: "Invoices",
                newName: "InvoiceNumber");

            migrationBuilder.RenameColumn(
                name: "montant_tva",
                table: "Invoices",
                newName: "TaxAmount");

            migrationBuilder.RenameColumn(
                name: "montant_ttc",
                table: "Invoices",
                newName: "AmountTTC");

            migrationBuilder.RenameColumn(
                name: "montant_ht",
                table: "Invoices",
                newName: "AmountHT");

            migrationBuilder.RenameColumn(
                name: "fournisseur_id",
                table: "Invoices",
                newName: "SupplierId");

            migrationBuilder.RenameColumn(
                name: "date_maj",
                table: "Invoices",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "date_facture",
                table: "Invoices",
                newName: "InvoiceDate");

            migrationBuilder.RenameColumn(
                name: "date_echeance",
                table: "Invoices",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "date_creation",
                table: "Invoices",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "cree_par",
                table: "Invoices",
                newName: "CreatedById");

            migrationBuilder.RenameColumn(
                name: "bl_id",
                table: "Invoices",
                newName: "DeliveryNoteId");

            migrationBuilder.RenameIndex(
                name: "IX_factures_numero_facture",
                table: "Invoices",
                newName: "IX_Invoices_InvoiceNumber");

            migrationBuilder.RenameIndex(
                name: "IX_factures_fournisseur_id",
                table: "Invoices",
                newName: "IX_Invoices_SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_factures_cree_par",
                table: "Invoices",
                newName: "IX_Invoices_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_factures_bl_id",
                table: "Invoices",
                newName: "IX_Invoices_DeliveryNoteId");

            migrationBuilder.RenameColumn(
                name: "observations",
                table: "DeliveryNotes",
                newName: "Observations");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "DeliveryNotes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "numero_bl",
                table: "DeliveryNotes",
                newName: "DeliveryNumber");

            migrationBuilder.RenameColumn(
                name: "fournisseur_id",
                table: "DeliveryNotes",
                newName: "SupplierId");

            migrationBuilder.RenameColumn(
                name: "date_reception",
                table: "DeliveryNotes",
                newName: "ReceptionDate");

            migrationBuilder.RenameColumn(
                name: "bc_id",
                table: "DeliveryNotes",
                newName: "PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_bons_livraison_ReceivedById",
                table: "DeliveryNotes",
                newName: "IX_DeliveryNotes_ReceivedById");

            migrationBuilder.RenameIndex(
                name: "IX_bons_livraison_numero_bl",
                table: "DeliveryNotes",
                newName: "IX_DeliveryNotes_DeliveryNumber");

            migrationBuilder.RenameIndex(
                name: "IX_bons_livraison_fournisseur_id",
                table: "DeliveryNotes",
                newName: "IX_DeliveryNotes_SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_bons_livraison_bc_id",
                table: "DeliveryNotes",
                newName: "IX_DeliveryNotes_PurchaseOrderId");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Quotations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "Quotations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPriceHT",
                table: "Quotations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "PurchaseOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "PurchaseOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "Payments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountPaid",
                table: "Payments",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxAmount",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountTTC",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountHT",
                table: "Invoices",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Invoices",
                table: "Invoices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeliveryNotes",
                table: "DeliveryNotes",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Quotations_ProductId",
                table: "Quotations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ProductId",
                table: "PurchaseOrders",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryNotes_PurchaseOrders_PurchaseOrderId",
                table: "DeliveryNotes",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryNotes_Suppliers_SupplierId",
                table: "DeliveryNotes",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryNotes_Users_ReceivedById",
                table: "DeliveryNotes",
                column: "ReceivedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_DeliveryNotes_DeliveryNoteId",
                table: "Invoices",
                column: "DeliveryNoteId",
                principalTable: "DeliveryNotes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Suppliers_SupplierId",
                table: "Invoices",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Users_CreatedById",
                table: "Invoices",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Invoices_InvoiceId",
                table: "Payments",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_CreatedById",
                table: "Payments",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Products_ProductId",
                table: "PurchaseOrders",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Quotations_Products_ProductId",
                table: "Quotations",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
