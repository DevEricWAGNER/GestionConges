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
        private List<Pole> _poles;
        private List<TypeAbsence> _typesAbsence;

        public CalendrierControl()
        {
            InitializeComponent();
            _moisAffiche = DateTime.Now;
            _tousLesConges = new List<DemandeConge>();
            _congesFiltres = new List<DemandeConge>();
            _congesParJour = new Dictionary<DateTime, List<DemandeConge>>();
            _poles = new List<Pole>();
            _typesAbsence = new List<TypeAbsence>();

            InitialiserInterface();
            ChargerDonneesInitiales();
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

                // Charger les pôles
                _poles = await context.Poles
                    .Where(p => p.Actif)
                    .OrderBy(p => p.Nom)
                    .ToListAsync();

                // Charger les types d'absence
                _typesAbsence = await context.TypesAbsences
                    .Where(t => t.Actif)
                    .OrderBy(t => t.OrdreAffichage)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    // Remplir le combo des pôles
                    CmbFiltrePole.Items.Clear();
                    CmbFiltrePole.Items.Add(new ComboBoxItem { Content = "Tous les pôles", Tag = null });
                    foreach (var pole in _poles)
                    {
                        CmbFiltrePole.Items.Add(new ComboBoxItem { Content = pole.Nom, Tag = pole.Id });
                    }
                    CmbFiltrePole.SelectedIndex = 0;

                    // Remplir le combo des types
                    CmbFiltreType.Items.Clear();
                    CmbFiltreType.Items.Add(new ComboBoxItem { Content = "Tous les types", Tag = null });
                    foreach (var type in _typesAbsence)
                    {
                        CmbFiltreType.Items.Add(new ComboBoxItem { Content = type.Nom, Tag = type.Id });
                    }
                    CmbFiltreType.SelectedIndex = 0;

                    // Créer la légende
                    CreerLegendeTypes();
                });

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

        private void CreerLegendeTypes()
        {
            PanneauLegendeTypes.Children.Clear();

            foreach (var type in _typesAbsence)
            {
                var stackPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 15, 0)
                };

                var rectangle = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(type.CouleurHex)),
                    Width = 20,
                    Height = 15,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var texte = new TextBlock
                {
                    Text = type.Nom,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12
                };

                stackPanel.Children.Add(rectangle);
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

            // Filtre par pôle
            if (CmbFiltrePole.SelectedItem is ComboBoxItem itemPole && itemPole.Tag != null)
            {
                var poleId = (int)itemPole.Tag;
                _congesFiltres = _congesFiltres.Where(c => c.Utilisateur.PoleId == poleId).ToList();
            }

            // Filtre par type
            if (CmbFiltreType.SelectedItem is ComboBoxItem itemType && itemType.Tag != null)
            {
                var typeId = (int)itemType.Tag;
                _congesFiltres = _congesFiltres.Where(c => c.TypeAbsenceId == typeId).ToList();
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
            TxtTitreMois.Text = _moisAffiche.ToString("MMMM yyyy", culture);

            // Nettoyer la grille (garder seulement les en-têtes)
            var elementsASupprimer = GridCalendrier.Children
                .Cast<UIElement>()
                .Where(child => Grid.GetRow(child) > 0)
                .ToList();

            foreach (var element in elementsASupprimer)
            {
                GridCalendrier.Children.Remove(element);
            }

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
                    var caseJour = CreerCaseJour(dateCase);

                    Grid.SetRow(caseJour, semaine + 1);
                    Grid.SetColumn(caseJour, jour);

                    GridCalendrier.Children.Add(caseJour);
                }
            }
        }

        private Border CreerCaseJour(DateTime date)
        {
            var border = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = Brushes.White,
                Margin = new Thickness(0)
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(5)
            };

            // Numéro du jour
            var estDansLeMoisActuel = date.Month == _moisAffiche.Month;
            var numeroJour = new TextBlock
            {
                Text = date.Day.ToString(),
                FontWeight = estDansLeMoisActuel ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = estDansLeMoisActuel ? Brushes.Black : Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                FontSize = 12
            };

            stackPanel.Children.Add(numeroJour);

            // Couleur de fond pour weekend
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(255, 245, 245));
            }

            // Aujourd'hui
            if (date.Date == DateTime.Today)
            {
                border.Background = new SolidColorBrush(Color.FromRgb(230, 245, 255));
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                border.BorderThickness = new Thickness(2);
            }

            // Afficher les congés pour ce jour
            if (_congesParJour.ContainsKey(date.Date))
            {
                var congesDuJour = _congesParJour[date.Date];
                var congesGroupes = congesDuJour
                    .GroupBy(c => new { c.TypeAbsence.CouleurHex, c.TypeAbsence.Nom })
                    .ToList();

                foreach (var groupe in congesGroupes.Take(3))
                {
                    var personnes = groupe.Select(g => $"{g.Utilisateur.Prenom} {g.Utilisateur.Nom}").Distinct().ToList();
                    var poles = groupe.Select(g => g.Utilisateur.Pole?.Nom ?? "Sans pôle").Distinct().ToList();

                    var tooltipText = $"{groupe.Key.Nom}\n";
                    tooltipText += $"👥 {string.Join(", ", personnes)}\n";
                    if (poles.Count == 1)
                        tooltipText += $"🏢 {poles[0]}";
                    else
                        tooltipText += $"🏢 Plusieurs pôles : {string.Join(", ", poles)}";

                    var nomAffiche = personnes.Count == 1
                        ? personnes[0].Split(' ')[0] // Prénom seulement
                        : $"{personnes.Count} pers.";

                    var rectangleConge = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(groupe.Key.CouleurHex)),
                        CornerRadius = new CornerRadius(3),
                        Margin = new Thickness(0, 1, 0, 0),
                        Height = 18,
                        ToolTip = tooltipText,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    var textConge = new TextBlock
                    {
                        Text = nomAffiche,
                        Foreground = Brushes.White,
                        FontSize = 9,
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };

                    rectangleConge.Child = textConge;
                    stackPanel.Children.Add(rectangleConge);
                }

                // Indicateur s'il y a plus de congés
                if (congesGroupes.Count > 3)
                {
                    var nombreRestant = congesGroupes.Count - 3;
                    var personnesRestantes = congesGroupes.Skip(3)
                        .SelectMany(g => g.Select(c => $"{c.Utilisateur.Prenom} {c.Utilisateur.Nom}"))
                        .Distinct().ToList();

                    var indicateurPlus = new Border
                    {
                        Background = Brushes.Gray,
                        CornerRadius = new CornerRadius(2),
                        Height = 14,
                        Margin = new Thickness(0, 1, 0, 0),
                        ToolTip = $"+{nombreRestant} autre(s) type(s)\n👥 {string.Join(", ", personnesRestantes)}",
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    var textPlus = new TextBlock
                    {
                        Text = $"+{nombreRestant}",
                        Foreground = Brushes.White,
                        FontSize = 8,
                        FontWeight = FontWeights.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    indicateurPlus.Child = textPlus;
                    stackPanel.Children.Add(indicateurPlus);
                }
            }

            border.Child = stackPanel;
            return border;
        }

        private void MettreAJourStatistiques()
        {
            var personnesUniques = _congesFiltres
                .Select(c => c.UtilisateurId)
                .Distinct()
                .Count();

            TxtNombrePersonnes.Text = $"👥 {personnesUniques} personne{(personnesUniques > 1 ? "s" : "")}";
            TxtNombreConges.Text = $"📅 {_congesFiltres.Count} congé{(_congesFiltres.Count > 1 ? "s" : "")} ce mois";
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

        private void CmbFiltrePole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tousLesConges != null && _tousLesConges.Any())
            {
                AppliquerFiltres();
                AfficherCalendrier();
                MettreAJourStatistiques();

                var itemSelectionne = CmbFiltrePole.SelectedItem as ComboBoxItem;
                var filtreActif = itemSelectionne?.Tag != null ? itemSelectionne.Content.ToString() : "tous les pôles";
                TxtStatutCalendrier.Text = $"{_congesFiltres.Count} congés ({filtreActif})";
            }
        }

        private void CmbFiltreType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tousLesConges != null && _tousLesConges.Any())
            {
                AppliquerFiltres();
                AfficherCalendrier();
                MettreAJourStatistiques();

                var itemSelectionne = CmbFiltreType.SelectedItem as ComboBoxItem;
                var filtreActif = itemSelectionne?.Tag != null ? itemSelectionne.Content.ToString() : "tous les types";
                TxtStatutCalendrier.Text = $"{_congesFiltres.Count} congés ({filtreActif})";
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
    }
}