using System.Windows;

namespace GestionConges.WPF.Views
{
    public partial class PreviewTemplateWindow : Window
    {
        public PreviewTemplateWindow(string sujet, string corps)
        {
            InitializeComponent();
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
                .Replace("{ListeDemandes}", "• Jean Dupont : Congés Payés du 15/08/2025 au 18/08/2025\n• Marie Martin : RTT du 20/08/2025 au 20/08/2025")
                .Replace("{NombredemAdes}", "2");
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}