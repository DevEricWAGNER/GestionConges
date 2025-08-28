using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;
using GestionConges.WPF.Views;

namespace GestionConges.WPF.Controls
{
    public partial class DashboardUserControl : UserControl
    {
        private readonly Utilisateur _utilisateurConnecte;

        // Événements pour la navigation
        public event EventHandler? NaviguerVersNouvelleDemandeRequested;
        public event EventHandler? NaviguerVersCalendrierRequested;
        public event EventHandler? NaviguerVersMesCongesRequested;

        public DashboardUserControl()
        {
            InitializeComponent();

            _utilisateurConnecte = App.UtilisateurConnecte ?? throw new InvalidOperationException("Aucun utilisateur connecté");

            // Personnaliser le message de bienvenue
            TxtBienvenue.Text = $"Bienvenue, {_utilisateurConnecte.Prenom} !";

            // Initialiser l'affichage de la date et heure en français
            InitialiserAffichageDateTime();

            // Charger les données après l'initialisation complète
            Dispatcher.BeginInvoke(new Action(ChargerDonneesDashboard),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void InitialiserAffichageDateTime()
        {
            var cultureFrancaise = new CultureInfo("fr-FR");

            // Mettre à jour immédiatement
            MettreAJourDateTime(cultureFrancaise);

            // Timer pour mettre à jour toutes les minutes
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += (s, e) => MettreAJourDateTime(cultureFrancaise);
            timer.Start();
        }

        private void MettreAJourDateTime(CultureInfo culture)
        {
            var maintenant = DateTime.Now;

            // Formater la date en français
            TxtDateFrancaise.Text = maintenant.ToString("dddd dd MMMM yyyy", culture);
            TxtHeureCourante.Text = maintenant.ToString("HH:mm");
        }

        private GestionCongesContext CreerContexte()
        {
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=GestionCongesDB;Trusted_Connection=true;MultipleActiveResultSets=true";
            var options = new DbContextOptionsBuilder<GestionCongesContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new GestionCongesContext(options);
        }

        private async void ChargerDonneesDashboard()
        {
            try
            {
                using var context = CreerContexte();

                // Charger toutes les données nécessaires
                await Task.WhenAll(
                    ChargerStatistiquesPersonnelles(context),
                    ChargerStatistiquesEquipe(context),
                    ChargerActiviteRecente(context)
                );
            }
            catch (Exception ex)
            {
                // En cas d'erreur, afficher des valeurs par défaut et un message d'erreur discret
                AfficherValeursParDefaut();
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement du dashboard: {ex.Message}");
            }
        }

        private async Task ChargerStatistiquesPersonnelles(GestionCongesContext context)
        {
            var anneeCourante = DateTime.Now.Year;
            var utilisateurId = _utilisateurConnecte.Id;

            // Récupérer les demandes de l'utilisateur pour l'année courante
            var demandesAnnee = await context.DemandesConges
                .Include(d => d.TypeAbsence)
                .Where(d => d.UtilisateurId == utilisateurId &&
                           (d.DateDebut.Year == anneeCourante || d.DateFin.Year == anneeCourante))
                .ToListAsync();

            // Calculer les statistiques
            var demandesEnAttente = demandesAnnee.Count(d => d.EstEnAttente);
            var joursPrisApprouves = demandesAnnee
                .Where(d => d.Statut == StatusDemande.Approuve)
                .Sum(d => (double)d.NombreJours);

            // Pour le solde, on peut utiliser une règle métier simple
            // (à adapter selon vos règles de gestion)
            var soldeCongesCalcule = CalculerSoldeConges(joursPrisApprouves);

            // Mettre à jour l'interface sur le thread UI
            await Dispatcher.InvokeAsync(() =>
            {
                TxtSoldeCongés.Text = soldeCongesCalcule.ToString("F1");
                TxtDemandesEnAttente.Text = demandesEnAttente.ToString();
                TxtJoursPrisCetteAnnee.Text = joursPrisApprouves.ToString();

                // Adapter le label selon le nombre
                TxtLabelJoursPris.Text = joursPrisApprouves <= 1 ? "jour utilisé" : "jours utilisés";
            });
        }

        private async Task ChargerStatistiquesEquipe(GestionCongesContext context)
        {
            try
            {
                var aujourdhui = DateTime.Today;

                // Compter les collègues absents aujourd'hui (même pôle)
                var absentsAujourdhui = await context.DemandesConges
                    .Include(d => d.Utilisateur)
                    .Where(d => d.Statut == StatusDemande.Approuve &&
                               d.DateDebut <= aujourdhui && d.DateFin >= aujourdhui &&
                               d.Utilisateur.PoleId == _utilisateurConnecte.PoleId &&
                               d.UtilisateurId != _utilisateurConnecte.Id)
                    .CountAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    TxtAbsentsAujourdhui.Text = absentsAujourdhui.ToString();
                });
            }
            catch (Exception)
            {
                // En cas d'erreur, afficher 0
                await Dispatcher.InvokeAsync(() =>
                {
                    TxtAbsentsAujourdhui.Text = "0";
                });
            }
        }

        private async Task ChargerActiviteRecente(GestionCongesContext context)
        {
            try
            {
                // Récupérer les 5 dernières demandes de l'utilisateur
                var activitesRecentes = await context.DemandesConges
                    .Include(d => d.TypeAbsence)
                    .Where(d => d.UtilisateurId == _utilisateurConnecte.Id)
                    .OrderByDescending(d => d.DateModification ?? d.DateCreation)
                    .Take(5)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    AfficherActiviteRecente(activitesRecentes);
                });
            }
            catch (Exception)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    AfficherAucuneActivite();
                });
            }
        }

        private void AfficherActiviteRecente(List<DemandeConge> activites)
        {
            PanelActiviteRecente.Children.Clear();

            if (!activites.Any())
            {
                AfficherAucuneActivite();
                return;
            }

            PanelAucuneActivite.Visibility = Visibility.Collapsed;

            foreach (var activite in activites)
            {
                var elementActivite = CreerElementActivite(activite);
                PanelActiviteRecente.Children.Add(elementActivite);
            }
        }

        private Border CreerElementActivite(DemandeConge demande)
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

            // Barre colorée selon le statut
            var (couleurBarre, iconeStatut) = ObtenirCouleurEtIconeStatut(demande.Statut);
            var colorBar = new Border
            {
                Background = (Brush)FindResource(couleurBarre),
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
                Text = $"{iconeStatut} {demande.StatutLibelle}",
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimary")
            };

            var periode = demande.DateDebut == demande.DateFin
                ? $"Le {demande.DateDebut:dd/MM/yyyy}"
                : $"Du {demande.DateDebut:dd/MM/yyyy} au {demande.DateFin:dd/MM/yyyy}";

            var descText = new TextBlock
            {
                Text = $"{demande.TypeAbsence?.Nom} - {periode} ({demande.NombreJours} jour{(demande.NombreJours > 1 ? "s" : "")})",
                FontSize = 12,
                Foreground = (Brush)FindResource("TextMuted")
            };
            contentStack.Children.Add(titreText);
            contentStack.Children.Add(descText);
            Grid.SetColumn(contentStack, 1);

            // Temps relatif
            var tempsRelatif = CalculerTempsRelatif(demande.DateModification ?? demande.DateCreation);
            var tempsText = new TextBlock
            {
                Text = tempsRelatif,
                FontSize = 11,
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

        private (string couleur, string icone) ObtenirCouleurEtIconeStatut(StatusDemande statut)
        {
            return statut switch
            {
                StatusDemande.Brouillon => ("Gray400", "📝"),
                StatusDemande.EnAttenteValidateur => ("Warning", "⏳"),
                StatusDemande.EnAttenteAdmin => ("Primary", "🔍"),
                StatusDemande.Approuve => ("Secondary", "✅"),
                StatusDemande.Refuse => ("Danger", "❌"),
                StatusDemande.Annule => ("Gray500", "🚫"),
                _ => ("Gray400", "❓")
            };
        }

        private string CalculerTempsRelatif(DateTime date)
        {
            var maintenant = DateTime.Now;
            var difference = maintenant - date;

            if (difference.TotalMinutes < 60)
                return "Il y a quelques minutes";
            if (difference.TotalHours < 24)
                return $"Il y a {(int)difference.TotalHours}h";
            if (difference.TotalDays < 7)
                return $"Il y a {(int)difference.TotalDays} jour{((int)difference.TotalDays > 1 ? "s" : "")}";
            if (difference.TotalDays < 30)
                return $"Il y a {(int)(difference.TotalDays / 7)} semaine{((int)(difference.TotalDays / 7) > 1 ? "s" : "")}";

            return date.ToString("dd/MM/yyyy");
        }

        private void AfficherAucuneActivite()
        {
            PanelActiviteRecente.Children.Clear();
            PanelAucuneActivite.Visibility = Visibility.Visible;
        }

        private double CalculerSoldeConges(double joursPris)
        {
            // Règle simple : 25 jours par an - jours pris
            // À adapter selon vos règles métier
            const double CONGES_ANNUELS_STANDARD = 25.0;
            return Math.Max(0, CONGES_ANNUELS_STANDARD - joursPris);
        }

        private void AfficherValeursParDefaut()
        {
            TxtSoldeCongés.Text = "25.0";
            TxtDemandesEnAttente.Text = "0";
            TxtJoursPrisCetteAnnee.Text = "0";
            TxtAbsentsAujourdhui.Text = "0";
            AfficherAucuneActivite();
        }

        // Méthode publique pour rafraîchir les données
        public void Rafraichir()
        {
            ChargerDonneesDashboard();
        }

        // Gestionnaires d'événements pour les boutons
        private void BtnNouvelleDemande_Click(object sender, RoutedEventArgs e)
        {
            NaviguerVersNouvelleDemandeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnCalendrier_Click(object sender, RoutedEventArgs e)
        {
            NaviguerVersCalendrierRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnMesConges_Click(object sender, RoutedEventArgs e)
        {
            NaviguerVersMesCongesRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnVoirToutActivite_Click(object sender, RoutedEventArgs e)
        {
            NaviguerVersMesCongesRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}