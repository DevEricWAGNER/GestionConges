using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    public class Equipe
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool Actif { get; set; } = true;

        // Relations
        [Required]
        public int SocieteId { get; set; }
        public virtual Societe Societe { get; set; } = null!;

        // Navigation - Employés de l'équipe
        public virtual ICollection<Utilisateur> Employes { get; set; } = new List<Utilisateur>();

        // Navigation - Pôles rattachés à cette équipe
        public virtual ICollection<Pole> Poles { get; set; } = new List<Pole>();

        public DateTime DateCreation { get; set; } = DateTime.Now;
    }
}
