using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;

namespace GestionConges.WPF.Services
{
    public interface INotificationService
    {
        Task EnvoyerRappelsValidationAutomatiques();
        Task<List<DemandeConge>> ObtenirDemandesAnciennesEnAttente(int ancienneteJours = 3);
    }

    public class NotificationService : INotificationService
    {
        private readonly GestionCongesContext _context;
        private readonly EmailService _emailService;
        private readonly ParametresService _parametresService;

        public NotificationService(GestionCongesContext context)
        {
            _context = context;
            _emailService = new EmailService(context);
            _parametresService = new ParametresService(context);
        }

        public async Task EnvoyerRappelsValidationAutomatiques()
        {
            try
            {
                if (!await _emailService.EstActive())
                    return;

                // Obtenir les demandes anciennes en attente
                var demandesAnciennes = await ObtenirDemandesAnciennesEnAttente();

                if (!demandesAnciennes.Any())
                    return;

                // Grouper par validateur
                var demandesParValidateur = new Dictionary<Utilisateur, List<DemandeConge>>();

                foreach (var demande in demandesAnciennes)
                {
                    var validateur = await ObtenirValidateurConcerne(demande);
                    if (validateur != null)
                    {
                        if (!demandesParValidateur.ContainsKey(validateur))
                            demandesParValidateur[validateur] = new List<DemandeConge>();

                        demandesParValidateur[validateur].Add(demande);
                    }
                }

                // Envoyer les rappels
                foreach (var (validateur, demandes) in demandesParValidateur)
                {
                    await _emailService.EnvoyerRappelValidation(demandes, validateur);

                    // Marquer les demandes comme "rappel envoyé" (optionnel)
                    System.Diagnostics.Debug.WriteLine($"Rappel envoyé à {validateur.NomComplet} pour {demandes.Count} demande(s)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'envoi des rappels automatiques: {ex.Message}");
            }
        }

        public async Task<List<DemandeConge>> ObtenirDemandesAnciennesEnAttente(int ancienneteJours = 3)
        {
            var dateLimit = DateTime.Now.AddDays(-ancienneteJours);

            return await _context.DemandesConges
                .Include(d => d.Utilisateur)
                    .ThenInclude(u => u.Pole)
                .Include(d => d.TypeAbsence)
                .Where(d => (d.Statut == StatusDemande.EnAttenteValidateur ||
                            d.Statut == StatusDemande.EnAttenteAdmin) &&
                           d.DateCreation <= dateLimit)
                .OrderBy(d => d.DateCreation)
                .ToListAsync();
        }

        private async Task<Utilisateur?> ObtenirValidateurConcerne(DemandeConge demande)
        {
            return demande.Statut switch
            {
                StatusDemande.EnAttenteValidateur => await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Role == RoleUtilisateur.Validateur &&
                                             u.PoleId == demande.Utilisateur.PoleId &&
                                             u.Actif),

                StatusDemande.EnAttenteAdmin => await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Role == RoleUtilisateur.Admin && u.Actif),

                _ => null
            };
        }

        /// <summary>
        /// Méthode pour tester manuellement les rappels depuis l'interface admin
        /// </summary>
        public async Task<string> TesterRappelsAutomatiques()
        {
            try
            {
                var demandesAnciennes = await ObtenirDemandesAnciennesEnAttente();

                if (!demandesAnciennes.Any())
                    return "✅ Aucune demande ancienne en attente trouvée.";

                await EnvoyerRappelsValidationAutomatiques();

                return $"✅ Rappels envoyés pour {demandesAnciennes.Count} demande(s) ancienne(s).";
            }
            catch (Exception ex)
            {
                return $"❌ Erreur lors du test des rappels: {ex.Message}";
            }
        }
    }
}