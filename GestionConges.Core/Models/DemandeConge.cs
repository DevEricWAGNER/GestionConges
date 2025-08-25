using System.ComponentModel.DataAnnotations;
using GestionConges.Core.Enums;

namespace GestionConges.Core.Models
{
    public class DemandeConge
    {
        [Key]
        public int Id { get; set; }

        // Relations
        [Required]
        public int UtilisateurId { get; set; }
        public virtual Utilisateur Utilisateur { get; set; } = null!;

        [Required]
        public int TypeAbsenceId { get; set; }
        public virtual TypeAbsence TypeAbsence { get; set; } = null!;

        // Dates
        [Required]
        public DateTime DateDebut { get; set; }

        [Required]
        public DateTime DateFin { get; set; }

        public TypeJournee TypeJourneeDebut { get; set; } = TypeJournee.JourneeComplete;
        public TypeJournee TypeJourneeFin { get; set; } = TypeJournee.JourneeComplete;

        // Calculs
        public decimal NombreJours { get; set; } // Calculé automatiquement

        // Statut et workflow
        public StatusDemande Statut { get; set; } = StatusDemande.Brouillon;

        [MaxLength(1000)]
        public string? Commentaire { get; set; }

        [MaxLength(1000)]
        public string? CommentaireRefus { get; set; }

        // Métadonnées
        public DateTime DateCreation { get; set; } = DateTime.Now;
        public DateTime? DateModification { get; set; }
        public DateTime? DateValidationFinale { get; set; }

        // Navigation - Validations
        public virtual ICollection<ValidationDemande> Validations { get; set; } = new List<ValidationDemande>();

        // Propriétés calculées
        public string StatutLibelle => Statut switch
        {
            StatusDemande.Brouillon => "Brouillon",
            StatusDemande.EnAttenteValidateur => "En attente Validateur",
            StatusDemande.EnAttenteAdmin => "En attente admin",
            StatusDemande.Approuve => "Approuvé",
            StatusDemande.Refuse => "Refusé",
            StatusDemande.Annule => "Annulé",
            _ => "Inconnu"
        };

        public bool EstEnAttente => Statut == StatusDemande.EnAttenteValidateur ||
                                   Statut == StatusDemande.EnAttenteAdmin;

        public bool EstApprouve => Statut == StatusDemande.Approuve;
    }
}
