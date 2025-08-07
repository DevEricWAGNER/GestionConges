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

            InitialiserInterface();
            ChargerDemandes();
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
            DgDemandes.ItemsSource = _demandes;

            // Remplir le filtre des années
            var anneeCourante = DateTime.Now.Year;
            for (int annee = anneeCourante + 1; annee >= anneeCourante - 3; annee--)
            {
                CmbFiltreAnnee.Items.Add(new ComboBoxItem { Content = annee.ToString(), Tag = annee });
            }
            CmbFiltreAnnee.Items.Insert(0, new ComboBoxItem { Content = "Toutes les années", Tag = null, IsSelected = true });
            CmbFiltreAnnee.SelectedIndex = 0;
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
                    MessageBox.Show($"Erreur lors du chargement des demandes : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void AppliquerFiltres()
        {
            var demandesFiltrees = _toutesLesDemandes.AsEnumerable();

            // Filtre par statut
            if (CmbFiltreStatut.SelectedItem is ComboBoxItem itemStatut && itemStatut.Tag != null)
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
            if (CmbFiltreAnnee.SelectedItem is ComboBoxItem itemAnnee && itemAnnee.Tag != null)
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du formulaire : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la modification : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la duplication : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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

                            MessageBox.Show("Demande supprimée avec succès.", "Succès",
                                          MessageBoxButton.OK, MessageBoxImage.Information);

                            ChargerDemandes(); // Recharger la liste
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la suppression : {ex.Message}",
                                      "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (_toutesLesDemandes != null)
            {
                AppliquerFiltres();
                MettreAJourStatistiques();
            }
        }

        private void CmbFiltreAnnee_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_toutesLesDemandes != null)
            {
                AppliquerFiltres();
                MettreAJourStatistiques();
            }
        }

        private void AfficherDetailsDemande(DemandeConge demande)
        {
            var details = $"📋 DÉTAILS DE LA DEMANDE\n\n";
            details += $"🏷️ Type : {demande.TypeAbsence?.Nom}\n";
            details += $"📅 Période : du {demande.DateDebut:dd/MM/yyyy} au {demande.DateFin:dd/MM/yyyy}\n";
            details += $"⏱️ Durée : {demande.NombreJours} jour(s)\n";
            details += $"📊 Statut : {demande.StatutLibelle}\n";
            details += $"📝 Créée le : {demande.DateCreation:dd/MM/yyyy HH:mm}\n";

            if (demande.DateModification.HasValue)
                details += $"✏️ Modifiée le : {demande.DateModification:dd/MM/yyyy HH:mm}\n";

            if (demande.DateValidationFinale.HasValue)
                details += $"✅ Validée le : {demande.DateValidationFinale:dd/MM/yyyy HH:mm}\n";

            if (!string.IsNullOrWhiteSpace(demande.Commentaire))
                details += $"\n💬 Commentaire :\n{demande.Commentaire}";

            if (!string.IsNullOrWhiteSpace(demande.CommentaireRefus))
                details += $"\n❌ Motif de refus :\n{demande.CommentaireRefus}";

            MessageBox.Show(details, "Détails de la demande", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Méthode publique pour rafraîchir depuis l'extérieur
        public void Rafraichir()
        {
            ChargerDemandes();
        }
    }
}