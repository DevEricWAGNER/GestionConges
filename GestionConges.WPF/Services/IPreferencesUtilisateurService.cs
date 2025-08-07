namespace GestionConges.WPF.Services
{
    public interface IPreferencesUtilisateurService
    {
        bool NotificationEmail { get; set; }
        bool NotificationValidation { get; set; }
        bool NotificationRappel { get; set; }
        string FormatDate { get; set; }
        bool AffichageCompact { get; set; }

        void SauvegarderPreferences();
        void ChargerPreferences();
        void ResetPreferences();
    }
}