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
                    else if (demande.Statut == StatusDemande.EnAttenteChefEquipe && ancienStatut == StatusDemande.EnAttenteChefPole)
                    {
                        var prochainValidateur = await _context.Utilisateurs
                            .FirstOrDefaultAsync(u => u.Role == RoleUtilisateur.ChefEquipe && u.Actif);

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

                System.Diagnostics.Debug.WriteLine($"✅ Validateur trouvé : {validateur.NomComplet}");
                System.Diagnostics.Debug.WriteLine($"   - ID: {validateur.Id}");
                System.Diagnostics.Debug.WriteLine($"   - Rôle: {validateur.Role}");
                System.Diagnostics.Debug.WriteLine($"   - PoleId: {validateur.PoleId}");

                // ✅ REQUÊTE AVEC DEBUG DÉTAILLÉ
                var toutesLesDemandes = await _context.DemandesConges
                    .Include(d => d.Utilisateur)
                        .ThenInclude(u => u.Pole)
                    .Include(d => d.TypeAbsence)
                    .Where(d => d.Statut == StatusDemande.EnAttenteChefPole ||
                               d.Statut == StatusDemande.EnAttenteChefEquipe)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"📊 Nombre total de demandes en attente: {toutesLesDemandes.Count}");

                foreach (var demande in toutesLesDemandes)
                {
                    System.Diagnostics.Debug.WriteLine($"   Demande ID {demande.Id}:");
                    System.Diagnostics.Debug.WriteLine($"     - Statut: {demande.Statut}");
                    System.Diagnostics.Debug.WriteLine($"     - Demandeur: {demande.Utilisateur?.NomComplet}");
                    System.Diagnostics.Debug.WriteLine($"     - PoleId Demandeur: {demande.Utilisateur?.PoleId}");
                    System.Diagnostics.Debug.WriteLine($"     - Type: {demande.TypeAbsence?.Nom}");
                }

                List<DemandeConge> resultat;

                // Filtrer selon le rôle du validateur
                if (validateur.Role == RoleUtilisateur.ChefPole)
                {
                    System.Diagnostics.Debug.WriteLine($"🎯 Filtre Chef de Pôle - PoleId validateur: {validateur.PoleId}");

                    resultat = toutesLesDemandes
                        .Where(d => d.Statut == StatusDemande.EnAttenteChefPole &&
                                   d.Utilisateur.PoleId == validateur.PoleId)
                        .OrderBy(d => d.DateCreation)
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"📋 Demandes filtrées pour ce chef de pôle: {resultat.Count}");

                    // ✅ DEBUG SPÉCIFIQUE POUR JEAN ET MARIE
                    var demandeMarie = toutesLesDemandes.FirstOrDefault(d => d.UtilisateurId == 4); // Marie = ID 4
                    if (demandeMarie != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔍 DEMANDE DE MARIE TROUVÉE:");
                        System.Diagnostics.Debug.WriteLine($"   - ID: {demandeMarie.Id}");
                        System.Diagnostics.Debug.WriteLine($"   - Statut: {demandeMarie.Statut} (EnAttenteChefPole = 1)");
                        System.Diagnostics.Debug.WriteLine($"   - PoleId Marie: {demandeMarie.Utilisateur?.PoleId}");
                        System.Diagnostics.Debug.WriteLine($"   - PoleId Jean: {validateur.PoleId}");
                        System.Diagnostics.Debug.WriteLine($"   - Match PoleId? {demandeMarie.Utilisateur?.PoleId == validateur.PoleId}");
                        System.Diagnostics.Debug.WriteLine($"   - Statut OK? {demandeMarie.Statut == StatusDemande.EnAttenteChefPole}");

                        bool devraitEtreVisible = demandeMarie.Statut == StatusDemande.EnAttenteChefPole &&
                                                demandeMarie.Utilisateur?.PoleId == validateur.PoleId;
                        System.Diagnostics.Debug.WriteLine($"   - 🎯 DEVRAIT ÊTRE VISIBLE: {devraitEtreVisible}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ DEMANDE DE MARIE NON TROUVÉE dans toutes les demandes!");
                    }
                }
                else if (validateur.Role == RoleUtilisateur.ChefEquipe)
                {
                    System.Diagnostics.Debug.WriteLine($"🎯 Filtre Chef d'Équipe");
                    resultat = toutesLesDemandes
                        .Where(d => d.Statut == StatusDemande.EnAttenteChefEquipe)
                        .OrderBy(d => d.DateCreation)
                        .ToList();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Utilisateur n'est pas un validateur (rôle: {validateur.Role})");
                    return new List<DemandeConge>();
                }

                System.Diagnostics.Debug.WriteLine($"🎯 RÉSULTAT FINAL: {resultat.Count} demande(s) à valider");
                foreach (var demande in resultat)
                {
                    System.Diagnostics.Debug.WriteLine($"   ✅ {demande.Utilisateur?.NomComplet} - {demande.TypeAbsence?.Nom}");
                }

                return resultat;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 ERREUR dans ObtenirDemandesAValider : {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 StackTrace: {ex.StackTrace}");
                return new List<DemandeConge>();
            }
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
                                                  validateur.PoleId.HasValue &&
                                                  demande.Utilisateur?.PoleId.HasValue == true &&
                                                  validateur.PoleId == demande.Utilisateur.PoleId,
                StatusDemande.EnAttenteChefEquipe => validateur.Role == RoleUtilisateur.ChefEquipe,
                _ => false
            };
        }

        private async Task<StatusDemande> DeterminerPremierNiveauValidationAsync(DemandeConge demande)
        {
            // Si c'est un chef d'équipe → approuvé automatiquement
            if (demande.Utilisateur.Role == RoleUtilisateur.ChefEquipe)
            {
                return StatusDemande.Approuve;
            }

            // Si c'est un chef de pôle → va directement au chef équipe
            if (demande.Utilisateur.Role == RoleUtilisateur.ChefPole)
            {
                return StatusDemande.EnAttenteChefEquipe;
            }

            // Si l'utilisateur n'a pas de pôle → directement chef équipe
            if (!demande.Utilisateur.PoleId.HasValue)
            {
                return StatusDemande.EnAttenteChefEquipe;
            }

            // Chercher s'il y a un chef de pôle pour ce pôle (différent du demandeur)
            var aUnChefDePole = await _context.Utilisateurs
                .AnyAsync(u => u.PoleId == demande.Utilisateur.PoleId &&
                              u.Role == RoleUtilisateur.ChefPole &&
                              u.Actif &&
                              u.Id != demande.UtilisateurId);

            // S'il y a un chef de pôle → passer par lui d'abord
            if (aUnChefDePole)
            {
                return StatusDemande.EnAttenteChefPole;
            }

            // Sinon → directement chef équipe
            return StatusDemande.EnAttenteChefEquipe;
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