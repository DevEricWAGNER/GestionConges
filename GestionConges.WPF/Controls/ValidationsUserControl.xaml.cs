using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;
using GestionConges.WPF.Services;
using GestionConges.WPF.Views;

namespace GestionConges.WPF.Controls
{
    public partial class ValidationsUserControl : UserControl
    {
        private readonly Utilisateur _utilisateurConnecte;
        private readonly IValidationService _validationService;
        private ObservableCollection<DemandeValidationViewModel> _demandes;
        private DemandeValidationViewModel? _demandeSelectionnee;

        public ValidationsUserControl()
        {
            InitializeComponent();

            _utilisateurConnecte = App.UtilisateurConnecte ?? throw new InvalidOperationException("Aucun utilisateur connecté");

            // Créer le service de validation
            var context = App.GetService<GestionCongesContext>();
            _validationService = new ValidationService(context);

            _demandes = new ObservableCollection<DemandeValidationViewModel>();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitialiserInterface();
                ChargerDemandesAValider();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void InitialiserInterface()
        {
            if (DgDemandesAValider != null)
            {
                DgDemandesAValider.Items.Clear();
                DgDemandesAValider.ItemsSource = _demandes;
            }

            // Afficher les informations du validateur
            if (TxtValidateurInfo != null)
                TxtValidateurInfo.Text = _utilisateurConnecte.NomComplet;

            if (TxtRoleInfo != null)
                TxtRoleInfo.Text = _utilisateurConnecte.RoleLibelle;
        }

        private async void ChargerDemandesAValider()
        {
            try
            {
                var demandesAValider = await _validationService.ObtenirDemandesAValider(_utilisateurConnecte.Id);

                await Dispatcher.InvokeAsync(() =>
                {
                    _demandes.Clear();

                    foreach (var demande in demandesAValider)
                    {
                        var viewModel = new DemandeValidationViewModel(demande);
                        _demandes.Add(viewModel);
                    }

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

        private void MettreAJourStatistiques()
        {
            if (_demandes == null || TxtNombreEnAttente == null || TxtNombreTotal == null)
                return;

            var nombreEnAttente = _demandes.Count;
            var nombreUrgent = _demandes.Count(d => d.IsUrgent);

            TxtNombreEnAttente.Text = $"{nombreEnAttente} en attente";
            TxtNombreTotal.Text = nombreUrgent > 0 ? $"{nombreUrgent} urgent(s)" : $"{nombreEnAttente} total";
        }

        private void DgDemandesAValider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _demandeSelectionnee = DgDemandesAValider.SelectedItem as DemandeValidationViewModel;

            if (_demandeSelectionnee != null)
            {
                BtnApprouver.IsEnabled = true;
                BtnRefuser.IsEnabled = true;
                BtnVoirDetails.IsEnabled = true;

                if (TxtInfoSelection != null)
                    TxtInfoSelection.Text = $"Demande de {_demandeSelectionnee.Demande.Utilisateur.Prenom} sélectionnée";
            }
            else
            {
                BtnApprouver.IsEnabled = false;
                BtnRefuser.IsEnabled = false;
                BtnVoirDetails.IsEnabled = false;

                if (TxtInfoSelection != null)
                    TxtInfoSelection.Text = "Sélectionnez une demande pour la valider";
            }
        }

        private void DgDemandesAValider_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_demandeSelectionnee != null)
            {
                VoirDetailsDemanede();
            }
        }

        private async void BtnApprouver_Click(object sender, RoutedEventArgs e)
        {
            if (_demandeSelectionnee == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir APPROUVER cette demande ?\n\n" +
                $"Demandeur : {_demandeSelectionnee.Demande.Utilisateur.NomComplet}\n" +
                $"Type : {_demandeSelectionnee.Demande.TypeAbsence.Nom}\n" +
                $"Période : du {_demandeSelectionnee.Demande.DateDebut:dd/MM/yyyy} au {_demandeSelectionnee.Demande.DateFin:dd/MM/yyyy}",
                "Confirmer l'approbation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await ValiderDemande(true);
            }
        }

        private async void BtnRefuser_Click(object sender, RoutedEventArgs e)
        {
            if (_demandeSelectionnee == null) return;

            var commentaire = TxtCommentaireRapide?.Text?.Trim();
            if (string.IsNullOrEmpty(commentaire))
            {
                var inputResult = Microsoft.VisualBasic.Interaction.InputBox(
                    "Veuillez indiquer le motif du refus :",
                    "Motif de refus requis",
                    "");

                if (string.IsNullOrWhiteSpace(inputResult))
                {
                    MessageBox.Show("Un motif de refus est obligatoire.", "Validation",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                commentaire = inputResult.Trim();
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir REFUSER cette demande ?\n\n" +
                $"Demandeur : {_demandeSelectionnee.Demande.Utilisateur.NomComplet}\n" +
                $"Type : {_demandeSelectionnee.Demande.TypeAbsence.Nom}\n" +
                $"Période : du {_demandeSelectionnee.Demande.DateDebut:dd/MM/yyyy} au {_demandeSelectionnee.Demande.DateFin:dd/MM/yyyy}\n\n" +
                $"Motif : {commentaire}",
                "Confirmer le refus",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await ValiderDemande(false, commentaire);
            }
        }

        private async Task ValiderDemande(bool approuve, string? commentaire = null)
        {
            if (_demandeSelectionnee == null) return;

            try
            {
                // Récupérer le commentaire s'il n'est pas fourni
                if (string.IsNullOrEmpty(commentaire) && TxtCommentaireRapide != null)
                {
                    var commentaireText = TxtCommentaireRapide.Text?.Trim();
                    if (commentaireText != "Commentaire optionnel..." && !string.IsNullOrWhiteSpace(commentaireText))
                    {
                        commentaire = commentaireText;
                    }
                }

                var success = await _validationService.ValiderDemande(
                    _demandeSelectionnee.Demande.Id,
                    _utilisateurConnecte.Id,
                    approuve,
                    commentaire);

                if (success)
                {
                    var action = approuve ? "approuvée" : "refusée";
                    MessageBox.Show($"Demande {action} avec succès !", "Validation",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Réinitialiser le commentaire
                    if (TxtCommentaireRapide != null)
                    {
                        TxtCommentaireRapide.Text = "Commentaire optionnel...";
                        TxtCommentaireRapide.Foreground = Brushes.Gray;
                    }

                    // Recharger la liste
                    ChargerDemandesAValider();
                }
                else
                {
                    MessageBox.Show("Erreur lors de la validation de la demande.", "Erreur",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la validation : {ex.Message}", "Erreur",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnVoirDetails_Click(object sender, RoutedEventArgs e)
        {
            VoirDetailsDemanede();
        }

        private void VoirDetailsDemanede()
        {
            if (_demandeSelectionnee != null)
            {
                try
                {
                    var detailsWindow = new DemandeDetailsWindow(_demandeSelectionnee.Demande, modeConsultation: true);
                    detailsWindow.ShowDialog();

                    // Recharger au cas où il y aurait eu des changements
                    ChargerDemandesAValider();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'affichage des détails : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerDemandesAValider();
        }

        // Méthode publique pour rafraîchir depuis l'extérieur
        public void Rafraichir()
        {
            ChargerDemandesAValider();
        }

        // Gestion du placeholder pour le commentaire
        private void TxtCommentaireRapide_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtCommentaireRapide.Text == "Commentaire optionnel..." && TxtCommentaireRapide.Foreground == Brushes.Gray)
            {
                TxtCommentaireRapide.Text = "";
                TxtCommentaireRapide.Foreground = Brushes.Black;
            }
        }

        private void TxtCommentaireRapide_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCommentaireRapide.Text))
            {
                TxtCommentaireRapide.Text = "Commentaire optionnel...";
                TxtCommentaireRapide.Foreground = Brushes.Gray;
            }
        }
    }

    // ViewModel pour les demandes avec propriétés calculées
    public class DemandeValidationViewModel
    {
        public DemandeConge Demande { get; }
        public bool IsUrgent { get; }
        public string AncienneteTexte { get; }
        public Brush AncienneteCouleur { get; }

        public DemandeValidationViewModel(DemandeConge demande)
        {
            Demande = demande;

            // Calculer l'ancienneté
            var anciennete = DateTime.Now - demande.DateCreation;
            IsUrgent = anciennete.TotalDays > 3;

            if (anciennete.TotalDays < 1)
            {
                AncienneteTexte = "Aujourd'hui";
                AncienneteCouleur = Brushes.Green;
            }
            else if (anciennete.TotalDays < 2)
            {
                AncienneteTexte = "Hier";
                AncienneteCouleur = Brushes.Orange;
            }
            else if (anciennete.TotalDays < 7)
            {
                AncienneteTexte = $"{(int)anciennete.TotalDays} jours";
                AncienneteCouleur = IsUrgent ? Brushes.Red : Brushes.Orange;
            }
            else
            {
                AncienneteTexte = $"{(int)anciennete.TotalDays} jours";
                AncienneteCouleur = Brushes.Red;
            }
        }
    }
}