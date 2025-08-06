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
        private ObservableCollection<Utilisateur> _chefsDisponibles;
        private Pole? _poleSelectionne;
        private bool _modeEdition = false;

        public GestionPolesWindow()
        {
            InitializeComponent();
            _context = App.GetService<GestionCongesContext>();
            _poles = new ObservableCollection<Pole>();
            _chefsDisponibles = new ObservableCollection<Utilisateur>();

            InitialiserInterface();
            ChargerDonnees();
        }

        private void InitialiserInterface()
        {
            DgPoles.ItemsSource = _poles;
            CmbChef.ItemsSource = _chefsDisponibles;
        }

        private void ChargerDonnees()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Charger les pôles
                    var poles = await _context.Poles
                        .Include(p => p.Chef)
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

                    // Charger les chefs potentiels (Chef d'équipe + Chefs de pôle)
                    var chefsDisponibles = await _context.Utilisateurs
                        .Where(u => u.Actif && (u.Role == RoleUtilisateur.ChefEquipe || u.Role == RoleUtilisateur.ChefPole))
                        .OrderBy(u => u.Nom)
                        .ThenBy(u => u.Prenom)
                        .ToListAsync();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _chefsDisponibles.Clear();
                        _chefsDisponibles.Add(new Utilisateur { Id = 0, Nom = "Aucun", Prenom = "chef" }); // Option vide
                        foreach (var chef in chefsDisponibles)
                        {
                            _chefsDisponibles.Add(chef);
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
            CmbChef.SelectedValue = pole.ChefId ?? 0;
            ChkActif.IsChecked = pole.Actif;

            // Informations supplémentaires
            var infos = $"📅 Créé le : {pole.DateCreation:dd/MM/yyyy HH:mm}\n";
            infos += $"👥 Nombre d'employés : {pole.Employes.Count}\n";
            infos += $"🆔 ID : {pole.Id}";

            if (pole.Employes.Count > 0)
            {
                infos += "\n\n👥 Employés dans ce pôle :\n";
                var employesList = pole.Employes.ToList();
                foreach (var employe in employesList.Take(5)) // Limiter l'affichage
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
            CmbChef.SelectedValue = 0;
            ChkActif.IsChecked = true;
            TxtInfos.Text = "Sélectionnez un pôle pour voir les détails";

            DesactiverFormulaire();
        }

        private void ActiverFormulaire()
        {
            TxtNom.IsEnabled = true;
            TxtDescription.IsEnabled = true;
            CmbChef.IsEnabled = true;
            ChkActif.IsEnabled = true;
            BtnSauvegarder.IsEnabled = true;
            BtnAnnuler.IsEnabled = true;
            _modeEdition = true;
        }

        private void DesactiverFormulaire()
        {
            TxtNom.IsEnabled = false;
            TxtDescription.IsEnabled = false;
            CmbChef.IsEnabled = false;
            ChkActif.IsEnabled = false;
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
                    "Cette action est irréversible.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
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

                // Vérifier l'unicité du nom
                var nomExiste = await _context.Poles
                    .Where(p => p.Nom == TxtNom.Text.Trim() && p.Id != (_poleSelectionne != null ? _poleSelectionne.Id : 0))
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

                var selectedChefId = CmbChef.SelectedValue as int?;
                pole.ChefId = selectedChefId == 0 ? null : selectedChefId;
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
                // Charger les pôles
                var poles = await _context.Poles
                    .Include(p => p.Chef)
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

                // Charger les chefs potentiels (Chef d'équipe + Chefs de pôle)
                var chefsDisponibles = await _context.Utilisateurs
                    .Where(u => u.Actif && (u.Role == RoleUtilisateur.ChefEquipe || u.Role == RoleUtilisateur.ChefPole))
                    .OrderBy(u => u.Nom)
                    .ThenBy(u => u.Prenom)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    _chefsDisponibles.Clear();
                    _chefsDisponibles.Add(new Utilisateur { Id = 0, Nom = "Aucun", Prenom = "chef" }); // Option vide
                    foreach (var chef in chefsDisponibles)
                    {
                        _chefsDisponibles.Add(chef);
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