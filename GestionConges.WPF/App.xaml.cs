using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;

namespace GestionConges.WPF
{
    public partial class App : Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
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
                try
                {
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la création de la base de données :\n{ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
            }

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Configuration Entity Framework
            services.AddDbContext<GestionCongesContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Services applicatifs (on les ajoutera plus tard)
            // services.AddScoped<IUtilisateurService, UtilisateurService>();
            // services.AddScoped<IDemandeCongeService, DemandeCongeService>();

            // Fenêtres (on les ajoutera plus tard)
            // services.AddTransient<MainWindow>();
            // services.AddTransient<LoginWindow>();
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
