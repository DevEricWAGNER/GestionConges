using GestionConges.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required]
        public string MotDePasseHash { get; set; } = string.Empty;

        public string? CodePays { get; set; }

        [NotMapped]
        public Pays? Pays { get; set; }

        public RoleUtilisateur Role { get; set; } = RoleUtilisateur.Employe;

        public bool Actif { get; set; } = true;

        public bool Admin { get; set; } = false;

        // Relations principales (obligatoires)
        [Required]
        public int SocieteId { get; set; }
        public virtual Societe Societe { get; set; } = null!;

        [Required]
        public int EquipeId { get; set; }
        public virtual Equipe Equipe { get; set; } = null!;

        // Pôle optionnel (seulement si l'équipe a des pôles)
        public int? PoleId { get; set; }
        public virtual Pole? Pole { get; set; }

        // Relations secondaires
        public virtual ICollection<UtilisateurSocieteSecondaire> SocietesSecondaires { get; set; } = new List<UtilisateurSocieteSecondaire>();

        // Relations de validation
        public virtual ICollection<ValidateurSociete> SocietesValidation { get; set; } = new List<ValidateurSociete>();

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
            RoleUtilisateur.Validateur => "Employé / Valideur de congés",
            RoleUtilisateur.Admin => "Employé / Admin",
            _ => "Inconnu"
        };

        public bool EstValidateur => SocietesValidation.Any();
    }
}
