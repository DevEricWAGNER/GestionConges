using System.Windows;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace GestionConges.WPF.Views
{
    public partial class PremierAdminWindow : Window
    {
        private readonly string _login;
        private readonly string _motDePasse;
        public Utilisateur? AdminCree { get; private set; }

        public PremierAdminWindow(string login, string motDePasse)
        {
            InitializeComponent();
            _login = login;
            _motDePasse = motDePasse;

            ChargerDonnees();
        }

        private async void ChargerDonnees()
        {
            try
            {
                // Pré-remplir les champs
                TxtEmail.Text = _login;
                TxtMotDePasse.Password = _motDePasse;

                using var context = App.GetService<GestionCongesContext>();

                // Charger les sociétés existantes
                var societes = await context.Societes
                    .Where(s => s.Actif)
                    .OrderBy(s => s.Nom)
                    .ToListAsync();

                CmbSociete.Items.Clear();
                foreach (var societe in societes)
                {
                    CmbSociete.Items.Add(new System.Windows.Controls.ComboBoxItem
                    {
                        Content = societe.Nom,
                        Tag = societe.Id
                    });
                }

                // Sélectionner la première société si disponible
                if (CmbSociete.Items.Count > 0)
                {
                    CmbSociete.SelectedIndex = 0;
                    await ChargerEquipesPourSociete();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CmbSociete_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await ChargerEquipesPourSociete();
        }

        private async void CmbEquipe_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            await ChargerPolesPourEquipe();
        }

        private async Task ChargerEquipesPourSociete()
        {
            try
            {
                CmbEquipe.Items.Clear();
                CmbPole.Items.Clear();

                if (CmbSociete.SelectedItem is System.Windows.Controls.ComboBoxItem societeItem &&
                    societeItem.Tag is int societeId)
                {
                    using var context = App.GetService<GestionCongesContext>();
                    var equipes = await context.Equipes
                        .Where(e => e.SocieteId == societeId && e.Actif)
                        .OrderBy(e => e.Nom)
                        .ToListAsync();

                    foreach (var equipe in equipes)
                    {
                        CmbEquipe.Items.Add(new System.Windows.Controls.ComboBoxItem
                        {
                            Content = equipe.Nom,
                            Tag = equipe.Id
                        });
                    }

                    // Sélectionner la première équipe si disponible
                    if (CmbEquipe.Items.Count > 0)
                    {
                        CmbEquipe.SelectedIndex = 0;
                        await ChargerPolesPourEquipe();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des équipes : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ChargerPolesPourEquipe()
        {
            try
            {
                CmbPole.Items.Clear();

                // Ajouter l'option "Aucun pôle"
                CmbPole.Items.Add(new System.Windows.Controls.ComboBoxItem
                {
                    Content = "Aucun pôle",
                    Tag = null
                });

                if (CmbEquipe.SelectedItem is System.Windows.Controls.ComboBoxItem equipeItem &&
                    equipeItem.Tag is int equipeId)
                {
                    using var context = App.GetService<GestionCongesContext>();
                    var poles = await context.EquipesPoles
                        .Where(ep => ep.EquipeId == equipeId && ep.Actif)
                        .Include(ep => ep.Pole)
                        .Select(ep => ep.Pole)
                        .Where(p => p.Actif)
                        .OrderBy(p => p.Nom)
                        .ToListAsync();

                    foreach (var pole in poles)
                    {
                        CmbPole.Items.Add(new System.Windows.Controls.ComboBoxItem
                        {
                            Content = pole.Nom,
                            Tag = pole.Id
                        });
                    }
                }

                // Sélectionner "Aucun pôle" par défaut
                CmbPole.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des pôles : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnCreer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation des champs obligatoires
                if (string.IsNullOrWhiteSpace(TxtNom.Text))
                {
                    MessageBox.Show("Le nom est obligatoire.", "Validation",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNom.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtPrenom.Text))
                {
                    MessageBox.Show("Le prénom est obligatoire.", "Validation",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPrenom.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(TxtEmail.Text))
                {
                    MessageBox.Show("L'email est obligatoire.", "Validation",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtEmail.Focus();
                    return;
                }

                if (CmbSociete.SelectedItem == null)
                {
                    MessageBox.Show("La société est obligatoire.", "Validation",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    CmbSociete.Focus();
                    return;
                }

                if (CmbEquipe.SelectedItem == null)
                {
                    MessageBox.Show("L'équipe est obligatoire.", "Validation",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    CmbEquipe.Focus();
                    return;
                }

                // Validation format email
                if (!EstEmailValide(TxtEmail.Text))
                {
                    MessageBox.Show("Format d'email invalide.", "Validation",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtEmail.Focus();
                    return;
                }

                // Récupération des IDs sélectionnés
                int societeId = (int)((System.Windows.Controls.ComboBoxItem)CmbSociete.SelectedItem).Tag;
                int equipeId = (int)((System.Windows.Controls.ComboBoxItem)CmbEquipe.SelectedItem).Tag;
                int? poleId = null;

                if (CmbPole.SelectedItem is System.Windows.Controls.ComboBoxItem poleItem &&
                    poleItem.Tag is int selectedPoleId)
                {
                    poleId = selectedPoleId;
                }

                // Confirmation finale
                var confirmation = MessageBox.Show(
                    $"Confirmer la création du compte administrateur ?\n\n" +
                    $"Nom : {TxtNom.Text} {TxtPrenom.Text}\n" +
                    $"Email : {TxtEmail.Text}\n" +
                    $"Login : {_login}\n" +
                    $"Société : {((System.Windows.Controls.ComboBoxItem)CmbSociete.SelectedItem).Content}\n" +
                    $"Équipe : {((System.Windows.Controls.ComboBoxItem)CmbEquipe.SelectedItem).Content}\n" +
                    $"Pôle : {(poleId.HasValue ? ((System.Windows.Controls.ComboBoxItem)CmbPole.SelectedItem).Content : "Aucun")}\n\n" +
                    "Ce compte aura tous les droits d'administration.",
                    "Confirmer la création",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes)
                    return;

                // Création du compte administrateur
                using var context = App.GetService<GestionCongesContext>();

                // Vérifier une dernière fois qu'il n'y a aucun utilisateur
                var hasUsers = await context.Utilisateurs.AnyAsync();
                if (hasUsers)
                {
                    MessageBox.Show("Un utilisateur existe déjà dans la base de données.",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    Close();
                    return;
                }

                // Créer l'administrateur
                var admin = new Utilisateur
                {
                    Nom = TxtNom.Text.Trim(),
                    Prenom = TxtPrenom.Text.Trim(),
                    Email = TxtEmail.Text.Trim(),
                    MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(_motDePasse),
                    Role = RoleUtilisateur.ChefEquipe, // Administrateur complet
                    SocieteId = societeId,
                    EquipeId = equipeId,
                    PoleId = poleId,
                    Admin = true, // Marquer comme administrateur système
                    Actif = true,
                    DateCreation = DateTime.Now,
                    DerniereConnexion = DateTime.Now
                };

                context.Utilisateurs.Add(admin);
                await context.SaveChangesAsync();

                // Créer automatiquement les droits de validation pour toutes les sociétés
                var toutesLesSocietes = await context.Societes.Where(s => s.Actif).ToListAsync();
                foreach (var societe in toutesLesSocietes)
                {
                    var validateurSociete = new ValidateurSociete
                    {
                        ValidateurId = admin.Id,
                        SocieteId = societe.Id,
                        NiveauValidation = 2, // Chef d'équipe (niveau maximum)
                        Actif = true,
                        DateAffectation = DateTime.Now
                    };
                    context.ValidateursSocietes.Add(validateurSociete);
                }

                await context.SaveChangesAsync();

                AdminCree = admin;

                // Message de succès
                MessageBox.Show(
                    "Félicitations ! Votre compte administrateur a été créé avec succès.\n\n" +
                    "Vous allez maintenant être connecté à l'application.\n\n" +
                    "Informations importantes :\n" +
                    "• Vous avez des droits de validation sur toutes les sociétés\n" +
                    "• Configurez les paramètres dans Administration\n" +
                    "• Créez les types d'absences nécessaires\n" +
                    "• Ajoutez les autres utilisateurs de votre organisation\n" +
                    "• Gérez les droits de validation selon vos besoins",
                    "Compte créé avec succès !",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création du compte : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr d'annuler la création du compte administrateur ?\n\n" +
                "L'application ne pourra pas être utilisée sans au moins un compte utilisateur.",
                "Confirmer l'annulation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        private bool EstEmailValide(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}