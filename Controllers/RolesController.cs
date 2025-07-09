using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjetExempleCDA10.Models;
using Dapper;
using Npgsql;

namespace ProjetExempleCDA10.Controllers;

public class RolesController : Controller
{
    // attribut stockant la chaîne de connexion à la base de données
    private readonly string _connexionString;

    /// <summary>
    /// Constructeur de RolesController
    /// </summary>
    /// <param name="configuration">configuration de l'application</param>
    /// <exception cref="Exception"></exception>
    public RolesController(IConfiguration configuration)
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
        string query = "Select * from roles";
        List<Role> roles;
        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            roles = connexion.Query<Role>(query).ToList();
        }
        return View(roles);
    }

    public IActionResult Detail([FromRoute] int id)
    {
        // construction de la requête SQL
        string query = $"SELECT * FROM roles where id = @identifiant";
        // déclaration de la variable permettant de contenir le résultat de la requête SQL
        Role role;
        // exécuter la requête SQL
        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            try
            {
                role = connexion.QuerySingle<Role>(query, new { identifiant = id });
            }
            catch (System.Exception)
            {
                return NotFound();
            }
        }
        // retourner la vue contenant le résultat 
        return View(role);
    }

}
