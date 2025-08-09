using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    /// <summary>
    /// Modèle pour les jours fériés configurables
    /// </summary>
    public class JourFerie
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required, MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Type: National, Local, Religieux, Entreprise
        /// </summary>
        [MaxLength(50)]
        public string Type { get; set; } = "National";

        public bool Actif { get; set; } = true;

        /// <summary>
        /// Si vrai, ce jour férié se répète chaque année (ex: Noël)
        /// </summary>
        public bool Recurrent { get; set; } = true;

        public DateTime DateCreation { get; set; } = new DateTime(2025, 1, 1);
    }
}