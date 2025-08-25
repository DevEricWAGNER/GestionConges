using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;

namespace GestionConges.WPF.Controls
{
    public partial class CalendrierControl : UserControl
    {
        private DateTime _moisAffiche;
        private List<DemandeConge> _tousLesConges;
        private List<DemandeConge> _congesFiltres;
        private Dictionary<DateTime, List<DemandeConge>> _congesParJour;
        private List<Societe> _societes;
        private List<Equipe> _equipes;
        private List<Pole> _poles;
        private List<TypeAbsence> _typesAbsence;

        public CalendrierControl()
        {
            InitializeComponent();
            _moisAffiche = DateTime.Now;
            _tousLesConges = new List<DemandeConge>();
            _congesFiltres = new List<DemandeConge>();
            _congesParJour = new Dictionary<DateTime, List<DemandeConge>>();
            _societes = new List<Societe>();
            _equipes = new List<Equipe>();
            _poles = new List<Pole>();
            _typesAbsence = new List<TypeAbsence>();

            // Ajouter l'événement de redimensionnement
            this.SizeChanged += CalendrierControl_SizeChanged;

            InitialiserInterface();
            ChargerDonneesInitiales();
        }

        private void CalendrierControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdapterTexteHeaders();
            AdapterInterface();
        }

        private void AdapterInterface()
        {
            if (this.ActualWidth < 800)
            {
                // Mode compact - réduire les marges et espacements
                GridCalendrier.Margin = new Thickness(10);
            }
            else
            {
                // Mode normal
                GridCalendrier.Margin = new Thickness(20);
            }
        }

        private void AdapterTexteHeaders()
        {
            if (GridCalendrierHeaders == null) return;

            // Calculer la largeur disponible par colonne
            double largeurDisponible = this.ActualWidth;
            double largeurParColonne = largeurDisponible / 7;

            // Adapter les textes selon la largeur
            if (largeurParColonne < 80)
            {
                // Très petit : utiliser des abréviations à 1 lettre
                TxtLundi.Text = "L";
                TxtMardi.Text = "M";
                TxtMercredi.Text = "M";
                TxtJeudi.Text = "J";
                TxtVendredi.Text = "V";
                TxtSamedi.Text = "S";
                TxtDimanche.Text = "D";
            }
            else if (largeurParColonne < 120)
            {
                // Petit : utiliser des abréviations à 3 lettres
                TxtLundi.Text = "Lun";
                TxtMardi.Text = "Mar";
                TxtMercredi.Text = "Mer";
                TxtJeudi.Text = "Jeu";
                TxtVendredi.Text = "Ven";
                TxtSamedi.Text = "Sam";
                TxtDimanche.Text = "Dim";
            }
            else
            {
                // Normal : texte complet
                TxtLundi.Text = "Lundi";
                TxtMardi.Text = "Mardi";
                TxtMercredi.Text = "Mercredi";
                TxtJeudi.Text = "Jeudi";
                TxtVendredi.Text = "Vendredi";
                TxtSamedi.Text = "Samedi";
                TxtDimanche.Text = "Dimanche";
            }
        }

        private GestionCongesContext CreerContexte()
        {
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=GestionCongesDB;Trusted_Connection=true;MultipleActiveResultSets=true";
            var options = new DbContextOptionsBuilder<GestionCongesContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new GestionCongesContext(options);
        }

        private async void InitialiserInterface()
        {
            TxtStatutCalendrier.Text = "Initialisation...";
        }

        private async void ChargerDonneesInitiales()
        {
            try
            {
                using var context = CreerContexte();

                // Charger les types d'absence pour la légende
                _typesAbsence = await context.TypesAbsences
                    .Where(t => t.Actif)
                    .OrderBy(t => t.OrdreAffichage)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    // Créer la légende
                    CreerLegendeTypes();
                });

                // Charger les filtres comme dans GestionUtilisateurs
                await ChargerFiltres();
                await ChargerCalendrierAsync();
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    TxtStatutCalendrier.Text = $"Erreur : {ex.Message}";
                    MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task ChargerFiltres()
        {
            // Vérifier que les contrôles sont initialisés
            if (CmbFiltreSociete == null)
                return;

            try
            {
                using var context = CreerContexte();

                // Charger seulement les sociétés accessibles à l'utilisateur connecté (comme dans GestionUtilisateurs)
                var societesUtilisateur = new List<Societe>();

                if (App.UtilisateurConnecte != null)
                {
                    // Société principale
                    var societePrincipale = await context.Societes
                        .FirstOrDefaultAsync(s => s.Id == App.UtilisateurConnecte.SocieteId && s.Actif);
                    if (societePrincipale != null)
                        societesUtilisateur.Add(societePrincipale);

                    // Sociétés secondaires
                    var societesSecondaires = await context.UtilisateursSocietesSecondaires
                        .Where(uss => uss.UtilisateurId == App.UtilisateurConnecte.Id && uss.Actif)
                        .Include(uss => uss.Societe)
                        .Select(uss => uss.Societe)
                        .Where(s => s.Actif)
                        .ToListAsync();

                    societesUtilisateur.AddRange(societesSecondaires);
                }

                // Trier et ajouter l'option "Toutes"
                societesUtilisateur = societesUtilisateur.Distinct().OrderBy(s => s.Nom).ToList();
                societesUtilisateur.Insert(0, new Societe { Id = 0, Nom = "Toutes les sociétés" });

                await Dispatcher.InvokeAsync(() =>
                {
                    CmbFiltreSociete.ItemsSource = societesUtilisateur;
                    CmbFiltreSociete.DisplayMemberPath = "Nom";
                    CmbFiltreSociete.SelectedValuePath = "Id";
                    CmbFiltreSociete.SelectedIndex = 0;

                    // Masquer équipes et pôles par défaut
                    MasquerFiltresEquipeEtPole();
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des filtres : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void MasquerFiltresEquipeEtPole()
        {
            LblFiltreEquipe.Visibility = Visibility.Collapsed;
            CmbFiltreEquipe.Visibility = Visibility.Collapsed;
            LblFiltrePole.Visibility = Visibility.Collapsed;
            CmbFiltrePole.Visibility = Visibility.Collapsed;
        }

        private void CreerLegendeTypes()
        {
            PanneauLegendeTypes.Children.Clear();

            foreach (var type in _typesAbsence)
            {
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 20, 5)
                };

                // Badge coloré moderne
                var badge = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(type.CouleurHex)),
                    Width = 24,
                    Height = 16,
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var texte = new TextBlock
                {
                    Text = type.Nom,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = (Brush)FindResource("TextPrimary")
                };

                stackPanel.Children.Add(badge);
                stackPanel.Children.Add(texte);
                PanneauLegendeTypes.Children.Add(stackPanel);
            }
        }

        private async Task ChargerCalendrierAsync()
        {
            try
            {
                TxtStatutCalendrier.Text = "Chargement des congés...";

                using var context = CreerContexte();

                // Récupérer toutes les demandes approuvées du mois (avec marge)
                var debutMois = new DateTime(_moisAffiche.Year, _moisAffiche.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1);
                var debutPeriode = debutMois.AddDays(-7);
                var finPeriode = finMois.AddDays(7);

                _tousLesConges = await context.DemandesConges
                    .Include(d => d.Utilisateur)
                        .ThenInclude(u => u.Pole)
                    .Include(d => d.TypeAbsence)
                    .Where(d => d.Statut == StatusDemande.Approuve &&
                               d.DateDebut <= finPeriode &&
                               d.DateFin >= debutPeriode)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    AppliquerFiltres();
                    AfficherCalendrier();
                    MettreAJourStatistiques();
                    TxtStatutCalendrier.Text = $"{_congesFiltres.Count} congés affichés";
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    TxtStatutCalendrier.Text = "Erreur de chargement";
                    MessageBox.Show($"Erreur lors du chargement des congés : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void AppliquerFiltres()
        {
            _congesFiltres = _tousLesConges.ToList();

            // Filtre par société
            if (CmbFiltreSociete.SelectedValue is int societeId && societeId > 0)
            {
                _congesFiltres = _congesFiltres.Where(c => c.Utilisateur.SocieteId == societeId).ToList();
            }

            // Filtre par équipe
            if (CmbFiltreEquipe.SelectedValue is int equipeId && equipeId > 0)
            {
                _congesFiltres = _congesFiltres.Where(c => c.Utilisateur.EquipeId == equipeId).ToList();
            }

            // Filtre par pôle
            if (CmbFiltrePole.SelectedValue is int poleId && poleId > 0)
            {
                _congesFiltres = _congesFiltres.Where(c => c.Utilisateur.PoleId == poleId).ToList();
            }

            // Réorganiser par jour
            _congesParJour.Clear();
            foreach (var demande in _congesFiltres)
            {
                var dateDebut = demande.DateDebut.Date;
                var dateFin = demande.DateFin.Date;

                for (var date = dateDebut; date <= dateFin; date = date.AddDays(1))
                {
                    if (!_congesParJour.ContainsKey(date))
                        _congesParJour[date] = new List<DemandeConge>();

                    _congesParJour[date].Add(demande);
                }
            }
        }

        private void AfficherCalendrier()
        {
            // Mettre à jour le titre
            var culture = new CultureInfo("fr-FR");
            TxtTitreMois.Text = _moisAffiche.ToString("MMMM yyyy", culture).ToUpper();

            // Nettoyer la grille (garder seulement la structure)
            GridCalendrier.Children.Clear();

            // Calculer le premier jour à afficher (lundi de la première semaine)
            var premierDuMois = new DateTime(_moisAffiche.Year, _moisAffiche.Month, 1);
            var premierLundi = premierDuMois.AddDays(-(int)premierDuMois.DayOfWeek + 1);
            if (premierDuMois.DayOfWeek == DayOfWeek.Sunday)
                premierLundi = premierLundi.AddDays(-7);

            // Générer les 42 cases du calendrier
            for (int semaine = 0; semaine < 6; semaine++)
            {
                for (int jour = 0; jour < 7; jour++)
                {
                    var dateCase = premierLundi.AddDays(semaine * 7 + jour);
                    var caseJour = CreerCaseJourStylee(dateCase);

                    Grid.SetRow(caseJour, semaine);
                    Grid.SetColumn(caseJour, jour);

                    GridCalendrier.Children.Add(caseJour);
                }
            }
        }

        private Border CreerCaseJourStylee(DateTime date)
        {
            var border = new Border
            {
                BorderBrush = (Brush)FindResource("Gray200"),
                BorderThickness = new Thickness(1),
                Background = (Brush)FindResource("Surface"),
                Margin = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                MinHeight = 80,
                Tag = date,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(4)
            };

            // Numéro du jour avec style adaptatif
            var estDansLeMoisActuel = date.Month == _moisAffiche.Month;
            var numeroJour = new TextBlock
            {
                Text = date.Day.ToString(),
                FontWeight = estDansLeMoisActuel ? FontWeights.Bold : FontWeights.Normal,
                FontSize = 12,
                Foreground = estDansLeMoisActuel ?
                    (Brush)FindResource("TextPrimary") :
                    (Brush)FindResource("TextMuted"),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 2)
            };

            stackPanel.Children.Add(numeroJour);

            // Styles spéciaux pour weekend et aujourd'hui
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(255, 245, 245));
                numeroJour.Foreground = (Brush)FindResource("Danger");
            }

            // Aujourd'hui - style compact
            if (date.Date == DateTime.Today)
            {
                border.Background = new SolidColorBrush(Color.FromArgb(255, 230, 245, 255));
                border.BorderBrush = (Brush)FindResource("Primary");
                border.BorderThickness = new Thickness(2);

                // Badge "aujourd'hui" plus petit
                var badgeAujourdhui = new Border
                {
                    Background = (Brush)FindResource("Primary"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(4, 1, 4, 1),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, -1, -1, 2)
                };

                var texteBadge = new TextBlock
                {
                    Text = "●",
                    Foreground = Brushes.White,
                    FontSize = 8,
                    FontWeight = FontWeights.Bold
                };

                badgeAujourdhui.Child = texteBadge;
                stackPanel.Children.Add(badgeAujourdhui);
            }

            // Afficher les congés avec style compact
            if (_congesParJour.ContainsKey(date.Date))
            {
                var congesDuJour = _congesParJour[date.Date];
                var congesGroupes = congesDuJour
                    .GroupBy(c => new { c.TypeAbsence.CouleurHex, c.TypeAbsence.Nom })
                    .ToList();

                // Limite selon l'espace disponible
                int maxCongesAffiches = this.ActualWidth < 800 ? 2 : 3;

                foreach (var groupe in congesGroupes.Take(maxCongesAffiches))
                {
                    var personnes = groupe.Select(g => $"{g.Utilisateur.Prenom} {g.Utilisateur.Nom}").Distinct().ToList();
                    var poles = groupe.Select(g => g.Utilisateur.Pole?.Nom ?? "Sans pôle").Distinct().ToList();

                    var tooltipText = $"{groupe.Key.Nom}\n{string.Join(", ", personnes)}";
                    if (poles.Count == 1)
                        tooltipText += $"\n🏢 {poles[0]}";

                    var nomAffiche = personnes.Count == 1
                        ? personnes[0].Split(' ')[0]
                        : $"{personnes.Count}p";

                    // Style plus compact pour les congés
                    var rectangleConge = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(groupe.Key.CouleurHex)),
                        CornerRadius = new CornerRadius(4),
                        Margin = new Thickness(0, 1, 0, 0),
                        Height = 16,
                        ToolTip = tooltipText,
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Tag = date
                    };

                    var textConge = new TextBlock
                    {
                        Text = nomAffiche,
                        Foreground = Brushes.White,
                        FontSize = 9,
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Padding = new Thickness(3, 0, 3, 0)
                    };

                    rectangleConge.Child = textConge;

                    rectangleConge.MouseLeftButtonDown += (s, e) =>
                    {
                        if (e.ClickCount == 2)
                        {
                            e.Handled = true;
                            AfficherDetailsJour(date);
                        }
                    };

                    stackPanel.Children.Add(rectangleConge);
                }

                // Indicateur s'il y a plus de congés - version compacte
                if (congesGroupes.Count > maxCongesAffiches)
                {
                    var nombreRestant = congesGroupes.Count - maxCongesAffiches;

                    var indicateurPlus = new Border
                    {
                        Background = (Brush)FindResource("Gray400"),
                        CornerRadius = new CornerRadius(3),
                        Height = 12,
                        Margin = new Thickness(0, 1, 0, 0),
                        ToolTip = $"+{nombreRestant} autre(s)",
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Tag = date
                    };

                    var textPlus = new TextBlock
                    {
                        Text = $"+{nombreRestant}",
                        Foreground = Brushes.White,
                        FontSize = 8,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    indicateurPlus.Child = textPlus;

                    indicateurPlus.MouseLeftButtonDown += (s, e) =>
                    {
                        if (e.ClickCount == 2)
                        {
                            e.Handled = true;
                            AfficherDetailsJour(date);
                        }
                    };

                    stackPanel.Children.Add(indicateurPlus);
                }
            }

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    AfficherDetailsJour(date);
                }
            };

            border.Child = stackPanel;
            return border;
        }

        private void MettreAJourStatistiques()
        {
            var personnesUniques = _congesFiltres
                .Select(c => c.UtilisateurId)
                .Distinct()
                .Count();

            TxtNombrePersonnes.Text = $"{personnesUniques} personne{(personnesUniques > 1 ? "s" : "")}";
            TxtNombreConges.Text = $"{_congesFiltres.Count} congé{(_congesFiltres.Count > 1 ? "s" : "")} ce mois";
        }

        private void BtnPrecedent_Click(object sender, RoutedEventArgs e)
        {
            _moisAffiche = _moisAffiche.AddMonths(-1);
            _ = ChargerCalendrierAsync();
        }

        private void BtnSuivant_Click(object sender, RoutedEventArgs e)
        {
            _moisAffiche = _moisAffiche.AddMonths(1);
            _ = ChargerCalendrierAsync();
        }

        private void BtnAujourdhui_Click(object sender, RoutedEventArgs e)
        {
            _moisAffiche = DateTime.Now;
            _ = ChargerCalendrierAsync();
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            _ = ChargerCalendrierAsync();
        }

        private async void CmbFiltreSociete_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFiltreSociete.SelectedValue is int societeId && societeId > 0)
            {
                // Société spécifique sélectionnée - afficher le filtre équipe
                await ChargerEquipesPourFiltre(societeId);
                AfficherFiltreEquipe();

                // Masquer le filtre pôle
                LblFiltrePole.Visibility = Visibility.Collapsed;
                CmbFiltrePole.Visibility = Visibility.Collapsed;
            }
            else
            {
                // "Toutes les sociétés" - masquer équipes et pôles
                MasquerFiltresEquipeEtPole();
            }

            ChargerDonneesFiltres();
        }

        private async void CmbFiltreEquipe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFiltreEquipe.SelectedValue is int equipeId && equipeId > 0)
            {
                // Équipe spécifique sélectionnée - afficher le filtre pôle
                await ChargerPolesPourFiltre(equipeId);
                AfficherFiltrePole();
            }
            else
            {
                // "Toutes les équipes" - masquer le filtre pôle
                LblFiltrePole.Visibility = Visibility.Collapsed;
                CmbFiltrePole.Visibility = Visibility.Collapsed;
            }

            ChargerDonneesFiltres();
        }

        private async Task ChargerPolesPourFiltre(int equipeId)
        {
            try
            {
                using var context = CreerContexte();
                if (context == null) return;

                var poles = await context.EquipesPoles
                    .Where(ep => ep.EquipeId == equipeId && ep.Actif)
                    .Include(ep => ep.Pole)
                    .Select(ep => ep.Pole)
                    .Where(p => p.Actif)
                    .OrderBy(p => p.Nom)
                    .ToListAsync();

                var polesFiltre = new List<Pole> { new Pole { Id = 0, Nom = "Tous les pôles" } };
                polesFiltre.AddRange(poles);

                CmbFiltrePole.ItemsSource = polesFiltre;
                CmbFiltrePole.DisplayMemberPath = "Nom";
                CmbFiltrePole.SelectedValuePath = "Id";
                CmbFiltrePole.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des pôles : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AfficherFiltreEquipe()
        {
            LblFiltreEquipe.Visibility = Visibility.Visible;
            CmbFiltreEquipe.Visibility = Visibility.Visible;
        }

        private void AfficherFiltrePole()
        {
            LblFiltrePole.Visibility = Visibility.Visible;
            CmbFiltrePole.Visibility = Visibility.Visible;
        }

        private async Task ChargerEquipesPourFiltre(int societeId)
        {
            try
            {
                using var context = CreerContexte();
                if (context == null) return;

                var equipes = await context.Equipes
                    .Where(e => e.SocieteId == societeId && e.Actif)
                    .OrderBy(e => e.Nom)
                    .ToListAsync();

                var equipesFiltre = new List<Equipe> { new Equipe { Id = 0, Nom = "Toutes les équipes" } };
                equipesFiltre.AddRange(equipes);

                CmbFiltreEquipe.ItemsSource = equipesFiltre;
                CmbFiltreEquipe.DisplayMemberPath = "Nom";
                CmbFiltreEquipe.SelectedValuePath = "Id";
                CmbFiltreEquipe.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des équipes : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ChargerDonneesFiltres()
        {
            try
            {
                TxtStatutCalendrier.Text = "Chargement des congés...";

                using var context = CreerContexte();

                int societeId = 0;
                if (CmbFiltreSociete.SelectedItem is Societe societeSelectionnee)
                    societeId = societeSelectionnee.Id;

                int equipeId = 0;
                if (CmbFiltreEquipe.SelectedItem is Equipe equipeSelectionnee)
                    equipeId = equipeSelectionnee.Id;

                int poleId = 0;
                if (CmbFiltrePole.SelectedItem is Pole poleSelectionne)
                    poleId = poleSelectionne.Id;

                // Récupérer toutes les demandes approuvées du mois (avec marge)
                var debutMois = new DateTime(_moisAffiche.Year, _moisAffiche.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1);
                var debutPeriode = debutMois.AddDays(-7);
                var finPeriode = finMois.AddDays(7);

                var query = context.DemandesConges
                    .Include(d => d.Utilisateur)
                        .ThenInclude(u => u.Societe)
                    .Include(d => d.Utilisateur)
                        .ThenInclude(u => u.Equipe)
                    .Include(d => d.Utilisateur)
                        .ThenInclude(u => u.Pole)
                    .Include(d => d.TypeAbsence)
                    .AsQueryable();
                
                query = query.Where(d => d.Statut == StatusDemande.Approuve &&
                               d.DateDebut <= finPeriode &&
                               d.DateFin >= debutPeriode);
                if (societeId != 0)
                    query = query.Where(d => d.Utilisateur.SocieteId == societeId);

                if (equipeId != 0)
                    query = query.Where(d => d.Utilisateur.EquipeId == equipeId);

                if (poleId != 0)
                    query = query.Where(d => d.Utilisateur.PoleId == poleId);

                _tousLesConges = await query.ToListAsync(); 

                await Dispatcher.InvokeAsync(() =>
                {
                    AppliquerFiltres();
                    AfficherCalendrier();
                    MettreAJourStatistiques();
                    TxtStatutCalendrier.Text = $"{_congesFiltres.Count} congés affichés";
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    TxtStatutCalendrier.Text = $"Erreur : {ex.Message}";
                    MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void CmbFiltrePole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Vérifier que les contrôles sont initialisés
            if (CmbFiltrePole == null)
                return;

            AppliquerFiltresEtRafraichir();
        }

        private async Task ChargerEquipesPourSociete(int societeId)
        {
            try
            {
                using var context = CreerContexte();

                var equipes = await context.Equipes
                    .Where(e => e.SocieteId == societeId && e.Actif)
                    .OrderBy(e => e.Nom)
                    .ToListAsync();

                var equipesListe = new List<Equipe> { new Equipe { Id = 0, Nom = "Toutes les équipes" } };
                equipesListe.AddRange(equipes);

                await Dispatcher.InvokeAsync(() =>
                {
                    CmbFiltreEquipe.ItemsSource = equipesListe;
                    CmbFiltreEquipe.DisplayMemberPath = "Nom";
                    CmbFiltreEquipe.SelectedValuePath = "Id";
                    CmbFiltreEquipe.SelectedIndex = 0;
                });

                // Reset du filtre pôle
                var polesDefaut = new List<Pole> { new Pole { Id = 0, Nom = "Tous les pôles" } };
                await Dispatcher.InvokeAsync(() =>
                {
                    CmbFiltrePole.ItemsSource = polesDefaut;
                    CmbFiltrePole.SelectedIndex = 0;
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des équipes : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task ChargerPolesPourEquipe(int equipeId)
        {
            try
            {
                using var context = CreerContexte();

                var poles = await context.EquipesPoles
                    .Where(ep => ep.EquipeId == equipeId && ep.Actif)
                    .Include(ep => ep.Pole)
                    .Select(ep => ep.Pole)
                    .Where(p => p.Actif)
                    .OrderBy(p => p.Nom)
                    .ToListAsync();

                var polesListe = new List<Pole> { new Pole { Id = 0, Nom = "Tous les pôles" } };
                polesListe.AddRange(poles);

                await Dispatcher.InvokeAsync(() =>
                {
                    CmbFiltrePole.ItemsSource = polesListe;
                    CmbFiltrePole.DisplayMemberPath = "Nom";
                    CmbFiltrePole.SelectedValuePath = "Id";
                    CmbFiltrePole.SelectedIndex = 0;
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des pôles : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void AppliquerFiltresEtRafraichir()
        {
            if (_tousLesConges != null && _tousLesConges.Any())
            {
                AppliquerFiltres();
                AfficherCalendrier();
                MettreAJourStatistiques();

                // Créer un texte de statut intelligent
                var filtresActifs = new List<string>();

                // Récupération correcte des valeurs sélectionnées
                if (CmbFiltreSociete.SelectedValue is int societeId && societeId > 0)
                {
                    var societeNom = ((Societe)CmbFiltreSociete.SelectedItem)?.Nom;
                    if (!string.IsNullOrEmpty(societeNom))
                        filtresActifs.Add(societeNom);
                }

                if (CmbFiltreEquipe.SelectedValue is int equipeId && equipeId > 0)
                {
                    var equipeNom = ((Equipe)CmbFiltreEquipe.SelectedItem)?.Nom;
                    if (!string.IsNullOrEmpty(equipeNom))
                        filtresActifs.Add(equipeNom);
                }

                if (CmbFiltrePole.SelectedValue is int poleId && poleId > 0)
                {
                    var poleNom = ((Pole)CmbFiltrePole.SelectedItem)?.Nom;
                    if (!string.IsNullOrEmpty(poleNom))
                        filtresActifs.Add(poleNom);
                }

                var statusText = $"{_congesFiltres.Count} congé{(_congesFiltres.Count > 1 ? "s" : "")} affiché{(_congesFiltres.Count > 1 ? "s" : "")}";
                if (filtresActifs.Any())
                    statusText += $" ({string.Join(" → ", filtresActifs)})";

                TxtStatutCalendrier.Text = statusText;
            }
        }

        // Méthodes publiques pour l'interface externe
        public void Rafraichir()
        {
            _ = ChargerCalendrierAsync();
        }

        public void AfficherMois(DateTime mois)
        {
            _moisAffiche = new DateTime(mois.Year, mois.Month, 1);
            _ = ChargerCalendrierAsync();
        }

        private void AfficherDetailsJour(DateTime date)
        {
            if (!_congesParJour.ContainsKey(date.Date))
            {
                MessageBox.Show($"Aucun congé prévu le {date:dd/MM/yyyy}.",
                              "Détails du jour", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var congesDuJour = _congesParJour[date.Date];

            // Créer une fenêtre de détails stylée
            var detailsWindow = new Window
            {
                Title = $"Congés du {date:dddd dd/MM/yyyy}",
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Background = (Brush)FindResource("Background"),
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true
            };

            var mainBorder = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = (Brush)FindResource("Background"),
                BorderBrush = (Brush)FindResource("Gray300"),
                BorderThickness = new Thickness(1)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // En-tête stylé
            var headerBorder = new Border
            {
                Background = (Brush)FindResource("Primary"),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                Padding = new Thickness(24, 20, 24, 20)
            };

            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };

            var headerIcon = new Border
            {
                Background = Brushes.White,
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Margin = new Thickness(0, 0, 16, 0)
            };

            var iconText = new TextBlock
            {
                Text = "📅",
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerIcon.Child = iconText;

            var headerTextStack = new StackPanel();
            headerTextStack.Children.Add(new TextBlock
            {
                Text = $"Congés du {date:dddd dd MMMM yyyy}",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            headerTextStack.Children.Add(new TextBlock
            {
                Text = $"{congesDuJour.Count} personne{(congesDuJour.Count > 1 ? "s" : "")} absente{(congesDuJour.Count > 1 ? "s" : "")}",
                FontSize = 14,
                Foreground = Brushes.White,
                Opacity = 0.9
            });

            headerStack.Children.Add(headerIcon);
            headerStack.Children.Add(headerTextStack);

            // Bouton fermer dans le header
            var btnCloseHeader = new Button
            {
                Content = "✕",
                Width = 32,
                Height = 32,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -10, -10, 0)
            };
            btnCloseHeader.Click += (s, e) => detailsWindow.Close();

            var headerGrid = new Grid();
            headerGrid.Children.Add(headerStack);
            headerGrid.Children.Add(btnCloseHeader);
            headerBorder.Child = headerGrid;

            Grid.SetRow(headerBorder, 0);
            mainGrid.Children.Add(headerBorder);

            // Contenu avec style moderne
            var scrollViewer = new ScrollViewer
            {
                Margin = new Thickness(0),
                Background = (Brush)FindResource("Surface")
            };

            var contentStack = new StackPanel { Margin = new Thickness(24, 20, 24, 20) };

            // Remplacement de la ligne problématique dans la méthode AfficherDetailsJour
            // Ancienne ligne (provoquant CS0833) :
            // var congesParPersonne = congesDuJour
            //     .GroupBy(c => new { c.Utilisateur.Id, c.Utilisateur.NomComplet, c.Utilisateur.Societe?.Nom, c.Utilisateur.Equipe?.Nom, c.Utilisateur.Pole?.Nom })
            //     .Select(g => new
            //     {
            //         Personne = g.Key.NomComplet,
            //         Societe = g.Key.Nom ?? "Sans société",
            //         Equipe = g.Key.Nom ?? "Sans équipe",
            //         Pole = g.Key.Nom ?? "Sans pôle",
            //         Conges = g.ToList()
            //     })
            //     .OrderBy(x => x.Personne)
            //     .ToList();

            // Correction : donner des noms uniques à chaque propriété du type anonyme
            var congesParPersonne = congesDuJour
                .GroupBy(c => new
                {
                    UtilisateurId = c.Utilisateur.Id,
                    NomComplet = c.Utilisateur.NomComplet,
                    SocieteNom = c.Utilisateur.Societe?.Nom,
                    EquipeNom = c.Utilisateur.Equipe?.Nom,
                    PoleNom = c.Utilisateur.Pole?.Nom
                })
                .Select(g => new
                {
                    Personne = g.Key.NomComplet,
                    Societe = g.Key.SocieteNom ?? "Sans société",
                    Equipe = g.Key.EquipeNom ?? "Sans équipe",
                    Pole = g.Key.PoleNom ?? "Sans pôle",
                    Conges = g.ToList()
                })
                .OrderBy(x => x.Personne)
                .ToList();

            foreach (var groupe in congesParPersonne)
            {
                // Carte moderne pour chaque personne
                var personneCard = new Border
                {
                    Background = (Brush)FindResource("Surface"),
                    BorderBrush = (Brush)FindResource("Gray200"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(20),
                    Margin = new Thickness(0, 0, 0, 16)
                };

                var personneStack = new StackPanel();

                // En-tête personne
                var personneHeader = new Grid { Margin = new Thickness(0, 0, 0, 12) };
                personneHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                personneHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var avatarBorder = new Border
                {
                    Background = (Brush)FindResource("Primary"),
                    Width = 36,
                    Height = 36,
                    CornerRadius = new CornerRadius(18),
                    Margin = new Thickness(0, 0, 12, 0)
                };

                var avatarText = new TextBlock
                {
                    Text = "👤",
                    Foreground = Brushes.White,
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                avatarBorder.Child = avatarText;

                var personneInfo = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                personneInfo.Children.Add(new TextBlock
                {
                    Text = groupe.Personne,
                    FontWeight = FontWeights.Bold,
                    FontSize = 16,
                    Foreground = (Brush)FindResource("TextPrimary")
                });
                personneInfo.Children.Add(new TextBlock
                {
                    Text = $"🏢 Société : {groupe.Societe}",
                    FontSize = 13,
                    Foreground = (Brush)FindResource("TextMuted")
                });
                personneInfo.Children.Add(new TextBlock
                {
                    Text = $"👥 Equipe : {groupe.Equipe}",
                    FontSize = 13,
                    Foreground = (Brush)FindResource("TextMuted")
                });
                personneInfo.Children.Add(new TextBlock
                {
                    Text = $"Pôle : {groupe.Pole}",
                    FontSize = 13,
                    Foreground = (Brush)FindResource("TextMuted")
                });

                Grid.SetColumn(avatarBorder, 0);
                Grid.SetColumn(personneInfo, 1);
                personneHeader.Children.Add(avatarBorder);
                personneHeader.Children.Add(personneInfo);

                personneStack.Children.Add(personneHeader);

                // Types d'absences
                foreach (var conge in groupe.Conges)
                {
                    var congeCard = new Border
                    {
                        Background = (Brush)FindResource("Gray50"),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(16),
                        Margin = new Thickness(0, 8, 0, 0)
                    };

                    var congeGrid = new Grid();
                    congeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    congeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    congeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var colorBadge = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(conge.TypeAbsence.CouleurHex)),
                        Width = 20,
                        Height = 20,
                        CornerRadius = new CornerRadius(10),
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 0, 12, 0)
                    };

                    var congeDetails = new StackPanel();
                    congeDetails.Children.Add(new TextBlock
                    {
                        Text = conge.TypeAbsence.Nom,
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 14,
                        Foreground = (Brush)FindResource("TextPrimary")
                    });

                    // Détails de la période
                    if (conge.DateDebut != date || conge.DateFin != date)
                    {
                        congeDetails.Children.Add(new TextBlock
                        {
                            Text = $"Période : {conge.DateDebut:dd/MM} au {conge.DateFin:dd/MM} ({conge.NombreJours} jour{(conge.NombreJours > 1 ? "s" : "")})",
                            FontSize = 12,
                            Foreground = (Brush)FindResource("TextMuted"),
                            Margin = new Thickness(0, 2, 0, 0)
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(conge.Commentaire))
                    {
                        congeDetails.Children.Add(new TextBlock
                        {
                            Text = $"💬 {conge.Commentaire}",
                            FontSize = 12,
                            Foreground = (Brush)FindResource("TextMuted"),
                            FontStyle = FontStyles.Italic,
                            Margin = new Thickness(0, 4, 0, 0)
                        });
                    }

                    // Bouton détails
                    var detailsBtn = new Button
                    {
                        Content = "👁️",
                        Width = 32,
                        Height = 32,
                        Background = (Brush)FindResource("Primary"),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        ToolTip = "Voir les détails de cette demande",
                        Tag = conge,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    detailsBtn.Click += (s, e) =>
                    {
                        try
                        {
                            var demandeDetails = new Views.DemandeDetailsWindow(conge, modeConsultation: true);
                            demandeDetails.ShowDialog();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    Grid.SetColumn(colorBadge, 0);
                    Grid.SetColumn(congeDetails, 1);
                    Grid.SetColumn(detailsBtn, 2);

                    congeGrid.Children.Add(colorBadge);
                    congeGrid.Children.Add(congeDetails);
                    congeGrid.Children.Add(detailsBtn);

                    congeCard.Child = congeGrid;
                    personneStack.Children.Add(congeCard);
                }

                personneCard.Child = personneStack;
                contentStack.Children.Add(personneCard);
            }

            scrollViewer.Content = contentStack;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // Footer avec bouton fermer
            var footerBorder = new Border
            {
                Background = (Brush)FindResource("Gray50"),
                CornerRadius = new CornerRadius(0, 0, 12, 12),
                BorderBrush = (Brush)FindResource("Gray200"),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 16, 24, 16)
            };

            var btnFermer = new Button
            {
                Content = "🚪 Fermer",
                Style = (Style)FindResource("MaterialDesignRaisedButton"),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            btnFermer.Click += (s, e) => detailsWindow.Close();

            footerBorder.Child = btnFermer;
            Grid.SetRow(footerBorder, 2);
            mainGrid.Children.Add(footerBorder);

            mainBorder.Child = mainGrid;
            detailsWindow.Content = mainBorder;

            // Permettre de déplacer la fenêtre
            detailsWindow.MouseLeftButtonDown += (s, e) => detailsWindow.DragMove();

            detailsWindow.ShowDialog();
        }
    }
}