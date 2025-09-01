using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionConges.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPaysToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodePays",
                table: "Utilisateurs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodePays",
                table: "Utilisateurs");
        }
    }
}
