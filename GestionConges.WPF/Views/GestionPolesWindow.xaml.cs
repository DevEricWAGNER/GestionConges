using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;

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
                    // Charger les pôles avec leurs équipes
                    var poles = await _context.Poles
                        .Include(p => p.Equipes)
                            .ThenInclude(e => e.Societe)
                        .Include(p => p.Employes)
                        .OrderBy(p => p.Nom)
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

            // Afficher les équipes actuellement associées
            var equipesAssociees = pole.Equipes.Where(e => e.Actif).ToList();
            LstEquipesAssociees.ItemsSource = equipesAssociees;

            // Informations supplémentaires
            var infos = $"📅 Créé le : {pole.DateCreation:dd/MM/yyyy HH:mm}\n";
            infos += $"👥 Nombre d'employés : {pole.Employes.Count}\n";
            infos += $"🏢 Équipes associées : {equipesAssociees.Count}\n";
            infos += $"🆔 ID : {pole.Id}";

            if (equipesAssociees.Count > 0)
            {
                infos += "\n\n🏢 Équipes associées :\n";
                foreach (var equipe in equipesAssociees)
                {
                    infos += $"• {equipe.Nom} ({equipe.Societe?.Nom})\n";
                }
            }

            if (pole.Employes.Count > 0)
            {
                infos += "\n👥 Employés dans ce pôle :\n";
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
            LstEquipesAssociees.ItemsSource = null;
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
            BtnAjouterEquipe.IsEnabled = true;
            BtnRetirerEquipe.IsEnabled = true;
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
            BtnAjouterEquipe.IsEnabled = false;
            BtnRetirerEquipe.IsEnabled = false;
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
                    "Cette action supprimera également toutes les associations avec les équipes.\n" +
                    "Cette action est irréversible.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Supprimer d'abord les relations équipe-pôle
                        var relationsEquipePole = await _context.EquipesPoles
                            .Where(ep => ep.PoleId == _poleSelectionne.Id)
                            .ToListAsync();

                        _context.EquipesPoles.RemoveRange(relationsEquipePole);

                        // Puis supprimer le pôle
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
                    _poleSelectionne.Actif = !_poleSelectionne.Actif;
                    await _context.SaveChangesAsync();

                    await ChargerDonneesAsync();

                    string message = _poleSelectionne.Actif ? "activé" : "désactivé";
                    MessageBox.Show($"Pôle {message} avec succès.", "Succès",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la modification : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnAjouterEquipe_Click(object sender, RoutedEventArgs e)
        {
            if (CmbEquipes.SelectedItem is Equipe equipeSelectionnee && _poleSelectionne != null)
            {
                try
                {
                    // Vérifier si l'association existe déjà
                    var existeDejaAssociation = await _context.EquipesPoles
                        .AnyAsync(ep => ep.EquipeId == equipeSelectionnee.Id &&
                                       ep.PoleId == _poleSelectionne.Id &&
                                       ep.Actif);

                    if (existeDejaAssociation)
                    {
                        MessageBox.Show("Cette équipe est déjà associée à ce pôle.",
                                      "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Créer la nouvelle association
                    var nouvelleAssociation = new EquipePole
                    {
                        EquipeId = equipeSelectionnee.Id,
                        PoleId = _poleSelectionne.Id,
                        Actif = true,
                        DateAffectation = DateTime.Now
                    };

                    _context.EquipesPoles.Add(nouvelleAssociation);
                    await _context.SaveChangesAsync();

                    // Recharger les données pour mettre à jour l'affichage
                    await ChargerDonneesAsync();

                    // Réafficher les détails du pôle
                    var poleRechargé = await _context.Poles
                        .Include(p => p.Equipes)
                            .ThenInclude(e => e.Societe)
                        .Include(p => p.Employes)
                        .FirstOrDefaultAsync(p => p.Id == _poleSelectionne.Id);

                    if (poleRechargé != null)
                    {
                        _poleSelectionne = poleRechargé;
                        AfficherDetailsPole(_poleSelectionne);
                    }

                    MessageBox.Show("Équipe ajoutée avec succès au pôle.", "Succès",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ajout de l'équipe : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnRetirerEquipe_Click(object sender, RoutedEventArgs e)
        {
            if (LstEquipesAssociees.SelectedItem is Equipe equipeSelectionnee && _poleSelectionne != null)
            {
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir retirer l'équipe '{equipeSelectionnee.Nom}' de ce pôle ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var association = await _context.EquipesPoles
                            .FirstOrDefaultAsync(ep => ep.EquipeId == equipeSelectionnee.Id &&
                                                     ep.PoleId == _poleSelectionne.Id);

                        if (association != null)
                        {
                            _context.EquipesPoles.Remove(association);
                            await _context.SaveChangesAsync();

                            // Recharger les données
                            await ChargerDonneesAsync();

                            // Réafficher les détails du pôle
                            var poleRechargé = await _context.Poles
                                .Include(p => p.Equipes)
                                    .ThenInclude(e => e.Societe)
                                .Include(p => p.Employes)
                                .FirstOrDefaultAsync(p => p.Id == _poleSelectionne.Id);

                            if (poleRechargé != null)
                            {
                                _poleSelectionne = poleRechargé;
                                AfficherDetailsPole(_poleSelectionne);
                            }

                            MessageBox.Show("Équipe retirée avec succès du pôle.", "Succès",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors du retrait de l'équipe : {ex.Message}",
                                      "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une équipe à retirer.",
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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
                int poleIdActuel = _poleSelectionne != null ? _poleSelectionne.Id : 0;
                // Vérifier l'unicité du nom
                var nomExiste = await _context.Poles
                    .Where(p => p.Nom == TxtNom.Text.Trim() && p.Id != poleIdActuel)
                    .AnyAsync();

                if (nomExiste)
                {
                    MessageBox.Show("Ce nom de pôle est déjà utilisé.",
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
                // Charger les pôles avec leurs équipes
                var poles = await _context.Poles
                    .Include(p => p.Equipes)
                        .ThenInclude(e => e.Societe)
                    .Include(p => p.Employes)
                    .OrderBy(p => p.Nom)
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