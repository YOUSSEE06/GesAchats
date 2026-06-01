using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GesAchats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardKpiSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardKpiSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SnapshotDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BesEnCoursCount = table.Column<int>(type: "integer", nullable: false),
                    BesTransmisCount = table.Column<int>(type: "integer", nullable: false),
                    DevEnAttenteCount = table.Column<int>(type: "integer", nullable: false),
                    DevValideCount = table.Column<int>(type: "integer", nullable: false),
                    FournisseursActifsCount = table.Column<int>(type: "integer", nullable: false),
                    TotalBcCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardKpiSnapshots", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardKpiSnapshots");
        }
    }
}
