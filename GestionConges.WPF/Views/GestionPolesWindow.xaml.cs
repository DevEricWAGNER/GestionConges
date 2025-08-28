using GestionConges.Core.Data;
using GestionConges.Core.Enums;
using GestionConges.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestionConges.WPF.Views
{
    public partial class GestionPolesWindow : Window
    {
        private readonly GestionCongesContext _context;
        private ObservableCollection<Pole> _poles;
        private ObservableCollection<Equipe> _equipesDisponibles;
        private Pole? _poleSelectionne;
        private bool _modeEdition = false;

        public GestionPolesWindow()
        {
            InitializeComponent();
            _context = App.GetService<GestionCongesContext>();
            _poles = new ObservableCollection<Pole>();
            _equipesDisponibles = new ObservableCollection<Equipe>();

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
            DgPoles.ItemsSource = _poles;
            CmbEquipes.ItemsSource = _equipesDisponibles;
        }

        private void ChargerDonnees()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Charger les pôles avec leur équipe et société
                    var poles = await _context.Poles
                        .Include(p => p.Equipe)
                            .ThenInclude(e => e.Societe)
                        .Include(p => p.Employes)
                        .OrderBy(p => p.Equipe.Societe.Nom)
                        .ThenBy(p => p.Equipe.Nom)
                        .ThenBy(p => p.Nom)
                        .ToListAsync();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _poles.Clear();
                        foreach (var pole in poles)
                        {
                            _poles.Add(pole);
                        }
                    });

                    // Charger les équipes disponibles
                    var equipesDisponibles = await _context.Equipes
                        .Include(e => e.Societe)
                        .Where(e => e.Actif)
                        .OrderBy(e => e.Societe.Nom)
                        .ThenBy(e => e.Nom)
                        .ToListAsync();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _equipesDisponibles.Clear();
                        foreach (var equipe in equipesDisponibles)
                        {
                            _equipesDisponibles.Add(equipe);
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

        private void DgPoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _poleSelectionne = DgPoles.SelectedItem as Pole;

            if (_poleSelectionne != null)
            {
                AfficherDetailsPole(_poleSelectionne);
                BtnModifier.IsEnabled = true;
                BtnSupprimer.IsEnabled = _poleSelectionne.Employes.Count == 0; // Pas de suppression si des employés
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

        private void AfficherDetailsPole(Pole pole)
        {
            TxtNom.Text = pole.Nom;
            TxtDescription.Text = pole.Description ?? string.Empty;
            ChkActif.IsChecked = pole.Actif;
            CmbEquipes.SelectedItem = pole.Equipe;

            // Informations supplémentaires
            var infos = $"📅 Créé le : {pole.DateCreation:dd/MM/yyyy HH:mm}\n";
            infos += $"👥 Nombre d'employés : {pole.Employes.Count}\n";
            infos += $"🏛️ Équipe : {pole.Equipe?.Nom} ({pole.Equipe?.Societe?.Nom})\n";
            infos += $"🆔 ID : {pole.Id}";

            if (pole.Employes.Count > 0)
            {
                infos += "\n\n👥 Employés dans ce pôle :\n";
                var employesList = pole.Employes.ToList();
                foreach (var employe in employesList.Take(5))
                {
                    infos += $"• {employe.NomComplet} ({employe.RoleLibelle})\n";
                }
                if (employesList.Count > 5)
                {
                    infos += $"• ... et {employesList.Count - 5} autre(s)";
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
            CmbEquipes.SelectedItem = null;
            TxtInfos.Text = "Sélectionnez un pôle pour voir les détails";

            DesactiverFormulaire();
        }

        private void ActiverFormulaire()
        {
            TxtNom.IsEnabled = true;
            TxtDescription.IsEnabled = true;
            ChkActif.IsEnabled = true;
            CmbEquipes.IsEnabled = true;
            BtnSauvegarder.IsEnabled = true;
            BtnAnnuler.IsEnabled = true;
            _modeEdition = true;
        }

        private void DesactiverFormulaire()
        {
            TxtNom.IsEnabled = false;
            TxtDescription.IsEnabled = false;
            ChkActif.IsEnabled = false;
            CmbEquipes.IsEnabled = false;
            BtnSauvegarder.IsEnabled = false;
            BtnAnnuler.IsEnabled = false;
            _modeEdition = false;
        }

        private void BtnNouveauPole_Click(object sender, RoutedEventArgs e)
        {
            _poleSelectionne = null;
            DgPoles.SelectedItem = null;

            ViderFormulaire();
            ActiverFormulaire();
            TxtNom.Focus();
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (_poleSelectionne != null)
            {
                ActiverFormulaire();
                TxtNom.Focus();
            }
        }

        private async void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (_poleSelectionne != null)
            {
                // Vérifier qu'aucun employé n'est assigné
                if (_poleSelectionne.Employes.Count > 0)
                {
                    MessageBox.Show("Impossible de supprimer ce pôle car des employés y sont encore assignés.\n\n" +
                                  "Veuillez d'abord réassigner ou supprimer tous les employés de ce pôle.",
                                  "Suppression impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer le pôle '{_poleSelectionne.Nom}' ?\n\n" +
                    $"Ce pôle appartient à l'équipe '{_poleSelectionne.Equipe?.Nom}' de la société '{_poleSelectionne.Equipe?.Societe?.Nom}'.\n" +
                    "Cette action est irréversible.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Supprimer directement le pôle
                        _context.Poles.Remove(_poleSelectionne);
                        await _context.SaveChangesAsync();

                        await ChargerDonneesAsync();
                        ViderFormulaire();

                        MessageBox.Show("Pôle supprimé avec succès.", "Succès",
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
            if (_poleSelectionne != null)
            {
                try
                {
                    // Stocker l'ID et le nouvel état avant de modifier
                    int poleId = _poleSelectionne.Id;
                    bool nouvelEtat = !_poleSelectionne.Actif;

                    _poleSelectionne.Actif = nouvelEtat;
                    await _context.SaveChangesAsync();

                    await ChargerDonneesAsync();

                    // Utiliser les valeurs stockées
                    string message = nouvelEtat ? "activé" : "désactivé";
                    MessageBox.Show($"Pôle {message} avec succès.", "Succès",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Resélectionner le pôle dans la grille
                    var poleRecharge = _poles.FirstOrDefault(p => p.Id == poleId);
                    if (poleRecharge != null)
                    {
                        DgPoles.SelectedItem = poleRecharge;
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
                    MessageBox.Show("Le nom du pôle est obligatoire.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CmbEquipes.SelectedItem is not Equipe equipeSelectionnee)
                {
                    MessageBox.Show("Veuillez sélectionner une équipe.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int poleIdActuel = _poleSelectionne != null ? _poleSelectionne.Id : 0;

                // Vérifier l'unicité du nom dans l'équipe
                var nomExiste = await _context.Poles
                    .Where(p => p.Nom == TxtNom.Text.Trim()
                             && p.EquipeId == equipeSelectionnee.Id
                             && p.Id != poleIdActuel)
                    .AnyAsync();

                if (nomExiste)
                {
                    MessageBox.Show("Ce nom de pôle est déjà utilisé dans cette équipe.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool nouveauPole = _poleSelectionne == null;

                // Créer ou modifier le pôle
                Pole pole;
                if (nouveauPole)
                {
                    pole = new Pole
                    {
                        DateCreation = DateTime.Now
                    };
                    _context.Poles.Add(pole);
                }
                else
                {
                    pole = _poleSelectionne!;
                }

                // Mettre à jour les propriétés
                pole.Nom = TxtNom.Text.Trim();
                pole.Description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();
                pole.Actif = ChkActif.IsChecked ?? true;
                pole.EquipeId = equipeSelectionnee.Id;

                await _context.SaveChangesAsync();

                ChargerDonnees();
                DesactiverFormulaire();

                string message = nouveauPole ? "créé" : "modifié";
                MessageBox.Show($"Pôle {message} avec succès.", "Succès",
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
            if (_poleSelectionne != null)
            {
                AfficherDetailsPole(_poleSelectionne);
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
                // Charger les pôles avec leur équipe et société
                var poles = await _context.Poles
                    .Include(p => p.Equipe)
                        .ThenInclude(e => e.Societe)
                    .Include(p => p.Employes)
                    .OrderBy(p => p.Equipe.Societe.Nom)
                    .ThenBy(p => p.Equipe.Nom)
                    .ThenBy(p => p.Nom)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    _poles.Clear();
                    foreach (var pole in poles)
                    {
                        _poles.Add(pole);
                    }
                });

                // Charger les équipes disponibles
                var equipesDisponibles = await _context.Equipes
                    .Include(e => e.Societe)
                    .Where(e => e.Actif)
                    .OrderBy(e => e.Societe.Nom)
                    .ThenBy(e => e.Nom)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    _equipesDisponibles.Clear();
                    foreach (var equipe in equipesDisponibles)
                    {
                        _equipesDisponibles.Add(equipe);
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