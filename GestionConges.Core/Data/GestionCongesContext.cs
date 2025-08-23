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
        public DbSet<Pole> Poles { get; set; }
        public DbSet<TypeAbsence> TypesAbsences { get; set; }
        public DbSet<DemandeConge> DemandesConges { get; set; }
        public DbSet<ValidationDemande> ValidationsDemanades { get; set; }
        public DbSet<JourFerie> JoursFeries { get; set; }
        public DbSet<ParametreGlobal> ParametresGlobaux { get; set; }
        public DbSet<RegleTypeAbsence> ReglesTypesAbsences { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration Utilisateur
            modelBuilder.Entity<Utilisateur>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Login).IsUnique();

                entity.HasOne(u => u.Pole)
                      .WithMany(p => p.Employes)
                      .HasForeignKey(u => u.PoleId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.Role)
                      .HasConversion<int>();
            });

            // Configuration Pole
            modelBuilder.Entity<Pole>(entity =>
            {
                entity.HasOne(p => p.Chef)
                      .WithMany()
                      .HasForeignKey(p => p.ChefId)
                      .OnDelete(DeleteBehavior.SetNull);
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
            var seedDate = new DateTime(2025, 1, 1, 12, 0, 0); // ✅ Date fixe au lieu de DateTime.Now

            // Types d'absences par défaut (GARDER EXISTANT)
            modelBuilder.Entity<TypeAbsence>().HasData(
                new TypeAbsence { Id = 1, Nom = "Congés Payés", CouleurHex = "#e74c3c", OrdreAffichage = 1, Actif = true, NecessiteValidation = true, DateCreation = seedDate },
                new TypeAbsence { Id = 2, Nom = "RTT", CouleurHex = "#3498db", OrdreAffichage = 2, Actif = true, NecessiteValidation = true, DateCreation = seedDate },
                new TypeAbsence { Id = 3, Nom = "Maladie", CouleurHex = "#f39c12", OrdreAffichage = 3, Actif = true, NecessiteValidation = true, DateCreation = seedDate },
                new TypeAbsence { Id = 4, Nom = "Déplacement", CouleurHex = "#27ae60", OrdreAffichage = 4, Actif = true, NecessiteValidation = true, DateCreation = seedDate },
                new TypeAbsence { Id = 5, Nom = "Formation", CouleurHex = "#9b59b6", OrdreAffichage = 5, Actif = true, NecessiteValidation = true, DateCreation = seedDate }
            );

            // Pôles par défaut (GARDER EXISTANT)
            modelBuilder.Entity<Pole>().HasData(
                new Pole { Id = 1, Nom = "Développement", Description = "Équipe de développement logiciel", Actif = true, DateCreation = seedDate },
                new Pole { Id = 2, Nom = "Réseaux", Description = "Équipe infrastructure et réseaux", Actif = true, DateCreation = seedDate },
                new Pole { Id = 3, Nom = "Reflex", Description = "Équipe Reflex", Actif = true, DateCreation = seedDate },
                new Pole { Id = 4, Nom = "Logistique", Description = "Équipe logistique et support", Actif = true, DateCreation = seedDate }
            );

            // ✅ NOUVEAUX PARAMÈTRES PAR DÉFAUT
            modelBuilder.Entity<ParametreGlobal>().HasData(
                // Calendrier
                new ParametreGlobal { Id = 1, Cle = "JoursOuvres", Valeur = "1,2,3,4,5", Categorie = "Calendrier", Description = "Jours ouvrés (1=Lundi, 7=Dimanche)", DateModification = seedDate },
                new ParametreGlobal { Id = 2, Cle = "ExclureFeries", Valeur = "true", Categorie = "Calendrier", Description = "Exclure jours fériés du calcul", DateModification = seedDate },
                new ParametreGlobal { Id = 3, Cle = "DebutAnneeConges", Valeur = "1", Categorie = "Calendrier", Description = "Mois de début d'année de congés (1-12)", DateModification = seedDate },

                // Validation
                new ParametreGlobal { Id = 4, Cle = "DelaiValidationChefPole", Valeur = "7", Categorie = "Validation", Description = "Délai validation chef pôle (jours)", DateModification = seedDate },
                new ParametreGlobal { Id = 5, Cle = "DelaiValidationChefEquipe", Valeur = "5", Categorie = "Validation", Description = "Délai validation chef équipe (jours)", DateModification = seedDate },
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

            // ✅ JOURS FÉRIÉS FRANÇAIS 2025 - DATES FIXES
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
