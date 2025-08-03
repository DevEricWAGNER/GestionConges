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

            // Données de seed
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Types d'absences par défaut
            modelBuilder.Entity<TypeAbsence>().HasData(
                new TypeAbsence { Id = 1, Nom = "Congés Payés", CouleurHex = "#e74c3c", OrdreAffichage = 1 },
                new TypeAbsence { Id = 2, Nom = "RTT", CouleurHex = "#3498db", OrdreAffichage = 2 },
                new TypeAbsence { Id = 3, Nom = "Maladie", CouleurHex = "#f39c12", OrdreAffichage = 3 },
                new TypeAbsence { Id = 4, Nom = "Déplacement", CouleurHex = "#27ae60", OrdreAffichage = 4 },
                new TypeAbsence { Id = 5, Nom = "Formation", CouleurHex = "#9b59b6", OrdreAffichage = 5 }
            );

            // Pôles par défaut
            modelBuilder.Entity<Pole>().HasData(
                new Pole { Id = 1, Nom = "Développement", Description = "Équipe de développement logiciel" },
                new Pole { Id = 2, Nom = "Réseaux", Description = "Équipe infrastructure et réseaux" },
                new Pole { Id = 3, Nom = "Reflex", Description = "Équipe Reflex" },
                new Pole { Id = 4, Nom = "Logistique", Description = "Équipe logistique et support" }
            );

            // Utilisateur admin par défaut (mot de passe: "admin123")
            modelBuilder.Entity<Utilisateur>().HasData(
                new Utilisateur
                {
                    Id = 1,
                    Nom = "Admin",
                    Prenom = "Super",
                    Email = "admin@entreprise.com",
                    Login = "admin",
                    MotDePasseHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = RoleUtilisateur.ChefEquipe,
                    PoleId = null
                }
            );
        }
    }
}