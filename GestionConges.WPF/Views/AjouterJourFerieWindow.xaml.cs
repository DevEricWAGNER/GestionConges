using GestionConges.Core.Data;
using GestionConges.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace GestionConges.WPF.Views
{
    public partial class AjouterJourFerieWindow : Window
    {
        private readonly int _annee;
        public JourFerie? JourFerie { get; private set; }

        public AjouterJourFerieWindow(int annee)
        {
            InitializeComponent();
            _annee = annee;
            InitialiserInterface();
        }

        private void InitialiserInterface()
        {
            // Initialiser la date avec l'année fournie
            DpDate.SelectedDate = new DateTime(_annee, DateTime.Now.Month, DateTime.Now.Day);

            // Limiter la sélection à l'année spécifiée
            DpDate.DisplayDateStart = new DateTime(_annee, 1, 1);
            DpDate.DisplayDateEnd = new DateTime(_annee, 12, 31);

            Title = $"Ajouter un Jour Férié - {_annee}";

            // Focus sur le champ nom
            Loaded += (s, e) => TxtNom.Focus();
        }

        private async void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            if (!ValiderFormulaire())
                return;

            try
            {
                await AjouterJourFerie();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                AfficherMessageErreur($"Erreur lors de l'ajout du jour férié : {ex.Message}");
            }
        }

        private bool ValiderFormulaire()
        {
            // Validation de la date
            if (!DpDate.SelectedDate.HasValue)
            {
                AfficherMessageValidation("Veuillez sélectionner une date.");
                DpDate.Focus();
                return false;
            }

            var dateSelectionnee = DpDate.SelectedDate.Value;

            // Vérifier que la date est bien dans l'année demandée
            if (dateSelectionnee.Year != _annee)
            {
                AfficherMessageValidation($"La date doit être dans l'année {_annee}.");
                DpDate.Focus();
                return false;
            }

            // Vérifier que la date n'est pas dans le passé (sauf si c'est l'année courante)
            if (_annee == DateTime.Now.Year && dateSelectionnee.Date < DateTime.Now.Date)
            {
                var result = MessageBox.Show(
                    "Vous ajoutez un jour férié dans le passé. Voulez-vous continuer ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    DpDate.Focus();
                    return false;
                }
            }

            // Validation du nom
            if (string.IsNullOrWhiteSpace(TxtNom.Text))
            {
                AfficherMessageValidation("Veuillez saisir le nom du jour férié.");
                TxtNom.Focus();
                return false;
            }

            if (TxtNom.Text.Trim().Length < 3)
            {
                AfficherMessageValidation("Le nom du jour férié doit contenir au moins 3 caractères.");
                TxtNom.Focus();
                return false;
            }

            // Validation du type
            if (CmbType.SelectedItem == null)
            {
                AfficherMessageValidation("Veuillez sélectionner un type.");
                CmbType.Focus();
                return false;
            }

            return true;
        }

        private async Task AjouterJourFerie()
        {
            using var context = App.GetService<GestionCongesContext>();

            var dateSelectionnee = DpDate.SelectedDate!.Value;
            var nomJourFerie = TxtNom.Text.Trim();

            // Vérifier si le jour férié existe déjà à cette date
            var jourFerieExistant = await context.JoursFeries
                .FirstOrDefaultAsync(j => j.Date.Date == dateSelectionnee.Date);

            if (jourFerieExistant != null)
            {
                var result = MessageBox.Show(
                    $"Un jour férié existe déjà le {dateSelectionnee:dd/MM/yyyy} :\n" +
                    $"« {jourFerieExistant.Nom} »\n\n" +
                    "Voulez-vous le remplacer ?",
                    "Jour férié existant",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Remplacer le jour férié existant
                    jourFerieExistant.Nom = nomJourFerie;
                    jourFerieExistant.Type = ((ComboBoxItem)CmbType.SelectedItem).Content.ToString() ?? "National";

                    await context.SaveChangesAsync();
                    JourFerie = jourFerieExistant;

                    AfficherNotificationSucces("Jour férié mis à jour avec succès !");
                    return;
                }
                else
                {
                    DpDate.Focus();
                    return;
                }
            }

            // Vérifier si un jour férié avec le même nom existe déjà cette année
            var nomExistant = await context.JoursFeries
                .AnyAsync(j => j.Date.Year == _annee &&
                              j.Nom.ToLower() == nomJourFerie.ToLower());

            if (nomExistant)
            {
                AfficherMessageValidation($"Un jour férié avec le nom « {nomJourFerie} » existe déjà en {_annee}.");
                TxtNom.Focus();
                return;
            }

            // Créer le nouveau jour férié
            JourFerie = new JourFerie
            {
                Date = dateSelectionnee,
                Nom = nomJourFerie,
                Type = ((ComboBoxItem)CmbType.SelectedItem).Content.ToString() ?? "National",
                Actif = true,
                Recurrent = false, // Par défaut, les jours ajoutés manuellement ne sont pas récurrents
                DateCreation = DateTime.Now
            };

            context.JoursFeries.Add(JourFerie);
            await context.SaveChangesAsync();

            AfficherNotificationSucces("Jour férié ajouté avec succès !");
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AfficherMessageValidation(string message)
        {
            MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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