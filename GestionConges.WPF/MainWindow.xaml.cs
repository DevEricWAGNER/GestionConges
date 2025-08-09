using GestionConges.Core.Enums;
using GestionConges.WPF.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

            // Nettoyer la zone de contenu
            ContentArea.Children.Clear();

            try
            {
                // Créer et ajouter le contrôle mes demandes
                var mesDemandesControl = new Controls.MesDemandesUserControl();
                ContentArea.Children.Add(mesDemandesControl);
            }
            catch (Exception ex)
            {
                // Fallback en cas d'erreur
                AfficherMessageTemporaire($"❌ Erreur lors du chargement de vos demandes : {ex.Message}");
            }
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

            // Nettoyer la zone de contenu
            ContentArea.Children.Clear();

            try
            {
                // Créer et ajouter le contrôle validations
                var validationsControl = new Controls.ValidationsUserControl();
                ContentArea.Children.Add(validationsControl);
            }
            catch (Exception ex)
            {
                // Fallback en cas d'erreur
                AfficherMessageTemporaire($"❌ Erreur lors du chargement des validations : {ex.Message}");
            }
        }


        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnAdmin);
            TxtStatut.Text = "Administration";

            // Créer un menu d'administration avec plusieurs options
            ContentArea.Children.Clear();

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Titre
            var titre = new TextBlock
            {
                Text = "⚙️ Administration",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30)
            };
            stackPanel.Children.Add(titre);

            // Description
            var description = new TextBlock
            {
                Text = "Choisissez une section à administrer :",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30),
                Foreground = Brushes.Gray
            };
            stackPanel.Children.Add(description);

            // Boutons d'administration
            var boutonsPanel = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Orientation = Orientation.Horizontal
            };

            // Bouton Gestion Utilisateurs
            var btnUtilisateurs = new Button
            {
                Content = "👥\nGestion des Utilisateurs",
                Width = 180,
                Height = 100,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            btnUtilisateurs.Click += (s, e) =>
            {
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
            };
            boutonsPanel.Children.Add(btnUtilisateurs);

            // ✅ NOUVEAU : Bouton Gestion Types d'Absences
            var btnTypesAbsences = new Button
            {
                Content = "🏷️\nTypes d'Absences",
                Width = 180,
                Height = 100,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            btnTypesAbsences.Click += (s, e) =>
            {
                try
                {
                    var gestionTypesWindow = new Views.GestionTypesAbsencesWindow();
                    gestionTypesWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture de la gestion des types d'absences : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            boutonsPanel.Children.Add(btnTypesAbsences);

            // Bouton Gestion Pôles
            var btnPoles = new Button
            {
                Content = "🏢\nGestion des Pôles",
                Width = 180,
                Height = 100,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            btnPoles.Click += (s, e) =>
            {
                try
                {
                    var gestionPolesWindow = new Views.GestionPolesWindow();
                    gestionPolesWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture de la gestion des pôles : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            boutonsPanel.Children.Add(btnPoles);

            // 🔮 PARAMÈTRES GLOBAUX (maintenant disponible !)
            var btnParametres = new Button
            {
                Content = "⚙️\nParamètres Globaux",
                Width = 180,
                Height = 100,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                IsEnabled = true // ✅ Maintenant activé !
            };
            btnParametres.Click += (s, e) =>
            {
                try
                {
                    var parametresWindow = new Views.ParametresGlobauxWindow();
                    parametresWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture des paramètres : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            boutonsPanel.Children.Add(btnParametres);

            var btnSauvegarde = new Button
            {
                Content = "💾\nSauvegarde & Export",
                Width = 180,
                Height = 100,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(121, 85, 72)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                IsEnabled = false // Désactivé pour l'instant
            };
            btnSauvegarde.ToolTip = "Sauvegarde et export - Prochainement disponible";
            boutonsPanel.Children.Add(btnSauvegarde);

            stackPanel.Children.Add(boutonsPanel);

            // Info utilisateur admin
            var infoAdmin = new TextBlock
            {
                Text = $"👤 Connecté en tant que {App.UtilisateurConnecte?.NomComplet} (Chef d'Équipe)",
                FontSize = 12,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 30, 0, 0)
            };
            stackPanel.Children.Add(infoAdmin);

            ContentArea.Children.Add(stackPanel);
        }

        private void BtnNouvelleDemandeRaccourci_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var nouvelleDemandeWindow = new Views.NouvelleDemandeWindow();
                var result = nouvelleDemandeWindow.ShowDialog();

                if (result == true && nouvelleDemandeWindow.DemandeCreee)
                {
                    // Optionnel : rafraîchir le calendrier si il est ouvert
                    MessageBox.Show("Demande créée avec succès !", "Information",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du formulaire de demande : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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