using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Mail;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.WPF.Services;

namespace GestionConges.WPF.Views
{
    public partial class ParametresGlobauxWindow : Window
    {
        private ObservableCollection<JourFerie> _joursFeries;
        private ObservableCollection<RegleTypeViewModel> _reglesTypes;
        private int _anneeSelectionnee;

        public ParametresGlobauxWindow()
        {
            InitializeComponent();

            _joursFeries = new ObservableCollection<JourFerie>();
            _reglesTypes = new ObservableCollection<RegleTypeViewModel>();
            _anneeSelectionnee = DateTime.Now.Year;

            InitialiserInterface();
            ChargerParametres();
        }

        private GestionCongesContext CreerContexte()
        {
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=GestionCongesDB;Trusted_Connection=true;MultipleActiveResultSets=true";
            var options = new DbContextOptionsBuilder<GestionCongesContext>()
                .UseSqlServer(connectionString)
                .Options;
            return new GestionCongesContext(options);
        }

        private void InitialiserInterface()
        {
            // Initialiser la combo des années
            for (int annee = DateTime.Now.Year - 1; annee <= DateTime.Now.Year + 2; annee++)
            {
                CmbAnnee.Items.Add(annee);
            }
            CmbAnnee.SelectedItem = _anneeSelectionnee;

            // Initialiser les DataGrids
            DgJoursFeries.ItemsSource = _joursFeries;
            DgReglesTypes.ItemsSource = _reglesTypes;

            // Initialiser les templates d'emails
            LstTemplates.SelectedIndex = 0;

            // Événements pour les jours ouvrés
            ChkLundi.Checked += JoursOuvres_Changed;
            ChkMardi.Checked += JoursOuvres_Changed;
            ChkMercredi.Checked += JoursOuvres_Changed;
            ChkJeudi.Checked += JoursOuvres_Changed;
            ChkVendredi.Checked += JoursOuvres_Changed;
            ChkSamedi.Checked += JoursOuvres_Changed;
            ChkDimanche.Checked += JoursOuvres_Changed;

            ChkLundi.Unchecked += JoursOuvres_Changed;
            ChkMardi.Unchecked += JoursOuvres_Changed;
            ChkMercredi.Unchecked += JoursOuvres_Changed;
            ChkJeudi.Unchecked += JoursOuvres_Changed;
            ChkVendredi.Unchecked += JoursOuvres_Changed;
            ChkSamedi.Unchecked += JoursOuvres_Changed;
            ChkDimanche.Unchecked += JoursOuvres_Changed;
        }

        private async Task ChargerParametres()
        {
            try
            {
                await ChargerParametresCalendrier();
                await ChargerParametresValidation();
                await ChargerParametresEmail();
                await ChargerJoursFeries();
                await ChargerReglesTypes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des paramètres : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ChargerParametresCalendrier()
        {
            using var context = CreerContexte();
            var parametresService = new ParametresService(context);

            // Jours ouvrés
            var joursOuvres = await parametresService.ObtenirParametre("JoursOuvres", "1,2,3,4,5");
            var jours = joursOuvres!.Split(',').Select(int.Parse).ToList();

            ChkLundi.IsChecked = jours.Contains(1);
            ChkMardi.IsChecked = jours.Contains(2);
            ChkMercredi.IsChecked = jours.Contains(3);
            ChkJeudi.IsChecked = jours.Contains(4);
            ChkVendredi.IsChecked = jours.Contains(5);
            ChkSamedi.IsChecked = jours.Contains(6);
            ChkDimanche.IsChecked = jours.Contains(7);

            MettreAJourInfoJoursOuvres();

            // Options calcul
            var exclureFeries = await parametresService.ObtenirParametre<bool>("ExclureFeries", true);
            ChkExclureFeries.IsChecked = exclureFeries;

            ChkExclureWeekends.IsChecked = true; // Toujours vrai par conception
            ChkDemiJournees.IsChecked = true; // Feature déjà implémentée

            var debutAnnee = await parametresService.ObtenirParametre<int>("DebutAnneeConges", 1);
            CmbDebutAnnee.SelectedIndex = debutAnnee == 4 ? 1 : debutAnnee == 9 ? 2 : 0;
        }

        private async Task ChargerParametresValidation()
        {
            using var context = CreerContexte();
            var parametresService = new ParametresService(context);

            var delaiChefPole = await parametresService.ObtenirParametre("DelaiValidationChefPole", "7");
            TxtDelaiChefPole.Text = delaiChefPole;

            var delaiChefEquipe = await parametresService.ObtenirParametre("DelaiValidationChefEquipe", "5");
            TxtDelaiChefEquipe.Text = delaiChefEquipe;

            var preavis = await parametresService.ObtenirParametre("PreavisMinimum", "14");
            TxtPreavisConges.Text = preavis;

            var anticipation = await parametresService.ObtenirParametre("AnticipationMaximum", "365");
            TxtAnticipationMax.Text = anticipation;

            var escalade = await parametresService.ObtenirParametre<bool>("EscaladeAutomatique", false);
            ChkEscaladeAuto.IsChecked = escalade;

            // Options supplémentaires (nouvelles)
            ChkBloquerPreavis.IsChecked = false; // À implémenter
            ChkAlertePrevisVacances.IsChecked = true; // À implémenter
        }

        private async Task ChargerParametresEmail()
        {
            using var context = CreerContexte();
            var parametresService = new ParametresService(context);

            var emailActif = await parametresService.ObtenirParametre<bool>("EmailActif", false);
            ChkActiverEmails.IsChecked = emailActif;
            PanneauSMTP.IsEnabled = emailActif;
            BtnTesterEmail.IsEnabled = emailActif;

            if (emailActif)
            {
                var serveur = await parametresService.ObtenirParametre("ServeurSMTP", "smtp.gmail.com");
                TxtServeurSMTP.Text = serveur;

                var port = await parametresService.ObtenirParametre("PortSMTP", "587");
                TxtPortSMTP.Text = port;

                var utilisateur = await parametresService.ObtenirParametre("UtilisateurSMTP", "");
                TxtUtilisateurSMTP.Text = utilisateur;

                var ssl = await parametresService.ObtenirParametre<bool>("SSLSMTP", true);
                ChkSSL.IsChecked = ssl;

                // Le mot de passe n'est pas chargé pour des raisons de sécurité
            }
        }

        private async Task ChargerJoursFeries()
        {
            using var context = CreerContexte();
            var parametresService = new ParametresService(context);

            var joursFeries = await parametresService.ObtenirJoursFeries(_anneeSelectionnee);

            _joursFeries.Clear();
            foreach (var jour in joursFeries)
            {
                _joursFeries.Add(jour);
            }
        }

        private async Task ChargerReglesTypes()
        {
            using var context = CreerContexte();

            var typesAvecRegles = await context.TypesAbsences
                .Where(t => t.Actif)
                .Select(t => new RegleTypeViewModel
                {
                    TypeAbsenceId = t.Id,
                    Nom = t.Nom,
                    CouleurHex = t.CouleurHex,
                    MaximumParAn = 25, // Valeurs par défaut à personnaliser
                    MaximumConsecutif = 15,
                    PreavisMinimum = 14
                })
                .ToListAsync();

            _reglesTypes.Clear();
            foreach (var regle in typesAvecRegles)
            {
                _reglesTypes.Add(regle);
            }
        }

        private void JoursOuvres_Changed(object sender, RoutedEventArgs e)
        {
            MettreAJourInfoJoursOuvres();
        }

        private void MettreAJourInfoJoursOuvres()
        {
            var joursSelectionnes = 0;
            if (ChkLundi.IsChecked == true) joursSelectionnes++;
            if (ChkMardi.IsChecked == true) joursSelectionnes++;
            if (ChkMercredi.IsChecked == true) joursSelectionnes++;
            if (ChkJeudi.IsChecked == true) joursSelectionnes++;
            if (ChkVendredi.IsChecked == true) joursSelectionnes++;
            if (ChkSamedi.IsChecked == true) joursSelectionnes++;
            if (ChkDimanche.IsChecked == true) joursSelectionnes++;

            TxtJoursOuvresInfo.Text = $"{joursSelectionnes} jour{(joursSelectionnes > 1 ? "s" : "")} ouvré{(joursSelectionnes > 1 ? "s" : "")} par semaine sélectionné{(joursSelectionnes > 1 ? "s" : "")}";
        }

        private async void CmbAnnee_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbAnnee.SelectedItem != null)
            {
                _anneeSelectionnee = (int)CmbAnnee.SelectedItem;
                await ChargerJoursFeries();
            }
        }

        private async void BtnChargerFeries_Click(object sender, RoutedEventArgs e)
        {
            await ChargerJoursFeries();
            MessageBox.Show($"Jours fériés de {_anneeSelectionnee} rechargés.", "Information",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnAjouterFerie_Click(object sender, RoutedEventArgs e)
        {
            var nouveauFerie = new AjouterJourFerieWindow(_anneeSelectionnee);
            var result = nouveauFerie.ShowDialog();

            if (result == true && nouveauFerie.JourFerie != null)
            {
                _joursFeries.Add(nouveauFerie.JourFerie);
            }
        }

        private async void BtnSupprimerFerie_Click(object sender, RoutedEventArgs e)
        {
            var ferieSelectionne = DgJoursFeries.SelectedItem as JourFerie;
            if (ferieSelectionne != null)
            {
                var result = MessageBox.Show($"Supprimer le jour férié '{ferieSelectionne.Nom}' du {ferieSelectionne.Date:dd/MM/yyyy} ?",
                                           "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = CreerContexte();
                        var ferieASupprimer = context.JoursFeries.Find(ferieSelectionne.Id);
                        if (ferieASupprimer != null)
                        {
                            context.JoursFeries.Remove(ferieASupprimer);
                            await context.SaveChangesAsync();
                            _joursFeries.Remove(ferieSelectionne);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur lors de la suppression : {ex.Message}",
                                      "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void DgJoursFeries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnSupprimerFerie.IsEnabled = DgJoursFeries.SelectedItem != null;
        }

        private async void BtnImporterFeries_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show($"Importer les jours fériés français pour {_anneeSelectionnee} ?\n\n" +
                                           "Cela ajoutera les jours fériés nationaux français.",
                                           "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await ImporterJoursFeriesFrance(_anneeSelectionnee);
                    await ChargerJoursFeries();
                    MessageBox.Show("Jours fériés français importés avec succès.", "Succès",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'import : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ImporterJoursFeriesFrance(int annee)
        {
            using var context = CreerContexte();

            // Calculer les dates variables (Pâques, etc.)
            var paques = CalculerDatePaques(annee);
            var lundiPaques = paques.AddDays(1);
            var ascension = paques.AddDays(39);
            var lundiPentecote = paques.AddDays(50);

            var joursFeries = new List<JourFerie>
            {
                new() { Date = new DateTime(annee, 1, 1), Nom = "Nouvel An", Type = "National", Recurrent = true },
                new() { Date = lundiPaques, Nom = "Lundi de Pâques", Type = "National", Recurrent = false },
                new() { Date = new DateTime(annee, 5, 1), Nom = "Fête du Travail", Type = "National", Recurrent = true },
                new() { Date = new DateTime(annee, 5, 8), Nom = "Fête de la Victoire", Type = "National", Recurrent = true },
                new() { Date = ascension, Nom = "Ascension", Type = "National", Recurrent = false },
                new() { Date = lundiPentecote, Nom = "Lundi de Pentecôte", Type = "National", Recurrent = false },
                new() { Date = new DateTime(annee, 7, 14), Nom = "Fête Nationale", Type = "National", Recurrent = true },
                new() { Date = new DateTime(annee, 8, 15), Nom = "Assomption", Type = "National", Recurrent = true },
                new() { Date = new DateTime(annee, 11, 1), Nom = "Toussaint", Type = "National", Recurrent = true },
                new() { Date = new DateTime(annee, 11, 11), Nom = "Armistice", Type = "National", Recurrent = true },
                new() { Date = new DateTime(annee, 12, 25), Nom = "Noël", Type = "National", Recurrent = true }
            };

            foreach (var ferie in joursFeries)
            {
                // Vérifier si le jour férié n'existe pas déjà
                var existe = await context.JoursFeries
                    .AnyAsync(j => j.Date.Date == ferie.Date.Date);

                if (!existe)
                {
                    ferie.DateCreation = DateTime.Now;
                    context.JoursFeries.Add(ferie);
                }
            }

            await context.SaveChangesAsync();
        }

        private DateTime CalculerDatePaques(int annee)
        {
            // Algorithme de calcul de Pâques (algorithme de Gauss)
            int a = annee % 19;
            int b = annee / 100;
            int c = annee % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int n = (h + l - 7 * m + 114) / 31;
            int p = (h + l - 7 * m + 114) % 31;

            return new DateTime(annee, n, p + 1);
        }

        private void ChkActiverEmails_Checked(object sender, RoutedEventArgs e)
        {
            PanneauSMTP.IsEnabled = true;
            BtnTesterEmail.IsEnabled = true;
        }

        private void ChkActiverEmails_Unchecked(object sender, RoutedEventArgs e)
        {
            PanneauSMTP.IsEnabled = false;
            BtnTesterEmail.IsEnabled = false;
        }

        private async void BtnTesterEmail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TxtUtilisateurSMTP.Text) ||
                    string.IsNullOrWhiteSpace(TxtMotDePasseSMTP.Password))
                {
                    MessageBox.Show("Veuillez remplir le nom d'utilisateur et le mot de passe SMTP.",
                                  "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var client = new SmtpClient(TxtServeurSMTP.Text, int.Parse(TxtPortSMTP.Text))
                {
                    Credentials = new NetworkCredential(TxtUtilisateurSMTP.Text, TxtMotDePasseSMTP.Password),
                    EnableSsl = ChkSSL.IsChecked == true
                };

                var message = new MailMessage
                {
                    From = new MailAddress(TxtUtilisateurSMTP.Text),
                    Subject = "Test de configuration SMTP - Gestion des Congés",
                    Body = $"Ceci est un email de test envoyé le {DateTime.Now:dd/MM/yyyy HH:mm} pour vérifier la configuration SMTP.",
                    IsBodyHtml = false
                };

                message.To.Add(TxtUtilisateurSMTP.Text);

                await client.SendMailAsync(message);

                MessageBox.Show("Email de test envoyé avec succès !", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'envoi de l'email de test :\n{ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LstTemplates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Charger le template sélectionné
            if (LstTemplates.SelectedItem is ListBoxItem item)
            {
                var template = item.Tag?.ToString();
                ChargerTemplate(template);
            }
        }

        private void ChargerTemplate(string? template)
        {
            // Templates par défaut (à personnaliser)
            switch (template)
            {
                case "nouvelle_demande":
                    TxtSujetTemplate.Text = "Nouvelle demande de congés - {NomUtilisateur}";
                    TxtCorpsTemplate.Text = "Bonjour,\n\n{NomUtilisateur} a soumis une nouvelle demande de {TypeConge} du {DateDebut} au {DateFin} ({NombreJours} jours).\n\nVeuillez vous connecter à l'application pour valider cette demande.\n\nCordialement,\nGestion des Congés";
                    break;
                case "demande_approuvee":
                    TxtSujetTemplate.Text = "Demande approuvée - {TypeConge}";
                    TxtCorpsTemplate.Text = "Bonjour {NomUtilisateur},\n\nVotre demande de {TypeConge} du {DateDebut} au {DateFin} ({NombreJours} jours) a été approuvée.\n\nBonnes vacances !\n\nCordialement,\nGestion des Congés";
                    break;
                case "demande_refusee":
                    TxtSujetTemplate.Text = "Demande refusée - {TypeConge}";
                    TxtCorpsTemplate.Text = "Bonjour {NomUtilisateur},\n\nVotre demande de {TypeConge} du {DateDebut} au {DateFin} a été refusée.\n\nMotif : {MotifRefus}\n\nN'hésitez pas à contacter votre manager pour plus d'informations.\n\nCordialement,\nGestion des Congés";
                    break;
                case "rappel_validation":
                    TxtSujetTemplate.Text = "Rappel - Demandes en attente de validation";
                    TxtCorpsTemplate.Text = "Bonjour,\n\nVous avez des demandes de congés en attente de validation :\n\n- {NomUtilisateur} : {TypeConge} du {DateDebut} au {DateFin}\n\nVeuillez vous connecter à l'application pour traiter ces demandes.\n\nCordialement,\nGestion des Congés";
                    break;
                default:
                    TxtSujetTemplate.Text = "";
                    TxtCorpsTemplate.Text = "";
                    break;
            }
        }

        private async void BtnSauvegarderParametres_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SauvegarderTousLesParametres();
                MessageBox.Show("Tous les paramètres ont été sauvegardés avec succès !", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SauvegarderTousLesParametres()
        {
            using var context = CreerContexte();
            var parametresService = new ParametresService(context);

            // Sauvegarder jours ouvrés
            var joursOuvres = new List<int>();
            if (ChkLundi.IsChecked == true) joursOuvres.Add(1);
            if (ChkMardi.IsChecked == true) joursOuvres.Add(2);
            if (ChkMercredi.IsChecked == true) joursOuvres.Add(3);
            if (ChkJeudi.IsChecked == true) joursOuvres.Add(4);
            if (ChkVendredi.IsChecked == true) joursOuvres.Add(5);
            if (ChkSamedi.IsChecked == true) joursOuvres.Add(6);
            if (ChkDimanche.IsChecked == true) joursOuvres.Add(7);

            await parametresService.SauvegarderParametre("JoursOuvres", string.Join(",", joursOuvres), "Calendrier");
            await parametresService.SauvegarderParametre("ExclureFeries", (ChkExclureFeries.IsChecked == true).ToString(), "Calendrier");

            var debutAnnee = CmbDebutAnnee.SelectedIndex switch
            {
                1 => 4,  // Avril
                2 => 9,  // Septembre
                _ => 1   // Janvier
            };
            await parametresService.SauvegarderParametre("DebutAnneeConges", debutAnnee.ToString(), "Calendrier");

            // Sauvegarder paramètres validation
            await parametresService.SauvegarderParametre("DelaiValidationChefPole", TxtDelaiChefPole.Text, "Validation");
            await parametresService.SauvegarderParametre("DelaiValidationChefEquipe", TxtDelaiChefEquipe.Text, "Validation");
            await parametresService.SauvegarderParametre("PreavisMinimum", TxtPreavisConges.Text, "Validation");
            await parametresService.SauvegarderParametre("AnticipationMaximum", TxtAnticipationMax.Text, "Validation");
            await parametresService.SauvegarderParametre("EscaladeAutomatique", (ChkEscaladeAuto.IsChecked == true).ToString(), "Validation");

            // Sauvegarder paramètres email
            await parametresService.SauvegarderParametre("EmailActif", (ChkActiverEmails.IsChecked == true).ToString(), "Email");

            if (ChkActiverEmails.IsChecked == true)
            {
                await parametresService.SauvegarderParametre("ServeurSMTP", TxtServeurSMTP.Text, "Email");
                await parametresService.SauvegarderParametre("PortSMTP", TxtPortSMTP.Text, "Email");
                await parametresService.SauvegarderParametre("UtilisateurSMTP", TxtUtilisateurSMTP.Text, "Email");
                await parametresService.SauvegarderParametre("SSLSMTP", (ChkSSL.IsChecked == true).ToString(), "Email");

                // Sauvegarder le mot de passe seulement s'il a été modifié
                if (!string.IsNullOrWhiteSpace(TxtMotDePasseSMTP.Password))
                {
                    // En production, vous devriez crypter le mot de passe
                    await parametresService.SauvegarderParametre("MotDePasseSMTP", TxtMotDePasseSMTP.Password, "Email");
                }
            }

            // Vider le cache pour forcer le rechargement
            parametresService.ViderCache();
        }

        private async void BtnResetParametres_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Êtes-vous sûr de vouloir réinitialiser tous les paramètres aux valeurs par défaut ?",
                                       "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await ResetParametresParDefaut();
                    await ChargerParametres();
                    MessageBox.Show("Paramètres réinitialisés avec succès.", "Succès",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la réinitialisation : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ResetParametresParDefaut()
        {
            using var context = CreerContexte();
            var parametresService = new ParametresService(context);

            await parametresService.SauvegarderParametre("JoursOuvres", "1,2,3,4,5", "Calendrier");
            await parametresService.SauvegarderParametre("ExclureFeries", "true", "Calendrier");
            await parametresService.SauvegarderParametre("DebutAnneeConges", "1", "Calendrier");
            await parametresService.SauvegarderParametre("DelaiValidationChefPole", "7", "Validation");
            await parametresService.SauvegarderParametre("DelaiValidationChefEquipe", "5", "Validation");
            await parametresService.SauvegarderParametre("PreavisMinimum", "14", "Validation");
            await parametresService.SauvegarderParametre("AnticipationMaximum", "365", "Validation");
            await parametresService.SauvegarderParametre("EscaladeAutomatique", "false", "Validation");
            await parametresService.SauvegarderParametre("EmailActif", "false", "Email");

            parametresService.ViderCache();
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Nettoyage si nécessaire
            base.OnClosed(e);
        }
    }

    // ViewModels pour les DataGrids
    public class RegleTypeViewModel
    {
        public int TypeAbsenceId { get; set; }
        public string Nom { get; set; } = "";
        public string CouleurHex { get; set; } = "#3498db";
        public int? MaximumParAn { get; set; }
        public int? MaximumConsecutif { get; set; }
        public int? PreavisMinimum { get; set; }
    }
}