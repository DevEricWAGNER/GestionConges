using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;
using GestionConges.WPF.Views;

namespace GestionConges.WPF.Controls
{
    public partial class MesDemandesUserControl : UserControl
    {
        private readonly Utilisateur _utilisateurConnecte;
        private ObservableCollection<DemandeConge> _demandes;
        private List<DemandeConge> _toutesLesDemandes;
        private DemandeConge? _demandeSelectionnee;

        public MesDemandesUserControl()
        {
            InitializeComponent();

            _utilisateurConnecte = App.UtilisateurConnecte ?? throw new InvalidOperationException("Aucun utilisateur connecté");
            _demandes = new ObservableCollection<DemandeConge>();
            _toutesLesDemandes = new List<DemandeConge>();

            // Utiliser Dispatcher pour s'assurer que tout est initialisé
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitialiserInterface();
                ChargerDemandes();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private GestionCongesContext CreerContexte()
        {
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=GestionCongesDB;Trusted_Connection=true;MultipleActiveResultSets=true";
            var options = new DbContextOptionsBuilder<GestionCongesContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new GestionCongesContext(options);
        }

        private void InitialiserInterface()
        {
            // Vérifier que les contrôles existent avant de les utiliser
            if (DgDemandes != null)
            {
                // Vider la collection Items avant de définir ItemsSource
                DgDemandes.Items.Clear();
                DgDemandes.ItemsSource = _demandes;
            }

            // Remplir le filtre des années seulement si le contrôle existe
            if (CmbFiltreAnnee != null)
            {
                // Vider les items existants
                CmbFiltreAnnee.Items.Clear();

                var anneeCourante = DateTime.Now.Year;
                for (int annee = anneeCourante + 1; annee >= anneeCourante - 3; annee--)
                {
                    CmbFiltreAnnee.Items.Add(new ComboBoxItem { Content = annee.ToString(), Tag = annee });
                }
                CmbFiltreAnnee.Items.Insert(0, new ComboBoxItem { Content = "Toutes les années", Tag = null, IsSelected = true });
                CmbFiltreAnnee.SelectedIndex = 0;
            }

            // Initialiser les statistiques
            MettreAJourStatistiques();
        }

        private async void ChargerDemandes()
        {
            try
            {
                using var context = CreerContexte();

                _toutesLesDemandes = await context.DemandesConges
                    .Include(d => d.TypeAbsence)
                    .Where(d => d.UtilisateurId == _utilisateurConnecte.Id)
                    .OrderByDescending(d => d.DateCreation)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    AppliquerFiltres();
                    MettreAJourStatistiques();
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    AfficherMessageErreur($"Erreur lors du chargement des demandes : {ex.Message}");
                });
            }
        }

        private void AppliquerFiltres()
        {
            var demandesFiltrees = _toutesLesDemandes.AsEnumerable();

            // Filtre par statut
            if (CmbFiltreStatut?.SelectedItem is ComboBoxItem itemStatut && itemStatut.Tag != null)
            {
                var statutFiltre = itemStatut.Tag.ToString();
                switch (statutFiltre)
                {
                    case "Brouillon":
                        demandesFiltrees = demandesFiltrees.Where(d => d.Statut == StatusDemande.Brouillon);
                        break;
                    case "EnAttente":
                        demandesFiltrees = demandesFiltrees.Where(d => d.EstEnAttente);
                        break;
                    case "Approuve":
                        demandesFiltrees = demandesFiltrees.Where(d => d.Statut == StatusDemande.Approuve);
                        break;
                    case "Refuse":
                        demandesFiltrees = demandesFiltrees.Where(d => d.Statut == StatusDemande.Refuse);
                        break;
                }
            }

            // Filtre par année
            if (CmbFiltreAnnee?.SelectedItem is ComboBoxItem itemAnnee && itemAnnee.Tag != null)
            {
                var annee = (int)itemAnnee.Tag;
                demandesFiltrees = demandesFiltrees.Where(d => d.DateDebut.Year == annee || d.DateFin.Year == annee);
            }

            var resultats = demandesFiltrees.ToList();

            _demandes.Clear();
            foreach (var demande in resultats)
            {
                _demandes.Add(demande);
            }
        }

        private void MettreAJourStatistiques()
        {
            if (_demandes == null || TxtNombreDemandes == null || TxtTotalJours == null)
                return;

            var totalDemandes = _demandes.Count;
            var totalJours = _demandes.Sum(d => d.NombreJours);

            TxtNombreDemandes.Text = $"{totalDemandes} demande{(totalDemandes > 1 ? "s" : "")}";
            TxtTotalJours.Text = $"{totalJours} jour{(totalJours > 1 ? "s" : "")} demandé{(totalJours > 1 ? "s" : "")}";
        }

        private void DgDemandes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _demandeSelectionnee = DgDemandes.SelectedItem as DemandeConge;

            if (_demandeSelectionnee != null)
            {
                // Activer les boutons selon le statut de la demande
                var peutModifier = _demandeSelectionnee.Statut == StatusDemande.Brouillon;
                var peutSupprimer = _demandeSelectionnee.Statut == StatusDemande.Brouillon ||
                                   _demandeSelectionnee.Statut == StatusDemande.Refuse;

                BtnModifier.IsEnabled = peutModifier;
                BtnDupliquer.IsEnabled = true;
                BtnSupprimer.IsEnabled = peutSupprimer;
            }
            else
            {
                BtnModifier.IsEnabled = false;
                BtnDupliquer.IsEnabled = false;
                BtnSupprimer.IsEnabled = false;
            }
        }

        private void DgDemandes_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_demandeSelectionnee != null)
            {
                AfficherDetailsDemande(_demandeSelectionnee);
            }
        }

        private void BtnNouvelleDemande_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var nouvelleDemandeWindow = new NouvelleDemandeWindow();
                var result = nouvelleDemandeWindow.ShowDialog();

                if (result == true && nouvelleDemandeWindow.DemandeCreee)
                {
                    ChargerDemandes(); // Recharger la liste
                    AfficherNotificationSucces("Demande créée avec succès !");
                }
            }
            catch (Exception ex)
            {
                AfficherMessageErreur($"Erreur lors de l'ouverture du formulaire : {ex.Message}");
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (_demandeSelectionnee?.Statut == StatusDemande.Brouillon)
            {
                try
                {
                    var modificationWindow = new NouvelleDemandeWindow(_demandeSelectionnee);
                    var result = modificationWindow.ShowDialog();

                    if (result == true && modificationWindow.DemandeCreee)
                    {
                        ChargerDemandes(); // Recharger la liste
                        AfficherNotificationSucces("Demande modifiée avec succès !");
                    }
                }
                catch (Exception ex)
                {
                    AfficherMessageErreur($"Erreur lors de la modification : {ex.Message}");
                }
            }
        }

        private void BtnDupliquer_Click(object sender, RoutedEventArgs e)
        {
            if (_demandeSelectionnee != null)
            {
                try
                {
                    // Créer une nouvelle demande basée sur la sélectionnée
                    var demandeDupliquee = new DemandeConge
                    {
                        TypeAbsenceId = _demandeSelectionnee.TypeAbsenceId,
                        DateDebut = _demandeSelectionnee.DateDebut.AddYears(1), // Proposition : même période l'année suivante
                        DateFin = _demandeSelectionnee.DateFin.AddYears(1),
                        TypeJourneeDebut = _demandeSelectionnee.TypeJourneeDebut,
                        TypeJourneeFin = _demandeSelectionnee.TypeJourneeFin,
                        Commentaire = _demandeSelectionnee.Commentaire
                    };

                    // Charger le type d'absence pour l'affichage
                    using var context = CreerContexte();
                    demandeDupliquee.TypeAbsence = context.TypesAbsences.Find(_demandeSelectionnee.TypeAbsenceId);

                    var modificationWindow = new NouvelleDemandeWindow(demandeDupliquee);
                    var result = modificationWindow.ShowDialog();

                    if (result == true && modificationWindow.DemandeCreee)
                    {
                        ChargerDemandes(); // Recharger la liste
                        AfficherNotificationSucces("Demande dupliquée avec succès !");
                    }
                }
                catch (Exception ex)
                {
                    AfficherMessageErreur($"Erreur lors de la duplication : {ex.Message}");
                }
            }
        }

        private async void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (_demandeSelectionnee != null)
            {
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer cette demande ?\n\n" +
                    $"Type : {_demandeSelectionnee.TypeAbsence?.Nom}\n" +
                    $"Période : du {_demandeSelectionnee.DateDebut:dd/MM/yyyy} au {_demandeSelectionnee.DateFin:dd/MM/yyyy}\n\n" +
                    "Cette action est irréversible.",
                    "Confirmer la suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = CreerContexte();
                        var demandeASupprimer = context.DemandesConges.Find(_demandeSelectionnee.Id);

                        if (demandeASupprimer != null)
                        {
                            context.DemandesConges.Remove(demandeASupprimer);
                            await context.SaveChangesAsync();

                            AfficherNotificationSucces("Demande supprimée avec succès.");
                            ChargerDemandes(); // Recharger la liste
                        }
                    }
                    catch (Exception ex)
                    {
                        AfficherMessageErreur($"Erreur lors de la suppression : {ex.Message}");
                    }
                }
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerDemandes();
        }

        private void CmbFiltreStatut_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_toutesLesDemandes != null && _demandes != null && IsLoaded)
            {
                AppliquerFiltres();
                MettreAJourStatistiques();
            }
        }

        private void CmbFiltreAnnee_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_toutesLesDemandes != null && _demandes != null && IsLoaded)
            {
                AppliquerFiltres();
                MettreAJourStatistiques();
            }
        }

        private void AfficherDetailsDemande(DemandeConge demande)
        {
            try
            {
                var detailsWindow = new DemandeDetailsWindow(demande);
                var result = detailsWindow.ShowDialog();

                // Si une action a été effectuée (modification, suppression), recharger
                if (detailsWindow.ActionEffectuee)
                {
                    ChargerDemandes();
                }
            }
            catch (Exception ex)
            {
                AfficherMessageErreur($"Erreur lors de l'affichage des détails : {ex.Message}");
            }
        }

        // Méthode publique pour rafraîchir depuis l'extérieur
        public void Rafraichir()
        {
            ChargerDemandes();
        }

        // Gestionnaires du menu contextuel
        private void MenuVoirDetails_Click(object sender, RoutedEventArgs e)
        {
            if (_demandeSelectionnee != null)
            {
                AfficherDetailsDemande(_demandeSelectionnee);
            }
        }

        private void MenuModifier_Click(object sender, RoutedEventArgs e)
        {
            BtnModifier_Click(sender, e);
        }

        private void MenuDupliquer_Click(object sender, RoutedEventArgs e)
        {
            BtnDupliquer_Click(sender, e);
        }

        private void MenuSupprimer_Click(object sender, RoutedEventArgs e)
        {
            BtnSupprimer_Click(sender, e);
        }

        private void MenuContextuel_Opened(object sender, RoutedEventArgs e)
        {
            // Activer/désactiver les éléments du menu selon la sélection
            var hasSelection = _demandeSelectionnee != null;
            MenuVoirDetails.IsEnabled = hasSelection;
            MenuDupliquer.IsEnabled = hasSelection;

            if (hasSelection)
            {
                var peutModifier = _demandeSelectionnee.Statut == StatusDemande.Brouillon;
                var peutSupprimer = _demandeSelectionnee.Statut == StatusDemande.Brouillon ||
                                   _demandeSelectionnee.Statut == StatusDemande.Refuse;

                MenuModifier.IsEnabled = peutModifier;
                MenuSupprimer.IsEnabled = peutSupprimer;
            }
            else
            {
                MenuModifier.IsEnabled = false;
                MenuSupprimer.IsEnabled = false;
            }
        }

        private void AfficherMessageErreur(string message)
        {
            MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void AfficherNotificationSucces(string message)
        {
            MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}