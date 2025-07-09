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
    public IActionResult Index()
    {
        string query = "Select * from Livres";
        List<Livre> livres;
        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            livres = connexion.Query<Livre>(query).ToList();
        }
        return View(livres);
    }

    public IActionResult Detail([FromRoute] int id) {
        
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
