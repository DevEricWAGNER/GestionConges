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

                // ✅ Utiliser la nouvelle méthode async
                demande.Statut = await DeterminerPremierNiveauValidationAsync(demande);
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
                    .Include(d => d.TypeAbsence)
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
                var ancienStatut = demande.Statut;

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

                // ✅ NOUVEAU : Envoyer notifications email
                await EnvoyerNotificationsValidation(demande, validateur, approuve, commentaire, ancienStatut);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task EnvoyerNotificationsValidation(DemandeConge demande, Utilisateur validateur, bool approuve, string? commentaire, StatusDemande ancienStatut)
        {
            try
            {
                var emailService = new EmailService(_context);

                // Vérifier si les emails sont activés
                if (!await emailService.EstActive())
                {
                    System.Diagnostics.Debug.WriteLine("📧 Emails désactivés - pas de notification envoyée");
                    return;
                }

                if (approuve)
                {
                    // Si complètement approuvée → notifier le demandeur
                    if (demande.Statut == StatusDemande.Approuve)
                    {
                        System.Diagnostics.Debug.WriteLine($"📧 Envoi notification approbation à {demande.Utilisateur.NomComplet}");
                        await emailService.EnvoyerNotificationDemandeApprouvee(demande);
                    }
                    // Sinon, si passe au niveau suivant → notifier le prochain validateur
                    else if (demande.Statut == StatusDemande.EnAttenteAdmin && ancienStatut == StatusDemande.EnAttenteValidateur)
                    {
                        var prochainValidateur = await _context.Utilisateurs
                            .FirstOrDefaultAsync(u => u.Role == RoleUtilisateur.Admin && u.Actif);

                        if (prochainValidateur?.Email != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"📧 Envoi notification escalade à {prochainValidateur.NomComplet}");
                            await emailService.EnvoyerNotificationNouvelleDemande(demande, prochainValidateur);
                        }
                    }
                }
                else
                {
                    // Demande refusée → notifier le demandeur
                    System.Diagnostics.Debug.WriteLine($"📧 Envoi notification refus à {demande.Utilisateur.NomComplet}");
                    await emailService.EnvoyerNotificationDemandeRefusee(demande, commentaire ?? "Aucun motif spécifié");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Erreur envoi notifications validation : {ex.Message}");
                // Ne pas faire échouer la validation pour un problème d'email
            }
        }

        public async Task<List<DemandeConge>> ObtenirDemandesAValider(int validateurId)
        {
            try
            {
                var validateur = await _context.Utilisateurs
                    .Include(u => u.Pole)
                    .FirstOrDefaultAsync(u => u.Id == validateurId);

                if (validateur == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Validateur ID {validateurId} non trouvé");
                    return new List<DemandeConge>();
                }

                var query = _context.DemandesConges
                    .Include(d => d.Utilisateur)
                        .ThenInclude(u => u.Pole)
                    .Include(d => d.TypeAbsence)
                    .Where(d => d.UtilisateurId != validateurId); // Exclure ses propres demandes

                List<DemandeConge> resultat;

                if (validateur.Role == RoleUtilisateur.Admin)
                {
                    // Admin voit toutes les demandes en attente (Validateur + Admin)
                    resultat = await query
                        .Where(d => d.Statut == StatusDemande.EnAttenteValidateur ||
                                   d.Statut == StatusDemande.EnAttenteAdmin)
                        .OrderBy(d => d.DateCreation)
                        .ToListAsync();
                }
                else if (validateur.Role == RoleUtilisateur.Validateur)
                {
                    // Validateur ne voit que les demandes en attente validateur
                    resultat = await query
                        .Where(d => d.Statut == StatusDemande.EnAttenteValidateur)
                        .OrderBy(d => d.DateCreation)
                        .ToListAsync();
                }
                else
                {
                    // Les employés ne peuvent pas valider
                    return new List<DemandeConge>();
                }

                System.Diagnostics.Debug.WriteLine($"🎯 {validateur.NomComplet} ({validateur.Role}) peut valider {resultat.Count} demande(s)");
                return resultat;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERREUR dans ObtenirDemandesAValider : {ex.Message}");
                return new List<DemandeConge>();
            }
        }

        public StatusDemande DeterminerProchainStatut(DemandeConge demande, bool approuve)
        {
            if (!approuve)
                return StatusDemande.Refuse;

            return demande.Statut switch
            {
                // Un employé validé par un Validateur → Approuvé (plus besoin de passer par Admin)
                StatusDemande.EnAttenteValidateur => StatusDemande.Approuve,

                // Une demande en attente Admin → Approuvé
                StatusDemande.EnAttenteAdmin => StatusDemande.Approuve,

                _ => demande.Statut
            };
        }

        public bool PeutValiderDemande(DemandeConge demande, Utilisateur validateur)
        {
            // Ne peut pas valider sa propre demande
            if (demande.UtilisateurId == validateur.Id)
                return false;

            // Vérifier selon le statut de la demande
            return demande.Statut switch
            {
                // En attente validateur → seuls Validateur et Admin peuvent valider
                StatusDemande.EnAttenteValidateur => validateur.Role == RoleUtilisateur.Validateur ||
                                                   validateur.Role == RoleUtilisateur.Admin,

                // En attente admin → seul Admin peut valider
                StatusDemande.EnAttenteAdmin => validateur.Role == RoleUtilisateur.Admin,

                _ => false
            };
        }

        private async Task<StatusDemande> DeterminerPremierNiveauValidationAsync(DemandeConge demande)
        {
            // Si c'est un Admin → approuvé automatiquement (pas besoin de validation)
            if (demande.Utilisateur.Role == RoleUtilisateur.Admin)
            {
                return StatusDemande.Approuve;
            }

            // Si c'est un Validateur → doit être validé par un Admin
            if (demande.Utilisateur.Role == RoleUtilisateur.Validateur)
            {
                return StatusDemande.EnAttenteAdmin;
            }

            // Si c'est un Employé → peut être validé par un Validateur OU un Admin
            if (demande.Utilisateur.Role == RoleUtilisateur.Employe)
            {
                // Vérifier s'il y a un Validateur disponible (actif et différent du demandeur)
                var aUnValidateurDisponible = await _context.Utilisateurs
                    .AnyAsync(u => u.Role == RoleUtilisateur.Validateur &&
                                  u.Actif &&
                                  u.Id != demande.UtilisateurId);

                // S'il y a un Validateur disponible → passer par lui d'abord
                if (aUnValidateurDisponible)
                {
                    return StatusDemande.EnAttenteValidateur;
                }
                else
                {
                    // Sinon → directement à l'Admin
                    return StatusDemande.EnAttenteAdmin;
                }
            }

            // Par défaut → directement à l'Admin (sécurité)
            return StatusDemande.EnAttenteAdmin;
        }

        private int DeterminerOrdreValidation(DemandeConge demande, Utilisateur validateur)
        {
            return validateur.Role switch
            {
                RoleUtilisateur.Validateur => 1,
                _ => 0
            };
        }
    }
}