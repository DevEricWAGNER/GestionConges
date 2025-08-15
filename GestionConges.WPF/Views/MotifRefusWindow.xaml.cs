using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GestionConges.Core.Models;

namespace GestionConges.WPF.Views
{
    public partial class MotifRefusWindow : Window
    {
        public string MotifRefus { get; private set; } = string.Empty;

        public MotifRefusWindow(DemandeConge demande)
        {
            InitializeComponent();
            ChargerInformationsDemande(demande);
            InitialiserInterface();
        }

        private void ChargerInformationsDemande(DemandeConge demande)
        {
            // Informations du demandeur
            TxtDemandeur.Text = $"{demande.Utilisateur?.NomComplet ?? "Utilisateur inconnu"}";
            if (!string.IsNullOrEmpty(demande.Utilisateur?.Pole?.Nom))
            {
                TxtDemandeur.Text += $" - {demande.Utilisateur.Pole.Nom}";
            }

            // Type d'absence avec couleur
            if (demande.TypeAbsence != null)
            {
                TxtTypeAbsence.Text = demande.TypeAbsence.Nom;
                BorderCouleurType.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(demande.TypeAbsence.CouleurHex));
            }

            // Période
            var periode = $"{demande.DateDebut:dd/MM/yyyy} au {demande.DateFin:dd/MM/yyyy}";
            if (demande.DateDebut == demande.DateFin)
            {
                periode = $"{demande.DateDebut:dd/MM/yyyy}";
                // Ajouter le type de journée si applicable
                if (demande.TypeJourneeDebut != Core.Enums.TypeJournee.JourneeComplete)
                {
                    periode += demande.TypeJourneeDebut == Core.Enums.TypeJournee.MatiMatin
                        ? " (matin)" : " (après-midi)";
                }
            }
            TxtPeriode.Text = periode;

            // Durée
            TxtDuree.Text = $"{demande.NombreJours} jour{(demande.NombreJours > 1 ? "s" : "")}";
        }

        private void InitialiserInterface()
        {
            // Initialiser le compteur de caractères
            TxtMotifRefus.TextChanged += (s, e) =>
            {
                var longueur = TxtMotifRefus.Text.Length;
                TxtCompteurCaracteres.Text = $"{longueur}/1000 caractères";

                // Changer la couleur si on approche de la limite
                if (longueur > 900)
                    TxtCompteurCaracteres.Foreground = Brushes.Red;
                else if (longueur > 750)
                    TxtCompteurCaracteres.Foreground = Brushes.Orange;
                else
                    TxtCompteurCaracteres.Foreground = Brushes.Gray;

                // Activer/désactiver le bouton selon la saisie
                BtnConfirmerRefus.IsEnabled = longueur > 0;
            };

            // Désactiver le bouton par défaut
            BtnConfirmerRefus.IsEnabled = false;

            // S'assurer que la TextBox est focusable et focusée
            TxtMotifRefus.IsTabStop = true;
            TxtMotifRefus.Focusable = true;

            // Mettre le focus après que la fenêtre soit complètement chargée
            this.Loaded += (s, e) =>
            {
                TxtMotifRefus.Focus();
                Keyboard.Focus(TxtMotifRefus);
            };
        }

        private void BtnMotifPredéfini_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button bouton && !string.IsNullOrEmpty(bouton.Content?.ToString()))
            {
                var motifPredéfini = bouton.Content.ToString();

                // Ajouter au texte existant ou remplacer si vide
                if (string.IsNullOrWhiteSpace(TxtMotifRefus.Text))
                {
                    TxtMotifRefus.Text = motifPredéfini;
                }
                else
                {
                    // Ajouter avec un point-virgule si il y a déjà du texte
                    if (!TxtMotifRefus.Text.EndsWith(".") && !TxtMotifRefus.Text.EndsWith(";"))
                    {
                        TxtMotifRefus.Text += "; ";
                    }
                    else
                    {
                        TxtMotifRefus.Text += " ";
                    }
                    TxtMotifRefus.Text += motifPredéfini;
                }

                // Mettre le focus à la fin du texte
                TxtMotifRefus.CaretIndex = TxtMotifRefus.Text.Length;
                TxtMotifRefus.Focus();
            }
        }

        private void BtnConfirmerRefus_Click(object sender, RoutedEventArgs e)
        {
            var motif = TxtMotifRefus.Text?.Trim();

            if (string.IsNullOrWhiteSpace(motif))
            {
                MessageBox.Show("Veuillez saisir un motif de refus.", "Motif requis",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtMotifRefus.Focus();
                return;
            }

            if (motif.Length < 10)
            {
                var result = MessageBox.Show(
                    "Le motif semble très court. Êtes-vous sûr de vouloir continuer ?\n\n" +
                    "Un motif détaillé aide le demandeur à mieux comprendre le refus.",
                    "Motif court",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    TxtMotifRefus.Focus();
                    return;
                }
            }

            // Confirmation finale
            var confirmation = MessageBox.Show(
                $"Êtes-vous sûr de vouloir refuser cette demande avec le motif suivant ?\n\n" +
                $"« {motif} »\n\n" +
                "Cette action est définitive.",
                "Confirmer le refus",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation == MessageBoxResult.Yes)
            {
                MotifRefus = motif;
                DialogResult = true;
                Close();
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            // Vérifier si l'utilisateur a commencé à taper
            if (!string.IsNullOrWhiteSpace(TxtMotifRefus.Text))
            {
                var result = MessageBox.Show(
                    "Vous avez commencé à saisir un motif. Voulez-vous vraiment annuler ?",
                    "Annuler la saisie",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            DialogResult = false;
            Close();
        }

        // Permettre la fermeture avec Escape
        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                BtnAnnuler_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }
    }
}