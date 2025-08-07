using System.Windows;
using System.Windows.Controls;
using GestionConges.Core.Enums;
using GestionConges.WPF.Views;

namespace GestionConges.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitialiserInterface();
        }

        private void InitialiserInterface()
        {
            var utilisateur = App.UtilisateurConnecte;

            if (utilisateur == null)
            {
                MessageBox.Show("Erreur: Aucun utilisateur connecté", "Erreur");
                Close();
                return;
            }

            // Affichage des infos utilisateur
            TxtUtilisateurNom.Text = utilisateur.NomComplet;
            TxtUtilisateurRole.Text = $"{utilisateur.RoleLibelle} - {utilisateur.Pole?.Nom ?? "Équipe Projets"}";
            TxtBienvenue.Text = $"Connecté en tant que {utilisateur.NomComplet}";

            // Affichage des boutons selon les droits
            ConfigurerMenuSelonDroits(utilisateur.Role);

            // Sélection de l'onglet accueil par défaut
            SelectionnerOnglet(BtnAccueil);
        }

        private void ConfigurerMenuSelonDroits(RoleUtilisateur role)
        {
            // Tous les utilisateurs voient : Accueil, Mes Congés, Calendrier
            BtnAccueil.Visibility = Visibility.Visible;
            BtnMesConges.Visibility = Visibility.Visible;
            BtnCalendrier.Visibility = Visibility.Visible;

            // Les chefs de pôle et chef d'équipe voient les validations
            if (role == RoleUtilisateur.ChefPole || role == RoleUtilisateur.ChefEquipe)
            {
                BtnValidation.Visibility = Visibility.Visible;
            }

            // Seul le chef d'équipe voit l'administration
            if (role == RoleUtilisateur.ChefEquipe)
            {
                BtnAdmin.Visibility = Visibility.Visible;
            }
        }

        private void SelectionnerOnglet(Button boutonActif)
        {
            // Reset de tous les boutons - couleurs directes
            var boutons = new[] { BtnAccueil, BtnMesConges, BtnCalendrier, BtnValidation, BtnAdmin };

            foreach (var btn in boutons)
            {
                btn.Background = System.Windows.Media.Brushes.Transparent;
                btn.Foreground = System.Windows.Media.Brushes.Blue;
                btn.BorderBrush = System.Windows.Media.Brushes.Blue;
                btn.BorderThickness = new Thickness(1);
            }

            // Activation du bouton sélectionné
            boutonActif.Background = System.Windows.Media.Brushes.Blue;
            boutonActif.Foreground = System.Windows.Media.Brushes.White;
            boutonActif.BorderThickness = new Thickness(0);
        }

        // ===============================================
        // Gestionnaires d'événements du menu
        // ===============================================

        private void BtnAccueil_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnAccueil);
            TxtStatut.Text = "Accueil";
        }

        private void BtnMesConges_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnMesConges);
            TxtStatut.Text = "Mes congés";
            AfficherMessageTemporaire("📅 Vue 'Mes Congés' - En cours de développement");
        }

        private void BtnCalendrier_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnCalendrier);
            TxtStatut.Text = "Calendrier équipe";

            // Nettoyer la zone de contenu
            ContentArea.Children.Clear();

            try
            {
                // Créer et ajouter le contrôle calendrier
                var calendrierControl = new Controls.CalendrierControl();
                ContentArea.Children.Add(calendrierControl);
            }
            catch (Exception ex)
            {
                // Fallback en cas d'erreur
                AfficherMessageTemporaire($"❌ Erreur lors du chargement du calendrier : {ex.Message}");
            }
        }

        private void BtnValidation_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnValidation);
            TxtStatut.Text = "Validations";
            AfficherMessageTemporaire("✅ Vue 'Validations' - En cours de développement");
        }

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnAdmin);
            TxtStatut.Text = "Administration";

            // Ouvrir la fenêtre de gestion des utilisateurs
            try
            {
                var gestionUtilisateursWindow = new Views.GestionUtilisateursWindow();
                gestionUtilisateursWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la gestion des utilisateurs : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnNouvelleDemandeRaccourci_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Fonctionnalité 'Nouvelle demande' en cours de développement !",
                          "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnMonProfil_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var monProfilWindow = new Views.MonProfilWindow();
                monProfilWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du profil : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeconnexion_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Êtes-vous sûr de vouloir vous déconnecter ?",
                                       "Déconnexion",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                App.UtilisateurConnecte = null;

                // Fermer et relancer
                Application.Current.Shutdown();
                System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!);
            }
        }

        // ===============================================
        // Méthodes utilitaires
        // ===============================================

        private void AfficherMessageTemporaire(string message)
        {
            ContentArea.Children.Clear();

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20)
            };

            var boutonRetour = new Button
            {
                Content = "🏠 Retour à l'accueil",
                Margin = new Thickness(20),
                Padding = new Thickness(20, 10, 20, 10),
                Background = System.Windows.Media.Brushes.Blue,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            boutonRetour.Click += (s, e) => BtnAccueil_Click(s, e);

            stackPanel.Children.Add(textBlock);
            stackPanel.Children.Add(boutonRetour);

            ContentArea.Children.Add(stackPanel);
        }
    }
}