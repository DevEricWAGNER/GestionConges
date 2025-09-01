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
    public partial class GestionEquipesWindow : Window
    {
        private readonly GestionCongesContext _context;
        private ObservableCollection<Equipe> _equipes;
        private ObservableCollection<Societe> _societesDisponibles;
        private Equipe? _equipeSelectionnee;
        private bool _modeEdition = false;

        public GestionEquipesWindow()
        {
            InitializeComponent();
            _context = App.GetService<GestionCongesContext>();
            _equipes = new ObservableCollection<Equipe>();
            _societesDisponibles = new ObservableCollection<Societe>();

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
            DgEquipes.ItemsSource = _equipes;
            CmbSocietes.ItemsSource = _societesDisponibles;
        }

        private void ChargerDonnees()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Charger les équipes avec leur société
                    var equipes = await _context.Equipes
                        .Include(e => e.Societe)
                        .Include(e => e.Employes)
                        .Include(e => e.Poles)
                        .OrderBy(e => e.Societe.Nom)
                        .ThenBy(e => e.Nom)
                        .ToListAsync();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _equipes.Clear();
                        foreach (var equipe in equipes)
                        {
                            _equipes.Add(equipe);
                        }
                    });

                    // Charger les sociétés disponibles
                    var societesDisponibles = await _context.Societes
                        .Where(s => s.Actif)
                        .OrderBy(s => s.Nom)
                        .ToListAsync();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _societesDisponibles.Clear();
                        foreach (var societe in societesDisponibles)
                        {
                            _societesDisponibles.Add(societe);
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

        private void DgEquipes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _equipeSelectionnee = DgEquipes.SelectedItem as Equipe;

            if (_equipeSelectionnee != null)
            {
                AfficherDetailsEquipe(_equipeSelectionnee);
                BtnModifier.IsEnabled = true;
                BtnSupprimer.IsEnabled = _equipeSelectionnee.Employes.Count == 0 && _equipeSelectionnee.Poles.Count == 0; // Pas de suppression si des employés ou des pôles
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

        private void AfficherDetailsEquipe(Equipe equipe)
        {
            TxtNom.Text = equipe.Nom;
            TxtDescription.Text = equipe.Description ?? string.Empty;
            ChkActif.IsChecked = equipe.Actif;
            CmbSocietes.SelectedItem = equipe.Societe;

            // Informations supplémentaires
            var infos = $"📅 Créé le : {equipe.DateCreation:dd/MM/yyyy HH:mm}\n";
            infos += $"👥 Nombre d'employés : {equipe.Employes.Count}\n";
            infos += $"📋 Nombre de pôles : {equipe.Poles.Count}\n";
            infos += $"🏢 Société : {equipe.Societe?.Nom}\n";
            infos += $"🆔 ID : {equipe.Id}";

            if (equipe.Poles.Count > 0)
            {
                infos += "\n\n📋 Pôles dans cette équipe :\n";
                var polesList = equipe.Poles.ToList();
                foreach (var pole in polesList.Take(5))
                {
                    infos += $"• {pole.Nom} ({pole.Employes.Count} employés)\n";
                }
                if (polesList.Count > 5)
                {
                    infos += $"• ... et {polesList.Count - 5} autre(s)";
                }
            }

            if (equipe.Employes.Count > 0)
            {
                infos += "\n\n👥 Employés dans cette équipe :\n";
                var employesList = equipe.Employes.ToList();
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
            CmbSocietes.SelectedItem = null;
            TxtInfos.Text = "Sélectionnez une équipe pour voir les détails";

            DesactiverFormulaire();
        }

        private void ActiverFormulaire()
        {
            TxtNom.IsEnabled = true;
            TxtDescription.IsEnabled = true;
            ChkActif.IsEnabled = true;
            CmbSocietes.IsEnabled = true;
            BtnSauvegarder.IsEnabled = true;
            BtnAnnuler.IsEnabled = true;
            _modeEdition = true;
        }

        private void DesactiverFormulaire()
        {
            TxtNom.IsEnabled = false;
            TxtDescription.IsEnabled = false;
            ChkActif.IsEnabled = false;
            CmbSocietes.IsEnabled = false;
            BtnSauvegarder.IsEnabled = false;
            BtnAnnuler.IsEnabled = false;
            _modeEdition = false;
        }

        private void BtnNouvelleEquipe_Click(object sender, RoutedEventArgs e)
        {
            _equipeSelectionnee = null;
            DgEquipes.SelectedItem = null;

            ViderFormulaire();
            ActiverFormulaire();
            TxtNom.Focus();
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (_equipeSelectionnee != null)
            {
                ActiverFormulaire();
                TxtNom.Focus();
            }
        }

        private async void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (_equipeSelectionnee != null)
            {
                // Vérifier qu'aucun employé n'est assigné
                if (_equipeSelectionnee.Employes.Count > 0)
                {
                    MessageBox.Show("Impossible de supprimer cette équipe car des employés y sont encore assignés.\n\n" +
                                  "Veuillez d'abord réassigner ou supprimer tous les employés de cette équipe.",
                                  "Suppression impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Vérifier qu'aucun pôle n'est assigné
                if (_equipeSelectionnee.Poles.Count > 0)
                {
                    MessageBox.Show("Impossible de supprimer cette équipe car des pôles y sont encore rattachés.\n\n" +
                                  "Veuillez d'abord supprimer tous les pôles de cette équipe.",
                                  "Suppression impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer l'équipe '{_equipeSelectionnee.Nom}' ?\n\n" +
                    $"Cette équipe appartient à la société '{_equipeSelectionnee.Societe?.Nom}'.\n" +
                    "Cette action est irréversible.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Supprimer directement l'équipe
                        _context.Equipes.Remove(_equipeSelectionnee);
                        await _context.SaveChangesAsync();

                        await ChargerDonneesAsync();
                        ViderFormulaire();

                        MessageBox.Show("Équipe supprimée avec succès.", "Succès",
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
            if (_equipeSelectionnee != null)
            {
                try
                {
                    // Stocker l'ID et le nouvel état avant de modifier
                    int equipeId = _equipeSelectionnee.Id;
                    bool nouvelEtat = !_equipeSelectionnee.Actif;

                    _equipeSelectionnee.Actif = nouvelEtat;
                    await _context.SaveChangesAsync();

                    await ChargerDonneesAsync();

                    // Utiliser les valeurs stockées
                    string message = nouvelEtat ? "activée" : "désactivée";
                    MessageBox.Show($"Équipe {message} avec succès.", "Succès",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Resélectionner l'équipe dans la grille
                    var equipeRechargee = _equipes.FirstOrDefault(e => e.Id == equipeId);
                    if (equipeRechargee != null)
                    {
                        DgEquipes.SelectedItem = equipeRechargee;
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
                    MessageBox.Show("Le nom de l'équipe est obligatoire.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (CmbSocietes.SelectedItem is not Societe societeSelectionnee)
                {
                    MessageBox.Show("Veuillez sélectionner une société.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int equipeIdActuelle = _equipeSelectionnee != null ? _equipeSelectionnee.Id : 0;

                // Vérifier l'unicité du nom dans la société
                var nomExiste = await _context.Equipes
                    .Where(e => e.Nom == TxtNom.Text.Trim()
                             && e.SocieteId == societeSelectionnee.Id
                             && e.Id != equipeIdActuelle)
                    .AnyAsync();

                if (nomExiste)
                {
                    MessageBox.Show("Ce nom d'équipe est déjà utilisé dans cette société.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool nouvelleEquipe = _equipeSelectionnee == null;

                // Créer ou modifier l'équipe
                Equipe equipe;
                if (nouvelleEquipe)
                {
                    equipe = new Equipe
                    {
                        DateCreation = DateTime.Now
                    };
                    _context.Equipes.Add(equipe);
                }
                else
                {
                    equipe = _equipeSelectionnee!;
                }

                // Mettre à jour les propriétés
                equipe.Nom = TxtNom.Text.Trim();
                equipe.Description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();
                equipe.Actif = ChkActif.IsChecked ?? true;
                equipe.SocieteId = societeSelectionnee.Id;

                await _context.SaveChangesAsync();

                ChargerDonnees();
                DesactiverFormulaire();

                string message = nouvelleEquipe ? "créée" : "modifiée";
                MessageBox.Show($"Équipe {message} avec succès.", "Succès",
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
            if (_equipeSelectionnee != null)
            {
                AfficherDetailsEquipe(_equipeSelectionnee);
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
                // Charger les équipes avec leur société
                var equipes = await _context.Equipes
                    .Include(e => e.Societe)
                    .Include(e => e.Employes)
                    .Include(e => e.Poles)
                    .OrderBy(e => e.Societe.Nom)
                    .ThenBy(e => e.Nom)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    _equipes.Clear();
                    foreach (var equipe in equipes)
                    {
                        _equipes.Add(equipe);
                    }
                });

                // Charger les sociétés disponibles
                var societesDisponibles = await _context.Societes
                    .Where(s => s.Actif)
                    .OrderBy(s => s.Nom)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    _societesDisponibles.Clear();
                    foreach (var societe in societesDisponibles)
                    {
                        _societesDisponibles.Add(societe);
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