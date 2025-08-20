using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProjetExempleCDA10.Models;
using Dapper;
using Npgsql;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjetExempleCDA10.ViewModels;

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

    private List<SelectListItem> GetCategories()
    {

        List<SelectListItem> selectListItems = new List<SelectListItem>();

        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            string queryCategories = "SELECT id, nom FROM categories";
            List<Categorie> categories = connexion.Query<Categorie>(queryCategories).ToList();

            foreach (Categorie categorie in categories)
            {
                selectListItems.Add(new SelectListItem(categorie.nom, categorie.id.ToString()));
            }
        }

        return selectListItems;

    }

    [HttpGet]
    public IActionResult Nouveau()
    {
        EditeurLivreViewModel livreViewModel = new EditeurLivreViewModel();
        livreViewModel.categories = GetCategories();
        return View("Editeur", livreViewModel);
    }



    [HttpPost]
    public IActionResult Nouveau([FromForm] Livre livre)
    {
        try
        {
            // vérification de la validité du model (livre)
            if (!ModelState.IsValid)
            {
                throw new Exception("Les données rentrées ne sont pas correctes, veuillez réessayer.");
            }

            // vérification de la non existance du titre dans la bdd
            string queryTitre = "SELECT titre from Livres where titre=@titre";
            using (var connexion = new NpgsqlConnection(_connexionString))
            {
                if (connexion.Query(queryTitre, new { titre = livre.titre }).Count() > 0)
                {
                    ModelState["livre.titre"]!.Errors.Add(new ModelError("Ce titre existe déjà."));
                    throw new Exception("Les données rentrées ne sont pas correctes, veuillez réessayer.");
                }
            }

            // gestion de la couverture si une image est fournie
            if (livre.couvertureFile != null && livre.couvertureFile.Length > 0)
            {
                // vérification de la validité de l'image fournie pour la couverture
                string[] permittedExtensions = { ".jpeg", ".jpg", ".png", ".gif" };

                var ext = Path.GetExtension(livre.couvertureFile.FileName).ToLowerInvariant();

                if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
                {
                    // cette ligne permet de mettre le message d'erreur au bon endroit dans la vue (c.a.d à côté du file picker)
                    ModelState["livre.couvertureFile"]!.Errors.Add(new ModelError("Ce type de fichier n'est pas accepté."));
                    throw new Exception("Les données rentrées ne sont pas correctes, veuillez réessayer.");
                }

                //  enregistrement de l'image sur le système de fichiers et création du chemin de l'image afin de l'enregistrer en BDD
                string? filePath = Path.Combine("/images/livres/",
                    Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(livre.couvertureFile.FileName)).ToString();

                using (var stream = System.IO.File.Create("wwwroot" + filePath))
                {
                    livre.couvertureFile.CopyTo(stream);
                }
                livre.couverture = filePath;

            }

            // 
            string queryLivre = "INSERT INTO Livres (titre,auteur,isbn,date_publication,couverture) VALUES(@titre,@auteur,@isbn,@date_publication,@couverture) RETURNING id;";
            string queryCategories = "INSERT INTO livre_categorie (livre_id, categorie_id) VALUES (@livre_id,@categorie_id)";

            using (var connexion = new NpgsqlConnection(_connexionString))
            {
                connexion.Open();
                using (var transaction = connexion.BeginTransaction())
                {
                    // insert du livre et récupération de son id
                    int livre_id = connexion.ExecuteScalar<int>(queryLivre, livre);
                    if (livre_id == 0)
                    {
                        transaction.Rollback();
                        throw new Exception("Erreur pendant la création du livre. Veuillez réessayer plus tard. Si le problème persiste merci de contacter l'administrateur.");
                    }
                    else
                    {
                        // ajout des associations avec les catégories
                        List<object> list = new List<object>();
                        foreach (int categorie_id in livre.categoriesIDs)
                        {
                            list.Add(new { livre_id = livre_id, categorie_id = categorie_id });
                        }
                        int res = connexion.Execute(queryCategories, list);
                        if (res != livre.categoriesIDs.Count)
                        {
                            transaction.Rollback();
                            throw new Exception("Erreur pendant la création du livre. Veuillez réessayer plus tard. Si le problème persiste merci de contacter l'administrateur.");
                        }
                        transaction.Commit();
                    }
                }

            }

            ViewData["ValidateMessage"] = "Livre bien créé !";
            EditeurLivreViewModel livreViewModel = new EditeurLivreViewModel();
            livreViewModel.categories = GetCategories();
            return View("Editeur", livreViewModel);


        }
        catch (Exception e)
        {
            // suppresion de la couverture dans le système de fichier si il y en a une
            if (livre.couvertureFile != null && System.IO.File.Exists("wwwroot" + livre.couverture))
            {
                System.IO.File.Delete("wwwroot" + livre.couverture);
            }
            EditeurLivreViewModel livreViewModel = new EditeurLivreViewModel();
            livreViewModel.livre = livre;
            livreViewModel.categories = GetCategories();
            ViewData["ValidateMessage"] = e.Message;
            return View("Editeur", livreViewModel);
        }
    }


}
