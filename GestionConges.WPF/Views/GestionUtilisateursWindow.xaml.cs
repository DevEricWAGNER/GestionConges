using GestionConges.Core.Data;
using GestionConges.Core.Enums;
using GestionConges.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestionConges.WPF.Views
{
    public partial class GestionUtilisateursWindow : Window
    {
        private readonly GestionCongesContext _context;
        private ObservableCollection<Utilisateur> _utilisateurs;
        private ObservableCollection<Societe> _societes;
        private ObservableCollection<Equipe> _equipes;
        private ObservableCollection<Pole> _poles;
        private ObservableCollection<UtilisateurSocieteSecondaire> _societesSecondaires;
        private Utilisateur? _utilisateurSelectionne;
        private bool _modeEdition = false;
        private readonly SemaphoreSlim _semaphoreFiltre = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _semaphore = new(1, 1);


        public GestionUtilisateursWindow()
        {
            InitializeComponent();

            // Initialiser les collections
            _utilisateurs = new ObservableCollection<Utilisateur>();
            _societes = new ObservableCollection<Societe>();
            _equipes = new ObservableCollection<Equipe>();
            _poles = new ObservableCollection<Pole>();
            _societesSecondaires = new ObservableCollection<UtilisateurSocieteSecondaire>();

            try
            {
                _context = CreerContexte();
                if (_context == null)
                {
                    MessageBox.Show("Impossible de créer le contexte EF.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                InitialiserInterface(); // Ajoutez cette ligne
                ChargerFiltres();
                ChargerDonnees(); // Changez FiltrerUtilisateurs() par ChargerDonnees()
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'initialisation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private GestionCongesContext CreerContexte()
        {
            try
            {
                var connectionString = "Server=(localdb)\\mssqllocaldb;Database=GestionCongesDB;Trusted_Connection=true;MultipleActiveResultSets=true";
                var options = new DbContextOptionsBuilder<GestionCongesContext>()
                    .UseSqlServer(connectionString)
                    .Options;

                return new GestionCongesContext(options);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création du contexte : {ex.Message}");
                return null;
            }
        }

        private async void ChargerFiltres()
        {
            try
            {
                using var context = CreerContexte();
                if (context == null) return;

                // Charger seulement les sociétés accessibles à l'utilisateur connecté
                var societesUtilisateur = new List<Societe>();

                if (App.UtilisateurConnecte != null)
                {
                    // Société principale
                    var societePrincipale = await context.Societes
                        .FirstOrDefaultAsync(s => s.Id == App.UtilisateurConnecte.SocieteId && s.Actif);
                    if (societePrincipale != null)
                        societesUtilisateur.Add(societePrincipale);

                    // Sociétés secondaires
                    var societesSecondaires = await context.UtilisateursSocietesSecondaires
                        .Where(uss => uss.UtilisateurId == App.UtilisateurConnecte.Id && uss.Actif)
                        .Include(uss => uss.Societe)
                        .Select(uss => uss.Societe)
                        .Where(s => s.Actif)
                        .ToListAsync();

                    societesUtilisateur.AddRange(societesSecondaires);
                }

                // Trier et ajouter l'option "Toutes"
                societesUtilisateur = societesUtilisateur.Distinct().OrderBy(s => s.Nom).ToList();
                societesUtilisateur.Insert(0, new Societe { Id = 0, Nom = "Toutes les sociétés" });

                CmbFiltreSociete.ItemsSource = societesUtilisateur;
                CmbFiltreSociete.DisplayMemberPath = "Nom";
                CmbFiltreSociete.SelectedValuePath = "Id";
                CmbFiltreSociete.SelectedIndex = 0;

                // Masquer équipes et pôles par défaut
                MasquerFiltresEquipeEtPole();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des filtres : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MasquerFiltresEquipeEtPole()
        {
            LblFiltreEquipe.Visibility = Visibility.Collapsed;
            CmbFiltreEquipe.Visibility = Visibility.Collapsed;
            LblFiltrePole.Visibility = Visibility.Collapsed;
            CmbFiltrePole.Visibility = Visibility.Collapsed;
        }

        private void AfficherFiltreEquipe()
        {
            LblFiltreEquipe.Visibility = Visibility.Visible;
            CmbFiltreEquipe.Visibility = Visibility.Visible;
        }

        private void AfficherFiltrePole()
        {
            LblFiltrePole.Visibility = Visibility.Visible;
            CmbFiltrePole.Visibility = Visibility.Visible;
        }

        private void InitialiserInterface()
        {
            DgUtilisateurs.ItemsSource = _utilisateurs;
            CmbSociete.ItemsSource = _societes;
            CmbSociete.DisplayMemberPath = "Nom";
            CmbSociete.SelectedValuePath = "Id";

            CmbEquipe.ItemsSource = _equipes;
            CmbEquipe.DisplayMemberPath = "Nom";
            CmbEquipe.SelectedValuePath = "Id";

            CmbPole.ItemsSource = _poles;
            CmbPole.DisplayMemberPath = "Nom";
            CmbPole.SelectedValuePath = "Id";

            CmbSocieteSecondaire.ItemsSource = _societes;
            CmbSocieteSecondaire.DisplayMemberPath = "Nom";
            CmbSocieteSecondaire.SelectedValuePath = "Id";

            LstSocietesSecondaires.ItemsSource = _societesSecondaires;
        }

        private void ChargerDonnees()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    // Charger les utilisateurs avec toutes leurs relations
                    var tousUtilisateurs = await _context.Utilisateurs
                        .Include(u => u.Societe)
                        .Include(u => u.Equipe)
                        .Include(u => u.Pole)
                        .Include(u => u.SocietesSecondaires)
                            .ThenInclude(ss => ss.Societe)
                        .ToListAsync();

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

                    // Charger les sociétés
                    var societesList = await _context.Societes
                        .Where(s => s.Actif)
                        .OrderBy(s => s.Nom)
                        .ToListAsync();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        _societes.Clear();
                        foreach (var societe in societesList)
                        {
                            _societes.Add(societe);
                        }

                        // Pour les filtres, recréer les collections avec les éléments "Tous"
                        var societesFiltres = new List<Societe> { new Societe { Id = 0, Nom = "Toutes les sociétés" } };
                        societesFiltres.AddRange(societesList);

                        CmbFiltreSociete.ItemsSource = societesFiltres;
                        CmbFiltreSociete.SelectedIndex = 0;
                    });

                    // Charger les pôles pour le filtre
                    var polesList = await _context.Poles
                        .Where(p => p.Actif)
                        .OrderBy(p => p.Nom)
                        .ToListAsync();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        // Pour les filtres, recréer les collections avec les éléments "Tous"
                        var polesFiltres = new List<Pole> { new Pole { Id = 0, Nom = "Tous les pôles" } };
                        polesFiltres.AddRange(polesList);

                        CmbFiltrePole.ItemsSource = polesFiltres;
                        CmbFiltrePole.SelectedIndex = 0;
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

        private async void CmbSociete_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbSociete.SelectedValue is int societeId && societeId > 0)
            {
                await ChargerEquipesPourSociete(societeId);
            }
            else
            {
                _equipes.Clear();
                _poles.Clear();
            }
        }

        private async void CmbEquipe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbEquipe.SelectedValue is int equipeId && equipeId > 0)
            {
                await ChargerPolesPourEquipe(equipeId);
            }
            else
            {
                _poles.Clear();
            }
        }

        private async Task ChargerEquipesPourSociete(int societeId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var equipes = await _context.Equipes
                    .Where(e => e.SocieteId == societeId && e.Actif)
                    .OrderBy(e => e.Nom)
                    .ToListAsync();

                _equipes.Clear();
                foreach (var equipe in equipes)
                {
                    _equipes.Add(equipe);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des équipes : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task ChargerPolesPourEquipe(int equipeId)
        {
            try
            {
                using var context = CreerContexte();

                // CORRECTION : Utiliser la relation directe Pole -> Equipe
                var poles = await context.Poles
                    .Where(p => p.EquipeId == equipeId && p.Actif)
                    .OrderBy(p => p.Nom)
                    .ToListAsync();

                var polesListe = new List<Pole> { new Pole { Id = 0, Nom = "Tous les pôles" } };
                polesListe.AddRange(poles);

                await Dispatcher.InvokeAsync(() =>
                {
                    CmbFiltrePole.ItemsSource = polesListe;
                    CmbFiltrePole.DisplayMemberPath = "Nom";
                    CmbFiltrePole.SelectedValuePath = "Id";
                    CmbFiltrePole.SelectedIndex = 0;
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des pôles : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async void DgUtilisateurs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _utilisateurSelectionne = DgUtilisateurs.SelectedItem as Utilisateur;

            if (_utilisateurSelectionne != null)
            {
                await AfficherDetailsUtilisateur(_utilisateurSelectionne);
                BtnModifier.IsEnabled = true;
                BtnSupprimer.IsEnabled = _utilisateurSelectionne.Id != App.UtilisateurConnecte?.Id;
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

        private async Task AfficherDetailsUtilisateur(Utilisateur utilisateur)
        {
            TxtNom.Text = utilisateur.Nom;
            TxtPrenom.Text = utilisateur.Prenom;
            TxtEmail.Text = utilisateur.Email;
            CmbRole.SelectedIndex = (int)utilisateur.Role;
            ChkActif.IsChecked = utilisateur.Actif;

            // Société principale
            CmbSociete.SelectedValue = utilisateur.SocieteId;
            await ChargerEquipesPourSociete(utilisateur.SocieteId);

            // Équipe
            CmbEquipe.SelectedValue = utilisateur.EquipeId;
            await ChargerPolesPourEquipe(utilisateur.EquipeId);

            // Pôle
            CmbPole.SelectedValue = utilisateur.PoleId ?? 0;

            // Charger les sociétés secondaires
            var societesSecondaires = await _context.UtilisateursSocietesSecondaires
                .Where(uss => uss.UtilisateurId == utilisateur.Id && uss.Actif)
                .Include(uss => uss.Societe)
                .ToListAsync();

            _societesSecondaires.Clear();
            foreach (var ss in societesSecondaires)
            {
                _societesSecondaires.Add(ss);
            }

            // Informations supplémentaires
            var infos = $"Créé le : {utilisateur.DateCreation:dd/MM/yyyy HH:mm}\n";
            if (utilisateur.DerniereConnexion.HasValue)
            {
                infos += $"Dernière connexion : {utilisateur.DerniereConnexion:dd/MM/yyyy HH:mm}\n";
            }
            infos += $"Société : {utilisateur.Societe?.Nom}\n";
            infos += $"Équipe : {utilisateur.Equipe?.Nom}\n";
            if (utilisateur.Pole != null)
            {
                infos += $"Pôle : {utilisateur.Pole.Nom}\n";
            }
            if (_societesSecondaires.Count > 0)
            {
                infos += $"Sociétés secondaires : {_societesSecondaires.Count}\n";
            }
            infos += $"ID : {utilisateur.Id}";

            TxtInfos.Text = infos;

            // Désactiver les champs par défaut
            DesactiverFormulaire();
        }

        private void ViderFormulaire()
        {
            TxtNom.Clear();
            TxtPrenom.Clear();
            TxtEmail.Clear();
            CmbRole.SelectedIndex = 0;
            CmbSociete.SelectedValue = null;
            CmbEquipe.SelectedValue = null;
            CmbPole.SelectedValue = 0;
            ChkActif.IsChecked = true;
            _societesSecondaires.Clear();
            TxtInfos.Text = "Sélectionnez un utilisateur pour voir les détails";

            DesactiverFormulaire();
        }

        private void ActiverFormulaire()
        {
            TxtNom.IsEnabled = true;
            TxtPrenom.IsEnabled = true;
            TxtEmail.IsEnabled = true;
            CmbRole.IsEnabled = true;
            CmbSociete.IsEnabled = true;
            CmbEquipe.IsEnabled = true;
            CmbPole.IsEnabled = true;
            ChkActif.IsEnabled = true;
            BtnAjouterSocieteSecondaire.IsEnabled = true;
            BtnRetirerSocieteSecondaire.IsEnabled = true;
            BtnSauvegarder.IsEnabled = true;
            BtnAnnuler.IsEnabled = true;
            _modeEdition = true;
        }

        private void DesactiverFormulaire()
        {
            TxtNom.IsEnabled = false;
            TxtPrenom.IsEnabled = false;
            TxtEmail.IsEnabled = false;
            CmbRole.IsEnabled = false;
            CmbSociete.IsEnabled = false;
            CmbEquipe.IsEnabled = false;
            CmbPole.IsEnabled = false;
            ChkActif.IsEnabled = false;
            BtnAjouterSocieteSecondaire.IsEnabled = false;
            BtnRetirerSocieteSecondaire.IsEnabled = false;
            BtnSauvegarder.IsEnabled = false;
            BtnAnnuler.IsEnabled = false;
            _modeEdition = false;
        }

        private void BtnNouvelUtilisateur_Click(object sender, RoutedEventArgs e)
        {
            _utilisateurSelectionne = null;
            DgUtilisateurs.SelectedItem = null;

            ViderFormulaire();

            // Charger les équipes pour la première société si disponible
            if (_societes.Count > 0)
            {
                CmbSociete.SelectedIndex = 0;
                ChargerEquipesPourSociete(_societes[0].Id);
            }


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
                    "Cette action supprimera également toutes ses demandes de congés et relations.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.Utilisateurs.Remove(_utilisateurSelectionne);
                        await _context.SaveChangesAsync();

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
                    _utilisateurSelectionne.Actif = !_utilisateurSelectionne.Actif;
                    await _context.SaveChangesAsync();

                    string message = _utilisateurSelectionne.Actif ? "activé" : "désactivé";
                    MessageBox.Show($"Utilisateur {message} avec succès.", "Succès",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

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
                using var context = CreerContexte();
                if (context == null) return;

                // Validation des champs
                if (string.IsNullOrWhiteSpace(TxtNom.Text) ||
                    string.IsNullOrWhiteSpace(TxtPrenom.Text) ||
                    string.IsNullOrWhiteSpace(TxtEmail.Text) ||
                    CmbSociete.SelectedValue == null ||
                    CmbEquipe.SelectedValue == null)
                {
                    MessageBox.Show("Veuillez remplir tous les champs obligatoires (Nom, Prénom, Email, Société, Équipe).",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Vérifier l'unicité de l'email avec le MÊME contexte
                int utilisateurIdActuel = _utilisateurSelectionne?.Id ?? 0;
                var emailExiste = await context.Utilisateurs
                    .Where(u => u.Email == TxtEmail.Text && u.Id != utilisateurIdActuel)
                    .AnyAsync();

                if (emailExiste)
                {
                    MessageBox.Show("Cet email est déjà utilisé par un autre utilisateur.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool nouveauUtilisateur = _utilisateurSelectionne == null;
                Utilisateur utilisateur;

                if (nouveauUtilisateur)
                {
                    var configuration = App.GetService<IConfiguration>();
                    var password = configuration["AppSettings:DefaultPasswordForUsers"];
                    utilisateur = new Utilisateur
                    {
                        DateCreation = DateTime.Now,
                        MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(password)
                    };
                    context.Utilisateurs.Add(utilisateur);
                }
                else
                {
                    // Recharger l'utilisateur avec le contexte actuel
                    utilisateur = await context.Utilisateurs
                        .FirstOrDefaultAsync(u => u.Id == _utilisateurSelectionne.Id);

                    if (utilisateur == null)
                    {
                        MessageBox.Show("Utilisateur introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Mettre à jour les propriétés
                utilisateur.Nom = TxtNom.Text.Trim();
                utilisateur.Prenom = TxtPrenom.Text.Trim();
                utilisateur.Email = TxtEmail.Text.Trim();
                utilisateur.Role = (RoleUtilisateur)CmbRole.SelectedIndex;
                utilisateur.SocieteId = ((Societe)CmbSociete.SelectedItem).Id;
                utilisateur.EquipeId = ((Equipe)CmbEquipe.SelectedItem).Id;

                // Gestion du pôle
                int? selectedPoleId = null;
                if (CmbPole.SelectedItem is Pole poleSelectionne && poleSelectionne.Id != 0)
                {
                    selectedPoleId = poleSelectionne.Id;
                }
                utilisateur.PoleId = selectedPoleId;
                utilisateur.Actif = ChkActif.IsChecked ?? true;

                // Pas besoin de marquer manuellement les propriétés comme modifiées
                // EF Core le fait automatiquement pour les entités trackées

                await context.SaveChangesAsync();

                // Recharger les données et réinitialiser l'interface
                ChargerDonnees();
                DesactiverFormulaire();

                string message = nouveauUtilisateur ? "créé" : "modifié";
                MessageBox.Show($"Utilisateur {message} avec succès.", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (DbUpdateConcurrencyException)
            {
                MessageBox.Show("L'utilisateur a été modifié par une autre personne. Veuillez recharger les données et réessayer.",
                              "Conflit de concurrence", MessageBoxButton.OK, MessageBoxImage.Warning);
                ChargerDonnees(); // Recharger pour avoir les dernières données
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnAjouterSocieteSecondaire_Click(object sender, RoutedEventArgs e)
        {
            if (CmbSocieteSecondaire.SelectedValue is int societeId && _utilisateurSelectionne != null)
            {
                try
                {
                    // Vérifier si l'association existe déjà
                    var existeDejaAssociation = await _context.UtilisateursSocietesSecondaires
                        .AnyAsync(uss => uss.UtilisateurId == _utilisateurSelectionne.Id &&
                                        uss.SocieteId == societeId &&
                                        uss.Actif);

                    if (existeDejaAssociation)
                    {
                        MessageBox.Show("Cette société est déjà associée comme secondaire.",
                                      "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Vérifier que ce n'est pas la société principale
                    if (societeId == _utilisateurSelectionne.SocieteId)
                    {
                        MessageBox.Show("Impossible d'ajouter la société principale comme secondaire.",
                                      "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Créer la nouvelle association
                    var nouvelleAssociation = new UtilisateurSocieteSecondaire
                    {
                        UtilisateurId = _utilisateurSelectionne.Id,
                        SocieteId = societeId,
                        Actif = true,
                        DateAffectation = DateTime.Now
                    };

                    _context.UtilisateursSocietesSecondaires.Add(nouvelleAssociation);
                    await _context.SaveChangesAsync();

                    // Recharger les sociétés secondaires
                    await RechargerSocietesSecondaires();

                    MessageBox.Show("Société secondaire ajoutée avec succès.", "Succès",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ajout de la société : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnRetirerSocieteSecondaire_Click(object sender, RoutedEventArgs e)
        {
            if (LstSocietesSecondaires.SelectedItem is UtilisateurSocieteSecondaire societeSelectionnee && _utilisateurSelectionne != null)
            {
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir retirer la société '{societeSelectionnee.Societe?.Nom}' des sociétés secondaires ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _context.UtilisateursSocietesSecondaires.Remove(societeSelectionnee);
                        await _context.SaveChangesAsync();

                        await RechargerSocietesSecondaires();

                        MessageBox.Show("Société secondaire retirée avec succès.", "Succès",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors du retrait de la société : {ex.Message}",
                                      "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une société à retirer.",
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task RechargerSocietesSecondaires()
        {
            if (_utilisateurSelectionne != null)
            {
                var societesSecondaires = await _context.UtilisateursSocietesSecondaires
                    .Where(uss => uss.UtilisateurId == _utilisateurSelectionne.Id && uss.Actif)
                    .Include(uss => uss.Societe)
                    .ToListAsync();

                _societesSecondaires.Clear();
                foreach (var ss in societesSecondaires)
                {
                    _societesSecondaires.Add(ss);
                }
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            if (_utilisateurSelectionne != null)
            {
                _ = AfficherDetailsUtilisateur(_utilisateurSelectionne);
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

        private async void CmbFiltreSociete_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFiltreSociete.SelectedValue is int societeId && societeId > 0)
            {
                // Société spécifique sélectionnée - afficher le filtre équipe
                await ChargerEquipesPourFiltre(societeId);
                AfficherFiltreEquipe();

                // Masquer le filtre pôle
                LblFiltrePole.Visibility = Visibility.Collapsed;
                CmbFiltrePole.Visibility = Visibility.Collapsed;
            }
            else
            {
                // "Toutes les sociétés" - masquer équipes et pôles
                MasquerFiltresEquipeEtPole();
            }

            FiltrerUtilisateurs();
        }

        private async void CmbFiltreEquipe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFiltreEquipe.SelectedValue is int equipeId && equipeId > 0)
            {
                // Équipe spécifique sélectionnée - afficher le filtre pôle
                await ChargerPolesPourFiltre(equipeId);
                AfficherFiltrePole();
            }
            else
            {
                // "Toutes les équipes" - masquer le filtre pôle
                LblFiltrePole.Visibility = Visibility.Collapsed;
                CmbFiltrePole.Visibility = Visibility.Collapsed;
            }

            FiltrerUtilisateurs();
        }

        private async Task ChargerEquipesPourFiltre(int societeId)
        {
            try
            {
                using var context = CreerContexte();
                if (context == null) return;

                var equipes = await context.Equipes
                    .Where(e => e.SocieteId == societeId && e.Actif)
                    .OrderBy(e => e.Nom)
                    .ToListAsync();

                var equipesFiltre = new List<Equipe> { new Equipe { Id = 0, Nom = "Toutes les équipes" } };
                equipesFiltre.AddRange(equipes);

                CmbFiltreEquipe.ItemsSource = equipesFiltre;
                CmbFiltreEquipe.DisplayMemberPath = "Nom";
                CmbFiltreEquipe.SelectedValuePath = "Id";
                CmbFiltreEquipe.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des équipes : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CmbFiltrePole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FiltrerUtilisateurs();
        }

        private void ChkAfficherInactifs_Checked(object sender, RoutedEventArgs e)
        {
            FiltrerUtilisateurs();
        }

        private void ChkAfficherInactifs_Unchecked(object sender, RoutedEventArgs e)
        {
            FiltrerUtilisateurs();
        }

        private async void FiltrerUtilisateurs()
        {
            try
            {
                using var context = CreerContexte();
                if (context == null) return;

                string recherche = TxtRecherche.Text?.ToLower() ?? "";

                // Récupération correcte des IDs
                int societeId = 0;
                if (CmbFiltreSociete.SelectedItem is Societe societeSelectionnee)
                    societeId = societeSelectionnee.Id;

                int equipeId = 0;
                if (CmbFiltreEquipe.SelectedItem is Equipe equipeSelectionnee)
                    equipeId = equipeSelectionnee.Id;

                int poleId = 0;
                if (CmbFiltrePole.SelectedItem is Pole poleSelectionne)
                    poleId = poleSelectionne.Id;

                bool afficherInactifs = ChkAfficherInactifs.IsChecked == true;

                var query = context.Utilisateurs
                    .Include(u => u.Societe)
                    .Include(u => u.Pole)
                    .Include(u => u.Equipe)
                    .AsQueryable();

                // Appliquer les filtres...
                if (!string.IsNullOrWhiteSpace(recherche))
                {
                    query = query.Where(u =>
                        u.Nom.ToLower().Contains(recherche) ||
                        u.Prenom.ToLower().Contains(recherche) ||
                        u.Email.ToLower().Contains(recherche));
                }

                if (societeId != 0)
                    query = query.Where(u => u.SocieteId == societeId);

                if (equipeId != 0)
                    query = query.Where(u => u.EquipeId == equipeId);

                if (poleId != 0)
                    query = query.Where(u => u.PoleId == poleId);

                if (!afficherInactifs)
                    query = query.Where(u => u.Actif);

                var resultats = await query.ToListAsync();
                DgUtilisateurs.ItemsSource = resultats;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du filtrage : {ex.Message}");
            }
        }

        private async Task ChargerPolesPourFiltre(int equipeId)
        {
            try
            {
                using var context = CreerContexte();
                if (context == null) return;

                // CORRECTION : Utiliser la relation directe Pole -> Equipe
                var poles = await context.Poles
                    .Where(p => p.EquipeId == equipeId && p.Actif)
                    .OrderBy(p => p.Nom)
                    .ToListAsync();

                var polesFiltre = new List<Pole> { new Pole { Id = 0, Nom = "Tous les pôles" } };
                polesFiltre.AddRange(poles);

                CmbFiltrePole.ItemsSource = polesFiltre;
                CmbFiltrePole.DisplayMemberPath = "Nom";
                CmbFiltrePole.SelectedValuePath = "Id";
                CmbFiltrePole.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des pôles : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void BtnGestionPoles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gestionPolesWindow = new GestionPolesWindow();
                gestionPolesWindow.ShowDialog();

                ChargerDonnees();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la gestion des pôles : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnGestionValidation_Click(object sender, RoutedEventArgs e)
        {
            if (_utilisateurSelectionne != null)
            {
                try
                {
                    //var gestionValidationWindow = new GestionValidateurWindow(_utilisateurSelectionne.Id);
                    //gestionValidationWindow.ShowDialog();

                    ChargerDonnees();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture de la gestion des validateurs : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un utilisateur pour gérer ses droits de validation.",
                              "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #region Window Controls

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                BtnMaximize_Click(sender, e);
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

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

            BtnMaximize.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            _semaphoreFiltre?.Dispose();
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}