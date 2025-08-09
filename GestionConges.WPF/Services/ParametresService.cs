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

        public void ViderCache()
        {
            _cacheParametres.Clear();
        }
    }
}