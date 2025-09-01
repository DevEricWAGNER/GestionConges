using GestionConges.Core.Enums;
using GestionConges.WPF.Controls;
using GestionConges.WPF.Views;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GestionConges.WPF
{
    public partial class MainWindow : Window
    {
        private Button? _activeNavButton;
        private DashboardUserControl? _dashboardControl;

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
            TxtUtilisateurRole.Text = $"{utilisateur.RoleLibelle}";

            // Configuration des droits d'accès
            ConfigurerMenuSelonDroits(utilisateur.Role);

            // Sélection de l'onglet accueil par défaut et chargement du dashboard
            SelectionnerOnglet(BtnAccueil);
            ChargerDashboard();

            // Mise à jour du statut rapide
            MettreAJourStatutRapide();
        }

        private void ConfigurerMenuSelonDroits(RoleUtilisateur role)
        {
            // Les chefs de pôle et admin voient les validations
            if (role == RoleUtilisateur.Validateur || role == RoleUtilisateur.Admin)
            {
                BtnValidation.Visibility = Visibility.Visible;
            }

            // Seul l'admin voit l'administration
            if (role == RoleUtilisateur.Admin)
            {
                BtnAdmin.Visibility = Visibility.Visible;
                TxtSectionAdmin.Visibility = Visibility.Visible;
            }
        }

        private void SelectionnerOnglet(Button boutonActif)
        {
            // Reset de tous les boutons navigation
            var boutons = new[] { BtnAccueil, BtnMesConges, BtnCalendrier, BtnValidation, BtnAdmin };

            foreach (var btn in boutons)
            {
                btn.Style = (Style)FindResource("NavButton");
            }

            // Activation du bouton sélectionné
            boutonActif.Style = (Style)FindResource("NavButtonActive");
            _activeNavButton = boutonActif;
        }

        private void MettreAJourStatutRapide()
        {
            // Le dashboard se chargera de mettre à jour ces informations
            if (_dashboardControl != null)
            {
                _dashboardControl.Rafraichir();
            }
        }

        private void ChargerDashboard()
        {
            ContentArea.Children.Clear();

            // Créer le dashboard dynamique
            _dashboardControl = new DashboardUserControl();

            // Connecter les événements de navigation
            _dashboardControl.NaviguerVersNouvelleDemandeRequested += (s, e) => BtnNouvelleDemandeRaccourci_Click(s, new RoutedEventArgs());
            _dashboardControl.NaviguerVersCalendrierRequested += (s, e) => BtnCalendrier_Click(s, new RoutedEventArgs());
            _dashboardControl.NaviguerVersMesCongesRequested += (s, e) => BtnMesConges_Click(s, new RoutedEventArgs());

            ContentArea.Children.Add(_dashboardControl);

            // Mettre à jour le statut rapide basé sur les données du dashboard
            MettreAJourStatutRapideDepuisDashboard();
        }

        private async void MettreAJourStatutRapideDepuisDashboard()
        {
            try
            {
                // Calculer le statut rapide depuis la base de données
                using var context = App.GetService<GestionConges.Core.Data.GestionCongesContext>();
                var utilisateurId = App.UtilisateurConnecte?.Id ?? 0;

                var demandesEnAttente = await context.DemandesConges
                    .Where(d => d.UtilisateurId == utilisateurId && d.EstEnAttente)
                    .CountAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    TxtStatutRapide.Text = demandesEnAttente > 0
                        ? $"{demandesEnAttente} demande{(demandesEnAttente > 1 ? "s" : "")} en attente"
                        : "0 demandes en attente";
                });
            }
            catch
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    TxtStatutRapide.Text = "0 demandes en attente";
                });
            }
        }

        #region Window Controls

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                BtnMaximize_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

            BtnMaximize.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Navigation

        private void BtnAccueil_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnAccueil);
            ChargerDashboard();
        }

        private void BtnMesConges_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnMesConges);

            ContentArea.Children.Clear();

            try
            {
                var mesDemandesControl = new Controls.MesDemandesUserControl();
                ContentArea.Children.Add(mesDemandesControl);

                // Rafraîchir le dashboard si on revient à l'accueil plus tard
                RefreshDashboardIfNeeded();
            }
            catch (Exception ex)
            {
                AfficherMessageErreur($"Erreur lors du chargement de vos demandes : {ex.Message}");
            }
        }

        private void BtnCalendrier_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnCalendrier);

            ContentArea.Children.Clear();

            try
            {
                var calendrierControl = new Controls.CalendrierControl();
                ContentArea.Children.Add(calendrierControl);

                RefreshDashboardIfNeeded();
            }
            catch (Exception ex)
            {
                AfficherMessageErreur($"Erreur lors du chargement du calendrier : {ex.Message}");
            }
        }

        private void BtnValidation_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnValidation);

            ContentArea.Children.Clear();

            try
            {
                var validationsControl = new Controls.ValidationsUserControl();
                ContentArea.Children.Add(validationsControl);

                RefreshDashboardIfNeeded();
            }
            catch (Exception ex)
            {
                AfficherMessageErreur($"Erreur lors du chargement des validations : {ex.Message}");
            }
        }

        private void BtnAdmin_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnAdmin);

            ContentArea.Children.Clear();
            var adminPanel = CreerPanneauAdmin();
            ContentArea.Children.Add(adminPanel);
        }

        #endregion

        #region Actions

        private void BtnNouvelleDemandeRaccourci_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var nouvelleDemandeWindow = new Views.NouvelleDemandeWindow();
                var result = nouvelleDemandeWindow.ShowDialog();

                if (result == true && nouvelleDemandeWindow.DemandeCreee)
                {
                    // Rafraîchir le dashboard et le statut rapide
                    _dashboardControl?.Rafraichir();
                    MettreAJourStatutRapideDepuisDashboard();

                    // Afficher notification de succès moderne
                    AfficherNotificationSucces("Demande créée avec succès !");
                }
            }
            catch (Exception ex)
            {
                AfficherMessageErreur($"Erreur lors de l'ouverture du formulaire de demande : {ex.Message}");
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
                AfficherMessageErreur($"Erreur lors de l'ouverture du profil : {ex.Message}");
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
                Application.Current.Shutdown();
                System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!);
            }
        }

        #endregion

        #region UI Helpers

        private void RefreshDashboardIfNeeded()
        {
            // Si le dashboard existe, le rafraîchir pour avoir les données à jour
            // quand l'utilisateur reviendra à l'accueil
            if (_dashboardControl != null)
            {
                // Utiliser un délai pour éviter de rafraîchir trop souvent
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    _dashboardControl?.Rafraichir();
                };
                timer.Start();
            }
        }

        private StackPanel CreerPanneauAdmin()
        {
            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(40)
            };

            // Titre
            var titre = new TextBlock
            {
                Text = "Administration",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("TextPrimary"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stackPanel.Children.Add(titre);

            // Description
            var description = new TextBlock
            {
                Text = "Choisissez une section à administrer",
                FontSize = 16,
                Foreground = (Brush)FindResource("TextSecondary"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 32)
            };
            stackPanel.Children.Add(description);

            // Grid des boutons
            var grid = new Grid();
            for (int i = 0; i < 3; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int i = 0; i < 3; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
            }

            // Bouton Utilisateurs
            var btnUtilisateurs = CreerBoutonAdmin("👥", "Gestion des\nUtilisateurs", (Brush)FindResource("Primary"));
            btnUtilisateurs.Click += (s, e) =>
            {
                try
                {
                    var gestionUtilisateursWindow = new Views.GestionUtilisateursWindow();
                    gestionUtilisateursWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    AfficherMessageErreur($"Erreur lors de l'ouverture de la gestion des utilisateurs : {ex.Message}");
                }
            };
            Grid.SetColumn(btnUtilisateurs, 0);
            Grid.SetRow(btnUtilisateurs, 0);
            grid.Children.Add(btnUtilisateurs);

            // Bouton Types d'Absences
            var btnTypes = CreerBoutonAdmin("🏷️", "Types\nd'Absences", (Brush)FindResource("Secondary"));
            btnTypes.Click += (s, e) =>
            {
                try
                {
                    var gestionTypesWindow = new Views.GestionTypesAbsencesWindow();
                    gestionTypesWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    AfficherMessageErreur($"Erreur lors de l'ouverture de la gestion des types d'absences : {ex.Message}");
                }
            };
            Grid.SetColumn(btnTypes, 1);
            Grid.SetRow(btnTypes, 0);
            grid.Children.Add(btnTypes);

            // Bouton Societe
            var btnSociete = CreerBoutonAdmin("🏢", "Gestion des\nSociétés", (Brush)FindResource("Warning"));
            btnSociete.Click += (s, e) =>
            {
                try
                {
                    var gestionSocietesWindow = new Views.GestionSocietesWindow();
                    gestionSocietesWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    AfficherMessageErreur($"Erreur lors de l'ouverture de la gestion des sociétés : {ex.Message}");
                }
            };
            Grid.SetColumn(btnSociete, 0);
            Grid.SetRow(btnSociete, 1);
            grid.Children.Add(btnSociete);

            // Bouton Equipes
            var btnEquipes = CreerBoutonAdmin("🏢", "Gestion des\nEquipes", (Brush)FindResource("Warning"));
            btnEquipes.Click += (s, e) =>
            {
                try
                {
                    var gestionEquipesWindow = new Views.GestionEquipesWindow();
                    gestionEquipesWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    AfficherMessageErreur($"Erreur lors de l'ouverture de la gestion des équipes : {ex.Message}");
                }
            };
            Grid.SetColumn(btnEquipes, 1);
            Grid.SetRow(btnEquipes, 1);
            grid.Children.Add(btnEquipes);

            // Bouton Pôles
            var btnPoles = CreerBoutonAdmin("🏢", "Gestion des\nPôles", (Brush)FindResource("Warning"));
            btnPoles.Click += (s, e) =>
            {
                try
                {
                    var gestionPolesWindow = new Views.GestionPolesWindow();
                    gestionPolesWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    AfficherMessageErreur($"Erreur lors de l'ouverture de la gestion des pôles : {ex.Message}");
                }
            };
            Grid.SetColumn(btnPoles, 2);
            Grid.SetRow(btnPoles, 1);
            grid.Children.Add(btnPoles);

            // Bouton Paramètres
            var btnParametres = CreerBoutonAdmin("⚙️", "Paramètres\nGlobaux", (Brush)FindResource("Danger"));
            btnParametres.Click += (s, e) =>
            {
                try
                {
                    var parametresWindow = new Views.ParametresGlobauxWindow();
                    parametresWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    AfficherMessageErreur($"Erreur lors de l'ouverture des paramètres : {ex.Message}");
                }
            };
            Grid.SetColumn(btnParametres, 2);
            Grid.SetRow(btnParametres, 0);
            grid.Children.Add(btnParametres);

            // Bouton Rapports (désactivé)
            var btnRapports = CreerBoutonAdmin("📊", "Rapports &\nStatistiques", (Brush)FindResource("Gray400"));
            btnRapports.IsEnabled = false;
            btnRapports.ToolTip = "Prochainement disponible";
            Grid.SetColumn(btnRapports, 0);
            Grid.SetRow(btnRapports, 2);
            grid.Children.Add(btnRapports);

            // Bouton Sauvegarde (désactivé)
            var btnSauvegarde = CreerBoutonAdmin("💾", "Sauvegarde &\nExport", (Brush)FindResource("Gray400"));
            btnSauvegarde.IsEnabled = false;
            btnSauvegarde.ToolTip = "Prochainement disponible";
            Grid.SetColumn(btnSauvegarde, 1);
            Grid.SetRow(btnSauvegarde, 2);
            grid.Children.Add(btnSauvegarde);

            stackPanel.Children.Add(grid);

            return stackPanel;
        }

        private Button CreerBoutonAdmin(string icone, string texte, Brush couleur)
        {
            var button = new Button
            {
                Width = 160,
                Height = 120,
                Margin = new Thickness(12),
                Background = couleur,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            // Template personnalisé pour le bouton admin
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(12));
            border.SetValue(Border.PaddingProperty, new Thickness(16));

            var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
            stackPanel.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            stackPanel.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);

            var iconeText = new FrameworkElementFactory(typeof(TextBlock));
            iconeText.SetValue(TextBlock.TextProperty, icone);
            iconeText.SetValue(TextBlock.FontSizeProperty, 32.0);
            iconeText.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            iconeText.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 0, 8));

            var texteText = new FrameworkElementFactory(typeof(TextBlock));
            texteText.SetValue(TextBlock.TextProperty, texte);
            texteText.SetValue(TextBlock.FontSizeProperty, 13.0);
            texteText.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            texteText.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            texteText.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);

            stackPanel.AppendChild(iconeText);
            stackPanel.AppendChild(texteText);
            border.AppendChild(stackPanel);
            template.VisualTree = border;

            // Triggers pour les effets hover
            var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Button.OpacityProperty, 0.9));
            template.Triggers.Add(trigger);

            var pressTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressTrigger.Setters.Add(new Setter(Button.OpacityProperty, 0.8));
            template.Triggers.Add(pressTrigger);

            button.Template = template;

            return button;
        }

        private void AfficherMessageErreur(string message)
        {
            ContentArea.Children.Clear();

            var border = new Border
            {
                Style = (Style)FindResource("Card"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 500
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Icône d'erreur
            var icone = new TextBlock
            {
                Text = "⚠️",
                FontSize = 48,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 16)
            };

            // Message d'erreur
            var texteErreur = new TextBlock
            {
                Text = message,
                FontSize = 16,
                Foreground = (Brush)FindResource("Danger"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 24)
            };

            // Bouton retour
            var boutonRetour = new Button
            {
                Content = "Retour à l'accueil",
                Style = (Style)FindResource("MaterialDesignRaisedButton"),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            boutonRetour.Click += (s, e) => BtnAccueil_Click(s, e);

            stackPanel.Children.Add(icone);
            stackPanel.Children.Add(texteErreur);
            stackPanel.Children.Add(boutonRetour);

            border.Child = stackPanel;
            ContentArea.Children.Add(border);
        }

        private void AfficherNotificationSucces(string message)
        {
            // Création d'une notification toast moderne
            var notification = new Border
            {
                Background = (Brush)FindResource("Secondary"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 20, 20, 0),
                Opacity = 0
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var icone = new TextBlock
            {
                Text = "✓",
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var texte = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(icone);
            stackPanel.Children.Add(texte);
            notification.Child = stackPanel;

            var mainBorder = (Border)this.Content;
            var mainGrid = (Grid)mainBorder.Child;
            mainGrid.Children.Add(notification);

            // Animation d'apparition et disparition
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
            {
                BeginTime = TimeSpan.FromSeconds(3)
            };

            fadeOut.Completed += (s, e) => mainGrid.Children.Remove(notification);

            notification.BeginAnimation(OpacityProperty, fadeIn);
            notification.BeginAnimation(OpacityProperty, fadeOut);
        }

        #endregion
    }
}