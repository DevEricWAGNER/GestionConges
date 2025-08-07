using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;

namespace GestionConges.WPF.Services
{
    public interface IValidationService
    {
        Task<bool> SoumettreDemande(int demandeId, int utilisateurId);
        Task<bool> ValiderDemande(int demandeId, int validateurId, bool approuve, string? commentaire = null);
        Task<List<DemandeConge>> ObtenirDemandesAValider(int validateurId);
        StatusDemande DeterminerProchainStatut(DemandeConge demande, bool approuve);
        bool PeutValiderDemande(DemandeConge demande, Utilisateur validateur);
    }

    public class ValidationService : IValidationService
    {
        private readonly GestionCongesContext _context;

        public ValidationService(GestionCongesContext context)
        {
            _context = context;
        }

        public async Task<bool> SoumettreDemande(int demandeId, int utilisateurId)
        {
            try
            {
                var demande = await _context.DemandesConges
                    .Include(d => d.Utilisateur)
                        .ThenInclude(u => u.Pole)
                    .FirstOrDefaultAsync(d => d.Id == demandeId);

                if (demande == null || demande.UtilisateurId != utilisateurId || demande.Statut != StatusDemande.Brouillon)
                    return false;

                // Déterminer le premier niveau de validation
                demande.Statut = DeterminerPremierNiveauValidation(demande);
                demande.DateModification = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValiderDemande(int demandeId, int validateurId, bool approuve, string? commentaire = null)
        {
            try
            {
                var demande = await _context.DemandesConges
                    .Include(d => d.Utilisateur)
                        .ThenInclude(u => u.Pole)
                    .Include(d => d.Validations)
                    .FirstOrDefaultAsync(d => d.Id == demandeId);

                var validateur = await _context.Utilisateurs
                    .Include(u => u.Pole)
                    .FirstOrDefaultAsync(u => u.Id == validateurId);

                if (demande == null || validateur == null || !PeutValiderDemande(demande, validateur))
                    return false;

                // Créer l'enregistrement de validation
                var validation = new ValidationDemande
                {
                    DemandeId = demandeId,
                    ValidateurId = validateurId,
                    Approuve = approuve,
                    Commentaire = commentaire,
                    DateValidation = DateTime.Now,
                    OrdreValidation = DeterminerOrdreValidation(demande, validateur)
                };

                _context.ValidationsDemanades.Add(validation);

                // Mettre à jour le statut de la demande
                if (approuve)
                {
                    demande.Statut = DeterminerProchainStatut(demande, true);
                    if (demande.Statut == StatusDemande.Approuve)
                    {
                        demande.DateValidationFinale = DateTime.Now;
                    }
                }
                else
                {
                    demande.Statut = StatusDemande.Refuse;
                    demande.CommentaireRefus = commentaire;
                }

                demande.DateModification = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<DemandeConge>> ObtenirDemandesAValider(int validateurId)
        {
            var validateur = await _context.Utilisateurs
                .Include(u => u.Pole)
                .FirstOrDefaultAsync(u => u.Id == validateurId);

            if (validateur == null)
                return new List<DemandeConge>();

            var query = _context.DemandesConges
                .Include(d => d.Utilisateur)
                    .ThenInclude(u => u.Pole)
                .Include(d => d.TypeAbsence)
                .Where(d => d.Statut == StatusDemande.EnAttenteChefPole ||
                           d.Statut == StatusDemande.EnAttenteChefEquipe);

            // Filtrer selon le rôle du validateur
            if (validateur.Role == RoleUtilisateur.ChefPole)
            {
                // Chef de pôle : seulement les demandes de son pôle en attente chef pôle
                query = query.Where(d => d.Statut == StatusDemande.EnAttenteChefPole &&
                                        d.Utilisateur.PoleId == validateur.PoleId);
            }
            else if (validateur.Role == RoleUtilisateur.ChefEquipe)
            {
                // Chef d'équipe : toutes les demandes en attente chef équipe
                query = query.Where(d => d.Statut == StatusDemande.EnAttenteChefEquipe);
            }

            return await query.OrderBy(d => d.DateCreation).ToListAsync();
        }

        public StatusDemande DeterminerProchainStatut(DemandeConge demande, bool approuve)
        {
            if (!approuve)
                return StatusDemande.Refuse;

            return demande.Statut switch
            {
                StatusDemande.EnAttenteChefPole => StatusDemande.EnAttenteChefEquipe,
                StatusDemande.EnAttenteChefEquipe => StatusDemande.Approuve,
                _ => demande.Statut
            };
        }

        public bool PeutValiderDemande(DemandeConge demande, Utilisateur validateur)
        {
            // Ne peut pas valider sa propre demande
            if (demande.UtilisateurId == validateur.Id)
                return false;

            return demande.Statut switch
            {
                StatusDemande.EnAttenteChefPole => validateur.Role == RoleUtilisateur.ChefPole &&
                                                  validateur.PoleId == demande.Utilisateur.PoleId,
                StatusDemande.EnAttenteChefEquipe => validateur.Role == RoleUtilisateur.ChefEquipe,
                _ => false
            };
        }

        private StatusDemande DeterminerPremierNiveauValidation(DemandeConge demande)
        {
            // Si c'est un chef d'équipe → approuvé automatiquement
            if (demande.Utilisateur.Role == RoleUtilisateur.ChefEquipe)
            {
                return StatusDemande.Approuve;
            }

            // Si l'utilisateur n'a pas de pôle OU si le pôle n'a pas de chef
            // OU si l'utilisateur EST le chef de son pôle
            // → Aller directement au chef d'équipe
            if (demande.Utilisateur.PoleId == null ||
                demande.Utilisateur.Pole?.ChefId == null ||
                demande.Utilisateur.Pole?.ChefId == demande.UtilisateurId)
            {
                return StatusDemande.EnAttenteChefEquipe;
            }

            // Sinon, commencer par le chef de pôle
            return StatusDemande.EnAttenteChefPole;
        }

        private int DeterminerOrdreValidation(DemandeConge demande, Utilisateur validateur)
        {
            return validateur.Role switch
            {
                RoleUtilisateur.ChefPole => 1,
                RoleUtilisateur.ChefEquipe => 2,
                _ => 0
            };
        }
    }
}