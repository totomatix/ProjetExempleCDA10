using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjetExempleCDA10.Models;
using Dapper;
using Npgsql;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProjetExempleCDA10.Controllers;

public class UtilisateursController : Controller
{
    // attribut stockant la chaîne de connexion à la base de données
    private readonly string _connexionString;

    /// <summary>
    /// Constructeur de UtilisateursController
    /// </summary>
    /// <param name="configuration">configuration de l'application</param>
    /// <exception cref="Exception"></exception>
    public UtilisateursController(IConfiguration configuration)
    {
        // récupération de la chaîne de connexion dans la configuration
        _connexionString = configuration.GetConnectionString("GestionBibliotheque")!;
        // si la chaîne de connexionn'a pas été trouvé => déclenche une exception => code http 500 retourné
        if (_connexionString == null)
        {
            throw new Exception("Error : Connexion string not found ! ");
        }
    }
    public IActionResult Index()
    {
        string query = "SELECT u.id,u.nom,u.email,u.prenom, r.nom FROM Utilisateurs u inner join roles r on u.role_id = r.id;";
        List<Utilisateur> utilisateurs;
        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            utilisateurs = connexion.Query<Utilisateur, Role, Utilisateur>(query,
            (utilisateur, role) =>
            {
                utilisateur.role = role;
                return utilisateur;
            },
            splitOn: "nom"
            ).ToList();

        }


        return View(utilisateurs);
    }

    public IActionResult Detail([FromRoute] int id)
    {

        string query = "SELECT * FROM Livres WHERE id=@identifiant";
        Livre livre;
        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            try
            {
                livre = connexion.QuerySingle<Livre>(query, new { identifiant = id });
            }
            catch (System.Exception)
            {
                return NotFound();
            }

        }

        ViewBag.DateDemande = DateTime.Now.ToShortDateString();

        ViewBag.NomUtilisateur = "Toto";

        return View(livre);
    }

    private List<SelectListItem> CreerListeSelectRoles()
    {
        // récupération des rôles dans la bdd
        string queryRoles = "SELECT * FROM Roles";
        List<Role> roles;
        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            roles = connexion.Query<Role>(queryRoles).ToList();
        }
        // création de la list des select items
        List<SelectListItem> listeRoles = new List<SelectListItem>();
        foreach (Role role in roles)
        {
            // chaque élément de la liste déroulante affichera le nom du rôle mais l'utilisateur choisira en fait l'id du rôle voulu
            listeRoles.Add(new SelectListItem(role.nom, role.id.ToString()));
        }

        return listeRoles;
    }
    // retourne le formulaire permettant de créer un utilisateur
    [HttpGet]
    public IActionResult Nouveau()
    {
        
        ViewData["listeRoles"] = CreerListeSelectRoles(); // passage de la liste de SelectListItem à la vue
        // retourne la vue spécifiée (qui est dans le dossier Utilisateurs)
        return View("EditeurUtilisateur");
    }

    [HttpPost]
    public IActionResult Nouveau([FromForm] Utilisateur utilisateur)
    {
        string query = "INSERT INTO utilisateurs (nom, prenom, email, date_inscription, role_id) VALUES(@nom,@prenom,@email,CURRENT_TIMESTAMP, @role_id);";

        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            try
            {
                int nbLignesInserees = connexion.Execute(query, utilisateur);
            }
            catch (PostgresException e)
            {
                if (e.ConstraintName.Contains("email"))
                {
                    ViewData["ValidateMessage"] = "Cet email est déjà utilisé.";
                } 
                if (e.ConstraintName.Contains("nom"))
                {
                    ViewData["ValidateMessage"] = "Ce nom est déjà utilisé.";
                } 
                ViewData["listeRoles"] = CreerListeSelectRoles(); 
                return View("EditeurUtilisateur", utilisateur);
            }
            catch (Exception e)
            {
                ViewData["ValidatMessage"] = "Erreur serveur. Veuillez réessayer ultérieurement. Si jamais ça continu contectez le support.";
                // message d'erreur
                return View("EditeurUtilisateur");
            }
        }

        return RedirectToAction("Index");
    }
}
