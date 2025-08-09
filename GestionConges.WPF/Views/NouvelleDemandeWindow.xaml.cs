using GestionConges.Core.Data;
using GestionConges.Core.Enums;
using GestionConges.Core.Models;
using GestionConges.WPF.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

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
                await SauvegarderDemande(StatusDemande.EnAttenteChefPole);
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
                    // ✅ CORRECTION : Utiliser la logique simplifiée
                    demande.Statut = DeterminerStatutInitial();
                    if (demande.Statut == StatusDemande.Approuve)
                    {
                        demande.DateValidationFinale = DateTime.Now;
                    }
                }

                await context.SaveChangesAsync();

                DemandeCreee = true;

                string message;
                if (statut == StatusDemande.Brouillon)
                {
                    message = "Demande sauvegardée en brouillon avec succès !";
                }
                else if (_utilisateurConnecte.Role == RoleUtilisateur.ChefEquipe)
                {
                    message = "Demande approuvée automatiquement ! (Chef d'équipe)";
                }
                else
                {
                    message = "Demande soumise avec succès ! Elle sera examinée par votre hiérarchie.";
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
            if (_utilisateurConnecte.Role == RoleUtilisateur.ChefEquipe)
            {
                return StatusDemande.Approuve;
            }

            // Si c'est un chef de pôle → va directement au chef équipe
            if (_utilisateurConnecte.Role == RoleUtilisateur.ChefPole)
            {
                return StatusDemande.EnAttenteChefEquipe;
            }

            // Pour un employé : vérifier s'il y a un chef de pôle dans son pôle
            if (_utilisateurConnecte.PoleId.HasValue)
            {
                // Dans ce contexte simple, on suppose qu'il y a toujours un chef de pôle
                // (vous pourriez ajouter une vérification en base si nécessaire)
                return StatusDemande.EnAttenteChefPole;
            }

            // Pas de pôle → directement chef équipe
            return StatusDemande.EnAttenteChefEquipe;
        }

        private async Task<StatusDemande> DeterminerStatutInitialAsync()
        {
            // Si c'est un chef d'équipe → approuvé automatiquement
            if (_utilisateurConnecte.Role == RoleUtilisateur.ChefEquipe)
            {
                return StatusDemande.Approuve;
            }

            // Si c'est un chef de pôle → va directement au chef équipe
            if (_utilisateurConnecte.Role == RoleUtilisateur.ChefPole)
            {
                return StatusDemande.EnAttenteChefEquipe;
            }

            // Si l'utilisateur n'a pas de pôle → directement chef équipe
            if (!_utilisateurConnecte.PoleId.HasValue)
            {
                return StatusDemande.EnAttenteChefEquipe;
            }

            try
            {
                // Chercher s'il y a un chef de pôle pour ce pôle
                using var context = CreerContexte();
                var aUnChefDePole = await context.Utilisateurs
                    .AnyAsync(u => u.PoleId == _utilisateurConnecte.PoleId &&
                                  u.Role == RoleUtilisateur.ChefPole &&
                                  u.Actif &&
                                  u.Id != _utilisateurConnecte.Id);

                // S'il y a un chef de pôle → passer par lui d'abord
                return aUnChefDePole ? StatusDemande.EnAttenteChefPole : StatusDemande.EnAttenteChefEquipe;
            }
            catch
            {
                // En cas d'erreur, par défaut aller au chef équipe
                return StatusDemande.EnAttenteChefEquipe;
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