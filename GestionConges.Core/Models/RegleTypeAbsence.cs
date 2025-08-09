using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    /// <summary>
    /// Modèle pour les règles spécifiques par type d'absence
    /// </summary>
    public class RegleTypeAbsence
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TypeAbsenceId { get; set; }
        public virtual TypeAbsence TypeAbsence { get; set; } = null!;

        /// <summary>
        /// Maximum de jours par an pour ce type (null = illimité)
        /// </summary>
        public int? MaximumParAn { get; set; }

        /// <summary>
        /// Maximum de jours consécutifs (null = illimité)
        /// </summary>
        public int? MaximumConsecutif { get; set; }

        /// <summary>
        /// Préavis minimum en jours (null = utilise le préavis global)
        /// </summary>
        public int? PreavisMinimum { get; set; }

        /// <summary>
        /// Anticipation maximum en jours (null = utilise l'anticipation globale)
        /// </summary>
        public int? AnticipationMaximum { get; set; }

        /// <summary>
        /// Si vrai, ce type nécessite une justification
        /// </summary>
        public bool NecessiteJustification { get; set; } = false;

        /// <summary>
        /// Règles personnalisées en JSON ou texte
        /// </summary>
        public string? ReglesPersonnalisees { get; set; }

        public DateTime DateCreation { get; set; } = new DateTime(2025, 1, 1);
        public DateTime? DateModification { get; set; }
    }
}