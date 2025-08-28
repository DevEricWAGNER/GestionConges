using System.Windows;
using System.Windows.Media;

namespace GestionConges.WPF.Views
{
    public partial class EditerRegleTypeWindow : Window
    {
        public RegleTypeViewModel? RegleModifiee { get; private set; }

        public EditerRegleTypeWindow(RegleTypeViewModel regle)
        {
            InitializeComponent();
            ChargerRegle(regle);
        }

        private void ChargerRegle(RegleTypeViewModel regle)
        {
            // Afficher les infos du type
            TxtNomType.Text = regle.Nom;
            BorderCouleurType.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(regle.CouleurHex));

            // Charger les valeurs
            if (regle.MaximumParAn.HasValue)
            {
                TxtMaximumParAn.Text = regle.MaximumParAn.ToString();
                ChkIllimiteParAn.IsChecked = false;
            }
            else
            {
                ChkIllimiteParAn.IsChecked = true;
                TxtMaximumParAn.IsEnabled = false;
            }

            if (regle.MaximumConsecutif.HasValue)
            {
                TxtMaximumConsecutif.Text = regle.MaximumConsecutif.ToString();
                ChkIllimiteConsecutif.IsChecked = false;
            }
            else
            {
                ChkIllimiteConsecutif.IsChecked = true;
                TxtMaximumConsecutif.IsEnabled = false;
            }

            if (regle.PreavisMinimum.HasValue)
            {
                TxtPreavisMinimum.Text = regle.PreavisMinimum.ToString();
                ChkPreavisGlobal.IsChecked = false;
            }
            else
            {
                ChkPreavisGlobal.IsChecked = true;
                TxtPreavisMinimum.IsEnabled = false;
            }

            if (regle.AnticipationMaximum.HasValue)
            {
                TxtAnticipationMaximum.Text = regle.AnticipationMaximum.ToString();
                ChkAnticipationGlobal.IsChecked = false;
            }
            else
            {
                ChkAnticipationGlobal.IsChecked = true;
                TxtAnticipationMaximum.IsEnabled = false;
            }

            ChkNecessiteJustification.IsChecked = regle.NecessiteJustification;

            // Stocker la référence pour la modification
            RegleModifiee = new RegleTypeViewModel
            {
                TypeAbsenceId = regle.TypeAbsenceId,
                Nom = regle.Nom,
                CouleurHex = regle.CouleurHex,
                MaximumParAn = regle.MaximumParAn,
                MaximumConsecutif = regle.MaximumConsecutif,
                PreavisMinimum = regle.PreavisMinimum,
                AnticipationMaximum = regle.AnticipationMaximum,
                NecessiteJustification = regle.NecessiteJustification
            };
        }

        private void ChkIllimiteParAn_Checked(object sender, RoutedEventArgs e)
        {
            TxtMaximumParAn.IsEnabled = false;
            TxtMaximumParAn.Text = "";
        }

        private void ChkIllimiteParAn_Unchecked(object sender, RoutedEventArgs e)
        {
            TxtMaximumParAn.IsEnabled = true;
            TxtMaximumParAn.Focus();
        }

        private void ChkIllimiteConsecutif_Checked(object sender, RoutedEventArgs e)
        {
            TxtMaximumConsecutif.IsEnabled = false;
            TxtMaximumConsecutif.Text = "";
        }

        private void ChkIllimiteConsecutif_Unchecked(object sender, RoutedEventArgs e)
        {
            TxtMaximumConsecutif.IsEnabled = true;
            TxtMaximumConsecutif.Focus();
        }

        private void ChkPreavisGlobal_Checked(object sender, RoutedEventArgs e)
        {
            TxtPreavisMinimum.IsEnabled = false;
            TxtPreavisMinimum.Text = "";
        }

        private void ChkPreavisGlobal_Unchecked(object sender, RoutedEventArgs e)
        {
            TxtPreavisMinimum.IsEnabled = true;
            TxtPreavisMinimum.Focus();
        }

        private void ChkAnticipationGlobal_Checked(object sender, RoutedEventArgs e)
        {
            TxtAnticipationMaximum.IsEnabled = false;
            TxtAnticipationMaximum.Text = "";
        }

        private void ChkAnticipationGlobal_Unchecked(object sender, RoutedEventArgs e)
        {
            TxtAnticipationMaximum.IsEnabled = true;
            TxtAnticipationMaximum.Focus();
        }

        private void BtnSauvegarder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation et conversion
                if (RegleModifiee == null) return;

                // Maximum par an
                if (ChkIllimiteParAn.IsChecked == true)
                {
                    RegleModifiee.MaximumParAn = null;
                }
                else
                {
                    if (int.TryParse(TxtMaximumParAn.Text, out int maxParAn) && maxParAn > 0)
                    {
                        RegleModifiee.MaximumParAn = maxParAn;
                    }
                    else if (!string.IsNullOrWhiteSpace(TxtMaximumParAn.Text))
                    {
                        AfficherMessageValidation("Le maximum par an doit être un nombre positif.");
                        TxtMaximumParAn.Focus();
                        return;
                    }
                    else
                    {
                        RegleModifiee.MaximumParAn = null;
                    }
                }

                // Maximum consécutif
                if (ChkIllimiteConsecutif.IsChecked == true)
                {
                    RegleModifiee.MaximumConsecutif = null;
                }
                else
                {
                    if (int.TryParse(TxtMaximumConsecutif.Text, out int maxConsecutif) && maxConsecutif > 0)
                    {
                        RegleModifiee.MaximumConsecutif = maxConsecutif;
                    }
                    else if (!string.IsNullOrWhiteSpace(TxtMaximumConsecutif.Text))
                    {
                        AfficherMessageValidation("Le maximum consécutif doit être un nombre positif.");
                        TxtMaximumConsecutif.Focus();
                        return;
                    }
                    else
                    {
                        RegleModifiee.MaximumConsecutif = null;
                    }
                }

                // Préavis minimum
                if (ChkPreavisGlobal.IsChecked == true)
                {
                    RegleModifiee.PreavisMinimum = null;
                }
                else
                {
                    if (int.TryParse(TxtPreavisMinimum.Text, out int preavis) && preavis >= 0)
                    {
                        RegleModifiee.PreavisMinimum = preavis;
                    }
                    else if (!string.IsNullOrWhiteSpace(TxtPreavisMinimum.Text))
                    {
                        AfficherMessageValidation("Le préavis minimum doit être un nombre positif ou zéro.");
                        TxtPreavisMinimum.Focus();
                        return;
                    }
                    else
                    {
                        RegleModifiee.PreavisMinimum = null;
                    }
                }

                // Anticipation maximum
                if (ChkAnticipationGlobal.IsChecked == true)
                {
                    RegleModifiee.AnticipationMaximum = null;
                }
                else
                {
                    if (int.TryParse(TxtAnticipationMaximum.Text, out int anticipation) && anticipation > 0)
                    {
                        RegleModifiee.AnticipationMaximum = anticipation;
                    }
                    else if (!string.IsNullOrWhiteSpace(TxtAnticipationMaximum.Text))
                    {
                        AfficherMessageValidation("L'anticipation maximum doit être un nombre positif.");
                        TxtAnticipationMaximum.Focus();
                        return;
                    }
                    else
                    {
                        RegleModifiee.AnticipationMaximum = null;
                    }
                }

                // Options
                RegleModifiee.NecessiteJustification = ChkNecessiteJustification.IsChecked == true;

                // Validation de cohérence
                if (RegleModifiee.MaximumParAn.HasValue && RegleModifiee.MaximumConsecutif.HasValue)
                {
                    if (RegleModifiee.MaximumConsecutif > RegleModifiee.MaximumParAn)
                    {
                        AfficherMessageValidation("Le maximum consécutif ne peut pas être supérieur au maximum par an.");
                        return;
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                AfficherMessageErreur($"Erreur lors de la validation : {ex.Message}");
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AfficherMessageValidation(string message)
        {
            MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void AfficherMessageErreur(string message)
        {
            MessageBox.Show(message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}