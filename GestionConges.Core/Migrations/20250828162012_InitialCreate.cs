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
                name: "Societes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Societes", x => x.Id);
                });

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
                name: "Equipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    SocieteId = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipes_Societes_SocieteId",
                        column: x => x.SocieteId,
                        principalTable: "Societes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "Poles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    EquipeId = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Poles_Equipes_EquipeId",
                        column: x => x.EquipeId,
                        principalTable: "Equipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    MotDePasseHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    Admin = table.Column<bool>(type: "bit", nullable: false),
                    SocieteId = table.Column<int>(type: "int", nullable: false),
                    EquipeId = table.Column<int>(type: "int", nullable: false),
                    PoleId = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DerniereConnexion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Equipes_EquipeId",
                        column: x => x.EquipeId,
                        principalTable: "Equipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Poles_PoleId",
                        column: x => x.PoleId,
                        principalTable: "Poles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Societes_SocieteId",
                        column: x => x.SocieteId,
                        principalTable: "Societes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_DemandesConges_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UtilisateursSocietesSecondaires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilisateurId = table.Column<int>(type: "int", nullable: false),
                    SocieteId = table.Column<int>(type: "int", nullable: false),
                    DateAffectation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilisateursSocietesSecondaires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UtilisateursSocietesSecondaires_Societes_SocieteId",
                        column: x => x.SocieteId,
                        principalTable: "Societes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UtilisateursSocietesSecondaires_Utilisateurs_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidateursSocietes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ValidateurId = table.Column<int>(type: "int", nullable: false),
                    SocieteId = table.Column<int>(type: "int", nullable: false),
                    NiveauValidation = table.Column<int>(type: "int", nullable: false),
                    DateAffectation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidateursSocietes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidateursSocietes_Societes_SocieteId",
                        column: x => x.SocieteId,
                        principalTable: "Societes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValidateursSocietes_Utilisateurs_ValidateurId",
                        column: x => x.ValidateurId,
                        principalTable: "Utilisateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    { 4, "Validation", "DelaiValidationValidateur", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Délai validation chef pôle (jours)", "7" },
                    { 5, "Validation", "DelaiValidationAdmin", new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Délai validation chef équipe (jours)", "5" },
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

            migrationBuilder.InsertData(
                table: "Societes",
                columns: new[] { "Id", "Actif", "DateCreation", "Description", "Nom" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Siège principal de l'entreprise", "Siège Social" },
                    { 2, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Site de Dambach", "Dambach" },
                    { 3, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Site de Kronembourg", "Kronembourg" }
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
                table: "Equipes",
                columns: new[] { "Id", "Actif", "DateCreation", "Description", "Nom", "SocieteId" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Direction générale", "Direction", 1 },
                    { 2, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe projets", "Projets", 1 },
                    { 3, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe commerciale", "Commercial", 1 },
                    { 4, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe transport Dambach", "Transport", 2 },
                    { 5, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe logistique Dambach", "Logistique", 2 },
                    { 6, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe production Kronembourg", "Production", 3 }
                });

            migrationBuilder.InsertData(
                table: "Poles",
                columns: new[] { "Id", "Actif", "DateCreation", "Description", "EquipeId", "Nom" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe de développement logiciel", 2, "Développement" },
                    { 2, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe infrastructure et réseaux", 2, "Réseaux" },
                    { 3, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe Reflex", 2, "Reflex" },
                    { 4, true, new DateTime(2025, 1, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), "Équipe logistique et support", 5, "Logistique" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandesConges_TypeAbsenceId",
                table: "DemandesConges",
                column: "TypeAbsenceId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandesConges_UtilisateurId",
                table: "DemandesConges",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipes_SocieteId_Nom",
                table: "Equipes",
                columns: new[] { "SocieteId", "Nom" },
                unique: true);

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
                name: "IX_Poles_EquipeId_Nom",
                table: "Poles",
                columns: new[] { "EquipeId", "Nom" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReglesTypesAbsences_TypeAbsenceId",
                table: "ReglesTypesAbsences",
                column: "TypeAbsenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Societes_Nom",
                table: "Societes",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Email",
                table: "Utilisateurs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_EquipeId",
                table: "Utilisateurs",
                column: "EquipeId");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_PoleId",
                table: "Utilisateurs",
                column: "PoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_SocieteId",
                table: "Utilisateurs",
                column: "SocieteId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilisateursSocietesSecondaires_SocieteId",
                table: "UtilisateursSocietesSecondaires",
                column: "SocieteId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilisateursSocietesSecondaires_UtilisateurId_SocieteId",
                table: "UtilisateursSocietesSecondaires",
                columns: new[] { "UtilisateurId", "SocieteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidateursSocietes_SocieteId",
                table: "ValidateursSocietes",
                column: "SocieteId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidateursSocietes_ValidateurId_SocieteId_NiveauValidation",
                table: "ValidateursSocietes",
                columns: new[] { "ValidateurId", "SocieteId", "NiveauValidation" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationsDemanades_DemandeId",
                table: "ValidationsDemanades",
                column: "DemandeId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationsDemanades_ValidateurId",
                table: "ValidationsDemanades",
                column: "ValidateurId");
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

            migrationBuilder.DropTable(
                name: "UtilisateursSocietesSecondaires");

            migrationBuilder.DropTable(
                name: "ValidateursSocietes");

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

            migrationBuilder.DropTable(
                name: "Equipes");

            migrationBuilder.DropTable(
                name: "Societes");
        }
    }
}
