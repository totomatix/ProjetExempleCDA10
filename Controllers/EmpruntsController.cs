using Microsoft.AspNetCore.Mvc;
using ProjetExempleCDA10.Models;
using Npgsql;
using Dapper;

namespace ProjetExempleCDA10.Controllers;

public class EmpruntsController : Controller
{
    private readonly string _connexionString;

    public EmpruntsController(ILogger<HomeController> logger, IConfiguration configuration)
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
        string query = "select e.id,e.date_emprunt,e.date_retour,l.titre,l.isbn,u.nom,u.prenom from emprunts e inner join livres l on e.livre_id = l.id inner join utilisateurs u on e.utilisateur_id = u.id";
        List<Emprunt> emprunts;
        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            emprunts = connexion.Query<Emprunt, Livre, Utilisateur, Emprunt>(query, (emprunt, livre, utilisateur) =>
            {
                emprunt.livre = livre;
                emprunt.utilisateur = utilisateur;
                return emprunt;
            },
            splitOn: "titre,nom").ToList();
        }
        return View(emprunts);
    }

    public IActionResult Detail(int id)
    {
        string query = "select e.id,e.date_emprunt,e.date_retour,l.titre,l.isbn,u.nom,u.prenom from emprunts e inner join livres l on e.livre_id = l.id inner join utilisateurs u on e.utilisateur_id = u.id where e.id=@id";

        Emprunt emprunt;

        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            emprunt = connexion.Query<Emprunt, Livre, Utilisateur, Emprunt>(query,
            (emprunt, livre, utilisateur) =>
            {
                emprunt.livre = livre;
                emprunt.utilisateur = utilisateur;
                return emprunt;
            }, new { id = id },
            splitOn: "titre,nom").First();
        }

        return View(emprunt);
    }
}