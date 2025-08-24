using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;
using GestionConges.Core.Models;

namespace GestionConges.WPF.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly GestionCongesContext _context;
        private Utilisateur? _utilisateurConnecte;

        public AuthenticationService(GestionCongesContext context)
        {
            _context = context;
        }

        public Utilisateur? UtilisateurConnecte => _utilisateurConnecte;
        public bool EstConnecte => _utilisateurConnecte != null;

        public event EventHandler<Utilisateur>? UtilisateurConnecteChange;

        public async Task<Utilisateur?> ConnecterAsync(string login, string motDePasse)
        {
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(motDePasse))
                return null;

            // Recherche de l'utilisateur
            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Pole)
                .FirstOrDefaultAsync(u => u.Email == login && u.Actif);

            if (utilisateur == null)
                return null;

            // Vérification du mot de passe
            if (!BCrypt.Net.BCrypt.Verify(motDePasse, utilisateur.MotDePasseHash))
                return null;

            // Mise à jour de la dernière connexion
            utilisateur.DerniereConnexion = DateTime.Now;
            await _context.SaveChangesAsync();

            // Stockage de l'utilisateur connecté
            _utilisateurConnecte = utilisateur;
            UtilisateurConnecteChange?.Invoke(this, utilisateur);

            return utilisateur;
        }

        public void Deconnecter()
        {
            _utilisateurConnecte = null;
            UtilisateurConnecteChange?.Invoke(this, null!);
        }
    }
}
