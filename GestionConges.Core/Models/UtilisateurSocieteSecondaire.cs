using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    /// <summary>
    /// Table de liaison pour les sociétés secondaires d'un utilisateur
    /// </summary>
    public class UtilisateurSocieteSecondaire
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UtilisateurId { get; set; }
        public virtual Utilisateur Utilisateur { get; set; } = null!;

        [Required]
        public int SocieteId { get; set; }
        public virtual Societe Societe { get; set; } = null!;

        public DateTime DateAffectation { get; set; } = DateTime.Now;
        public DateTime? DateFin { get; set; }
        public bool Actif { get; set; } = true;

        [MaxLength(500)]
        public string? Commentaire { get; set; }
    }
}
