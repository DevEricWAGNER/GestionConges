using GestionConges.Core.Models;
using GestionConges.Core.Enums;
using Microsoft.EntityFrameworkCore;
using GestionConges.Core.Data;

namespace GestionConges.WPF.Services
{
    public class SessionService : ISessionService
    {
        private readonly IAuthenticationService _authService;
        private readonly GestionCongesContext _context;

        public SessionService(IAuthenticationService authService, GestionCongesContext context)
        {
            _authService = authService;
            _context = context;
        }

        public Utilisateur UtilisateurConnecte => _authService.UtilisateurConnecte
            ?? throw new InvalidOperationException("Aucun utilisateur connecté");

        public bool PeutValiderDemandesPole(int poleId)
        {
            var user = UtilisateurConnecte;
            return user.Role == RoleUtilisateur.ChefEquipe ||
                   (user.Role == RoleUtilisateur.ChefPole && user.PoleId == poleId);
        }

        public bool PeutValiderToutesDemanades()
        {
            return UtilisateurConnecte.Role == RoleUtilisateur.ChefEquipe;
        }

        public bool EstChefDePole(int poleId)
        {
            var user = UtilisateurConnecte;
            return user.Role == RoleUtilisateur.ChefPole && user.PoleId == poleId;
        }

        public bool EstChefEquipe()
        {
            return UtilisateurConnecte.Role == RoleUtilisateur.ChefEquipe;
        }

        public List<int> PolesGeres()
        {
            var user = UtilisateurConnecte;

            if (user.Role == RoleUtilisateur.ChefEquipe)
            {
                // Chef d'équipe gère tous les pôles
                return _context.Poles.Where(p => p.Actif).Select(p => p.Id).ToList();
            }
            else if (user.Role == RoleUtilisateur.ChefPole && user.PoleId.HasValue)
            {
                // Chef de pôle gère uniquement son pôle
                return new List<int> { user.PoleId.Value };
            }

            return new List<int>();
        }
    }
}
