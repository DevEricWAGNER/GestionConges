using System.Windows;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;

namespace GestionConges.WPF.Views
{
    public partial class DemandeDetailsWindow : Window
    {
        private readonly DemandeConge _demande;
        private readonly bool _modeConsultation;

        public bool ActionEffectuee { get; private set; } = false;

        public DemandeDetailsWindow(DemandeConge demande, bool modeConsultation = false)
        {
            InitializeComponent();
            _demande = demande;
            _modeConsultation = modeConsultation;

            // Initialiser les collections pour éviter les erreurs
            LvValidations.ItemsSource = new List<object>();

            ChargerDetails();
            ConfigurerBoutons();
        }

        private GestionCongesContext CreerContexte()
        {
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=GestionCongesDB;Trusted_Connection=true;MultipleActiveResultSets=true";
            var options = new DbContextOptionsBuilder<GestionCongesContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new GestionCongesContext(options);
        }

        private async void ChargerDetails()
        {
            try
            {
                // Recharger la demande avec toutes ses relations
                using var context = CreerContexte();
                var demandeComplete = await context.DemandesConges
                    .Include(d => d.Utilisateur)
                        .ThenInclude(u => u.Pole)
                    .Include(d => d.TypeAbsence)
                    .Include(d => d.Validations)
                        .ThenInclude(v => v.Validateur)
                    .FirstOrDefaultAsync(d => d.Id == _demande.Id);

                if (demandeComplete != null)
                {
                    AfficherInformations(demandeComplete);
                    await AfficherValidations(demandeComplete);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des détails : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AfficherInformations(DemandeConge demande)
        {
            // Titre et statut
            TxtTitre.Text = $"Demande #{demande.Id}";
            TxtStatut.Text = demande.StatutLibelle.ToUpper();

            // Couleur du statut
            var (couleurFond, couleurTexte) = ObtenirCouleursStatut(demande.Statut);
            BorderStatut.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(couleurFond));
            TxtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(couleurTexte));

            // Informations principales
            if (demande.TypeAbsence != null)
            {
                TxtType.Text = demande.TypeAbsence.Nom;
                BorderTypeColor.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(demande.TypeAbsence.CouleurHex));
            }

            // Formatage de la période
            var periode = $"{demande.DateDebut:dd/MM/yyyy} au {demande.DateFin:dd/MM/yyyy}";
            if (demande.DateDebut == demande.DateFin)
            {
                periode = $"{demande.DateDebut:dd/MM/yyyy}";
                if (demande.TypeJourneeDebut != TypeJournee.JourneeComplete)
                {
                    periode += demande.TypeJourneeDebut == TypeJournee.MatiMatin ? " (matin)" : " (après-midi)";
                }
                else
                {
                    periode += " (journée complète)";
                }
            }
            else
            {
                if (demande.TypeJourneeDebut != TypeJournee.JourneeComplete)
                    periode += demande.TypeJourneeDebut == TypeJournee.MatiMatin ? " (début matin)" : " (début après-midi)";
                if (demande.TypeJourneeFin != TypeJournee.JourneeComplete)
                    periode += demande.TypeJourneeFin == TypeJournee.MatiMatin ? " (fin matin)" : " (fin après-midi)";
            }
            TxtPeriode.Text = periode;

            TxtDuree.Text = $"{demande.NombreJours} jour{(demande.NombreJours > 1 ? "s" : "")}";

            if (demande.Utilisateur != null)
            {
                TxtDemandeur.Text = $"{demande.Utilisateur.NomComplet}";
                if (demande.Utilisateur.Pole != null)
                    TxtDemandeur.Text += $" - {demande.Utilisateur.Pole.Nom}";
            }

            // Commentaire
            if (!string.IsNullOrWhiteSpace(demande.Commentaire))
            {
                BorderCommentaire.Visibility = Visibility.Visible;
                TxtCommentaire.Text = demande.Commentaire;
            }

            // Motif de refus
            if (demande.Statut == StatusDemande.Refuse && !string.IsNullOrWhiteSpace(demande.CommentaireRefus))
            {
                BorderRefus.Visibility = Visibility.Visible;
                TxtMotifRefus.Text = demande.CommentaireRefus;
            }

            // Informations techniques
            TxtId.Text = demande.Id.ToString();
            TxtDateCreation.Text = demande.DateCreation.ToString("dd/MM/yyyy HH:mm");

            if (demande.DateModification.HasValue)
            {
                LblDateModification.Visibility = Visibility.Visible;
                TxtDateModification.Text = demande.DateModification.Value.ToString("dd/MM/yyyy HH:mm");
            }

            if (demande.DateValidationFinale.HasValue)
            {
                LblDateValidation.Visibility = Visibility.Visible;
                TxtDateValidation.Text = demande.DateValidationFinale.Value.ToString("dd/MM/yyyy HH:mm");
            }
        }

        private async Task AfficherValidations(DemandeConge demande)
        {
            if (demande.Validations != null && demande.Validations.Any())
            {
                BorderValidations.Visibility = Visibility.Visible;

                var validationsAffichage = demande.Validations
                    .OrderBy(v => v.OrdreValidation)
                    .Select(v => new
                    {
                        ValidationIcon = v.Approuve ? "✅" : "❌",
                        ValidateurNom = v.Validateur?.NomComplet ?? "Validateur inconnu",
                        Commentaire = v.Commentaire ?? "",
                        CommentaireVisible = !string.IsNullOrWhiteSpace(v.Commentaire) ? Visibility.Visible : Visibility.Collapsed,
                        DateValidation = v.DateValidation.ToString("dd/MM/yyyy HH:mm")
                    })
                    .ToList();

                LvValidations.ItemsSource = validationsAffichage;
            }
            else
            {
                // Aucune validation - laisser la section cachée et une liste vide
                BorderValidations.Visibility = Visibility.Collapsed;
                LvValidations.ItemsSource = new List<object>();
            }
        }

        private (string couleurFond, string couleurTexte) ObtenirCouleursStatut(StatusDemande statut)
        {
            return statut switch
            {
                StatusDemande.Brouillon => ("#9E9E9E", "#FFFFFF"),
                StatusDemande.EnAttenteValidateur => ("#FF9800", "#FFFFFF"),
                StatusDemande.EnAttenteAdmin => ("#2196F3", "#FFFFFF"),
                StatusDemande.Approuve => ("#4CAF50", "#FFFFFF"),
                StatusDemande.Refuse => ("#F44336", "#FFFFFF"),
                StatusDemande.Annule => ("#607D8B", "#FFFFFF"),
                _ => ("#9E9E9E", "#FFFFFF")
            };
        }

        private void ConfigurerBoutons()
        {
            if (_modeConsultation)
            {
                // Mode consultation (pour les validateurs par exemple)
                BtnModifier.Visibility = Visibility.Collapsed;
                BtnSupprimer.Visibility = Visibility.Collapsed;
                return;
            }

            var utilisateurConnecte = App.UtilisateurConnecte;
            if (utilisateurConnecte == null || utilisateurConnecte.Id != _demande.UtilisateurId)
            {
                // Pas le propriétaire de la demande
                BtnModifier.Visibility = Visibility.Collapsed;
                BtnSupprimer.Visibility = Visibility.Collapsed;
                return;
            }

            // Propriétaire de la demande
            var peutModifier = _demande.Statut == StatusDemande.Brouillon;
            var peutSupprimer = _demande.Statut == StatusDemande.Brouillon ||
                               _demande.Statut == StatusDemande.Refuse;

            BtnModifier.Visibility = peutModifier ? Visibility.Visible : Visibility.Collapsed;
            BtnSupprimer.Visibility = peutSupprimer ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var modificationWindow = new NouvelleDemandeWindow(_demande);
                var result = modificationWindow.ShowDialog();

                if (result == true && modificationWindow.DemandeCreee)
                {
                    ActionEffectuee = true;
                    ChargerDetails(); // Recharger les détails mis à jour
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la modification : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDupliquer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Créer une nouvelle demande basée sur celle-ci
                var demandeDupliquee = new DemandeConge
                {
                    TypeAbsenceId = _demande.TypeAbsenceId,
                    DateDebut = _demande.DateDebut.AddYears(1),
                    DateFin = _demande.DateFin.AddYears(1),
                    TypeJourneeDebut = _demande.TypeJourneeDebut,
                    TypeJourneeFin = _demande.TypeJourneeFin,
                    Commentaire = _demande.Commentaire
                };

                // Charger le type d'absence
                using var context = CreerContexte();
                demandeDupliquee.TypeAbsence = context.TypesAbsences.Find(_demande.TypeAbsenceId);

                var nouvelleDemandeWindow = new NouvelleDemandeWindow(demandeDupliquee);
                var result = nouvelleDemandeWindow.ShowDialog();

                if (result == true && nouvelleDemandeWindow.DemandeCreee)
                {
                    ActionEffectuee = true;
                    MessageBox.Show("Demande dupliquée avec succès !", "Succès",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la duplication : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer cette demande ?\n\n" +
                $"Type : {_demande.TypeAbsence?.Nom}\n" +
                $"Période : du {_demande.DateDebut:dd/MM/yyyy} au {_demande.DateFin:dd/MM/yyyy}\n\n" +
                "Cette action est irréversible.",
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using var context = CreerContexte();
                    var demandeASupprimer = context.DemandesConges.Find(_demande.Id);

                    if (demandeASupprimer != null)
                    {
                        context.DemandesConges.Remove(demandeASupprimer);
                        await context.SaveChangesAsync();

                        ActionEffectuee = true;

                        MessageBox.Show("Demande supprimée avec succès.", "Succès",
                                      MessageBoxButton.OK, MessageBoxImage.Information);

                        Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    // Classe helper pour les validations
    public class ValidationAffichage
    {
        public string ValidationIcon { get; set; } = "";
        public string ValidateurNom { get; set; } = "";
        public string Commentaire { get; set; } = "";
        public Visibility CommentaireVisible { get; set; } = Visibility.Collapsed;
        public string DateValidation { get; set; } = "";
    }
}