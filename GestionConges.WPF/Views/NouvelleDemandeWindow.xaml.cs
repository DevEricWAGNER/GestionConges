using GestionConges.Core.Data;
using GestionConges.Core.Enums;
using GestionConges.Core.Models;
using GestionConges.WPF.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestionConges.WPF.Views
{
    public partial class NouvelleDemandeWindow : Window
    {
        private readonly Utilisateur _utilisateurConnecte;
        private List<TypeAbsence> _typesAbsence;
        private List<DemandeConge> _demandesExistantes;
        private DemandeConge? _demandeEnCours; // Pour modification d'un brouillon existant

        public bool DemandeCreee { get; private set; } = false;

        public NouvelleDemandeWindow(DemandeConge? demandeAModifier = null)
        {
            InitializeComponent();

            _utilisateurConnecte = App.UtilisateurConnecte ?? throw new InvalidOperationException("Aucun utilisateur connecté");
            _typesAbsence = new List<TypeAbsence>();
            _demandesExistantes = new List<DemandeConge>();
            _demandeEnCours = demandeAModifier;

            InitialiserInterface();
            ChargerDonneesInitiales();
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
            // Remplir les informations du demandeur
            TxtNomDemandeur.Text = _utilisateurConnecte.NomComplet;
            TxtPoleDemandeur.Text = _utilisateurConnecte.Pole?.Nom ?? "Aucun pôle";

            // Dates par défaut
            DpDateDebut.SelectedDate = DateTime.Today.AddDays(1);
            DpDateFin.SelectedDate = DateTime.Today.AddDays(1);

            // Si c'est une modification, adapter le titre
            if (_demandeEnCours != null)
            {
                Title = "Modification de Demande de Congés";
                ChargerDemande(_demandeEnCours);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Optionnel: gérer le double-clic pour maximiser
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir fermer ? Toutes les modifications seront perdues.",
                "Confirmer la fermeture",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        private async void ChargerDonneesInitiales()
        {
            try
            {
                using var context = CreerContexte();

                // Charger les types d'absence actifs
                _typesAbsence = await context.TypesAbsences
                    .Where(t => t.Actif)
                    .OrderBy(t => t.OrdreAffichage)
                    .ToListAsync();

                // Charger les demandes existantes de l'utilisateur (pour vérifier les conflits)
                _demandesExistantes = await context.DemandesConges
                    .Where(d => d.UtilisateurId == _utilisateurConnecte.Id &&
                               d.Statut != StatusDemande.Refuse &&
                               d.Statut != StatusDemande.Annule)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    // Remplir la combobox des types
                    CmbTypeAbsence.ItemsSource = _typesAbsence;
                    CmbTypeAbsence.SelectedValuePath = "Id";

                    if (_typesAbsence.Any())
                        CmbTypeAbsence.SelectedIndex = 0;

                    // Calculer les jours initialement
                    CalculerNombreJours();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerDemande(DemandeConge demande)
        {
            DpDateDebut.SelectedDate = demande.DateDebut;
            DpDateFin.SelectedDate = demande.DateFin;
            CmbTypeJourneeDebut.SelectedIndex = (int)demande.TypeJourneeDebut;
            CmbTypeJourneeFin.SelectedIndex = (int)demande.TypeJourneeFin;
            TxtCommentaire.Text = demande.Commentaire ?? string.Empty;

            // Le type sera sélectionné une fois les données chargées
            Dispatcher.BeginInvoke(() =>
            {
                CmbTypeAbsence.SelectedValue = demande.TypeAbsenceId;
            });
        }

        private void CalculerNombreJours()
        {
            if (DpDateDebut.SelectedDate == null || DpDateFin.SelectedDate == null)
            {
                TxtNombreJours.Text = "Durée calculée : 0 jour(s)";
                TxtDetailsCalcul.Text = "Sélectionnez les dates pour voir le calcul";
                return;
            }

            var dateDebut = DpDateDebut.SelectedDate.Value;
            var dateFin = DpDateFin.SelectedDate.Value;

            if (dateFin < dateDebut)
            {
                TxtNombreJours.Text = "Durée calculée : Erreur";
                TxtDetailsCalcul.Text = "La date de fin doit être après la date de début";
                return;
            }

            // Calcul simple : nombre de jours ouvrés (lundi à vendredi)
            decimal totalJours = 0;
            var dateActuelle = dateDebut;

            while (dateActuelle <= dateFin)
            {
                // Seulement les jours ouvrés (lundi à vendredi)
                if (dateActuelle.DayOfWeek >= DayOfWeek.Monday && dateActuelle.DayOfWeek <= DayOfWeek.Friday)
                {
                    decimal joursAjouter = 1; // Par défaut journée complète

                    // Premier jour
                    if (dateActuelle == dateDebut)
                    {
                        var typeDebut = (TypeJournee)CmbTypeJourneeDebut.SelectedIndex;
                        joursAjouter = typeDebut == TypeJournee.JourneeComplete ? 1 : 0.5m;
                    }
                    // Dernier jour (si différent du premier)
                    else if (dateActuelle == dateFin && dateFin != dateDebut)
                    {
                        var typeFin = (TypeJournee)CmbTypeJourneeFin.SelectedIndex;
                        joursAjouter = typeFin == TypeJournee.JourneeComplete ? 1 : 0.5m;
                    }

                    totalJours += joursAjouter;
                }
                dateActuelle = dateActuelle.AddDays(1);
            }

            TxtNombreJours.Text = $"Durée calculée : {totalJours} jour(s)";

            var details = $"Du {dateDebut:dd/MM/yyyy} au {dateFin:dd/MM/yyyy}";
            if (dateDebut == dateFin)
            {
                var type = (TypeJournee)CmbTypeJourneeDebut.SelectedIndex;
                details += type == TypeJournee.JourneeComplete ? " (journée complète)"
                    : type == TypeJournee.MatiMatin ? " (matin seulement)"
                    : " (après-midi seulement)";
            }
            TxtDetailsCalcul.Text = details;

            // Vérifier les conflits
            VerifierConflits();
        }

        private void VerifierConflits()
        {
            if (DpDateDebut.SelectedDate == null || DpDateFin.SelectedDate == null)
            {
                PanneauConflits.Visibility = Visibility.Collapsed;
                return;
            }

            var dateDebut = DpDateDebut.SelectedDate.Value;
            var dateFin = DpDateFin.SelectedDate.Value;

            var conflits = _demandesExistantes
                .Where(d => d.Id != (_demandeEnCours?.Id ?? 0)) // Exclure la demande en cours de modification
                .Where(d => !(d.DateFin < dateDebut || d.DateDebut > dateFin))
                .ToList();

            if (conflits.Any())
            {
                var messageConflits = "Vous avez déjà des demandes sur cette période :\n";
                foreach (var conflit in conflits)
                {
                    messageConflits += $"• {conflit.DateDebut:dd/MM/yyyy} au {conflit.DateFin:dd/MM/yyyy} - {conflit.StatutLibelle}\n";
                }

                TxtConflits.Text = messageConflits;
                PanneauConflits.Visibility = Visibility.Visible;
            }
            else
            {
                PanneauConflits.Visibility = Visibility.Collapsed;
            }
        }

        private void DpDateDebut_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Si on change la date de début, ajuster la date de fin si nécessaire
            if (DpDateDebut.SelectedDate.HasValue && DpDateFin.SelectedDate.HasValue)
            {
                if (DpDateFin.SelectedDate < DpDateDebut.SelectedDate)
                {
                    DpDateFin.SelectedDate = DpDateDebut.SelectedDate;
                }
            }
            CalculerNombreJours();
        }

        private void DpDateFin_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculerNombreJours();
        }

        private void CmbTypeAbsence_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Recalculer si nécessaire (certains types peuvent avoir des règles spéciales)
            CalculerNombreJours();
        }

        private async void BtnSauvegarderBrouillon_Click(object sender, RoutedEventArgs e)
        {
            await SauvegarderDemande(StatusDemande.Brouillon);
        }

        private async void BtnSoumettre_Click(object sender, RoutedEventArgs e)
        {
            // Validation avant soumission
            if (!ValiderFormulaire())
                return;

            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir soumettre cette demande ?\n\n" +
                "Une fois soumise, elle ne pourra plus être modifiée jusqu'à validation.",
                "Confirmer la soumission",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await SauvegarderDemande(StatusDemande.EnAttenteValidateur);
            }
        }

        private bool ValiderFormulaire()
        {
            if (CmbTypeAbsence.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un type d'absence.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbTypeAbsence.Focus();
                return false;
            }

            if (DpDateDebut.SelectedDate == null)
            {
                MessageBox.Show("Veuillez sélectionner une date de début.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                DpDateDebut.Focus();
                return false;
            }

            if (DpDateFin.SelectedDate == null)
            {
                MessageBox.Show("Veuillez sélectionner une date de fin.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                DpDateFin.Focus();
                return false;
            }

            if (DpDateFin.SelectedDate < DpDateDebut.SelectedDate)
            {
                MessageBox.Show("La date de fin doit être après la date de début.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                DpDateFin.Focus();
                return false;
            }

            // Vérifier que ce n'est pas dans le passé (sauf pour les brouillons)
            if (DpDateDebut.SelectedDate < DateTime.Today)
            {
                var result = MessageBox.Show(
                    "La date de début est dans le passé. Voulez-vous continuer ?",
                    "Date dans le passé",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return false;
            }

            return true;
        }

        private async Task EnvoyerNotificationNouvelleDemande(DemandeConge demande)
        {
            try
            {
                using var context = CreerContexte();
                var emailService = new EmailService(context);

                // Vérifier si les emails sont activés
                if (!await emailService.EstActive())
                {
                    System.Diagnostics.Debug.WriteLine("📧 Emails désactivés - pas de notification envoyée");
                    return;
                }

                // Déterminer qui doit recevoir la notification selon le statut
                Utilisateur? validateur = null;

                switch (demande.Statut)
                {
                    case StatusDemande.EnAttenteValidateur:
                        // Chercher le chef de pôle de l'utilisateur
                        validateur = await context.Utilisateurs
                            .FirstOrDefaultAsync(u => u.Role == RoleUtilisateur.Validateur &&
                                                     u.PoleId == _utilisateurConnecte.PoleId &&
                                                     u.Actif);
                        break;

                    case StatusDemande.EnAttenteAdmin:
                        // Chercher le chef d'équipe
                        validateur = await context.Utilisateurs
                            .FirstOrDefaultAsync(u => u.Role == RoleUtilisateur.Admin && u.Actif);
                        break;

                    case StatusDemande.Approuve:
                        // Pas de notification si auto-approuvée (chef d'équipe)
                        System.Diagnostics.Debug.WriteLine("📧 Demande auto-approuvée - pas de notification envoyée");
                        return;

                    default:
                        System.Diagnostics.Debug.WriteLine($"📧 Statut {demande.Statut} - pas de notification");
                        return;
                }

                if (validateur != null && !string.IsNullOrEmpty(validateur.Email))
                {
                    System.Diagnostics.Debug.WriteLine($"📧 Envoi notification à {validateur.NomComplet} ({validateur.Email})");

                    var success = await emailService.EnvoyerNotificationNouvelleDemande(demande, validateur);

                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Notification envoyée avec succès");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Échec envoi notification");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Aucun validateur trouvé ou email manquant pour le statut {demande.Statut}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 Erreur envoi notification : {ex.Message}");
                // Ne pas faire échouer la demande pour un problème d'email
            }
        }


        private async Task SauvegarderDemande(StatusDemande statut)
        {
            try
            {
                using var context = CreerContexte();

                DemandeConge demande;
                bool nouvelleDemande = _demandeEnCours == null;

                if (nouvelleDemande)
                {
                    demande = new DemandeConge
                    {
                        UtilisateurId = _utilisateurConnecte.Id,
                        DateCreation = DateTime.Now
                    };
                    context.DemandesConges.Add(demande);
                }
                else
                {
                    demande = context.DemandesConges.Find(_demandeEnCours!.Id)!;
                    demande.DateModification = DateTime.Now;
                }

                // Remplir les données
                demande.TypeAbsenceId = (int)CmbTypeAbsence.SelectedValue;
                demande.DateDebut = DpDateDebut.SelectedDate!.Value;
                demande.DateFin = DpDateFin.SelectedDate!.Value;
                demande.TypeJourneeDebut = (TypeJournee)CmbTypeJourneeDebut.SelectedIndex;
                demande.TypeJourneeFin = (TypeJournee)CmbTypeJourneeFin.SelectedIndex;
                demande.Commentaire = string.IsNullOrWhiteSpace(TxtCommentaire.Text) ? null : TxtCommentaire.Text.Trim();

                // Calculer le nombre de jours
                demande.NombreJours = CalculerNombreJoursReel(demande.DateDebut, demande.DateFin,
                    demande.TypeJourneeDebut, demande.TypeJourneeFin);

                if (statut == StatusDemande.Brouillon)
                {
                    demande.Statut = StatusDemande.Brouillon;
                }
                else
                {
                    demande.Statut = DeterminerStatutInitial();
                    if (demande.Statut == StatusDemande.Approuve)
                    {
                        demande.DateValidationFinale = DateTime.Now;
                    }
                }

                // Sauvegarder d'abord la demande
                await context.SaveChangesAsync();

                // ✅ NOUVEAU : Envoyer notification email si ce n'est pas un brouillon
                if (statut != StatusDemande.Brouillon)
                {
                    // Recharger la demande avec ses relations pour l'email
                    var demandeComplete = await context.DemandesConges
                        .Include(d => d.Utilisateur)
                            .ThenInclude(u => u.Pole)
                        .Include(d => d.TypeAbsence)
                        .FirstOrDefaultAsync(d => d.Id == demande.Id);

                    if (demandeComplete != null)
                    {
                        await EnvoyerNotificationNouvelleDemande(demandeComplete);
                    }
                }

                DemandeCreee = true;

                string message;
                if (statut == StatusDemande.Brouillon)
                {
                    message = "Demande sauvegardée en brouillon avec succès !";
                }
                else if (_utilisateurConnecte.Role == RoleUtilisateur.Admin)
                {
                    message = "Demande approuvée automatiquement ! (Chef d'équipe)";
                }
                else
                {
                    message = "Demande soumise avec succès ! Elle sera examinée par votre hiérarchie.\n\n📧 Une notification a été envoyée au validateur.";
                }

                MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private StatusDemande DeterminerStatutInitial()
        {
            // Si c'est un chef d'équipe → approuvé automatiquement
            if (_utilisateurConnecte.Role == RoleUtilisateur.Admin)
            {
                return StatusDemande.Approuve;
            }

            // Si c'est un chef de pôle → va directement au chef équipe
            if (_utilisateurConnecte.Role == RoleUtilisateur.Validateur)
            {
                return StatusDemande.EnAttenteAdmin;
            }

            // Pour un employé : vérifier s'il y a un chef de pôle dans son pôle
            if (_utilisateurConnecte.PoleId.HasValue)
            {
                // Dans ce contexte simple, on suppose qu'il y a toujours un chef de pôle
                // (vous pourriez ajouter une vérification en base si nécessaire)
                return StatusDemande.EnAttenteValidateur;
            }

            // Pas de pôle → directement chef équipe
            return StatusDemande.EnAttenteAdmin;
        }

        private async Task<StatusDemande> DeterminerStatutInitialAsync()
        {
            // Si c'est un chef d'équipe → approuvé automatiquement
            if (_utilisateurConnecte.Role == RoleUtilisateur.Admin)
            {
                return StatusDemande.Approuve;
            }

            // Si c'est un chef de pôle → va directement au chef équipe
            if (_utilisateurConnecte.Role == RoleUtilisateur.Validateur)
            {
                return StatusDemande.EnAttenteAdmin;
            }

            // Si l'utilisateur n'a pas de pôle → directement chef équipe
            if (!_utilisateurConnecte.PoleId.HasValue)
            {
                return StatusDemande.EnAttenteAdmin;
            }

            try
            {
                // Chercher s'il y a un chef de pôle pour ce pôle
                using var context = CreerContexte();
                var aUnChefDePole = await context.Utilisateurs
                    .AnyAsync(u => u.PoleId == _utilisateurConnecte.PoleId &&
                                  u.Role == RoleUtilisateur.Validateur &&
                                  u.Actif &&
                                  u.Id != _utilisateurConnecte.Id);

                // S'il y a un chef de pôle → passer par lui d'abord
                return aUnChefDePole ? StatusDemande.EnAttenteValidateur : StatusDemande.EnAttenteAdmin;
            }
            catch
            {
                // En cas d'erreur, par défaut aller au chef équipe
                return StatusDemande.EnAttenteAdmin;
            }
        }

        private decimal CalculerNombreJoursReel(DateTime dateDebut, DateTime dateFin, TypeJournee typeDebut, TypeJournee typeFin)
        {
            decimal totalJours = 0;
            var dateActuelle = dateDebut;

            while (dateActuelle <= dateFin)
            {
                if (dateActuelle.DayOfWeek >= DayOfWeek.Monday && dateActuelle.DayOfWeek <= DayOfWeek.Friday)
                {
                    decimal joursAjouter = 1;

                    if (dateActuelle == dateDebut)
                    {
                        joursAjouter = typeDebut == TypeJournee.JourneeComplete ? 1 : 0.5m;
                    }
                    else if (dateActuelle == dateFin && dateFin != dateDebut)
                    {
                        joursAjouter = typeFin == TypeJournee.JourneeComplete ? 1 : 0.5m;
                    }

                    totalJours += joursAjouter;
                }
                dateActuelle = dateActuelle.AddDays(1);
            }

            return totalJours;
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Êtes-vous sûr de vouloir annuler ? Toutes les modifications seront perdues.",
                "Confirmer l'annulation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}