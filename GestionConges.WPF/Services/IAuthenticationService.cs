using GestionConges.Core.Models;

namespace GestionConges.WPF.Services
{
    public interface IAuthenticationService
    {
        Task<Utilisateur?> ConnecterAsync(string login, string motDePasse);
        void Deconnecter();
        Utilisateur? UtilisateurConnecte { get; }
        bool EstConnecte { get; }
        event EventHandler<Utilisateur>? UtilisateurConnecteChange;
    }
}
