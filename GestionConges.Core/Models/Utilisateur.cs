using System.ComponentModel.DataAnnotations;
using GestionConges.Core.Enums;

namespace GestionConges.Core.Models
{
    public class Utilisateur
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string MotDePasseHash { get; set; } = string.Empty;

        public RoleUtilisateur Role { get; set; } = RoleUtilisateur.Employe;

        public bool Actif { get; set; } = true;

        // Relations
        public int? PoleId { get; set; }
        public virtual Pole? Pole { get; set; }

        // Navigation - Demandes créées
        public virtual ICollection<DemandeConge> Demandes { get; set; } = new List<DemandeConge>();

        // Navigation - Validations effectuées
        public virtual ICollection<ValidationDemande> ValidationsEffectuees { get; set; } = new List<ValidationDemande>();

        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime? DerniereConnexion { get; set; }

        // Propriétés calculées
        public string NomComplet => $"{Prenom} {Nom}";
        public string RoleLibelle => Role switch
        {
            RoleUtilisateur.Employe => "Employé",
            RoleUtilisateur.ChefPole => "Chef de Pôle",
            RoleUtilisateur.ChefEquipe => "Chef d'Équipe",
            _ => "Inconnu"
        };
    }
}
