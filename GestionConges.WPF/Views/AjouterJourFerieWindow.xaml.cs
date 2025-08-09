using GestionConges.Core.Data;
using GestionConges.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace GestionConges.WPF.Views
{
    public partial class AjouterJourFerieWindow : Window
    {
        public JourFerie? JourFerie { get; private set; }

        public AjouterJourFerieWindow(int annee)
        {
            InitializeComponent();

            // Initialiser la date avec l'année fournie
            DpDate.SelectedDate = new DateTime(annee, 1, 1);
            Title = $"Ajouter un Jour Férié - {annee}";
        }

        private async void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (!DpDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Veuillez sélectionner une date.", "Validation",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtNom.Text))
            {
                MessageBox.Show("Veuillez saisir le nom du jour férié.", "Validation",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNom.Focus();
                return;
            }

            try
            {
                using var context = App.GetService<GestionCongesContext>();

                // Vérifier si le jour férié existe déjà
                var dateExiste = await context.JoursFeries
                    .AnyAsync(j => j.Date.Date == DpDate.SelectedDate.Value.Date);

                if (dateExiste)
                {
                    MessageBox.Show("Un jour férié existe déjà à cette date.", "Validation",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Créer le nouveau jour férié
                JourFerie = new JourFerie
                {
                    Date = DpDate.SelectedDate.Value,
                    Nom = TxtNom.Text.Trim(),
                    Type = ((ComboBoxItem)CmbType.SelectedItem).Content.ToString() ?? "National",
                    Actif = true,
                    Recurrent = false, // Par défaut, les jours ajoutés manuellement ne sont pas récurrents
                    DateCreation = DateTime.Now
                };

                context.JoursFeries.Add(JourFerie);
                await context.SaveChangesAsync();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout du jour férié : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}