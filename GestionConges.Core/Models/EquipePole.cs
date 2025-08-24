using System.ComponentModel.DataAnnotations;

namespace GestionConges.Core.Models
{
    /// <summary>
    /// Table de liaison many-to-many entre Equipe et Pole
    /// </summary>
    public class EquipePole
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EquipeId { get; set; }
        public virtual Equipe Equipe { get; set; } = null!;

        [Required]
        public int PoleId { get; set; }
        public virtual Pole Pole { get; set; } = null!;

        public DateTime DateAffectation { get; set; } = DateTime.Now;
        public DateTime? DateFin { get; set; }
        public bool Actif { get; set; } = true;
    }
}
