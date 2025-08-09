using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;

namespace GestionConges.WPF.Services
{
    public interface IParametresService
    {
        Task<string?> ObtenirParametre(string cle, string? valeurParDefaut = null);
        Task<T?> ObtenirParametre<T>(string cle, T? valeurParDefaut = default);
        Task SauvegarderParametre(string cle, string valeur, string categorie = "General", string? description = null);
        Task<Dictionary<string, string>> ObtenirParametresCategorie(string categorie);
        Task<List<JourFerie>> ObtenirJoursFeries(int annee);
        Task<bool> EstJourFerie(DateTime date);
        Task<List<DayOfWeek>> ObtenirJoursOuvres();
        Task<int> CalculerJoursOuvres(DateTime dateDebut, DateTime dateFin, bool exclureFeries = true);

        // ✅ NOUVELLES MÉTHODES POUR LA VALIDATION
        Task<bool> ValiderPreavisRespected(DateTime dateDebut, int? preavisPersonnalise = null);
        Task<bool> ValiderAnticipationRespectee(DateTime dateDebut, int? anticipationPersonnalisee = null);
        Task<ValidationResult> ValiderDemandeCongeComplete(DemandeConge demande);
        Task<List<RegleTypeAbsence>> ObtenirReglesTypesAbsences();
        Task SauvegarderRegleTypeAbsence(RegleTypeAbsence regle);
    }

    public class ParametresService : IParametresService
    {
        private readonly GestionCongesContext _context;
        private readonly Dictionary<string, string> _cacheParametres = new();

        public ParametresService(GestionCongesContext context)
        {
            _context = context;
        }

        public async Task<string?> ObtenirParametre(string cle, string? valeurParDefaut = null)
        {
            // Vérifier le cache d'abord
            if (_cacheParametres.TryGetValue(cle, out var valeurCache))
                return valeurCache;

            var parametre = await _context.ParametresGlobaux
                .FirstOrDefaultAsync(p => p.Cle == cle);

            var valeur = parametre?.Valeur ?? valeurParDefaut;

            // Mettre en cache
            if (valeur != null)
                _cacheParametres[cle] = valeur;

            return valeur;
        }

        public async Task<T?> ObtenirParametre<T>(string cle, T? valeurParDefaut = default)
        {
            var valeurString = await ObtenirParametre(cle);

            if (string.IsNullOrEmpty(valeurString))
                return valeurParDefaut;

            try
            {
                return (T)Convert.ChangeType(valeurString, typeof(T));
            }
            catch
            {
                return valeurParDefaut;
            }
        }

        public async Task SauvegarderParametre(string cle, string valeur, string categorie = "General", string? description = null)
        {
            var parametre = await _context.ParametresGlobaux
                .FirstOrDefaultAsync(p => p.Cle == cle);

            if (parametre == null)
            {
                parametre = new ParametreGlobal
                {
                    Cle = cle,
                    Categorie = categorie,
                    Description = description
                };
                _context.ParametresGlobaux.Add(parametre);
            }

            parametre.Valeur = valeur;
            parametre.DateModification = DateTime.Now;

            await _context.SaveChangesAsync();

            // Mettre à jour le cache
            _cacheParametres[cle] = valeur;
        }

        public async Task<Dictionary<string, string>> ObtenirParametresCategorie(string categorie)
        {
            var parametres = await _context.ParametresGlobaux
                .Where(p => p.Categorie == categorie)
                .ToDictionaryAsync(p => p.Cle, p => p.Valeur);

            return parametres;
        }

        public async Task<List<JourFerie>> ObtenirJoursFeries(int annee)
        {
            return await _context.JoursFeries
                .Where(j => j.Date.Year == annee && j.Actif)
                .OrderBy(j => j.Date)
                .ToListAsync();
        }

        public async Task<bool> EstJourFerie(DateTime date)
        {
            return await _context.JoursFeries
                .AnyAsync(j => j.Date.Date == date.Date && j.Actif);
        }

        public async Task<List<DayOfWeek>> ObtenirJoursOuvres()
        {
            var joursOuvresString = await ObtenirParametre("JoursOuvres", "1,2,3,4,5");
            var joursOuvresInt = joursOuvresString!.Split(',').Select(int.Parse);

            return joursOuvresInt.Select(j => (DayOfWeek)(j == 7 ? 0 : j)).ToList();
        }

        public async Task<int> CalculerJoursOuvres(DateTime dateDebut, DateTime dateFin, bool exclureFeries = true)
        {
            var joursOuvres = await ObtenirJoursOuvres();
            var joursFeries = exclureFeries ?
                await ObtenirJoursFeries(dateDebut.Year) :
                new List<JourFerie>();

            var count = 0;
            var date = dateDebut.Date;

            while (date <= dateFin.Date)
            {
                // Vérifier si c'est un jour ouvré
                if (joursOuvres.Contains(date.DayOfWeek))
                {
                    // Vérifier si ce n'est pas un jour férié
                    if (!joursFeries.Any(jf => jf.Date.Date == date))
                    {
                        count++;
                    }
                }
                date = date.AddDays(1);
            }

            return count;
        }

        // ✅ NOUVELLES MÉTHODES POUR LA VALIDATION

        public async Task<bool> ValiderPreavisRespected(DateTime dateDebut, int? preavisPersonnalise = null)
        {
            var preavisMinimum = preavisPersonnalise ?? await ObtenirParametre<int>("PreavisMinimum", 14);
            var joursAvance = (dateDebut.Date - DateTime.Today).Days;

            return joursAvance >= preavisMinimum;
        }

        public async Task<bool> ValiderAnticipationRespectee(DateTime dateDebut, int? anticipationPersonnalisee = null)
        {
            var anticipationMaximum = anticipationPersonnalisee ?? await ObtenirParametre<int>("AnticipationMaximum", 365);
            var joursAvance = (dateDebut.Date - DateTime.Today).Days;

            return joursAvance <= anticipationMaximum;
        }

        public async Task<ValidationResult> ValiderDemandeCongeComplete(DemandeConge demande)
        {
            var result = new ValidationResult();

            // 1. Validation préavis
            var preavisRespected = await ValiderPreavisRespected(demande.DateDebut);
            if (!preavisRespected)
            {
                var preavisMin = await ObtenirParametre<int>("PreavisMinimum", 14);
                result.Erreurs.Add($"Le préavis minimum de {preavisMin} jours n'est pas respecté.");
            }

            // 2. Validation anticipation
            var anticipationRespectee = await ValiderAnticipationRespectee(demande.DateDebut);
            if (!anticipationRespectee)
            {
                var anticipationMax = await ObtenirParametre<int>("AnticipationMaximum", 365);
                result.Erreurs.Add($"L'anticipation maximale de {anticipationMax} jours est dépassée.");
            }

            // 3. Validation période
            if (demande.DateFin < demande.DateDebut)
            {
                result.Erreurs.Add("La date de fin doit être après la date de début.");
            }

            // 4. Validation jours ouvrés
            var joursOuvresCalcules = await CalculerJoursOuvres(demande.DateDebut, demande.DateFin);
            if (joursOuvresCalcules == 0)
            {
                result.Avertissements.Add("Cette période ne contient aucun jour ouvré.");
            }

            // 5. Validation règles spécifiques au type
            var reglesType = await _context.ReglesTypesAbsences
                .FirstOrDefaultAsync(r => r.TypeAbsenceId == demande.TypeAbsenceId);

            if (reglesType != null)
            {
                // Vérifier maximum consécutif
                if (reglesType.MaximumConsecutif.HasValue && demande.NombreJours > reglesType.MaximumConsecutif.Value)
                {
                    result.Erreurs.Add($"Le maximum de {reglesType.MaximumConsecutif} jours consécutifs est dépassé pour ce type d'absence.");
                }

                // Vérifier préavis spécifique
                if (reglesType.PreavisMinimum.HasValue)
                {
                    var preavisRespectedSpecifique = await ValiderPreavisRespected(demande.DateDebut, reglesType.PreavisMinimum.Value);
                    if (!preavisRespectedSpecifique)
                    {
                        result.Erreurs.Add($"Le préavis minimum de {reglesType.PreavisMinimum} jours pour ce type d'absence n'est pas respecté.");
                    }
                }

                // Vérifier maximum par an (nécessite calcul sur l'année)
                if (reglesType.MaximumParAn.HasValue)
                {
                    var debutAnnee = new DateTime(demande.DateDebut.Year, 1, 1);
                    var finAnnee = new DateTime(demande.DateDebut.Year, 12, 31);

                    var joursDejaUtilises = await _context.DemandesConges
                        .Where(d => d.UtilisateurId == demande.UtilisateurId &&
                                   d.TypeAbsenceId == demande.TypeAbsenceId &&
                                   d.DateDebut >= debutAnnee &&
                                   d.DateFin <= finAnnee &&
                                   d.Statut == Core.Enums.StatusDemande.Approuve &&
                                   d.Id != demande.Id)
                        .SumAsync(d => d.NombreJours);

                    if (joursDejaUtilises + demande.NombreJours > reglesType.MaximumParAn.Value)
                    {
                        result.Erreurs.Add($"Le maximum de {reglesType.MaximumParAn} jours par an pour ce type d'absence serait dépassé ({joursDejaUtilises} déjà utilisés).");
                    }
                }
            }

            result.EstValide = !result.Erreurs.Any();
            return result;
        }

        public async Task<List<RegleTypeAbsence>> ObtenirReglesTypesAbsences()
        {
            return await _context.ReglesTypesAbsences
                .Include(r => r.TypeAbsence)
                .ToListAsync();
        }

        public async Task SauvegarderRegleTypeAbsence(RegleTypeAbsence regle)
        {
            var existante = await _context.ReglesTypesAbsences
                .FirstOrDefaultAsync(r => r.TypeAbsenceId == regle.TypeAbsenceId);

            if (existante == null)
            {
                regle.DateCreation = DateTime.Now;
                _context.ReglesTypesAbsences.Add(regle);
            }
            else
            {
                existante.MaximumParAn = regle.MaximumParAn;
                existante.MaximumConsecutif = regle.MaximumConsecutif;
                existante.PreavisMinimum = regle.PreavisMinimum;
                existante.AnticipationMaximum = regle.AnticipationMaximum;
                existante.NecessiteJustification = regle.NecessiteJustification;
                existante.ReglesPersonnalisees = regle.ReglesPersonnalisees;
                existante.DateModification = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        public void ViderCache()
        {
            _cacheParametres.Clear();
        }
    }

    // ✅ CLASSE POUR LES RÉSULTATS DE VALIDATION
    public class ValidationResult
    {
        public bool EstValide { get; set; } = true;
        public List<string> Erreurs { get; set; } = new();
        public List<string> Avertissements { get; set; } = new();

        public string MessageComplet => string.Join("\n", Erreurs.Concat(Avertissements));
    }
}