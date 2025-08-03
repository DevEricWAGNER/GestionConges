using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    public class Pole
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool Actif { get; set; } = true;

        // Navigation - Chef du pôle (optionnel)
        public int? ChefId { get; set; }
        public virtual Utilisateur? Chef { get; set; }

        // Navigation - Employés du pôle
        public virtual ICollection<Utilisateur> Employes { get; set; } = new List<Utilisateur>();

        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}
