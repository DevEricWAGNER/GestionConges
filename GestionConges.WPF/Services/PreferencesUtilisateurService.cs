using Microsoft.Win32;

namespace GestionConges.WPF.Services
{
    public class PreferencesUtilisateurService : IPreferencesUtilisateurService
    {
        private const string REGISTRY_KEY = @"SOFTWARE\GestionConges";

        public bool NotificationEmail { get; set; } = true;
        public bool NotificationValidation { get; set; } = true;
        public bool NotificationRappel { get; set; } = false;
        public string FormatDate { get; set; } = "dd/MM/yyyy";
        public bool AffichageCompact { get; set; } = false;

        public PreferencesUtilisateurService()
        {
            ChargerPreferences();
        }

        public void SauvegarderPreferences()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY);

                key.SetValue("NotificationEmail", NotificationEmail);
                key.SetValue("NotificationValidation", NotificationValidation);
                key.SetValue("NotificationRappel", NotificationRappel);
                key.SetValue("FormatDate", FormatDate);
                key.SetValue("AffichageCompact", AffichageCompact);
            }
            catch (Exception ex)
            {
                // En cas d'erreur registry, on ignore silencieusement
                // (les préférences resteront en mémoire pour la session)
                System.Diagnostics.Debug.WriteLine($"Erreur sauvegarde préférences: {ex.Message}");
            }
        }

        public void ChargerPreferences()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY);
                if (key != null)
                {
                    NotificationEmail = bool.Parse(key.GetValue("NotificationEmail", true).ToString()!);
                    NotificationValidation = bool.Parse(key.GetValue("NotificationValidation", true).ToString()!);
                    NotificationRappel = bool.Parse(key.GetValue("NotificationRappel", false).ToString()!);
                    FormatDate = key.GetValue("FormatDate", "dd/MM/yyyy").ToString()!;
                    AffichageCompact = bool.Parse(key.GetValue("AffichageCompact", false).ToString()!);
                }
            }
            catch (Exception ex)
            {
                // En cas d'erreur, on utilise les valeurs par défaut
                System.Diagnostics.Debug.WriteLine($"Erreur chargement préférences: {ex.Message}");
                ResetPreferences();
            }
        }

        public void ResetPreferences()
        {
            NotificationEmail = true;
            NotificationValidation = true;
            NotificationRappel = false;
            FormatDate = "dd/MM/yyyy";
            AffichageCompact = false;
        }
    }
}
