using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    public class Societe
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool Actif { get; set; } = true;

        // Navigation - Employés de la société
        public virtual ICollection<Utilisateur> EmployesPrincipaux { get; set; } = new List<Utilisateur>();

        // Navigation - Utilisateurs ayant cette société en secondaire
        public virtual ICollection<UtilisateurSocieteSecondaire> UtilisateursSecondaires { get; set; } = new List<UtilisateurSocieteSecondaire>();

        // Navigation - Équipes de la société
        public virtual ICollection<Equipe> Equipes { get; set; } = new List<Equipe>();

        // Navigation - Validateurs autorisés dans cette société
        public virtual ICollection<ValidateurSociete> Validateurs { get; set; } = new List<ValidateurSociete>();

        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}