using System.Windows;
using System.Windows.Input;
using GestionConges.Core.Models;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using Microsoft.Extensions.Configuration;

namespace GestionConges.WPF.Views
{
    public partial class LoginWindow : Window
    {
        public string TitreEntreprise { get; set; } = "";
        private bool _isFirstSetup = false;

        public LoginWindow()
        {
            InitializeComponent();

            // Récupérer le nom de l'entreprise depuis appsettings.json
            var configuration = App.GetService<IConfiguration>();
            var nomEntreprise = configuration["AppSettings:NomEntreprise"];
            TitreEntreprise = string.IsNullOrEmpty(nomEntreprise) ? "Gestion Congés" : nomEntreprise;

            this.DataContext = this;

            // Vérifier si c'est la première installation
            CheckFirstSetup();

            // Focus sur le champ login au démarrage
            Loaded += (s, e) => TxtLogin.Focus();
        }

        private async void CheckFirstSetup()
        {
            try
            {
                var context = App.GetService<GestionCongesContext>();
                var hasUsers = await context.Utilisateurs.AnyAsync();

                if (!hasUsers)
                {
                    _isFirstSetup = true;
                    ConfigurerModeInscription();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la vérification de la base de données : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigurerModeInscription()
        {
            Title = "Première Installation - JungLogistique";

            // Modifier les textes avec les noms définis
            TxtTitreFormulaire.Text = "Configuration Initiale";
            TxtSousTitreFormulaire.Text = "Créez le compte administrateur principal";
            BtnLogin.Content = "CRÉER LE COMPTE ADMINISTRATEUR";

            // Changer les tooltips
            TxtLogin.ToolTip = "Choisissez votre nom d'utilisateur administrateur";
            TxtPassword.ToolTip = "Créez un mot de passe sécurisé (min. 8 caractères)";
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (_isFirstSetup)
            {
                await CreerPremierAdministrateur();
            }
            else
            {
                await TentativeConnexion();
            }
        }

        private async Task CreerPremierAdministrateur()
        {
            try
            {
                // Validation renforcée pour le premier admin
                if (string.IsNullOrWhiteSpace(TxtLogin.Text))
                {
                    AfficherErreur("Veuillez saisir un nom d'utilisateur.");
                    TxtLogin.Focus();
                    return;
                }

                if (TxtLogin.Text.Length < 3)
                {
                    AfficherErreur("Le nom d'utilisateur doit contenir au moins 3 caractères.");
                    TxtLogin.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtPassword.Password))
                {
                    AfficherErreur("Veuillez créer un mot de passe.");
                    TxtPassword.Focus();
                    return;
                }

                if (TxtPassword.Password.Length < 8)
                {
                    AfficherErreur("Le mot de passe doit contenir au moins 8 caractères.");
                    TxtPassword.Focus();
                    return;
                }

                // Demander des informations supplémentaires
                var detailsWindow = new PremierAdminWindow(TxtLogin.Text, TxtPassword.Password);
                var result = detailsWindow.ShowDialog();

                if (result == true && detailsWindow.AdminCree != null)
                {
                    App.UtilisateurConnecte = detailsWindow.AdminCree;
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                AfficherErreur($"Erreur lors de la création du compte : {ex.Message}");
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
                if (_isFirstSetup)
                {
                    await CreerPremierAdministrateur();
                }
                else
                {
                    await TentativeConnexion();
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
    }
}