using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GesAchats.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmailVerificationCodeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make UserId nullable
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "EmailVerificationCodes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // Add Email column
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "EmailVerificationCodes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            // Add Purpose column
            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "EmailVerificationCodes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "EmailVerificationCodes");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "EmailVerificationCodes");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "EmailVerificationCodes",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
