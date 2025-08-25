using GestionConges.Core.Models;

namespace GestionConges.WPF.Services
{
    public interface ISessionService
    {
        Utilisateur UtilisateurConnecte { get; }
        bool PeutValiderDemandesPole(int poleId);
        bool PeutValiderToutesDemanades();
        bool EstChefDePole(int poleId);
        bool EstAdmin();
        List<int> PolesGeres();
    }
}
