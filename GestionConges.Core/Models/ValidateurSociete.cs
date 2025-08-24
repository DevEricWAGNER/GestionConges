using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    /// <summary>
    /// Table de liaison pour définir dans quelles sociétés un utilisateur peut valider des congés
    /// </summary>
    public class ValidateurSociete
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ValidateurId { get; set; }
        public virtual Utilisateur Validateur { get; set; } = null!;

        [Required]
        public int SocieteId { get; set; }
        public virtual Societe Societe { get; set; } = null!;

        /// <summary>
        /// Niveau de validation: 1 = Chef pôle, 2 = Chef équipe
        /// </summary>
        public int NiveauValidation { get; set; } = 1;

        public DateTime DateAffectation { get; set; } = DateTime.Now;
        public DateTime? DateFin { get; set; }
        public bool Actif { get; set; } = true;

        [MaxLength(500)]
        public string? Commentaire { get; set; }
    }
}
