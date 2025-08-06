using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;

namespace GestionConges.WPF.Views
{
    public partial class GestionUtilisateursWindow : Window
    {
        private ObservableCollection<Utilisateur> _utilisateurs;
        private ObservableCollection<Pole> _poles;
        private Utilisateur? _utilisateurSelectionne;
        private bool _modeEdition = false;

        public GestionUtilisateursWindow()
        {
            InitializeComponent();
            _utilisateurs = new ObservableCollection<Utilisateur>();
            _poles = new ObservableCollection<Pole>();

            InitialiserInterface();
            ChargerDonnees();
        }

        private void InitialiserInterface()
        {
            DgUtilisateurs.ItemsSource = _utilisateurs;
            CmbPole.ItemsSource = _poles;
            CmbPole.DisplayMemberPath = "Nom";
            CmbPole.SelectedValuePath = "Id";
        }

        private GestionCongesContext CreerContexte()
        {
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=GestionCongesDB;Trusted_Connection=true;MultipleActiveResultSets=true";
            var options = new DbContextOptionsBuilder<GestionCongesContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new GestionCongesContext(options);
        }

        private void ChargerDonnees()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var context = CreerContexte();

                    // Récupérer tous les utilisateurs avec leurs pôles
                    var tousUtilisateurs = await context.Utilisateurs
                        .Include(u => u.Pole)
                        .ToListAsync();

                    // Filtrer en mémoire
                    bool afficherInactifs = false;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        afficherInactifs = ChkAfficherInactifs.IsChecked == true;
                    });

                    var utilisateursFiltres = tousUtilisateurs
                        .Where(u => afficherInactifs || u.Actif)
                        .OrderBy(u => u.Nom)
                        .ThenBy(u => u.Prenom)
                        .ToList();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _utilisateurs.Clear();
                        foreach (var user in utilisateursFiltres)
                        {
                            _utilisateurs.Add(user);
                        }
                    });

                    // Charger les pôles
                    var polesList = await context.Poles
                        .Where(p => p.Actif)
                        .OrderBy(p => p.Nom)
                        .ToListAsync();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _poles.Clear();
                        _poles.Add(new Pole { Id = 0, Nom = "Aucun pôle" });
                        foreach (var pole in polesList)
                        {
                            _poles.Add(pole);
                        }

                        // Remplir le filtre des pôles
                        CmbFiltreRole.Items.Clear();
                        CmbFiltreRole.Items.Add(new ComboBoxItem { Content = "Tous les pôles", Tag = null });
                        foreach (var pole in polesList)
                        {
                            CmbFiltreRole.Items.Add(new ComboBoxItem { Content = pole.Nom, Tag = pole.Id });
                        }
                        CmbFiltreRole.SelectedIndex = 0;
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

        private void DgUtilisateurs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _utilisateurSelectionne = DgUtilisateurs.SelectedItem as Utilisateur;

            if (_utilisateurSelectionne != null)
            {
                AfficherDetailsUtilisateur(_utilisateurSelectionne);
                BtnModifier.IsEnabled = true;
                BtnSupprimer.IsEnabled = _utilisateurSelectionne.Id != App.UtilisateurConnecte?.Id; // Pas de suppression de soi-même
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

        private void AfficherDetailsUtilisateur(Utilisateur utilisateur)
        {
            TxtNom.Text = utilisateur.Nom;
            TxtPrenom.Text = utilisateur.Prenom;
            TxtEmail.Text = utilisateur.Email;
            TxtLogin.Text = utilisateur.Login;
            CmbRole.SelectedIndex = (int)utilisateur.Role;
            CmbPole.SelectedValue = utilisateur.PoleId ?? 0;
            ChkActif.IsChecked = utilisateur.Actif;

            // Masquer le champ mot de passe pour modification
            LblMotDePasse.Visibility = Visibility.Collapsed;
            TxtMotDePasse.Visibility = Visibility.Collapsed;

            // Informations supplémentaires
            var infos = $"📅 Créé le : {utilisateur.DateCreation:dd/MM/yyyy HH:mm}\n";
            if (utilisateur.DerniereConnexion.HasValue)
            {
                infos += $"🔗 Dernière connexion : {utilisateur.DerniereConnexion:dd/MM/yyyy HH:mm}\n";
            }
            infos += $"🆔 ID : {utilisateur.Id}";

            TxtInfos.Text = infos;

            // Désactiver les champs par défaut
            DesactiverFormulaire();
        }

        private void ViderFormulaire()
        {
            TxtNom.Clear();
            TxtPrenom.Clear();
            TxtEmail.Clear();
            TxtLogin.Clear();
            TxtMotDePasse.Clear();
            CmbRole.SelectedIndex = 0;
            CmbPole.SelectedValue = 0;
            ChkActif.IsChecked = true;
            TxtInfos.Text = "Sélectionnez un utilisateur pour voir les détails";

            DesactiverFormulaire();
        }

        private void ActiverFormulaire()
        {
            TxtNom.IsEnabled = true;
            TxtPrenom.IsEnabled = true;
            TxtEmail.IsEnabled = true;
            TxtLogin.IsEnabled = true;
            TxtMotDePasse.IsEnabled = true;
            CmbRole.IsEnabled = true;
            CmbPole.IsEnabled = true;
            ChkActif.IsEnabled = true;
            BtnSauvegarder.IsEnabled = true;
            BtnAnnuler.IsEnabled = true;
            _modeEdition = true;
        }

        private void DesactiverFormulaire()
        {
            TxtNom.IsEnabled = false;
            TxtPrenom.IsEnabled = false;
            TxtEmail.IsEnabled = false;
            TxtLogin.IsEnabled = false;
            TxtMotDePasse.IsEnabled = false;
            CmbRole.IsEnabled = false;
            CmbPole.IsEnabled = false;
            ChkActif.IsEnabled = false;
            BtnSauvegarder.IsEnabled = false;
            BtnAnnuler.IsEnabled = false;
            _modeEdition = false;
        }

        private void BtnNouvelUtilisateur_Click(object sender, RoutedEventArgs e)
        {
            _utilisateurSelectionne = null;
            DgUtilisateurs.SelectedItem = null;

            ViderFormulaire();

            // Afficher le champ mot de passe pour nouveau
            LblMotDePasse.Visibility = Visibility.Visible;
            TxtMotDePasse.Visibility = Visibility.Visible;

            ActiverFormulaire();
            TxtNom.Focus();
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (_utilisateurSelectionne != null)
            {
                ActiverFormulaire();
                TxtNom.Focus();
            }
        }

        private async void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (_utilisateurSelectionne != null)
            {
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer l'utilisateur {_utilisateurSelectionne.NomComplet} ?\n\n" +
                    "Cette action est irréversible et supprimera également toutes ses demandes de congés.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = CreerContexte();
                        var utilisateurASupprimer = context.Utilisateurs.Find(_utilisateurSelectionne.Id);
                        if (utilisateurASupprimer != null)
                        {
                            context.Utilisateurs.Remove(utilisateurASupprimer);
                            await context.SaveChangesAsync();
                        }

                        ChargerDonnees();
                        ViderFormulaire();

                        MessageBox.Show("Utilisateur supprimé avec succès.", "Succès",
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
            if (_utilisateurSelectionne != null)
            {
                try
                {
                    using var context = CreerContexte();
                    var utilisateur = context.Utilisateurs.Find(_utilisateurSelectionne.Id);
                    if (utilisateur != null)
                    {
                        utilisateur.Actif = !utilisateur.Actif;
                        await context.SaveChangesAsync();

                        string message = utilisateur.Actif ? "activé" : "désactivé";
                        MessageBox.Show($"Utilisateur {message} avec succès.", "Succès",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    ChargerDonnees();
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
                if (string.IsNullOrWhiteSpace(TxtNom.Text) ||
                    string.IsNullOrWhiteSpace(TxtPrenom.Text) ||
                    string.IsNullOrWhiteSpace(TxtEmail.Text) ||
                    string.IsNullOrWhiteSpace(TxtLogin.Text))
                {
                    MessageBox.Show("Veuillez remplir tous les champs obligatoires.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using var context = CreerContexte();

                // Vérifier l'unicité de l'email et du login
                var emailExiste = await context.Utilisateurs
                    .Where(u => u.Email == TxtEmail.Text && u.Id != (_utilisateurSelectionne != null ? _utilisateurSelectionne.Id : 0))
                    .AnyAsync();
                var loginExiste = await context.Utilisateurs
                    .Where(u => u.Login == TxtLogin.Text && u.Id != (_utilisateurSelectionne != null ? _utilisateurSelectionne.Id : 0))
                    .AnyAsync();

                if (emailExiste)
                {
                    MessageBox.Show("Cet email est déjà utilisé par un autre utilisateur.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (loginExiste)
                {
                    MessageBox.Show("Ce login est déjà utilisé par un autre utilisateur.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool nouveauUtilisateur = _utilisateurSelectionne == null;

                // Créer ou modifier l'utilisateur
                Utilisateur utilisateur;
                if (nouveauUtilisateur)
                {
                    if (string.IsNullOrWhiteSpace(TxtMotDePasse.Password))
                    {
                        MessageBox.Show("Le mot de passe est obligatoire pour un nouvel utilisateur.",
                                      "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    utilisateur = new Utilisateur
                    {
                        DateCreation = DateTime.Now,
                        MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(TxtMotDePasse.Password)
                    };
                    context.Utilisateurs.Add(utilisateur);
                }
                else
                {
                    utilisateur = context.Utilisateurs.Find(_utilisateurSelectionne!.Id)!;
                }

                // Mettre à jour les propriétés
                utilisateur.Nom = TxtNom.Text.Trim();
                utilisateur.Prenom = TxtPrenom.Text.Trim();
                utilisateur.Email = TxtEmail.Text.Trim();
                utilisateur.Login = TxtLogin.Text.Trim();
                utilisateur.Role = (RoleUtilisateur)CmbRole.SelectedIndex;

                var selectedPoleId = CmbPole.SelectedValue as int?;
                utilisateur.PoleId = selectedPoleId == 0 ? null : selectedPoleId;
                utilisateur.Actif = ChkActif.IsChecked ?? true;

                // Mettre à jour le mot de passe si fourni (modification)
                if (!nouveauUtilisateur && !string.IsNullOrWhiteSpace(TxtMotDePasse.Password))
                {
                    utilisateur.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(TxtMotDePasse.Password);
                }

                await context.SaveChangesAsync();

                ChargerDonnees();
                DesactiverFormulaire();

                string message = nouveauUtilisateur ? "créé" : "modifié";
                MessageBox.Show($"Utilisateur {message} avec succès.", "Succès",
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
            if (_utilisateurSelectionne != null)
            {
                AfficherDetailsUtilisateur(_utilisateurSelectionne);
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

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrerUtilisateurs();
        }

        private void CmbFiltreRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FiltrerUtilisateurs();
        }

        private void ChkAfficherInactifs_Checked(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
        }

        private void ChkAfficherInactifs_Unchecked(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
        }

        private void FiltrerUtilisateurs()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var context = CreerContexte();

                    // Récupérer TOUS les utilisateurs avec leurs pôles
                    var tousUtilisateurs = await context.Utilisateurs
                        .Include(u => u.Pole)
                        .ToListAsync();

                    // Le reste du code reste identique...
                    bool afficherInactifs = false;
                    string rechercheText = "";
                    int? selectedPoleId = null;

                    await Dispatcher.InvokeAsync(() =>
                    {
                        afficherInactifs = ChkAfficherInactifs.IsChecked == true;
                        rechercheText = (TxtRecherche.Text ?? "").Trim().ToLower();
                        if (CmbFiltreRole.SelectedItem is ComboBoxItem item && item.Tag is int poleId)
                        {
                            selectedPoleId = poleId;
                        }
                    });

                    var utilisateursFiltres = tousUtilisateurs.AsEnumerable();

                    if (!afficherInactifs)
                    {
                        utilisateursFiltres = utilisateursFiltres.Where(u => u.Actif);
                    }

                    if (!string.IsNullOrWhiteSpace(rechercheText))
                    {
                        utilisateursFiltres = utilisateursFiltres.Where(u =>
                            u.Nom.ToLower().Contains(rechercheText) ||
                            u.Prenom.ToLower().Contains(rechercheText) ||
                            u.Email.ToLower().Contains(rechercheText));
                    }

                    if (selectedPoleId.HasValue)
                    {
                        utilisateursFiltres = utilisateursFiltres.Where(u => u.PoleId == selectedPoleId.Value);
                    }

                    var resultat = utilisateursFiltres
                        .OrderBy(u => u.Nom)
                        .ThenBy(u => u.Prenom)
                        .ToList();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _utilisateurs.Clear();
                        foreach (var user in resultat)
                        {
                            _utilisateurs.Add(user);
                        }
                    });
                }
                catch (Exception ex)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"Erreur lors du filtrage : {ex.Message}",
                                      "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        private void BtnGestionPoles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gestionPolesWindow = new GestionPolesWindow();
                gestionPolesWindow.ShowDialog();

                // Recharger les données après fermeture (au cas où des pôles auraient été modifiés)
                ChargerDonnees();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la gestion des pôles : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Plus besoin de disposer un contexte local
            base.OnClosed(e);
        }
    }
}