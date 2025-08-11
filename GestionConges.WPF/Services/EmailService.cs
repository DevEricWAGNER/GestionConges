using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;
using GestionConges.Core.Enums;

namespace GestionConges.WPF.Services
{
    public interface IEmailService
    {
        Task<bool> EstActive();
        Task<bool> TesterConfiguration();
        Task<bool> EnvoyerNotificationNouvelleDemande(DemandeConge demande, Utilisateur validateur);
        Task<bool> EnvoyerNotificationDemandeApprouvee(DemandeConge demande);
        Task<bool> EnvoyerNotificationDemandeRefusee(DemandeConge demande, string motifRefus);
        Task<bool> EnvoyerRappelValidation(List<DemandeConge> demandesEnAttente, Utilisateur validateur);
        Task<bool> EnvoyerEmailPersonnalise(string destinataire, string sujet, string corps);
    }

    public class EmailService : IEmailService
    {
        private readonly GestionCongesContext _context;
        private readonly ParametresService _parametresService;

        public EmailService(GestionCongesContext context)
        {
            _context = context;
            _parametresService = new ParametresService(context);
        }

        public async Task<bool> EstActive()
        {
            return await _parametresService.ObtenirParametre<bool>("EmailActif", false);
        }

        public async Task<bool> TesterConfiguration()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 TesterConfiguration: Début du test...");

                if (!await EstActive())
                {
                    System.Diagnostics.Debug.WriteLine("❌ TesterConfiguration: Emails non activés");
                    return false;
                }

                var config = await ObtenirConfigurationSMTP();
                if (config == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ TesterConfiguration: Configuration SMTP manquante");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"📧 TesterConfiguration: {config.Serveur}:{config.Port}, SSL:{config.SSL}");

                // ✅ CORRECTION : Configuration spéciale pour port 465 vs 587
                var client = new SmtpClient(config.Serveur, config.Port);

                if (config.Port == 465 && config.SSL)
                {
                    // Port 465 : SSL direct (non STARTTLS)
                    client.EnableSsl = true;
                    System.Diagnostics.Debug.WriteLine("🔒 Mode SSL direct (port 465)");
                }
                else if (config.Port == 587 && config.SSL)
                {
                    // Port 587 : STARTTLS
                    client.EnableSsl = true;
                    System.Diagnostics.Debug.WriteLine("🔒 Mode STARTTLS (port 587)");
                }
                else
                {
                    client.EnableSsl = config.SSL;
                    System.Diagnostics.Debug.WriteLine($"🔒 Mode SSL: {config.SSL}");
                }

                client.Credentials = new NetworkCredential(config.Utilisateur, config.MotDePasse);
                client.Timeout = 20000; // 20 secondes
                client.DeliveryMethod = SmtpDeliveryMethod.Network;

                // ✅ AJOUT : Support explicite SSL/TLS pour port 465
                if (config.Port == 465)
                {
                    // Force SSL pour port 465 (submission over SSL)
                    client.EnableSsl = true;
                }

                var message = new MailMessage
                {
                    From = new MailAddress(config.Utilisateur),
                    Subject = "✅ Test SMTP réussi - Gestion des Congés",
                    Body = $"🎉 Félicitations !\n\n" +
                           $"Votre serveur SMTP fonctionne parfaitement.\n\n" +
                           $"📧 Configuration testée :\n" +
                           $"   • Serveur : {config.Serveur}:{config.Port}\n" +
                           $"   • SSL/TLS : {(config.SSL ? "Activé" : "Désactivé")}\n" +
                           $"   • Utilisateur : {config.Utilisateur}\n\n" +
                           $"📅 Test effectué le : {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n\n" +
                           $"L'application peut maintenant envoyer des notifications automatiques.\n\n" +
                           $"---\n" +
                           $"Application Gestion des Congés",
                    IsBodyHtml = false
                };

                message.To.Add(config.Utilisateur);

                System.Diagnostics.Debug.WriteLine($"📤 TesterConfiguration: Envoi vers {config.Utilisateur}...");

                using (client)
                {
                    await client.SendMailAsync(message);
                }

                System.Diagnostics.Debug.WriteLine("✅ TesterConfiguration: Email envoyé avec succès !");
                return true;
            }
            catch (SmtpFailedRecipientsException ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 TesterConfiguration: Erreur destinataires - {ex.Message}");
                return false;
            }
            catch (SmtpException ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 TesterConfiguration: Erreur SMTP - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 StatusCode: {ex.StatusCode}");
                return false;
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 TesterConfiguration: Erreur connexion - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 SocketErrorCode: {ex.SocketErrorCode}");
                return false;
            }
            catch (TimeoutException ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 TesterConfiguration: Timeout - {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"💥 TesterConfiguration: Erreur générale - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"💥 Type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"💥 StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> EnvoyerNotificationNouvelleDemande(DemandeConge demande, Utilisateur validateur)
        {
            if (!await EstActive() || validateur?.Email == null)
                return false;

            try
            {
                var template = await ObtenirTemplate("nouvelle_demande");
                var sujet = RemplacerVariables(template.Sujet, demande, validateur);
                var corps = RemplacerVariables(template.Corps, demande, validateur);

                return await EnvoyerEmail(validateur.Email, sujet, corps);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur envoi notification nouvelle demande: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EnvoyerNotificationDemandeApprouvee(DemandeConge demande)
        {
            if (!await EstActive() || demande.Utilisateur?.Email == null)
                return false;

            try
            {
                var template = await ObtenirTemplate("demande_approuvee");
                var sujet = RemplacerVariables(template.Sujet, demande);
                var corps = RemplacerVariables(template.Corps, demande);

                return await EnvoyerEmail(demande.Utilisateur.Email, sujet, corps);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur envoi notification approbation: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EnvoyerNotificationDemandeRefusee(DemandeConge demande, string motifRefus)
        {
            if (!await EstActive() || demande.Utilisateur?.Email == null)
                return false;

            try
            {
                var template = await ObtenirTemplate("demande_refusee");
                var sujet = RemplacerVariables(template.Sujet, demande);
                var corps = RemplacerVariables(template.Corps, demande);

                // Remplacer le motif de refus
                corps = corps.Replace("{MotifRefus}", motifRefus);

                return await EnvoyerEmail(demande.Utilisateur.Email, sujet, corps);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur envoi notification refus: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EnvoyerRappelValidation(List<DemandeConge> demandesEnAttente, Utilisateur validateur)
        {
            if (!await EstActive() || validateur?.Email == null || !demandesEnAttente.Any())
                return false;

            try
            {
                var template = await ObtenirTemplate("rappel_validation");
                var sujet = template.Sujet.Replace("{NombredemAdes}", demandesEnAttente.Count.ToString());

                var corps = template.Corps;
                var listeDemandes = string.Join("\n", demandesEnAttente.Select(d =>
                    $"• {d.Utilisateur?.NomComplet} : {d.TypeAbsence?.Nom} du {d.DateDebut:dd/MM/yyyy} au {d.DateFin:dd/MM/yyyy}"));

                corps = corps.Replace("{ListeDemandes}", listeDemandes);
                corps = corps.Replace("{NomValidateur}", validateur.NomComplet);

                return await EnvoyerEmail(validateur.Email, sujet, corps);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur envoi rappel: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EnvoyerEmailPersonnalise(string destinataire, string sujet, string corps)
        {
            if (!await EstActive())
                return false;

            return await EnvoyerEmail(destinataire, sujet, corps);
        }

        private async Task<bool> EnvoyerEmail(string destinataire, string sujet, string corps)
        {
            try
            {
                var config = await ObtenirConfigurationSMTP();
                if (config == null)
                    return false;

                using var client = new SmtpClient(config.Serveur, config.Port)
                {
                    Credentials = new NetworkCredential(config.Utilisateur, config.MotDePasse),
                    EnableSsl = config.SSL
                };

                var message = new MailMessage
                {
                    From = new MailAddress(config.Utilisateur),
                    Subject = sujet,
                    Body = corps,
                    IsBodyHtml = false
                };

                message.To.Add(destinataire);

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur envoi email: {ex.Message}");
                return false;
            }
        }

        private async Task<ConfigurationSMTP?> ObtenirConfigurationSMTP()
        {
            try
            {
                var serveur = await _parametresService.ObtenirParametre("ServeurSMTP");
                var portStr = await _parametresService.ObtenirParametre("PortSMTP");
                var utilisateur = await _parametresService.ObtenirParametre("UtilisateurSMTP");
                var motDePasse = await _parametresService.ObtenirParametre("MotDePasseSMTP");
                var sslStr = await _parametresService.ObtenirParametre("SSLSMTP");

                if (string.IsNullOrEmpty(serveur) || string.IsNullOrEmpty(utilisateur) || string.IsNullOrEmpty(motDePasse))
                    return null;

                return new ConfigurationSMTP
                {
                    Serveur = serveur,
                    Port = int.TryParse(portStr, out int port) ? port : 587,
                    Utilisateur = utilisateur,
                    MotDePasse = motDePasse,
                    SSL = bool.TryParse(sslStr, out bool ssl) ? ssl : true
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task<TemplateEmail> ObtenirTemplate(string typeTemplate)
        {
            // Essayer de charger depuis la base de données d'abord (si paramètres personnalisés)
            try
            {
                var parametresService = new ParametresService(_context);
                var cleSubject = $"EmailTemplate_{typeTemplate}_Sujet";
                var cleBody = $"EmailTemplate_{typeTemplate}_Corps";

                var customSubject = await parametresService.ObtenirParametre(cleSubject);
                var customBody = await parametresService.ObtenirParametre(cleBody);

                if (!string.IsNullOrEmpty(customSubject) && !string.IsNullOrEmpty(customBody))
                {
                    return new TemplateEmail { Sujet = customSubject, Corps = customBody };
                }
            }
            catch
            {
                // En cas d'erreur, utiliser templates par défaut
            }

            // Templates par défaut améliorés
            return typeTemplate switch
            {
                "nouvelle_demande" => new TemplateEmail
                {
                    Sujet = "🗓️ Nouvelle demande de congés - {NomUtilisateur}",
                    Corps = "Bonjour,\n\n" +
                           "📋 Une nouvelle demande de congés nécessite votre validation :\n\n" +
                           "👤 Demandeur : {NomUtilisateur}\n" +
                           "🏷️ Type : {TypeConge}\n" +
                           "📅 Période : du {DateDebut} au {DateFin}\n" +
                           "⏱️ Durée : {NombreJours} jour(s)\n\n" +
                           "💻 Veuillez vous connecter à l'application Gestion des Congés pour traiter cette demande.\n\n" +
                           "Cordialement,\n" +
                           "🤖 Système de Gestion des Congés"
                },
                "demande_approuvee" => new TemplateEmail
                {
                    Sujet = "✅ Demande approuvée - {TypeConge}",
                    Corps = "Bonjour {NomUtilisateur},\n\n" +
                           "🎉 Excellente nouvelle ! Votre demande de congés a été approuvée :\n\n" +
                           "🏷️ Type : {TypeConge}\n" +
                           "📅 Période : du {DateDebut} au {DateFin}\n" +
                           "⏱️ Durée : {NombreJours} jour(s)\n\n" +
                           "🏖️ Bonnes vacances !\n\n" +
                           "Cordialement,\n" +
                           "🤖 Système de Gestion des Congés"
                },
                "demande_refusee" => new TemplateEmail
                {
                    Sujet = "❌ Demande refusée - {TypeConge}",
                    Corps = "Bonjour {NomUtilisateur},\n\n" +
                           "📋 Votre demande de congés a été refusée :\n\n" +
                           "🏷️ Type : {TypeConge}\n" +
                           "📅 Période : du {DateDebut} au {DateFin}\n" +
                           "⏱️ Durée : {NombreJours} jour(s)\n\n" +
                           "💬 Motif du refus :\n{MotifRefus}\n\n" +
                           "💡 N'hésitez pas à contacter votre manager pour discuter d'une nouvelle date ou pour plus d'informations.\n\n" +
                           "Cordialement,\n" +
                           "🤖 Système de Gestion des Congés"
                },
                "rappel_validation" => new TemplateEmail
                {
                    Sujet = "⏰ Rappel - {NombredemAdes} demande(s) en attente de validation",
                    Corps = "Bonjour {NomValidateur},\n\n" +
                           "📋 Vous avez des demandes de congés en attente de validation :\n\n" +
                           "{ListeDemandes}\n\n" +
                           "💻 Veuillez vous connecter à l'application Gestion des Congés pour traiter ces demandes dans les meilleurs délais.\n\n" +
                           "📧 Ceci est un rappel automatique.\n\n" +
                           "Cordialement,\n" +
                           "🤖 Système de Gestion des Congés"
                },
                _ => new TemplateEmail { Sujet = "Notification - Gestion des Congés", Corps = "Contenu du message" }
            };
        }


        private string RemplacerVariables(string template, DemandeConge demande, Utilisateur? validateur = null)
        {
            if (demande.Utilisateur != null)
            {
                template = template.Replace("{NomUtilisateur}", demande.Utilisateur.NomComplet);
            }

            if (demande.TypeAbsence != null)
            {
                template = template.Replace("{TypeConge}", demande.TypeAbsence.Nom);
            }

            template = template.Replace("{DateDebut}", demande.DateDebut.ToString("dd/MM/yyyy"));
            template = template.Replace("{DateFin}", demande.DateFin.ToString("dd/MM/yyyy"));
            template = template.Replace("{NombreJours}", demande.NombreJours.ToString());

            if (validateur != null)
            {
                template = template.Replace("{NomValidateur}", validateur.NomComplet);
            }

            return template;
        }
    }

    // Classes de support
    public class ConfigurationSMTP
    {
        public string Serveur { get; set; } = "";
        public int Port { get; set; } = 587;
        public string Utilisateur { get; set; } = "";
        public string MotDePasse { get; set; } = "";
        public bool SSL { get; set; } = true;
    }

    public class TemplateEmail
    {
        public string Sujet { get; set; } = "";
        public string Corps { get; set; } = "";
    }
}