using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    public class TypeAbsence
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(7)] // Format #RRGGBB
        public string CouleurHex { get; set; } = "#3498db";

        public bool Actif { get; set; } = true;

        public bool NecessiteValidation { get; set; } = true;

        public int OrdreAffichage { get; set; } = 0;

        // Navigation
        public virtual ICollection<DemandeConge> Demandes { get; set; } = new List<DemandeConge>();

        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}
