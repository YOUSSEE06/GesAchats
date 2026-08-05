using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GesAchats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentsNavigation : Migration
    {
        // Migration modèle uniquement : la navigation inverse Invoice.Payments ne
        // modifie pas le schéma. (SupabaseAuthId a été retiré : la colonne est déjà
        // créée idempotente au démarrage de l'app, cf. App.xaml.cs.)

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
