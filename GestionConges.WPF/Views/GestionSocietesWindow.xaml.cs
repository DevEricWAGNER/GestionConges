using GestionConges.Core.Data;
using GestionConges.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestionConges.WPF.Views
{
    public partial class GestionSocietesWindow : Window
    {
        private readonly GestionCongesContext _context;
        private ObservableCollection<Societe> _societes;
        private Societe? _societeSelectionnee;
        private bool _modeEdition = false;

        public GestionSocietesWindow()
        {
            InitializeComponent();
            _context = App.GetService<GestionCongesContext>();
            _societes = new ObservableCollection<Societe>();

            InitialiserInterface();
            ChargerDonnees();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            BtnFermer_Click(sender, e);
        }

        private void InitialiserInterface()
        {
            DgSocietes.ItemsSource = _societes;
        }

        private void ChargerDonnees()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Charger les sociétés avec leurs relations
                    var societes = await _context.Societes
                        .Include(s => s.EmployesPrincipaux)
                        .Include(s => s.Equipes)
                        .Include(s => s.UtilisateursSecondaires)
                        .Include(s => s.Validateurs)
                        .OrderBy(s => s.Nom)
                        .ToListAsync();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _societes.Clear();
                        foreach (var societe in societes)
                        {
                            _societes.Add(societe);
                        }
                    });
                }
                catch (Exception ex)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}",
                                      "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        private void DgSocietes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _societeSelectionnee = DgSocietes.SelectedItem as Societe;

            if (_societeSelectionnee != null)
            {
                AfficherDetailsSociete(_societeSelectionnee);
                BtnModifier.IsEnabled = true;
                BtnSupprimer.IsEnabled = _societeSelectionnee.EmployesPrincipaux.Count == 0 &&
                                        _societeSelectionnee.Equipes.Count == 0; // Pas de suppression si des employés ou équipes
                BtnActiver.IsEnabled = true;
            }
            else
            {
                ViderFormulaire();
                BtnModifier.IsEnabled = false;
                BtnSupprimer.IsEnabled = false;
                BtnActiver.IsEnabled = false;
            }
        }

        private void AfficherDetailsSociete(Societe societe)
        {
            TxtNom.Text = societe.Nom;
            TxtDescription.Text = societe.Description ?? string.Empty;
            ChkActif.IsChecked = societe.Actif;

            // Informations supplémentaires
            var infos = $"📅 Créée le : {societe.DateCreation:dd/MM/yyyy HH:mm}\n";
            infos += $"👥 Employés principaux : {societe.EmployesPrincipaux.Count}\n";
            infos += $"🏛️ Équipes : {societe.Equipes.Count}\n";
            infos += $"👤 Utilisateurs secondaires : {societe.UtilisateursSecondaires.Count}\n";
            infos += $"🔐 Validateurs : {societe.Validateurs.Count}\n";
            infos += $"🆔 ID : {societe.Id}";

            if (societe.EmployesPrincipaux.Count > 0)
            {
                infos += "\n\n👥 Quelques employés :\n";
                var employesList = societe.EmployesPrincipaux.ToList();
                foreach (var employe in employesList.Take(5))
                {
                    infos += $"• {employe.NomComplet} ({employe.RoleLibelle})\n";
                }
                if (employesList.Count > 5)
                {
                    infos += $"• ... et {employesList.Count - 5} autre(s)";
                }
            }

            if (societe.Equipes.Count > 0)
            {
                infos += "\n\n🏛️ Équipes :\n";
                foreach (var equipe in societe.Equipes.Take(5))
                {
                    infos += $"• {equipe.Nom}\n";
                }
                if (societe.Equipes.Count > 5)
                {
                    infos += $"• ... et {societe.Equipes.Count - 5} autre(s)";
                }
            }

            TxtInfos.Text = infos;

            // Désactiver les champs par défaut
            DesactiverFormulaire();
        }

        private void ViderFormulaire()
        {
            TxtNom.Clear();
            TxtDescription.Clear();
            ChkActif.IsChecked = true;
            TxtInfos.Text = "Sélectionnez une société pour voir les détails";

            DesactiverFormulaire();
        }

        private void ActiverFormulaire()
        {
            TxtNom.IsEnabled = true;
            TxtDescription.IsEnabled = true;
            ChkActif.IsEnabled = true;
            BtnSauvegarder.IsEnabled = true;
            BtnAnnuler.IsEnabled = true;
            _modeEdition = true;
        }

        private void DesactiverFormulaire()
        {
            TxtNom.IsEnabled = false;
            TxtDescription.IsEnabled = false;
            ChkActif.IsEnabled = false;
            BtnSauvegarder.IsEnabled = false;
            BtnAnnuler.IsEnabled = false;
            _modeEdition = false;
        }

        private void BtnNouvelleSociete_Click(object sender, RoutedEventArgs e)
        {
            _societeSelectionnee = null;
            DgSocietes.SelectedItem = null;

            ViderFormulaire();
            ActiverFormulaire();
            TxtNom.Focus();
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (_societeSelectionnee != null)
            {
                ActiverFormulaire();
                TxtNom.Focus();
            }
        }

        private async void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (_societeSelectionnee != null)
            {
                // Vérifier qu'aucun employé ou équipe n'est assigné
                if (_societeSelectionnee.EmployesPrincipaux.Count > 0 || _societeSelectionnee.Equipes.Count > 0)
                {
                    MessageBox.Show("Impossible de supprimer cette société car des employés ou des équipes y sont encore rattachés.\n\n" +
                                  "Veuillez d'abord réassigner ou supprimer tous les employés et équipes de cette société.",
                                  "Suppression impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer la société '{_societeSelectionnee.Nom}' ?\n\n" +
                    "Cette action est irréversible.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Supprimer directement la société
                        _context.Societes.Remove(_societeSelectionnee);
                        await _context.SaveChangesAsync();

                        await ChargerDonneesAsync();
                        ViderFormulaire();

                        MessageBox.Show("Société supprimée avec succès.", "Succès",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la suppression : {ex.Message}",
                                      "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void BtnActiver_Click(object sender, RoutedEventArgs e)
        {
            if (_societeSelectionnee != null)
            {
                try
                {
                    // Stocker l'ID et le nouvel état avant de modifier
                    int societeId = _societeSelectionnee.Id;
                    bool nouvelEtat = !_societeSelectionnee.Actif;

                    _societeSelectionnee.Actif = nouvelEtat;
                    await _context.SaveChangesAsync();

                    await ChargerDonneesAsync();

                    // Utiliser les valeurs stockées
                    string message = nouvelEtat ? "activée" : "désactivée";
                    MessageBox.Show($"Société {message} avec succès.", "Succès",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Resélectionner la société dans la grille
                    var societeRechargee = _societes.FirstOrDefault(s => s.Id == societeId);
                    if (societeRechargee != null)
                    {
                        DgSocietes.SelectedItem = societeRechargee;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la modification : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnSauvegarder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation des champs
                if (string.IsNullOrWhiteSpace(TxtNom.Text))
                {
                    MessageBox.Show("Le nom de la société est obligatoire.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int societeIdActuelle = _societeSelectionnee != null ? _societeSelectionnee.Id : 0;

                // Vérifier l'unicité du nom
                var nomExiste = await _context.Societes
                    .Where(s => s.Nom == TxtNom.Text.Trim() && s.Id != societeIdActuelle)
                    .AnyAsync();

                if (nomExiste)
                {
                    MessageBox.Show("Ce nom de société est déjà utilisé.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool nouvelleSociete = _societeSelectionnee == null;

                // Créer ou modifier la société
                Societe societe;
                if (nouvelleSociete)
                {
                    societe = new Societe
                    {
                        DateCreation = DateTime.Now
                    };
                    _context.Societes.Add(societe);
                }
                else
                {
                    societe = _societeSelectionnee!;
                }

                // Mettre à jour les propriétés
                societe.Nom = TxtNom.Text.Trim();
                societe.Description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();
                societe.Actif = ChkActif.IsChecked ?? true;

                await _context.SaveChangesAsync();

                ChargerDonnees();
                DesactiverFormulaire();

                string message = nouvelleSociete ? "créée" : "modifiée";
                MessageBox.Show($"Société {message} avec succès.", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            if (_societeSelectionnee != null)
            {
                AfficherDetailsSociete(_societeSelectionnee);
            }
            else
            {
                ViderFormulaire();
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            if (_modeEdition)
            {
                var result = MessageBox.Show("Des modifications sont en cours. Voulez-vous quitter sans sauvegarder ?",
                                           "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;
            }

            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }

        private async Task ChargerDonneesAsync()
        {
            try
            {
                // Charger les sociétés avec leurs relations
                var societes = await _context.Societes
                    .Include(s => s.EmployesPrincipaux)
                    .Include(s => s.Equipes)
                    .Include(s => s.UtilisateursSecondaires)
                    .Include(s => s.Validateurs)
                    .OrderBy(s => s.Nom)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    _societes.Clear();
                    foreach (var societe in societes)
                    {
                        _societes.Add(societe);
                    }
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
    }
}