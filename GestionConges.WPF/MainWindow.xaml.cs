using GestionConges.Core.Enums;
using GestionConges.WPF.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GestionConges.WPF
{
    public partial class MainWindow : Window
    {
        private Button? _activeNavButton;

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
            TxtBienvenue.Text = $"Bienvenue, {utilisateur.Prenom} !";

            // Configuration des droits d'accès
            ConfigurerMenuSelonDroits(utilisateur.Role);

            // Sélection de l'onglet accueil par défaut
            SelectionnerOnglet(BtnAccueil);

            // Mise à jour du statut rapide
            MettreAJourStatutRapide();
        }

        private void ConfigurerMenuSelonDroits(RoleUtilisateur role)
        {
            // Les chefs de pôle et chef d'équipe voient les validations
            if (role == RoleUtilisateur.ChefPole || role == RoleUtilisateur.ChefEquipe)
            {
                BtnValidation.Visibility = Visibility.Visible;
            }

            // Seul le chef d'équipe voit l'administration
            if (role == RoleUtilisateur.ChefEquipe)
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
            // TODO: Récupérer les vraies données depuis la base
            TxtStatutRapide.Text = "2 demandes en attente";
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

            ContentArea.Children.Clear();

            // Créer le dashboard principal
            var scrollViewer = new ScrollViewer();
            var mainStack = new StackPanel { Margin = new Thickness(20) };

            // En-tête avec titre et date/heure
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 32) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Section titre
            var titleStack = new StackPanel();
            var welcomeText = new TextBlock
            {
                Text = "Bienvenue dans votre espace congés !",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("TextPrimary"),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var subtitleText = new TextBlock
            {
                Text = "Gérez vos demandes de congés en toute simplicité",
                FontSize = 16,
                Foreground = (Brush)FindResource("TextSecondary")
            };
            titleStack.Children.Add(welcomeText);
            titleStack.Children.Add(subtitleText);
            Grid.SetColumn(titleStack, 0);

            // Section date/heure
            var dateTimeStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var dateText = new TextBlock
            {
                Text = DateTime.Now.ToString("dddd dd MMMM yyyy"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextSecondary"),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("Primary"),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            dateTimeStack.Children.Add(dateText);
            dateTimeStack.Children.Add(timeText);
            Grid.SetColumn(dateTimeStack, 1);

            headerGrid.Children.Add(titleStack);
            headerGrid.Children.Add(dateTimeStack);
            mainStack.Children.Add(headerGrid);

            // Cartes statistiques
            var statsGrid = new Grid { Margin = new Thickness(0, 0, 0, 32) };
            for (int i = 0; i < 4; i++)
            {
                statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Carte 1 - Jours restants
            var card1 = CreerCarteStatistique("💰", "25.5", "Jours restants", "Secondary", 0);
            Grid.SetColumn(card1, 0);
            statsGrid.Children.Add(card1);

            // Carte 2 - En attente
            var card2 = CreerCarteStatistique("⏳", "2", "En attente", "Warning", 8);
            Grid.SetColumn(card2, 1);
            statsGrid.Children.Add(card2);

            // Carte 3 - Jours pris
            var card3 = CreerCarteStatistique("📅", "12", "Jours pris", "Primary", 8);
            Grid.SetColumn(card3, 2);
            statsGrid.Children.Add(card3);

            // Carte 4 - Absents aujourd'hui
            var card4 = CreerCarteStatistique("👥", "3", "Absents aujourd'hui", "Danger", 8);
            Grid.SetColumn(card4, 3);
            statsGrid.Children.Add(card4);

            mainStack.Children.Add(statsGrid);

            // Section Actions Principales
            var actionsCard = new Border
            {
                Background = (Brush)FindResource("Surface"),
                BorderBrush = (Brush)FindResource("Gray200"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(24),
                Margin = new Thickness(0, 0, 0, 24)
            };

            var actionsStack = new StackPanel();
            var actionsTitle = new TextBlock
            {
                Text = "Actions Principales",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimary"),
                Margin = new Thickness(0, 0, 0, 20)
            };
            actionsStack.Children.Add(actionsTitle);

            var actionsGrid = new Grid();
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // Bouton Nouvelle Demande
            var btnNouvelle = CreerBoutonAction("📝", "Nouvelle Demande", "MaterialDesignRaisedButton", new Thickness(0, 0, 8, 0));
            btnNouvelle.Click += BtnNouvelleDemandeRaccourci_Click;
            Grid.SetColumn(btnNouvelle, 0);
            actionsGrid.Children.Add(btnNouvelle);

            // Bouton Voir Calendrier
            var btnCalendrier = CreerBoutonAction("📊", "Voir Calendrier", "MaterialDesignOutlineButton", new Thickness(4, 0, 4, 0));
            btnCalendrier.Click += BtnCalendrier_Click;
            Grid.SetColumn(btnCalendrier, 1);
            actionsGrid.Children.Add(btnCalendrier);

            // Bouton Mes Congés
            var btnMesConges = CreerBoutonAction("📅", "Mes Congés", "MaterialDesignOutlineButton", new Thickness(8, 0, 0, 0));
            btnMesConges.Click += BtnMesConges_Click;
            Grid.SetColumn(btnMesConges, 2);
            actionsGrid.Children.Add(btnMesConges);

            actionsStack.Children.Add(actionsGrid);
            actionsCard.Child = actionsStack;
            mainStack.Children.Add(actionsCard);

            // Section Activité Récente
            var activiteCard = new Border
            {
                Background = (Brush)FindResource("Surface"),
                BorderBrush = (Brush)FindResource("Gray200"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(24)
            };

            var activiteStack = new StackPanel();

            // En-tête de la section activité
            var activiteHeaderGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            var activiteTitle = new TextBlock
            {
                Text = "Activité Récente",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimary")
            };
            var btnVoirTout = new Button
            {
                Content = "Voir tout",
                Style = (Style)FindResource("MaterialDesignOutlineButton"),
                Padding = new Thickness(12, 6, 12, 6),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            btnVoirTout.Click += BtnMesConges_Click;

            activiteHeaderGrid.Children.Add(activiteTitle);
            activiteHeaderGrid.Children.Add(btnVoirTout);
            activiteStack.Children.Add(activiteHeaderGrid);

            // Liste des activités
            var activiteListStack = new StackPanel();

            // Activité 1 - Demande approuvée
            var activite1 = CreerElementActivite("Secondary", "Demande approuvée", "Congés Payés du 15/01 au 19/01 (5 jours)", "Il y a 2h");
            activiteListStack.Children.Add(activite1);

            // Activité 2 - Demande en attente
            var activite2 = CreerElementActivite("Warning", "Demande en attente", "RTT du 22/01 au 22/01 (1 jour)", "Hier");
            activiteListStack.Children.Add(activite2);

            activiteStack.Children.Add(activiteListStack);
            activiteCard.Child = activiteStack;
            mainStack.Children.Add(activiteCard);

            scrollViewer.Content = mainStack;
            ContentArea.Children.Add(scrollViewer);
        }

        private Border CreerCarteStatistique(string icone, string valeur, string libelle, string couleurResource, double marginLeft)
        {
            var card = new Border
            {
                Background = (Brush)FindResource("Surface"),
                BorderBrush = (Brush)FindResource("Gray200"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Margin = new Thickness(marginLeft, 0, 8, 0)
            };

            var stack = new StackPanel();

            var iconeText = new TextBlock
            {
                Text = icone,
                FontSize = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var valeurText = new TextBlock
            {
                Text = valeur,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource(couleurResource),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var libelleText = new TextBlock
            {
                Text = libelle,
                FontSize = 12,
                Foreground = (Brush)FindResource("TextMuted"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(iconeText);
            stack.Children.Add(valeurText);
            stack.Children.Add(libelleText);
            card.Child = stack;

            return card;
        }

        private Button CreerBoutonAction(string icone, string texte, string styleResource, Thickness margin)
        {
            var button = new Button
            {
                Style = (Style)FindResource(styleResource),
                Height = 60,
                Margin = margin
            };

            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            var iconeText = new TextBlock
            {
                Text = icone,
                FontSize = 20,
                Margin = new Thickness(0, 0, 8, 0)
            };
            var texteText = new TextBlock
            {
                Text = texte,
                FontWeight = FontWeights.SemiBold
            };

            stack.Children.Add(iconeText);
            stack.Children.Add(texteText);
            button.Content = stack;

            return button;
        }

        private Border CreerElementActivite(string couleurResource, string titre, string description, string temps)
        {
            var border = new Border
            {
                Background = (Brush)FindResource("Gray50"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Barre colorée
            var colorBar = new Border
            {
                Background = (Brush)FindResource(couleurResource),
                Width = 8,
                Height = 40,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 16, 0)
            };
            Grid.SetColumn(colorBar, 0);

            // Contenu principal
            var contentStack = new StackPanel();
            var titreText = new TextBlock
            {
                Text = titre,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimary")
            };
            var descText = new TextBlock
            {
                Text = description,
                FontSize = 12,
                Foreground = (Brush)FindResource("TextMuted")
            };
            contentStack.Children.Add(titreText);
            contentStack.Children.Add(descText);
            Grid.SetColumn(contentStack, 1);

            // Temps
            var tempsText = new TextBlock
            {
                Text = temps,
                FontSize = 12,
                Foreground = (Brush)FindResource("TextMuted"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(tempsText, 2);

            grid.Children.Add(colorBar);
            grid.Children.Add(contentStack);
            grid.Children.Add(tempsText);
            border.Child = grid;

            return border;
        }

        private void BtnMesConges_Click(object sender, RoutedEventArgs e)
        {
            SelectionnerOnglet(BtnMesConges);

            ContentArea.Children.Clear();

            try
            {
                var mesDemandesControl = new Controls.MesDemandesUserControl();
                ContentArea.Children.Add(mesDemandesControl);
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
                    MettreAJourStatutRapide();

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

        private ScrollViewer CreerDashboard()
        {
            // Créer le contenu dashboard tel que défini dans le XAML
            // Pour simplifier, on retourne un dashboard basique
            var scrollViewer = new ScrollViewer();
            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            // Header
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 32) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel();
            var welcomeText = new TextBlock
            {
                Text = $"Bienvenue, {App.UtilisateurConnecte?.Prenom} !",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("TextPrimary"),
                Margin = new Thickness(0, 0, 0, 8)
            };
            var subtitleText = new TextBlock
            {
                Text = "Gérez vos demandes de congés en toute simplicité",
                FontSize = 16,
                Foreground = (Brush)FindResource("TextSecondary")
            };

            titleStack.Children.Add(welcomeText);
            titleStack.Children.Add(subtitleText);
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            stackPanel.Children.Add(headerGrid);

            // Actions principales
            var actionsCard = new Border
            {
                Style = (Style)FindResource("Card"),
                Margin = new Thickness(0, 0, 0, 24)
            };

            var actionsStack = new StackPanel();
            var actionsTitle = new TextBlock
            {
                Text = "Actions Principales",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimary"),
                Margin = new Thickness(0, 0, 0, 20)
            };
            actionsStack.Children.Add(actionsTitle);

            var actionsGrid = new Grid();
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            actionsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // Bouton Nouvelle Demande
            var btnNouvelle = new Button
            {
                Style = (Style)FindResource("MaterialDesignRaisedButton"),
                Height = 60,
                Margin = new Thickness(0, 0, 8, 0),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock { Text = "📝", FontSize = 20, Margin = new Thickness(0, 0, 8, 0) },
                        new TextBlock { Text = "Nouvelle Demande", FontWeight = FontWeights.SemiBold }
                    }
                }
            };
            btnNouvelle.Click += BtnNouvelleDemandeRaccourci_Click;
            Grid.SetColumn(btnNouvelle, 0);
            actionsGrid.Children.Add(btnNouvelle);

            // Bouton Calendrier
            var btnCalendrier = new Button
            {
                Style = (Style)FindResource("MaterialDesignOutlineButton"),
                Height = 60,
                Margin = new Thickness(4, 0, 4, 0),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock { Text = "📊", FontSize = 20, Margin = new Thickness(0, 0, 8, 0) },
                        new TextBlock { Text = "Voir Calendrier", FontWeight = FontWeights.SemiBold }
                    }
                }
            };
            btnCalendrier.Click += BtnCalendrier_Click;
            Grid.SetColumn(btnCalendrier, 1);
            actionsGrid.Children.Add(btnCalendrier);

            // Bouton Mes Congés
            var btnMesConges = new Button
            {
                Style = (Style)FindResource("MaterialDesignOutlineButton"),
                Height = 60,
                Margin = new Thickness(8, 0, 0, 0),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                    {
                        new TextBlock { Text = "📅", FontSize = 20, Margin = new Thickness(0, 0, 8, 0) },
                        new TextBlock { Text = "Mes Congés", FontWeight = FontWeights.SemiBold }
                    }
                }
            };
            btnMesConges.Click += BtnMesConges_Click;
            Grid.SetColumn(btnMesConges, 2);
            actionsGrid.Children.Add(btnMesConges);

            actionsStack.Children.Add(actionsGrid);
            actionsCard.Child = actionsStack;
            stackPanel.Children.Add(actionsCard);

            scrollViewer.Content = stackPanel;
            return scrollViewer;
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
            for (int i = 0; i < 2; i++)
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
            Grid.SetRow(btnPoles, 0);
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
            Grid.SetColumn(btnParametres, 0);
            Grid.SetRow(btnParametres, 1);
            grid.Children.Add(btnParametres);

            // Bouton Rapports (désactivé)
            var btnRapports = CreerBoutonAdmin("📊", "Rapports &\nStatistiques", (Brush)FindResource("Gray400"));
            btnRapports.IsEnabled = false;
            btnRapports.ToolTip = "Prochainement disponible";
            Grid.SetColumn(btnRapports, 1);
            Grid.SetRow(btnRapports, 1);
            grid.Children.Add(btnRapports);

            // Bouton Sauvegarde (désactivé)
            var btnSauvegarde = CreerBoutonAdmin("💾", "Sauvegarde &\nExport", (Brush)FindResource("Gray400"));
            btnSauvegarde.IsEnabled = false;
            btnSauvegarde.ToolTip = "Prochainement disponible";
            Grid.SetColumn(btnSauvegarde, 2);
            Grid.SetRow(btnSauvegarde, 1);
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

            // Ajouter à la grille principale
            var mainGrid = (Grid)this.Content;
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