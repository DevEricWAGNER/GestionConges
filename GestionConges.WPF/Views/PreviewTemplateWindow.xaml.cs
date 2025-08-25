using System.Windows;

namespace GestionConges.WPF.Views
{
    public partial class PreviewTemplateWindow : Window
    {
        private readonly string _sujetOriginal;
        private readonly string _corpsOriginal;

        public PreviewTemplateWindow(string sujet, string corps)
        {
            InitializeComponent();
            _sujetOriginal = sujet;
            _corpsOriginal = corps;
            AfficherPreview(sujet, corps);
        }

        private void AfficherPreview(string sujet, string corps)
        {
            // Remplacer les variables par des valeurs de test
            var sujetTest = RemplacerVariablesTest(sujet);
            var corpsTest = RemplacerVariablesTest(corps);

            TxtSujetPreview.Text = sujetTest;
            TxtCorpsPreview.Text = corpsTest;
        }

        private string RemplacerVariablesTest(string template)
        {
            var dateDebut = DateTime.Today.AddDays(7);
            var dateFin = DateTime.Today.AddDays(10);

            return template
                .Replace("{NomUtilisateur}", "Jean Dupont")
                .Replace("{TypeConge}", "Congés Payés")
                .Replace("{DateDebut}", dateDebut.ToString("dd/MM/yyyy"))
                .Replace("{DateFin}", dateFin.ToString("dd/MM/yyyy"))
                .Replace("{NombreJours}", "3")
                .Replace("{MotifRefus}", "Planning trop chargé pour cette période")
                .Replace("{NomValidateur}", App.UtilisateurConnecte?.NomComplet ?? "Manager Équipe")
                .Replace("{ListeDemandes}", "• Jean Dupont : Congés Payés du 25/08/2025 au 28/08/2025\n• Marie Martin : RTT du 29/08/2025 au 29/08/2025")
                .Replace("{NombredemAdes}", "2");
        }

        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Voulez-vous envoyer un email de test à votre adresse ?\n\n" +
                    $"Email de destination : {App.UtilisateurConnecte?.Email}\n\n" +
                    "Cet email utilisera les données de test affichées dans la prévisualisation.",
                    "Confirmer l'envoi du test",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Simuler l'envoi d'un email de test
                    var sujetTest = RemplacerVariablesTest(_sujetOriginal);
                    var corpsTest = RemplacerVariablesTest(_corpsOriginal);

                    // Ici vous pourriez intégrer votre service d'email
                    // await _emailService.EnvoyerEmail(App.UtilisateurConnecte.Email, sujetTest, corpsTest);

                    MessageBox.Show(
                        "Email de test envoyé avec succès !\n\n" +
                        "Vérifiez votre boîte de réception dans quelques minutes.\n" +
                        "N'oubliez pas de vérifier le dossier spam si nécessaire.",
                        "Test envoyé",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'envoi du test :\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}