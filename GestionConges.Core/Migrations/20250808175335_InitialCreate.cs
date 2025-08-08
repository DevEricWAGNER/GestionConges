using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionConges.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TypesAbsences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CouleurHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    NecessiteValidation = table.Column<bool>(type: "bit", nullable: false),
                    OrdreAffichage = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesAbsences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemandesConges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilisateurId = table.Column<int>(type: "int", nullable: false),
                    TypeAbsenceId = table.Column<int>(type: "int", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TypeJourneeDebut = table.Column<int>(type: "int", nullable: false),
                    TypeJourneeFin = table.Column<int>(type: "int", nullable: false),
                    NombreJours = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CommentaireRefus = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateValidationFinale = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandesConges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandesConges_TypesAbsences_TypeAbsenceId",
                        column: x => x.TypeAbsenceId,
                        principalTable: "TypesAbsences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Poles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    ChefId = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Login = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MotDePasseHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    PoleId = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DerniereConnexion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Poles_PoleId",
                        column: x => x.PoleId,
                        principalTable: "Poles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ValidationsDemanades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandeId = table.Column<int>(type: "int", nullable: false),
                    ValidateurId = table.Column<int>(type: "int", nullable: false),
                    Approuve = table.Column<bool>(type: "bit", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateValidation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrdreValidation = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationsDemanades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationsDemanades_DemandesConges_DemandeId",
                        column: x => x.DemandeId,
                        principalTable: "DemandesConges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValidationsDemanades_Utilisateurs_ValidateurId",
                        column: x => x.ValidateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Poles",
                columns: new[] { "Id", "Actif", "ChefId", "DateCreation", "Description", "Nom" },
                values: new object[,]
                {
                    { 1, true, null, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe de développement logiciel", "Développement" },
                    { 2, true, null, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe infrastructure et réseaux", "Réseaux" },
                    { 3, true, null, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe Reflex", "Reflex" },
                    { 4, true, null, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe logistique et support", "Logistique" }
                });

            migrationBuilder.InsertData(
                table: "TypesAbsences",
                columns: new[] { "Id", "Actif", "CouleurHex", "DateCreation", "Description", "NecessiteValidation", "Nom", "OrdreAffichage" },
                values: new object[,]
                {
                    { 1, true, "#e74c3c", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Congés Payés", 1 },
                    { 2, true, "#3498db", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, true, "RTT", 2 },
                    { 3, true, "#f39c12", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Maladie", 3 },
                    { 4, true, "#27ae60", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Déplacement", 4 },
                    { 5, true, "#9b59b6", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, true, "Formation", 5 }
                });

            migrationBuilder.InsertData(
                table: "Utilisateurs",
                columns: new[] { "Id", "Actif", "DateCreation", "DerniereConnexion", "Email", "Login", "MotDePasseHash", "Nom", "PoleId", "Prenom", "Role" },
                values: new object[] { 1, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "admin@entreprise.com", "admin", "$2a$11$rBV2HDeWlL.rsozGbz2gauO9H8sAzM7Ke.K5nCS5JoTa/4EK8TA4K", "Admin", null, "Super", 2 });

            migrationBuilder.CreateIndex(
                name: "IX_DemandesConges_TypeAbsenceId",
                table: "DemandesConges",
                column: "TypeAbsenceId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesConges_UtilisateurId",
                table: "DemandesConges",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Poles_ChefId",
                table: "Poles",
                column: "ChefId");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Email",
                table: "Utilisateurs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Login",
                table: "Utilisateurs",
                column: "Login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_PoleId",
                table: "Utilisateurs",
                column: "PoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationsDemanades_DemandeId",
                table: "ValidationsDemanades",
                column: "DemandeId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationsDemanades_ValidateurId",
                table: "ValidationsDemanades",
                column: "ValidateurId");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandesConges_Utilisateurs_UtilisateurId",
                table: "DemandesConges",
                column: "UtilisateurId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Poles_Utilisateurs_ChefId",
                table: "Poles",
                column: "ChefId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Poles_Utilisateurs_ChefId",
                table: "Poles");

            migrationBuilder.DropTable(
                name: "ValidationsDemanades");

            migrationBuilder.DropTable(
                name: "DemandesConges");

            migrationBuilder.DropTable(
                name: "TypesAbsences");

            migrationBuilder.DropTable(
                name: "Utilisateurs");

            migrationBuilder.DropTable(
                name: "Poles");
        }
    }
}
