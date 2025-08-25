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
                // DEBUG: Vérifier l'ID utilisateur
                System.Diagnostics.Debug.WriteLine($"ChargerDemandesAValider appelé pour utilisateur ID: {_utilisateurConnecte.Id}");
                System.Diagnostics.Debug.WriteLine($"   Nom: {_utilisateurConnecte.NomComplet}");
                System.Diagnostics.Debug.WriteLine($"   Rôle: {_utilisateurConnecte.Role}");

                var demandesAValider = await _validationService.ObtenirDemandesAValider(_utilisateurConnecte.Id);

                await Dispatcher.InvokeAsync(() =>
                {
                    _demandes.Clear();

                    System.Diagnostics.Debug.WriteLine($"Demandes reçues du service: {demandesAValider.Count}");

                    foreach (var demande in demandesAValider)
                    {
                        System.Diagnostics.Debug.WriteLine($"   Ajout demande ID {demande.Id} de {demande.Utilisateur?.NomComplet}");
                        var viewModel = new DemandeValidationViewModel(demande);
                        _demandes.Add(viewModel);
                    }

                    System.Diagnostics.Debug.WriteLine($"ObservableCollection final: {_demandes.Count} éléments");
                    MettreAJourStatistiques();
                    GererAffichageVide();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans ChargerDemandesAValider : {ex.Message}");
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des demandes : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void MettreAJourStatistiques()
        {
            if (_demandes == null) return;

            var nombreEnAttente = _demandes.Count;
            var nombreUrgent = _demandes.Count(d => d.IsUrgent);

            // Mise à jour des statistiques avec le nouveau design
            if (TxtNombreEnAttente != null)
                TxtNombreEnAttente.Text = nombreEnAttente.ToString();

            if (TxtNombreUrgent != null)
                TxtNombreUrgent.Text = nombreUrgent.ToString();

            if (TxtNombreTotal != null)
                TxtNombreTotal.Text = nombreEnAttente.ToString(); // ou calculer le total mensuel
        }

        private void GererAffichageVide()
        {
            if (EmptyStatePanel != null && DgDemandesAValider != null)
            {
                bool hasData = _demandes != null && _demandes.Count > 0;

                EmptyStatePanel.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;

                // Accéder correctement au parent Border du DataGrid
                if (DgDemandesAValider.Parent is Grid parentGrid && parentGrid.Parent is Border parentBorder)
                {
                    parentBorder.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
                }
            }
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

            // Utiliser la fenêtre personnalisée pour le motif de refus
            try
            {
                var motifRefusWindow = new MotifRefusWindow(_demandeSelectionnee.Demande);
                var result = motifRefusWindow.ShowDialog();

                if (result == true && !string.IsNullOrWhiteSpace(motifRefusWindow.MotifRefus))
                {
                    await ValiderDemande(false, motifRefusWindow.MotifRefus);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre de refus : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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

                    // Afficher notification moderne
                    AfficherNotificationSucces($"Demande {action} avec succès !");

                    // Réinitialiser le commentaire
                    if (TxtCommentaireRapide != null)
                    {
                        TxtCommentaireRapide.Text = "Commentaire optionnel...";
                        TxtCommentaireRapide.Foreground = (Brush)Application.Current.Resources["TextMuted"];
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
            try
            {
                var textMutedBrush = (Brush)Application.Current.Resources["TextMuted"];
                if (TxtCommentaireRapide.Text == "Commentaire optionnel..." &&
                    TxtCommentaireRapide.Foreground.ToString() == textMutedBrush.ToString())
                {
                    TxtCommentaireRapide.Text = "";
                    TxtCommentaireRapide.Foreground = (Brush)Application.Current.Resources["TextPrimary"];
                }
            }
            catch
            {
                // Fallback si les ressources ne sont pas disponibles
                if (TxtCommentaireRapide.Text == "Commentaire optionnel...")
                {
                    TxtCommentaireRapide.Text = "";
                    TxtCommentaireRapide.Foreground = Brushes.Black;
                }
            }
        }

        private void TxtCommentaireRapide_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtCommentaireRapide.Text))
            {
                TxtCommentaireRapide.Text = "Commentaire optionnel...";
                try
                {
                    TxtCommentaireRapide.Foreground = (Brush)Application.Current.Resources["TextMuted"];
                }
                catch
                {
                    TxtCommentaireRapide.Foreground = Brushes.Gray;
                }
            }
        }

        // Nouvelle méthode pour afficher une notification moderne
        private void AfficherNotificationSucces(string message)
        {
            try
            {
                // Créer une notification toast moderne
                var notification = new Border
                {
                    Background = (Brush)Application.Current.Resources["Secondary"],
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(20, 16, 20, 16),
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
                    Margin = new Thickness(0, 0, 12, 0),
                    FontSize = 16
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

                // Trouver le parent Grid pour ajouter la notification
                var parent = this.Parent;
                while (parent != null && !(parent is Grid))
                {
                    parent = ((FrameworkElement)parent).Parent;
                }

                if (parent is Grid mainGrid)
                {
                    mainGrid.Children.Add(notification);

                    // Animation d'apparition et disparition
                    var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                    var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
                    {
                        BeginTime = TimeSpan.FromSeconds(3)
                    };

                    fadeOut.Completed += (s, e) => mainGrid.Children.Remove(notification);

                    notification.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                    notification.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }
            }
            catch (Exception ex)
            {
                // Fallback : MessageBox simple si la notification échoue
                MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Debug.WriteLine($"Erreur notification: {ex.Message}");
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

        // Propriétés pour binding direct avec setters privés (éviter les problèmes de binding bidirectionnel)
        public Utilisateur Utilisateur { get; private set; }
        public TypeAbsence TypeAbsence { get; private set; }
        public DateTime DateDebut { get; private set; }
        public DateTime DateFin { get; private set; }
        public decimal NombreJours { get; private set; }
        public string Commentaire { get; private set; }
        public DateTime DateCreation { get; private set; }

        public DemandeValidationViewModel(DemandeConge demande)
        {
            Demande = demande ?? throw new ArgumentNullException(nameof(demande));

            // Initialiser les propriétés pour éviter les erreurs de binding
            Utilisateur = demande.Utilisateur;
            TypeAbsence = demande.TypeAbsence;
            DateDebut = demande.DateDebut;
            DateFin = demande.DateFin;
            NombreJours = demande.NombreJours;
            Commentaire = demande.Commentaire ?? "";
            DateCreation = demande.DateCreation;

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

            // Debug
            System.Diagnostics.Debug.WriteLine($"ViewModel créé pour demande ID {demande.Id}");
            System.Diagnostics.Debug.WriteLine($"  Utilisateur: {Utilisateur?.NomComplet}");
            System.Diagnostics.Debug.WriteLine($"  TypeAbsence: {TypeAbsence?.Nom}");
        }
    }
}