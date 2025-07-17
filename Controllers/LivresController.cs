using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjetExempleCDA10.Models;
using Dapper;
using Npgsql;

namespace ProjetExempleCDA10.Controllers;

public class LivresController : Controller
{
    // attribut stockant la chaîne de connexion à la base de données
    private readonly string _connexionString;

    /// <summary>
    /// Constructeur de LivresController
    /// </summary>
    /// <param name="configuration">configuration de l'application</param>
    /// <exception cref="Exception"></exception>
    public LivresController(IConfiguration configuration)
    {
        // récupération de la chaîne de connexion dans la configuration
        _connexionString = configuration.GetConnectionString("GestionBibliotheque")!;
        // si la chaîne de connexionn'a pas été trouvé => déclenche une exception => code http 500 retourné
        if (_connexionString == null)
        {
            throw new Exception("Error : Connexion string not found ! ");
        }
    }


    public IActionResult Index([FromQuery] string sort = "titre")
    {
        string route = Request.RouteValues.First().ToString();
        string query = "select * from livres left join livre_categorie on id = livre_categorie.livre_id left join categories on categorie_id = categories.id";
        List<Livre> livres;
        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            livres = connexion.Query<Livre, Categorie, Livre>(query,
            (livre, categorie) =>
            {
                if (categorie != null)
                {

                    livre.categories.Add(categorie);
                }
                return livre;
            }).ToList();
        }
        //LINQ
        livres = livres.GroupBy(l => l.id).Select(g =>
            {
                Livre groupedLivre = g.First();
                if (groupedLivre.categories.Count > 0)
                {
                    groupedLivre.categories = g.Select(l => l.categories.Single()).ToList();
                }

                return groupedLivre;
            }).ToList();
        return View(livres);


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



    public IActionResult Privacy()
    {
        return View();
    }



    [HttpGet]
    public IActionResult Nouveau()
    {

        return View();
    }

    [HttpPost]
    public IActionResult Nouveau([FromForm] Livre livre)
    {
        // vérification de la validité du model (livre)
        if (!ModelState.IsValid)
        {
            return View(livre);
        }

        // vérification de la non existance du titre dans la bdd
        string queryTitre = "SELECT titre from Livres where titre=@titre";
        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            if (connexion.Query(queryTitre, new { titre = livre.titre }).Count() > 0)
            {
                ViewData["ValidateMessage"] = "Titre déjà existant";
                return View(livre);
            }
        }

        string query = "INSERT INTO Livres (titre,auteur,isbn,date_publication) VALUES(@titre,@auteur,@isbn,@date_publication)";
        int res;
        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            res = connexion.Execute(query, livre);
        }
        if (res != 0)
        {
            TempData["ValidateMessage"] = "Livre bien créé !";
        }
        else
        {
            TempData["ValidateMessage"] = "Erreur";
        }
        return RedirectToAction("Index");
    }
    
    
}
