using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;

namespace GestionConges.WPF.Views
{
    public partial class GestionTypesAbsencesWindow : Window
    {
        private ObservableCollection<TypeAbsenceViewModel> _typesAbsences;
        private TypeAbsenceViewModel? _typeSelectionne;
        private bool _modeEdition = false;

        // Couleurs prédéfinies populaires
        private readonly string[] _couleursSuggerees = {
            "#e74c3c", "#3498db", "#2ecc71", "#f39c12", "#9b59b6",
            "#1abc9c", "#34495e", "#e67e22", "#95a5a6", "#f1c40f",
            "#27ae60", "#8e44ad", "#2980b9", "#c0392b", "#d35400"
        };

        public GestionTypesAbsencesWindow()
        {
            InitializeComponent();
            _typesAbsences = new ObservableCollection<TypeAbsenceViewModel>();

            InitialiserInterface();
            ChargerDonnees();
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
            DgTypesAbsences.ItemsSource = _typesAbsences;
            CreerPaletteCouleurs();

            // Valeur par défaut pour l'ordre
            TxtOrdre.Text = "1";
        }

        private void CreerPaletteCouleurs()
        {
            PaletteCouleurs.Children.Clear();

            foreach (var couleur in _couleursSuggerees)
            {
                var bouton = new Button
                {
                    Width = 25,
                    Height = 25,
                    Margin = new Thickness(2),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(couleur)),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    ToolTip = couleur,
                    Tag = couleur
                };

                bouton.Click += (s, e) =>
                {
                    TxtCouleur.Text = (string)((Button)s).Tag;
                    MettreAJourApercu();
                };

                PaletteCouleurs.Children.Add(bouton);
            }
        }

        private async void ChargerDonnees()
        {
            try
            {
                using var context = CreerContexte();

                var types = await context.TypesAbsences
                    .Include(t => t.Demandes) // Pour compter les utilisations
                    .OrderBy(t => t.OrdreAffichage)
                    .ThenBy(t => t.Nom)
                    .ToListAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    _typesAbsences.Clear();
                    foreach (var type in types)
                    {
                        var viewModel = new TypeAbsenceViewModel(type);
                        _typesAbsences.Add(viewModel);
                    }
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Erreur lors du chargement des types d'absences : {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void DgTypesAbsences_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _typeSelectionne = DgTypesAbsences.SelectedItem as TypeAbsenceViewModel;

            if (_typeSelectionne != null)
            {
                AfficherDetailsType(_typeSelectionne);
                BtnModifier.IsEnabled = true;
                BtnSupprimer.IsEnabled = _typeSelectionne.NombreUtilisations == 0; // Pas de suppression si utilisé
                BtnActiver.IsEnabled = true;
                BtnMonter.IsEnabled = _typeSelectionne.OrdreAffichage > 1;
                BtnDescendre.IsEnabled = _typeSelectionne.OrdreAffichage < _typesAbsences.Count;
            }
            else
            {
                ViderFormulaire();
                BtnModifier.IsEnabled = false;
                BtnSupprimer.IsEnabled = false;
                BtnActiver.IsEnabled = false;
                BtnMonter.IsEnabled = false;
                BtnDescendre.IsEnabled = false;
            }
        }

        private void AfficherDetailsType(TypeAbsenceViewModel type)
        {
            TxtNom.Text = type.Nom;
            TxtDescription.Text = type.Description ?? string.Empty;
            TxtCouleur.Text = type.CouleurHex;
            TxtOrdre.Text = type.OrdreAffichage.ToString();
            ChkActif.IsChecked = type.Actif;
            ChkValidation.IsChecked = type.NecessiteValidation;
            TxtRegles.Text = ""; // À implémenter plus tard

            MettreAJourApercu();

            // Informations supplémentaires
            var infos = $"📅 Créé le : {type.DateCreation:dd/MM/yyyy HH:mm}\n";
            infos += $"📊 Utilisé dans {type.NombreUtilisations} demande(s)\n";
            infos += $"🆔 ID : {type.Id}";

            TxtInfos.Text = infos;

            DesactiverFormulaire();
        }

        private void ViderFormulaire()
        {
            TxtNom.Clear();
            TxtDescription.Clear();
            TxtCouleur.Text = "#3498db";
            TxtOrdre.Text = (_typesAbsences.Count + 1).ToString();
            ChkActif.IsChecked = true;
            ChkValidation.IsChecked = true;
            TxtRegles.Clear();
            TxtInfos.Text = "Sélectionnez un type pour voir les détails";

            MettreAJourApercu();
            DesactiverFormulaire();
        }

        private void ActiverFormulaire()
        {
            TxtNom.IsEnabled = true;
            TxtDescription.IsEnabled = true;
            TxtCouleur.IsEnabled = true;
            TxtOrdre.IsEnabled = true;
            ChkActif.IsEnabled = true;
            ChkValidation.IsEnabled = true;
            TxtRegles.IsEnabled = true;
            BtnSauvegarder.IsEnabled = true;
            BtnAnnuler.IsEnabled = true;
            _modeEdition = true;
        }

        private void DesactiverFormulaire()
        {
            TxtNom.IsEnabled = false;
            TxtDescription.IsEnabled = false;
            TxtCouleur.IsEnabled = false;
            TxtOrdre.IsEnabled = false;
            ChkActif.IsEnabled = false;
            ChkValidation.IsEnabled = false;
            TxtRegles.IsEnabled = false;
            BtnSauvegarder.IsEnabled = false;
            BtnAnnuler.IsEnabled = false;
            _modeEdition = false;
        }

        private void MettreAJourApercu()
        {
            try
            {
                if (EstCouleurValide(TxtCouleur.Text))
                {
                    BorderApercu.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(TxtCouleur.Text));
                }
                else
                {
                    BorderApercu.Background = Brushes.Gray;
                }
            }
            catch
            {
                BorderApercu.Background = Brushes.Gray;
            }
        }

        private bool EstCouleurValide(string couleur)
        {
            if (string.IsNullOrWhiteSpace(couleur)) return false;
            return Regex.IsMatch(couleur, @"^#[0-9A-Fa-f]{6}$");
        }

        private void TxtCouleur_TextChanged(object sender, TextChangedEventArgs e)
        {
            MettreAJourApercu();
        }

        private void BtnNouveauType_Click(object sender, RoutedEventArgs e)
        {
            _typeSelectionne = null;
            DgTypesAbsences.SelectedItem = null;

            ViderFormulaire();
            ActiverFormulaire();
            TxtNom.Focus();
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (_typeSelectionne != null)
            {
                ActiverFormulaire();
                TxtNom.Focus();
            }
        }

        private async void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (_typeSelectionne != null)
            {
                if (_typeSelectionne.NombreUtilisations > 0)
                {
                    MessageBox.Show($"Impossible de supprimer ce type car il est utilisé dans {_typeSelectionne.NombreUtilisations} demande(s).\n\n" +
                                  "Vous pouvez le désactiver à la place.",
                                  "Suppression impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer le type '{_typeSelectionne.Nom}' ?\n\n" +
                    "Cette action est irréversible.",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var context = CreerContexte();
                        var typeASupprimer = context.TypesAbsences.Find(_typeSelectionne.Id);
                        if (typeASupprimer != null)
                        {
                            context.TypesAbsences.Remove(typeASupprimer);
                            await context.SaveChangesAsync();

                            ChargerDonnees();
                            ViderFormulaire();

                            MessageBox.Show("Type d'absence supprimé avec succès.", "Succès",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
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

        private async void BtnActiver_Click(object sender, RoutedEventArgs e)
        {
            if (_typeSelectionne != null)
            {
                try
                {
                    using var context = CreerContexte();
                    var type = context.TypesAbsences.Find(_typeSelectionne.Id);
                    if (type != null)
                    {
                        type.Actif = !type.Actif;
                        await context.SaveChangesAsync();

                        ChargerDonnees();

                        string message = type.Actif ? "activé" : "désactivé";
                        MessageBox.Show($"Type d'absence {message} avec succès.", "Succès",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
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
                    MessageBox.Show("Le nom du type est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNom.Focus();
                    return;
                }

                if (!EstCouleurValide(TxtCouleur.Text))
                {
                    MessageBox.Show("La couleur doit être au format hexadécimal (ex: #3498db).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtCouleur.Focus();
                    return;
                }

                if (!int.TryParse(TxtOrdre.Text, out int ordre) || ordre < 1)
                {
                    MessageBox.Show("L'ordre d'affichage doit être un nombre positif.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtOrdre.Focus();
                    return;
                }

                using var context = CreerContexte();

                // Vérifier l'unicité du nom
                var typeSelectionneId = _typeSelectionne != null ? _typeSelectionne.Id : 0;
                var nomExiste = await context.TypesAbsences
                    .Where(t => t.Nom == TxtNom.Text.Trim() && t.Id != typeSelectionneId)
                    .AnyAsync();

                if (nomExiste)
                {
                    MessageBox.Show("Ce nom de type est déjà utilisé.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNom.Focus();
                    return;
                }

                bool nouveauType = _typeSelectionne == null;
                TypeAbsence type;

                if (nouveauType)
                {
                    type = new TypeAbsence { DateCreation = DateTime.Now };
                    context.TypesAbsences.Add(type);
                }
                else
                {
                    type = context.TypesAbsences.Find(_typeSelectionne!.Id)!;
                }

                // Mettre à jour les propriétés
                type.Nom = TxtNom.Text.Trim();
                type.Description = string.IsNullOrWhiteSpace(TxtDescription.Text) ? null : TxtDescription.Text.Trim();
                type.CouleurHex = TxtCouleur.Text.ToUpper();
                type.OrdreAffichage = ordre;
                type.Actif = ChkActif.IsChecked ?? true;
                type.NecessiteValidation = ChkValidation.IsChecked ?? true;

                await context.SaveChangesAsync();

                ChargerDonnees();
                DesactiverFormulaire();

                string message = nouveauType ? "créé" : "modifié";
                MessageBox.Show($"Type d'absence {message} avec succès.", "Succès",
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
            if (_typeSelectionne != null)
            {
                AfficherDetailsType(_typeSelectionne);
            }
            else
            {
                ViderFormulaire();
            }
        }

        private async void BtnMonter_Click(object sender, RoutedEventArgs e)
        {
            if (_typeSelectionne != null && _typeSelectionne.OrdreAffichage > 1)
            {
                await ChangerOrdre(_typeSelectionne, -1);
            }
        }

        private async void BtnDescendre_Click(object sender, RoutedEventArgs e)
        {
            if (_typeSelectionne != null && _typeSelectionne.OrdreAffichage < _typesAbsences.Count)
            {
                await ChangerOrdre(_typeSelectionne, 1);
            }
        }

        private async Task ChangerOrdre(TypeAbsenceViewModel typeADeplacer, int direction)
        {
            try
            {
                using var context = CreerContexte();

                var typeActuel = await context.TypesAbsences.FindAsync(typeADeplacer.Id);
                if (typeActuel == null) return;

                var nouvelOrdre = typeActuel.OrdreAffichage + direction;

                // Trouver le type qui a le nouvel ordre
                var typeAEchanger = await context.TypesAbsences
                    .FirstOrDefaultAsync(t => t.OrdreAffichage == nouvelOrdre);

                if (typeAEchanger != null)
                {
                    // Échanger les ordres
                    typeAEchanger.OrdreAffichage = typeActuel.OrdreAffichage;
                    typeActuel.OrdreAffichage = nouvelOrdre;

                    await context.SaveChangesAsync();
                    ChargerDonnees();

                    // Reselectionner le type déplacé
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var typeApresDeplacment = _typesAbsences.FirstOrDefault(t => t.Id == typeADeplacer.Id);
                        if (typeApresDeplacment != null)
                        {
                            DgTypesAbsences.SelectedItem = typeApresDeplacment;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du changement d'ordre : {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualiser_Click(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
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
            // Pas de context à disposer ici car on utilise using
            base.OnClosed(e);
        }
    }

    // ViewModel pour les types d'absences avec propriétés calculées
    public class TypeAbsenceViewModel
    {
        public int Id { get; set; }
        public string Nom { get; set; } = "";
        public string? Description { get; set; }
        public string CouleurHex { get; set; } = "#3498db";
        public bool Actif { get; set; }
        public bool NecessiteValidation { get; set; }
        public int OrdreAffichage { get; set; }
        public DateTime DateCreation { get; set; }
        public int NombreUtilisations { get; set; }

        public TypeAbsenceViewModel() { }

        public TypeAbsenceViewModel(TypeAbsence type)
        {
            Id = type.Id;
            Nom = type.Nom;
            Description = type.Description;
            CouleurHex = type.CouleurHex;
            Actif = type.Actif;
            NecessiteValidation = type.NecessiteValidation;
            OrdreAffichage = type.OrdreAffichage;
            DateCreation = type.DateCreation;
            NombreUtilisations = type.Demandes != null ? type.Demandes.Count : 0;
        }
    }
}