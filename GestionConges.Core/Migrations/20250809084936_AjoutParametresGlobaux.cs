using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionConges.Core.Migrations
{
    /// <inheritdoc />
    public partial class AjoutParametresGlobaux : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JoursFeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    Recurrent = table.Column<bool>(type: "bit", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoursFeries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParametresGlobaux",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valeur = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Categorie = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametresGlobaux", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReglesTypesAbsences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeAbsenceId = table.Column<int>(type: "int", nullable: false),
                    MaximumParAn = table.Column<int>(type: "int", nullable: true),
                    MaximumConsecutif = table.Column<int>(type: "int", nullable: true),
                    PreavisMinimum = table.Column<int>(type: "int", nullable: true),
                    AnticipationMaximum = table.Column<int>(type: "int", nullable: true),
                    NecessiteJustification = table.Column<bool>(type: "bit", nullable: false),
                    ReglesPersonnalisees = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglesTypesAbsences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReglesTypesAbsences_TypesAbsences_TypeAbsenceId",
                        column: x => x.TypeAbsenceId,
                        principalTable: "TypesAbsences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "JoursFeries",
                columns: new[] { "Id", "Actif", "Date", "DateCreation", "Description", "Nom", "Recurrent", "Type" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Nouvel An", true, "National" },
                    { 2, true, new DateTime(2025, 4, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Lundi de Pâques", false, "National" },
                    { 3, true, new DateTime(2025, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Fête du Travail", true, "National" },
                    { 4, true, new DateTime(2025, 5, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Fête de la Victoire", true, "National" },
                    { 5, true, new DateTime(2025, 5, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Ascension", false, "National" },
                    { 6, true, new DateTime(2025, 6, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Lundi de Pentecôte", false, "National" },
                    { 7, true, new DateTime(2025, 7, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Fête Nationale", true, "National" },
                    { 8, true, new DateTime(2025, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Assomption", true, "National" },
                    { 9, true, new DateTime(2025, 11, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Toussaint", true, "National" },
                    { 10, true, new DateTime(2025, 11, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Armistice", true, "National" },
                    { 11, true, new DateTime(2025, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), null, "Noël", true, "National" }
                });

            migrationBuilder.InsertData(
                table: "ParametresGlobaux",
                columns: new[] { "Id", "Categorie", "Cle", "DateModification", "Description", "Valeur" },
                values: new object[,]
                {
                    { 1, "Calendrier", "JoursOuvres", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Jours ouvrés (1=Lundi, 7=Dimanche)", "1,2,3,4,5" },
                    { 2, "Calendrier", "ExclureFeries", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Exclure jours fériés du calcul", "true" },
                    { 3, "Calendrier", "DebutAnneeConges", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Mois de début d'année de congés (1-12)", "1" },
                    { 4, "Validation", "DelaiValidationChefPole", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Délai validation chef pôle (jours)", "7" },
                    { 5, "Validation", "DelaiValidationChefEquipe", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Délai validation chef équipe (jours)", "5" },
                    { 6, "Validation", "PreavisMinimum", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Préavis minimum congés (jours)", "14" },
                    { 7, "Validation", "AnticipationMaximum", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Anticipation maximum (jours)", "365" },
                    { 8, "Validation", "EscaladeAutomatique", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Escalade auto si délai dépassé", "false" },
                    { 9, "Email", "EmailActif", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Notifications email activées", "false" },
                    { 10, "Email", "ServeurSMTP", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Serveur SMTP", "smtp.gmail.com" },
                    { 11, "Email", "PortSMTP", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Port SMTP", "587" },
                    { 12, "Email", "UtilisateurSMTP", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Nom d'utilisateur SMTP", "" },
                    { 13, "Email", "MotDePasseSMTP", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Mot de passe SMTP (crypté)", "" },
                    { 14, "Email", "SSLSMTP", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Utiliser SSL/TLS", "true" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_JoursFeries_Date",
                table: "JoursFeries",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_JoursFeries_Date_Actif",
                table: "JoursFeries",
                columns: new[] { "Date", "Actif" });

            migrationBuilder.CreateIndex(
                name: "IX_ParametresGlobaux_Categorie",
                table: "ParametresGlobaux",
                column: "Categorie");

            migrationBuilder.CreateIndex(
                name: "IX_ParametresGlobaux_Cle",
                table: "ParametresGlobaux",
                column: "Cle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReglesTypesAbsences_TypeAbsenceId",
                table: "ReglesTypesAbsences",
                column: "TypeAbsenceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JoursFeries");

            migrationBuilder.DropTable(
                name: "ParametresGlobaux");

            migrationBuilder.DropTable(
                name: "ReglesTypesAbsences");
        }
    }
}
