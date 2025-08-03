using System.Windows;
using System.Windows.Input;
using GestionConges.Core.Models;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;

namespace GestionConges.WPF.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Focus sur le champ login au démarrage
            Loaded += (s, e) => TxtLogin.Focus();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            await TentativeConnexion();
        }

        private void TxtLogin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TxtPassword.Focus();
            }
        }

        private async void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await TentativeConnexion();
            }
        }

        private async Task TentativeConnexion()
        {
            try
            {
                // Validation des champs
                if (string.IsNullOrWhiteSpace(TxtLogin.Text))
                {
                    AfficherErreur("Veuillez saisir votre nom d'utilisateur.");
                    TxtLogin.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtPassword.Password))
                {
                    AfficherErreur("Veuillez saisir votre mot de passe.");
                    TxtPassword.Focus();
                    return;
                }

                // Affichage du loading
                MasquerErreur();
                AfficherLoading(true);

                // Authentification directe
                var context = App.GetService<GestionCongesContext>();
                var utilisateur = await context.Utilisateurs
                    .Include(u => u.Pole)
                    .FirstOrDefaultAsync(u => u.Login == TxtLogin.Text && u.Actif);

                if (utilisateur != null && BCrypt.Net.BCrypt.Verify(TxtPassword.Password, utilisateur.MotDePasseHash))
                {
                    // Connexion réussie
                    utilisateur.DerniereConnexion = DateTime.Now;
                    await context.SaveChangesAsync();

                    // Stocker l'utilisateur connecté
                    App.UtilisateurConnecte = utilisateur;

                    DialogResult = true;
                    Close();
                }
                else
                {
                    // Échec de connexion
                    AfficherErreur("Nom d'utilisateur ou mot de passe incorrect.");
                    TxtPassword.Clear();
                    TxtLogin.Focus();
                }
            }
            catch (Exception ex)
            {
                AfficherErreur($"Erreur lors de la connexion: {ex.Message}");
            }
            finally
            {
                AfficherLoading(false);
            }
        }

        private void AfficherErreur(string message)
        {
            TxtError.Text = message;
            ErrorPanel.Visibility = Visibility.Visible;
        }

        private void MasquerErreur()
        {
            ErrorPanel.Visibility = Visibility.Collapsed;
        }

        private void AfficherLoading(bool afficher)
        {
            LoadingPanel.Visibility = afficher ? Visibility.Visible : Visibility.Collapsed;
            BtnLogin.IsEnabled = !afficher;
            TxtLogin.IsEnabled = !afficher;
            TxtPassword.IsEnabled = !afficher;
        }

        // Méthodes de debug (garder pour l'instant)
        private async void BtnDebug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var context = App.GetService<GestionCongesContext>();
                var utilisateurs = await context.Utilisateurs.ToListAsync();

                var message = $"🔍 UTILISATEURS DANS LA BASE:\n\n";
                message += $"Total: {utilisateurs.Count}\n\n";

                foreach (var user in utilisateurs)
                {
                    message += $"ID: {user.Id}\n";
                    message += $"Login: {user.Login}\n";
                    message += $"Email: {user.Email}\n";
                    message += $"Actif: {user.Actif}\n";
                    message += $"Hash: {user.MotDePasseHash.Substring(0, Math.Min(20, user.MotDePasseHash.Length))}...\n\n";
                }

                MessageBox.Show(message, "Debug Utilisateurs");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur debug: {ex.Message}", "Erreur Debug");
            }
        }

        private async void BtnCreerAdmin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var context = App.GetService<GestionCongesContext>();

                // Supprimer l'ancien admin
                var ancienAdmin = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Login == "admin");
                if (ancienAdmin != null)
                {
                    context.Utilisateurs.Remove(ancienAdmin);
                }

                // Créer nouvel admin avec bon hash
                var admin = new Utilisateur
                {
                    Nom = "Admin",
                    Prenom = "Super",
                    Email = "admin@entreprise.com",
                    Login = "admin",
                    MotDePasseHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = GestionConges.Core.Enums.RoleUtilisateur.ChefEquipe,
                    Actif = true,
                    DateCreation = DateTime.Now
                };

                context.Utilisateurs.Add(admin);
                await context.SaveChangesAsync();

                MessageBox.Show("✅ Admin recréé avec succès !\nLogin: admin\nMot de passe: admin123", "Succès");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur création admin: {ex.Message}", "Erreur");
            }
        }
    }
}