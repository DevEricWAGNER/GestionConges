using GestionConges.Core.Models;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json;

namespace GestionConges.WPF.Services
{
    public static class PaysService
    {
        private static List<Pays>? _paysCache;
        private static readonly object _lock = new object();

        public static async Task<List<Pays>> GetPaysAsync()
        {
            if (_paysCache != null)
                return _paysCache;

            lock (_lock)
            {
                if (_paysCache != null)
                    return _paysCache;

                try
                {
                    // Chercher le fichier pays.json dans différents emplacements
                    var cheminsFichier = new[]
                    {
                        Path.Combine(AppContext.BaseDirectory, "Data", "pays.json"),
                        Path.Combine(AppContext.BaseDirectory, "pays.json"),
                        Path.Combine(Directory.GetCurrentDirectory(), "Data", "pays.json"),
                        Path.Combine(Directory.GetCurrentDirectory(), "pays.json"),
                        // Chemin relatif vers le projet Core
                        Path.Combine(Directory.GetCurrentDirectory(), "..", "GestionConges.Core", "Data", "pays.json"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "GestionConges.Core", "Data", "pays.json")
                    };

                    string? contenuJson = null;
                    string? cheminTrouve = null;

                    foreach (var chemin in cheminsFichier)
                    {
                        try
                        {
                            var cheminAbsolu = Path.GetFullPath(chemin);
                            if (File.Exists(cheminAbsolu))
                            {
                                contenuJson = File.ReadAllText(cheminAbsolu);
                                cheminTrouve = cheminAbsolu;
                                System.Diagnostics.Debug.WriteLine($"Fichier pays.json trouvé : {cheminTrouve}");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erreur lors de la recherche du fichier {chemin}: {ex.Message}");
                        }
                    }

                    if (string.IsNullOrEmpty(contenuJson))
                    {
                        System.Diagnostics.Debug.WriteLine("Fichier pays.json non trouvé, utilisation des données par défaut");
                        _paysCache = GetPaysParDefaut();
                        return _paysCache;
                    }

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var paysData = JsonSerializer.Deserialize<List<PaysJson>>(contenuJson, options);

                    _paysCache = paysData?.Select(p => new Pays
                    {
                        Code = p.Code ?? p.Iso2 ?? "",
                        Nom = p.Nom ?? p.Name ?? ""
                    }).Where(p => !string.IsNullOrEmpty(p.Code) && !string.IsNullOrEmpty(p.Nom))
                    .OrderBy(p => p.Nom)
                    .ToList() ?? GetPaysParDefaut();

                    System.Diagnostics.Debug.WriteLine($"Chargé {_paysCache.Count} pays depuis {cheminTrouve}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des pays : {ex.Message}");
                    _paysCache = GetPaysParDefaut();
                }

                return _paysCache;
            }
        }

        private static List<Pays> GetPaysParDefaut()
        {
            return new List<Pays>
            {
                new() { Code = "FR", Nom = "France" },
                new() { Code = "BE", Nom = "Belgique" },
                new() { Code = "CH", Nom = "Suisse" },
                new() { Code = "LU", Nom = "Luxembourg" },
                new() { Code = "DE", Nom = "Allemagne" },
                new() { Code = "IT", Nom = "Italie" },
                new() { Code = "ES", Nom = "Espagne" },
                new() { Code = "PT", Nom = "Portugal" },
                new() { Code = "NL", Nom = "Pays-Bas" },
                new() { Code = "GB", Nom = "Royaume-Uni"},
                new() { Code = "IE", Nom = "Irlande"},
                new() { Code = "AT", Nom = "Autriche"},
                new() { Code = "DK", Nom = "Danemark"},
                new() { Code = "SE", Nom = "Suède"},
                new() { Code = "NO", Nom = "Norvège"},
                new() { Code = "FI", Nom = "Finlande"},
                new() { Code = "PL", Nom = "Pologne"},
                new() { Code = "CZ", Nom = "République Tchèque"},
                new() { Code = "HU", Nom = "Hongrie"},
                new() { Code = "GR", Nom = "Grèce"},
                new() { Code = "CA", Nom = "Canada"},
                new() { Code = "US", Nom = "États-Unis"},
                new() { Code = "MA", Nom = "Maroc"},
                new() { Code = "TN", Nom = "Tunisie"},
                new() { Code = "DZ", Nom = "Algérie"},
                new() { Code = "SN", Nom = "Sénégal"}
            };
        }

        public static void ViderCache()
        {
            lock (_lock)
            {
                _paysCache = null;
            }
        }

        // Classes pour la désérialisation JSON
        private class PaysJson
        {
            public string? Code { get; set; }
            public string? Iso2 { get; set; }
            public string? Nom { get; set; }
            public string? Name { get; set; }
            public string? NomFrancais { get; set; }
            public string? NomAnglais { get; set; }
        }
    }
}