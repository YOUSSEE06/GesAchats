using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GesAchats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMagasin : Migration
    {
        /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Ajouter MagasinId à Products seulement si la colonne n'existe pas
        migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                               WHERE table_name = 'Products' AND column_name = 'MagasinId') THEN
                    ALTER TABLE ""Products"" ADD COLUMN ""MagasinId"" INTEGER NULL;
                END IF;
            END $$;
        ");

        // Créer la table Magasins seulement si elle n'existe pas
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS ""Magasins"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Nom"" VARCHAR(200) NOT NULL,
                ""IsActive"" BOOLEAN NOT NULL DEFAULT true
            );
        ");

        // Ajouter l'index IX_Products_MagasinId seulement si il n'existe pas
        migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM pg_indexes 
                               WHERE indexname = 'IX_Products_MagasinId') THEN
                    CREATE INDEX ""IX_Products_MagasinId"" ON ""Products""(""MagasinId"");
                END IF;
            END $$;
        ");

        // Ajouter la clé étrangère seulement si elle n'existe pas
        migrationBuilder.Sql(@"
            DO $$
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints 
                               WHERE constraint_name = 'FK_Products_Magasins_MagasinId') THEN
                    ALTER TABLE ""Products"" 
                    ADD CONSTRAINT ""FK_Products_Magasins_MagasinId"" 
                    FOREIGN KEY (""MagasinId"") REFERENCES ""Magasins""(""Id"");
                END IF;
            END $$;
        ");
    }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Magasins_MagasinId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Magasins");

            migrationBuilder.DropIndex(
                name: "IX_Products_MagasinId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MagasinId",
                table: "Products");
        }
    }
}
