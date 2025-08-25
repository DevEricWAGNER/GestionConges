using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;

namespace GestionConges.Core.Data
{
    public class GestionCongesContext : DbContext
    {
        public GestionCongesContext(DbContextOptions<GestionCongesContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Societe> Societes { get; set; }
        public DbSet<Equipe> Equipes { get; set; }
        public DbSet<Pole> Poles { get; set; }
        public DbSet<EquipePole> EquipesPoles { get; set; }
        public DbSet<UtilisateurSocieteSecondaire> UtilisateursSocietesSecondaires { get; set; }
        public DbSet<ValidateurSociete> ValidateursSocietes { get; set; }
        public DbSet<TypeAbsence> TypesAbsences { get; set; }
        public DbSet<DemandeConge> DemandesConges { get; set; }
        public DbSet<ValidationDemande> ValidationsDemanades { get; set; }
        public DbSet<JourFerie> JoursFeries { get; set; }
        public DbSet<ParametreGlobal> ParametresGlobaux { get; set; }
        public DbSet<RegleTypeAbsence> ReglesTypesAbsences { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration Societe
            modelBuilder.Entity<Societe>(entity =>
            {
                entity.HasIndex(e => e.Nom).IsUnique();
            });

            // Configuration Equipe
            modelBuilder.Entity<Equipe>(entity =>
            {
                entity.HasIndex(e => new { e.SocieteId, e.Nom }).IsUnique();

                entity.HasOne(e => e.Societe)
                      .WithMany(s => s.Equipes)
                      .HasForeignKey(e => e.SocieteId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuration Pole
            modelBuilder.Entity<Pole>(entity =>
            {
                entity.HasIndex(e => e.Nom).IsUnique();
            });

            // Configuration EquipePole (relation many-to-many)
            modelBuilder.Entity<EquipePole>(entity =>
            {
                entity.HasIndex(e => new { e.EquipeId, e.PoleId }).IsUnique();

                entity.HasOne(ep => ep.Equipe)
                      .WithMany()
                      .HasForeignKey(ep => ep.EquipeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ep => ep.Pole)
                      .WithMany()
                      .HasForeignKey(ep => ep.PoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration Utilisateur
            modelBuilder.Entity<Utilisateur>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();

                // Relations principales (obligatoires)
                entity.HasOne(u => u.Societe)
                      .WithMany(s => s.EmployesPrincipaux)
                      .HasForeignKey(u => u.SocieteId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Equipe)
                      .WithMany(e => e.Employes)
                      .HasForeignKey(u => u.EquipeId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Pôle optionnel
                entity.HasOne(u => u.Pole)
                      .WithMany(p => p.Employes)
                      .HasForeignKey(u => u.PoleId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.Role)
                      .HasConversion<int>();
            });

            // Configuration UtilisateurSocieteSecondaire
            modelBuilder.Entity<UtilisateurSocieteSecondaire>(entity =>
            {
                entity.HasIndex(e => new { e.UtilisateurId, e.SocieteId }).IsUnique();

                entity.HasOne(uss => uss.Utilisateur)
                      .WithMany(u => u.SocietesSecondaires)
                      .HasForeignKey(uss => uss.UtilisateurId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(uss => uss.Societe)
                      .WithMany(s => s.UtilisateursSecondaires)
                      .HasForeignKey(uss => uss.SocieteId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration ValidateurSociete
            modelBuilder.Entity<ValidateurSociete>(entity =>
            {
                entity.HasIndex(e => new { e.ValidateurId, e.SocieteId, e.NiveauValidation }).IsUnique();

                entity.HasOne(vs => vs.Validateur)
                      .WithMany(u => u.SocietesValidation)
                      .HasForeignKey(vs => vs.ValidateurId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(vs => vs.Societe)
                      .WithMany(s => s.Validateurs)
                      .HasForeignKey(vs => vs.SocieteId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuration DemandeConge
            modelBuilder.Entity<DemandeConge>(entity =>
            {
                entity.HasOne(d => d.Utilisateur)
                      .WithMany(u => u.Demandes)
                      .HasForeignKey(d => d.UtilisateurId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.TypeAbsence)
                      .WithMany(t => t.Demandes)
                      .HasForeignKey(d => d.TypeAbsenceId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Statut)
                      .HasConversion<int>();

                entity.Property(e => e.TypeJourneeDebut)
                      .HasConversion<int>();

                entity.Property(e => e.TypeJourneeFin)
                      .HasConversion<int>();

                entity.Property(e => e.NombreJours)
                      .HasPrecision(4, 1); // Ex: 999.5 jours max
            });

            // Configuration ValidationDemande
            modelBuilder.Entity<ValidationDemande>(entity =>
            {
                entity.HasOne(v => v.Demande)
                      .WithMany(d => d.Validations)
                      .HasForeignKey(v => v.DemandeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.Validateur)
                      .WithMany(u => u.ValidationsEffectuees)
                      .HasForeignKey(v => v.ValidateurId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuration JourFerie
            modelBuilder.Entity<JourFerie>(entity =>
            {
                entity.HasIndex(e => e.Date);
                entity.HasIndex(e => new { e.Date, e.Actif });
            });

            // Configuration ParametreGlobal
            modelBuilder.Entity<ParametreGlobal>(entity =>
            {
                entity.HasIndex(e => e.Cle).IsUnique();
                entity.HasIndex(e => e.Categorie);
            });

            // Configuration RegleTypeAbsence
            modelBuilder.Entity<RegleTypeAbsence>(entity =>
            {
                entity.HasOne(r => r.TypeAbsence)
                      .WithMany()
                      .HasForeignKey(r => r.TypeAbsenceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TypeAbsenceId).IsUnique();
            });

            // Données de seed
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var seedDate = new DateTime(2025, 1, 1, 12, 0, 0); // Date fixe au lieu de DateTime.Now

            // ===== SOCIÉTÉS DE DÉMONSTRATION =====
            modelBuilder.Entity<Societe>().HasData(
                new Societe { Id = 1, Nom = "Siège Social", Description = "Siège principal de l'entreprise", Actif = true, DateCreation = seedDate },
                new Societe { Id = 2, Nom = "Dambach", Description = "Site de Dambach", Actif = true, DateCreation = seedDate },
                new Societe { Id = 3, Nom = "Kronembourg", Description = "Site de Kronembourg", Actif = true, DateCreation = seedDate }
            );

            // ===== ÉQUIPES DE DÉMONSTRATION =====
            modelBuilder.Entity<Equipe>().HasData(
                // Siège Social
                new Equipe { Id = 1, Nom = "Direction", Description = "Direction générale", SocieteId = 1, Actif = true, DateCreation = seedDate },
                new Equipe { Id = 2, Nom = "Projets", Description = "Équipe projets", SocieteId = 1, Actif = true, DateCreation = seedDate },
                new Equipe { Id = 3, Nom = "Commercial", Description = "Équipe commerciale", SocieteId = 1, Actif = true, DateCreation = seedDate },

                // Dambach
                new Equipe { Id = 4, Nom = "Transport", Description = "Équipe transport Dambach", SocieteId = 2, Actif = true, DateCreation = seedDate },
                new Equipe { Id = 5, Nom = "Logistique", Description = "Équipe logistique Dambach", SocieteId = 2, Actif = true, DateCreation = seedDate },

                // Kronembourg
                new Equipe { Id = 6, Nom = "Production", Description = "Équipe production Kronembourg", SocieteId = 3, Actif = true, DateCreation = seedDate }
            );

            // ===== PÔLES DE DÉMONSTRATION =====
            modelBuilder.Entity<Pole>().HasData(
                new Pole { Id = 1, Nom = "Développement", Description = "Équipe de développement logiciel", Actif = true, DateCreation = seedDate },
                new Pole { Id = 2, Nom = "Réseaux", Description = "Équipe infrastructure et réseaux", Actif = true, DateCreation = seedDate },
                new Pole { Id = 3, Nom = "Reflex", Description = "Équipe Reflex", Actif = true, DateCreation = seedDate },
                new Pole { Id = 4, Nom = "Logistique", Description = "Équipe logistique et support", Actif = true, DateCreation = seedDate }
            );

            // ===== RELATIONS ÉQUIPES-PÔLES =====
            modelBuilder.Entity<EquipePole>().HasData(
                // L'équipe Projets du siège a les pôles Développement et Réseaux
                new EquipePole { Id = 1, EquipeId = 2, PoleId = 1, Actif = true, DateAffectation = seedDate },
                new EquipePole { Id = 2, EquipeId = 2, PoleId = 2, Actif = true, DateAffectation = seedDate },
                new EquipePole { Id = 3, EquipeId = 2, PoleId = 3, Actif = true, DateAffectation = seedDate },

                // L'équipe Logistique de Dambach a le pôle Logistique
                new EquipePole { Id = 4, EquipeId = 5, PoleId = 4, Actif = true, DateAffectation = seedDate }
            );

            // Types d'absences par défaut
            modelBuilder.Entity<TypeAbsence>().HasData(
                new TypeAbsence { Id = 1, Nom = "Congés Payés", CouleurHex = "#e74c3c", OrdreAffichage = 1, Actif = true, NecessiteValidation = true, DateCreation = seedDate },
                new TypeAbsence { Id = 2, Nom = "RTT", CouleurHex = "#3498db", OrdreAffichage = 2, Actif = true, NecessiteValidation = true, DateCreation = seedDate },
                new TypeAbsence { Id = 3, Nom = "Maladie", CouleurHex = "#f39c12", OrdreAffichage = 3, Actif = true, NecessiteValidation = true, DateCreation = seedDate },
                new TypeAbsence { Id = 4, Nom = "Déplacement", CouleurHex = "#27ae60", OrdreAffichage = 4, Actif = true, NecessiteValidation = true, DateCreation = seedDate },
                new TypeAbsence { Id = 5, Nom = "Formation", CouleurHex = "#9b59b6", OrdreAffichage = 5, Actif = true, NecessiteValidation = true, DateCreation = seedDate }
            );

            // Paramètres globaux
            modelBuilder.Entity<ParametreGlobal>().HasData(
                // Calendrier
                new ParametreGlobal { Id = 1, Cle = "JoursOuvres", Valeur = "1,2,3,4,5", Categorie = "Calendrier", Description = "Jours ouvrés (1=Lundi, 7=Dimanche)", DateModification = seedDate },
                new ParametreGlobal { Id = 2, Cle = "ExclureFeries", Valeur = "true", Categorie = "Calendrier", Description = "Exclure jours fériés du calcul", DateModification = seedDate },
                new ParametreGlobal { Id = 3, Cle = "DebutAnneeConges", Valeur = "1", Categorie = "Calendrier", Description = "Mois de début d'année de congés (1-12)", DateModification = seedDate },

                // Validation
                new ParametreGlobal { Id = 4, Cle = "DelaiValidationValidateur", Valeur = "7", Categorie = "Validation", Description = "Délai validation chef pôle (jours)", DateModification = seedDate },
                new ParametreGlobal { Id = 5, Cle = "DelaiValidationAdmin", Valeur = "5", Categorie = "Validation", Description = "Délai validation chef équipe (jours)", DateModification = seedDate },
                new ParametreGlobal { Id = 6, Cle = "PreavisMinimum", Valeur = "14", Categorie = "Validation", Description = "Préavis minimum congés (jours)", DateModification = seedDate },
                new ParametreGlobal { Id = 7, Cle = "AnticipationMaximum", Valeur = "365", Categorie = "Validation", Description = "Anticipation maximum (jours)", DateModification = seedDate },
                new ParametreGlobal { Id = 8, Cle = "EscaladeAutomatique", Valeur = "false", Categorie = "Validation", Description = "Escalade auto si délai dépassé", DateModification = seedDate },

                // Email
                new ParametreGlobal { Id = 9, Cle = "EmailActif", Valeur = "false", Categorie = "Email", Description = "Notifications email activées", DateModification = seedDate },
                new ParametreGlobal { Id = 10, Cle = "ServeurSMTP", Valeur = "smtp.gmail.com", Categorie = "Email", Description = "Serveur SMTP", DateModification = seedDate },
                new ParametreGlobal { Id = 11, Cle = "PortSMTP", Valeur = "587", Categorie = "Email", Description = "Port SMTP", DateModification = seedDate },
                new ParametreGlobal { Id = 12, Cle = "UtilisateurSMTP", Valeur = "", Categorie = "Email", Description = "Nom d'utilisateur SMTP", DateModification = seedDate },
                new ParametreGlobal { Id = 13, Cle = "MotDePasseSMTP", Valeur = "", Categorie = "Email", Description = "Mot de passe SMTP (crypté)", DateModification = seedDate },
                new ParametreGlobal { Id = 14, Cle = "SSLSMTP", Valeur = "true", Categorie = "Email", Description = "Utiliser SSL/TLS", DateModification = seedDate }
            );

            // Jours fériés français 2025
            modelBuilder.Entity<JourFerie>().HasData(
                new JourFerie { Id = 1, Date = new DateTime(2025, 1, 1), Nom = "Nouvel An", Type = "National", Recurrent = true, Actif = true, DateCreation = seedDate },
                new JourFerie { Id = 2, Date = new DateTime(2025, 4, 21), Nom = "Lundi de Pâques", Type = "National", Recurrent = false, Actif = true, DateCreation = seedDate },
                new JourFerie { Id = 3, Date = new DateTime(2025, 5, 1), Nom = "Fête du Travail", Type = "National", Recurrent = true, Actif = true, DateCreation = seedDate },
                new JourFerie { Id = 4, Date = new DateTime(2025, 5, 8), Nom = "Fête de la Victoire", Type = "National", Recurrent = true, Actif = true, DateCreation = seedDate },
                new JourFerie { Id = 5, Date = new DateTime(2025, 5, 29), Nom = "Ascension", Type = "National", Recurrent = false, Actif = true, DateCreation = seedDate },
                new JourFerie { Id = 6, Date = new DateTime(2025, 6, 9), Nom = "Lundi de Pentecôte", Type = "National", Recurrent = false, Actif = true, DateCreation = seedDate },
                new JourFerie { Id = 7, Date = new DateTime(2025, 7, 14), Nom = "Fête Nationale", Type = "National", Recurrent = true, Actif = true, DateCreation = seedDate },
                new JourFerie { Id = 8, Date = new DateTime(2025, 8, 15), Nom = "Assomption", Type = "National", Recurrent = true, Actif = true, DateCreation = seedDate },
                new JourFerie { Id = 9, Date = new DateTime(2025, 11, 1), Nom = "Toussaint", Type = "National", Recurrent = true, Actif = true, DateCreation = seedDate },
                new JourFerie { Id = 10, Date = new DateTime(2025, 11, 11), Nom = "Armistice", Type = "National", Recurrent = true, Actif = true, DateCreation = seedDate },
                new JourFerie { Id = 11, Date = new DateTime(2025, 12, 25), Nom = "Noël", Type = "National", Recurrent = true, Actif = true, DateCreation = seedDate }
            );
        }
    }
}