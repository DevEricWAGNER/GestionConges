using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    /// <summary>
    /// Modèle pour stocker les paramètres globaux de l'application
    /// </summary>
    public class ParametreGlobal
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Cle { get; set; } = string.Empty;

        [Required]
        public string Valeur { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Catégorie: Calendrier, Validation, Email, General
        /// </summary>
        [MaxLength(50)]
        public string Categorie { get; set; } = "General";

        public DateTime DateModification { get; set; } = new DateTime(2025, 1, 1);
    }
}