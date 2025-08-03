using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    public class ValidationDemande
    {
        [Key]
        public int Id { get; set; }

        // Relations
        [Required]
        public int DemandeId { get; set; }
        public virtual DemandeConge Demande { get; set; } = null!;

        [Required]
        public int ValidateurId { get; set; }
        public virtual Utilisateur Validateur { get; set; } = null!;

        // Validation
        public bool Approuve { get; set; }

        [MaxLength(1000)]
        public string? Commentaire { get; set; }

        public DateTime DateValidation { get; set; } = DateTime.Now;

        public int OrdreValidation { get; set; } // 1 = Chef pôle, 2 = Chef équipe
    }
}
