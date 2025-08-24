using System.Windows;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.WPF.Services;

namespace GestionConges.WPF.Views
{
    public partial class MonProfilWindow : Window
    {
        private readonly GestionCongesContext _context;
        private readonly Utilisateur _utilisateurConnecte;
        private readonly PreferencesUtilisateurService _preferencesService;

        public MonProfilWindow()
        {
            InitializeComponent();

            _context = App.GetService<GestionCongesContext>();
            _utilisateurConnecte = App.UtilisateurConnecte ?? throw new InvalidOperationException("Aucun utilisateur connecté");
            _preferencesService = new PreferencesUtilisateurService();

            ChargerDonneesProfil();
        }

        private async void ChargerDonneesProfil()
        {
            try
            {
                // Recharger l'utilisateur avec toutes ses relations
                var utilisateur = await _context.Utilisateurs
                    .Include(u => u.Societe)
                    .Include(u => u.Equipe)
                    .Include(u => u.Pole)
                    .Include(u => u.SocietesSecondaires)
                        .ThenInclude(ss => ss.Societe)
                    .FirstOrDefaultAsync(u => u.Id == _utilisateurConnecte.Id);

                if (utilisateur != null)
                {
                    // Remplir les informations personnelles
                    TxtNom.Text = utilisateur.Nom;
                    TxtPrenom.Text = utilisateur.Prenom;
                    TxtEmail.Text = utilisateur.Email;

                    // Remplir les informations organisation
                    TxtRole.Text = utilisateur.RoleLibelle;
                    TxtSociete.Text = utilisateur.Societe?.Nom ?? "Non définie";
                    TxtEquipe.Text = utilisateur.Equipe?.Nom ?? "Non définie";
                    TxtPole.Text = utilisateur.Pole?.Nom ?? "Aucun pôle assigné";

                    // Charger les sociétés secondaires
                    var societesSecondaires = utilisateur.SocietesSecondaires
                        .Where(ss => ss.Actif)
                        .ToList();

                    if (societesSecondaires.Any())
                    {
                        LstSocietesSecondaires.ItemsSource = societesSecondaires;
                        TxtAucuneSocieteSecondaire.Visibility = Visibility.Collapsed;
                        LstSocietesSecondaires.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        TxtAucuneSocieteSecondaire.Visibility = Visibility.Visible;
                        LstSocietesSecondaires.Visibility = Visibility.Collapsed;
                    }

                    // Remplir les informations compte
                    TxtDateCreation.Text = utilisateur.DateCreation.ToString("dd/MM/yyyy HH:mm");
                    TxtDerniereConnexion.Text = utilisateur.DerniereConnexion?.ToString("dd/MM/yyyy HH:mm") ?? "Jamais";
                    TxtStatut.Text = utilisateur.Actif ? "Actif" : "Inactif";

                    // Charger les préférences
                    ChargerPreferences();
                }
                else
                {
                    MessageBox.Show("Impossible de charger les informations du profil.",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement du profil : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerPreferences()
        {
            _preferencesService.ChargerPreferences();

            ChkNotificationEmail.IsChecked = _preferencesService.NotificationEmail;
            ChkNotificationValidation.IsChecked = _preferencesService.NotificationValidation;
            ChkNotificationRappel.IsChecked = _preferencesService.NotificationRappel;
            ChkAffichageCompact.IsChecked = _preferencesService.AffichageCompact;

            // Sélectionner le format de date
            var formatIndex = _preferencesService.FormatDate switch
            {
                "dd/MM/yyyy" => 0,
                "MM/dd/yyyy" => 1,
                "yyyy-MM-dd" => 2,
                _ => 0
            };
            CmbFormatDate.SelectedIndex = formatIndex;
        }

        private async void BtnChangerMotDePasse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation des champs
                if (string.IsNullOrWhiteSpace(TxtMotDePasseActuel.Password))
                {
                    AfficherErreurMotDePasse("Le mot de passe actuel est requis.");
                    TxtMotDePasseActuel.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtNouveauMotDePasse.Password))
                {
                    AfficherErreurMotDePasse("Le nouveau mot de passe est requis.");
                    TxtNouveauMotDePasse.Focus();
                    return;
                }

                if (TxtNouveauMotDePasse.Password.Length < 8)
                {
                    AfficherErreurMotDePasse("Le nouveau mot de passe doit contenir au moins 8 caractères.");
                    TxtNouveauMotDePasse.Focus();
                    return;
                }

                if (TxtNouveauMotDePasse.Password != TxtConfirmerMotDePasse.Password)
                {
                    AfficherErreurMotDePasse("La confirmation du mot de passe ne correspond pas.");
                    TxtConfirmerMotDePasse.Focus();
                    return;
                }

                var utilisateur = await _context.Utilisateurs.FindAsync(_utilisateurConnecte.Id);

                if (utilisateur == null)
                {
                    AfficherErreurMotDePasse("Utilisateur introuvable.");
                    return;
                }

                // Vérifier l'ancien mot de passe
                if (!BCrypt.Net.BCrypt.Verify(TxtMotDePasseActuel.Password, utilisateur.MotDePasseHash))
                {
                    AfficherErreurMotDePasse("Le mot de passe actuel est incorrect.");
                    TxtMotDePasseActuel.Focus();
                    return;
                }

                // Changer le mot de passe
                utilisateur.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(TxtNouveauMotDePasse.Password);
                await _context.SaveChangesAsync();

                // Vider les champs
                TxtMotDePasseActuel.Clear();
                TxtNouveauMotDePasse.Clear();
                TxtConfirmerMotDePasse.Clear();
                MasquerErreurMotDePasse();

                MessageBox.Show("Mot de passe modifié avec succès !", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AfficherErreurMotDePasse($"Erreur lors de la modification : {ex.Message}");
            }
        }

        private void BtnSauvegarderPreferences_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _preferencesService.NotificationEmail = ChkNotificationEmail.IsChecked ?? true;
                _preferencesService.NotificationValidation = ChkNotificationValidation.IsChecked ?? true;
                _preferencesService.NotificationRappel = ChkNotificationRappel.IsChecked ?? false;
                _preferencesService.AffichageCompact = ChkAffichageCompact.IsChecked ?? false;

                _preferencesService.FormatDate = CmbFormatDate.SelectedIndex switch
                {
                    0 => "dd/MM/yyyy",
                    1 => "MM/dd/yyyy",
                    2 => "yyyy-MM-dd",
                    _ => "dd/MM/yyyy"
                };

                _preferencesService.SauvegarderPreferences();

                MessageBox.Show("Préférences sauvegardées avec succès !", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde des préférences : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnResetPreferences_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Êtes-vous sûr de vouloir réinitialiser toutes vos préférences ?",
                                       "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _preferencesService.ResetPreferences();
                ChargerPreferences();
                MessageBox.Show("Préférences réinitialisées !", "Information",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AfficherErreurMotDePasse(string message)
        {
            TxtErrorPassword.Text = message;
            ErrorPanelPassword.Visibility = Visibility.Visible;
        }

        private void MasquerErreurMotDePasse()
        {
            ErrorPanelPassword.Visibility = Visibility.Collapsed;
        }

        protected override void OnClosed(EventArgs e)
        {
            _context?.Dispose();
            base.OnClosed(e);
        }
    }
}