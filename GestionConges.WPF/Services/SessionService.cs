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
            return user.Role == RoleUtilisateur.Admin ||
                   (user.Role == RoleUtilisateur.Validateur && user.PoleId == poleId);
        }

        public bool PeutValiderToutesDemanades()
        {
            return UtilisateurConnecte.Role == RoleUtilisateur.Admin;
        }

        public bool EstChefDePole(int poleId)
        {
            var user = UtilisateurConnecte;
            return user.Role == RoleUtilisateur.Validateur && user.PoleId == poleId;
        }

        public bool EstAdmin()
        {
            return UtilisateurConnecte.Role == RoleUtilisateur.Admin;
        }

        public List<int> PolesGeres()
        {
            var user = UtilisateurConnecte;

            if (user.Role == RoleUtilisateur.Admin)
            {
                // Chef d'équipe gère tous les pôles
                return _context.Poles.Where(p => p.Actif).Select(p => p.Id).ToList();
            }
            else if (user.Role == RoleUtilisateur.Validateur && user.PoleId.HasValue)
            {
                // Chef de pôle gère uniquement son pôle
                return new List<int> { user.PoleId.Value };
            }

            return new List<int>();
        }
    }
}
