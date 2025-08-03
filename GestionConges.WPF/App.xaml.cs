// ===============================================
// 📁 GestionConges.WPF/App.xaml.cs - FIX SHUTDOWNMODE
// ===============================================
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.WPF.Views;
using GestionConges.Core.Models;

namespace GestionConges.WPF
{
    public partial class App : Application
    {
        private IHost? _host;
        public static Utilisateur? UtilisateurConnecte { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // IMPORTANT: Empêcher la fermeture automatique
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                // Configuration de l'host avec DI
                _host = Host.CreateDefaultBuilder()
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        ConfigureServices(services, context.Configuration);
                    })
                    .Build();

                // Créer/migrer la base de données au démarrage
                using (var scope = _host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<GestionCongesContext>();
                    context.Database.Migrate();
                }

                // Afficher la fenêtre de login
                ShowLoginWindow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur au démarrage: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Configuration Entity Framework
            services.AddDbContext<GestionCongesContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        }

        private void ShowLoginWindow()
        {
            try
            {
                var loginWindow = new LoginWindow();
                var result = loginWindow.ShowDialog();

                if (result == true && UtilisateurConnecte != null)
                {
                    // Connexion réussie - créer MainWindow
                    var mainWindow = new MainWindow();

                    // Changer le mode de fermeture pour MainWindow
                    ShutdownMode = ShutdownMode.OnMainWindowClose;
                    MainWindow = mainWindow;

                    mainWindow.Show();
                }
                else
                {
                    // Connexion annulée ou échec
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de connexion: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }

        public static T GetService<T>() where T : class
        {
            return ((App)Current)._host?.Services.GetService(typeof(T)) as T
                ?? throw new InvalidOperationException($"Service {typeof(T)} non trouvé");
        }
    }
}