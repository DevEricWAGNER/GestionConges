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
        private List<DemandeConge> _congesApprouves;
        private Dictionary<DateTime, List<DemandeConge>> _congesParJour;

        public CalendrierControl()
        {
            InitializeComponent();
            _moisAffiche = DateTime.Now;
            _congesApprouves = new List<DemandeConge>();
            _congesParJour = new Dictionary<DateTime, List<DemandeConge>>();

            ChargerCalendrier();
        }

        private GestionCongesContext CreerContexte()
        {
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=GestionCongesDB;Trusted_Connection=true;MultipleActiveResultSets=true";
            var options = new DbContextOptionsBuilder<GestionCongesContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new GestionCongesContext(options);
        }

        private async void ChargerCalendrier()
        {
            await ChargerCongesAsync();
            AfficherCalendrier();
        }

        private async Task ChargerCongesAsync()
        {
            try
            {
                using var context = CreerContexte();

                // Récupérer toutes les demandes approuvées du mois affiché (avec un peu de marge)
                var debutMois = new DateTime(_moisAffiche.Year, _moisAffiche.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1);

                // Étendre un peu la période pour avoir les congés qui débordent
                var debutPeriode = debutMois.AddDays(-7);
                var finPeriode = finMois.AddDays(7);

                _congesApprouves = await context.DemandesConges
                    .Include(d => d.Utilisateur)
                    .Include(d => d.TypeAbsence)
                    .Where(d => d.Statut == StatusDemande.Approuve &&
                               d.DateDebut <= finPeriode &&
                               d.DateFin >= debutPeriode)
                    .ToListAsync();

                // Organiser les congés par jour
                _congesParJour.Clear();
                foreach (var demande in _congesApprouves)
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
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des congés : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Générer les 42 cases du calendrier (6 semaines × 7 jours)
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
                border.BorderBrush = Brushes.Blue;
                border.BorderThickness = new Thickness(2);
            }

            // Afficher les congés pour ce jour
            if (_congesParJour.ContainsKey(date.Date))
            {
                var congesDuJour = _congesParJour[date.Date];
                var congesGroupes = congesDuJour
                    .GroupBy(c => new { c.TypeAbsence.CouleurHex, c.TypeAbsence.Nom })
                    .ToList();

                foreach (var groupe in congesGroupes.Take(3)) // Limiter à 3 types par jour
                {
                    var personnes = groupe.Select(g => g.Utilisateur.Prenom).Distinct().ToList();
                    var nomPersonnes = personnes.Count > 2
                        ? $"{personnes[0]}, {personnes[1]}... (+{personnes.Count - 2})"
                        : string.Join(", ", personnes);

                    var rectangleConge = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(groupe.Key.CouleurHex)),
                        CornerRadius = new CornerRadius(2),
                        Margin = new Thickness(0, 1, 0, 0),
                        Height = 16,
                        ToolTip = $"{groupe.Key.Nom}\n{nomPersonnes}"
                    };

                    var textConge = new TextBlock
                    {
                        Text = personnes.Count == 1 ? personnes[0] : $"{personnes.Count} pers.",
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
                    var indicateurPlus = new TextBlock
                    {
                        Text = $"+{congesGroupes.Count - 3}",
                        FontSize = 8,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 1, 0, 0)
                    };
                    stackPanel.Children.Add(indicateurPlus);
                }
            }

            border.Child = stackPanel;
            return border;
        }

        private void BtnPrecedent_Click(object sender, RoutedEventArgs e)
        {
            _moisAffiche = _moisAffiche.AddMonths(-1);
            ChargerCalendrier();
        }

        private void BtnSuivant_Click(object sender, RoutedEventArgs e)
        {
            _moisAffiche = _moisAffiche.AddMonths(1);
            ChargerCalendrier();
        }

        // Méthode publique pour rafraîchir le calendrier depuis l'extérieur
        public void Rafraichir()
        {
            ChargerCalendrier();
        }

        // Méthode pour changer le mois affiché
        public void AfficherMois(DateTime mois)
        {
            _moisAffiche = new DateTime(mois.Year, mois.Month, 1);
            ChargerCalendrier();
        }
    }
}