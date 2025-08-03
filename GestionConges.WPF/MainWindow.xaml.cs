using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;

namespace GestionConges.WPF
{
    public partial class MainWindow : Window
    {
        private readonly GestionCongesContext _context;

        public MainWindow()
        {
            InitializeComponent();
            _context = App.GetService<GestionCongesContext>();
        }

        private async void BtnTestConnexion_Click(object sender, RoutedEventArgs e)
        {
            TxtStatut.Text = "Test de connexion en cours...";

            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var result = new StringBuilder();
                result.AppendLine("=== TEST DE CONNEXION ===");
                result.AppendLine($"✅ Connexion: {(canConnect ? "RÉUSSIE" : "ÉCHEC")}");
                result.AppendLine($"📍 Base: {_context.Database.GetConnectionString()}");
                result.AppendLine($"🕒 Date/Heure: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

                if (canConnect)
                {
                    var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                    var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();

                    result.AppendLine($"📊 Migrations appliquées: {appliedMigrations.Count()}");
                    result.AppendLine($"⏳ Migrations en attente: {pendingMigrations.Count()}");

                    if (pendingMigrations.Any())
                    {
                        result.AppendLine("⚠️ ATTENTION: Migrations en attente!");
                        foreach (var migration in pendingMigrations)
                        {
                            result.AppendLine($"   - {migration}");
                        }
                    }
                }

                TxtResultats.Text = result.ToString();
                TxtStatut.Text = canConnect ? "✅ Connexion réussie" : "❌ Échec connexion";
            }
            catch (Exception ex)
            {
                TxtResultats.Text = $"❌ ERREUR DE CONNEXION:\n\n{ex.Message}\n\nDétails:\n{ex}";
                TxtStatut.Text = "❌ Erreur de connexion";
            }
        }

        private async void BtnAfficherUtilisateurs_Click(object sender, RoutedEventArgs e)
        {
            TxtStatut.Text = "Chargement des utilisateurs...";

            try
            {
                var utilisateurs = await _context.Utilisateurs
                    .Include(u => u.Pole)
                    .ToListAsync();

                var result = new StringBuilder();
                result.AppendLine("=== UTILISATEURS ===");
                result.AppendLine($"📊 Total: {utilisateurs.Count} utilisateur(s)");
                result.AppendLine();

                foreach (var user in utilisateurs)
                {
                    result.AppendLine($"👤 {user.NomComplet}");
                    result.AppendLine($"   📧 {user.Email}");
                    result.AppendLine($"   🏷️ {user.RoleLibelle}");
                    result.AppendLine($"   🏢 {user.Pole?.Nom ?? "Aucun pôle"}");
                    result.AppendLine($"   ✅ {(user.Actif ? "Actif" : "Inactif")}");
                    result.AppendLine();
                }

                TxtResultats.Text = result.ToString();
                TxtStatut.Text = $"✅ {utilisateurs.Count} utilisateur(s) chargé(s)";
            }
            catch (Exception ex)
            {
                TxtResultats.Text = $"❌ ERREUR:\n\n{ex.Message}";
                TxtStatut.Text = "❌ Erreur chargement utilisateurs";
            }
        }

        private async void BtnAfficherPoles_Click(object sender, RoutedEventArgs e)
        {
            TxtStatut.Text = "Chargement des pôles...";

            try
            {
                var poles = await _context.Poles
                    .Include(p => p.Chef)
                    .Include(p => p.Employes)
                    .ToListAsync();

                var result = new StringBuilder();
                result.AppendLine("=== PÔLES ===");
                result.AppendLine($"📊 Total: {poles.Count} pôle(s)");
                result.AppendLine();

                foreach (var pole in poles)
                {
                    result.AppendLine($"🏢 {pole.Nom}");
                    result.AppendLine($"   📝 {pole.Description ?? "Pas de description"}");
                    result.AppendLine($"   👨‍💼 Chef: {pole.Chef?.NomComplet ?? "Aucun chef"}");
                    result.AppendLine($"   👥 Employés: {pole.Employes.Count}");
                    result.AppendLine($"   ✅ {(pole.Actif ? "Actif" : "Inactif")}");
                    result.AppendLine();
                }

                TxtResultats.Text = result.ToString();
                TxtStatut.Text = $"✅ {poles.Count} pôle(s) chargé(s)";
            }
            catch (Exception ex)
            {
                TxtResultats.Text = $"❌ ERREUR:\n\n{ex.Message}";
                TxtStatut.Text = "❌ Erreur chargement pôles";
            }
        }

        private async void BtnAfficherTypesAbsences_Click(object sender, RoutedEventArgs e)
        {
            TxtStatut.Text = "Chargement des types d'absences...";

            try
            {
                var types = await _context.TypesAbsences
                    .OrderBy(t => t.OrdreAffichage)
                    .ToListAsync();

                var result = new StringBuilder();
                result.AppendLine("=== TYPES D'ABSENCES ===");
                result.AppendLine($"📊 Total: {types.Count} type(s)");
                result.AppendLine();

                foreach (var type in types)
                {
                    result.AppendLine($"📝 {type.Nom}");
                    result.AppendLine($"   🎨 Couleur: {type.CouleurHex}");
                    result.AppendLine($"   📋 Description: {type.Description ?? "Aucune"}");
                    result.AppendLine($"   ⚡ Validation requise: {(type.NecessiteValidation ? "Oui" : "Non")}");
                    result.AppendLine($"   ✅ {(type.Actif ? "Actif" : "Inactif")}");
                    result.AppendLine();
                }

                TxtResultats.Text = result.ToString();
                TxtStatut.Text = $"✅ {types.Count} type(s) chargé(s)";
            }
            catch (Exception ex)
            {
                TxtResultats.Text = $"❌ ERREUR:\n\n{ex.Message}";
                TxtStatut.Text = "❌ Erreur chargement types";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}
