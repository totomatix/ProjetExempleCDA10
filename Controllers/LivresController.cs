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

    private List<SelectListItem> GetCategories(List<Categorie>? selectedCategories = null, List<int>? selectedCategoriesIDs = null)
    {

        List<SelectListItem> selectListItems = new List<SelectListItem>();

        using (var connexion = new NpgsqlConnection(_connexionString))
        {
            string queryCategories = "SELECT id, nom FROM categories";
            List<Categorie> categories = connexion.Query<Categorie>(queryCategories).ToList();

            foreach (Categorie categorie in categories)
            {
                bool selected = false;
                if (selectedCategories != null)
                {
                    selected = selectedCategories.Contains(categorie) ? true : false;
                }
                else if (selectedCategoriesIDs != null)
                {
                    selected = selectedCategoriesIDs.Contains(categorie.id) ? true : false;
                }
                selectListItems.Add(new SelectListItem(categorie.nom, categorie.id.ToString(), selected));
            }
        }

        return selectListItems;

    }

    private string ManageCover(IFormFile file)
    {

        // vérification de la validité de l'image fournie pour la couverture
        string[] permittedExtensions = { ".jpeg", ".jpg", ".png", ".gif" };

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
        {
            // cette ligne permet de mettre le message d'erreur au bon endroit dans la vue (c.a.d à côté du file picker)
            ModelState["livre.couvertureFile"]!.Errors.Add(new ModelError("Ce type de fichier n'est pas accepté."));
            throw new Exception("Les données rentrées ne sont pas correctes, veuillez réessayer.");
        }

        //  enregistrement de l'image sur le système de fichiers et création du chemin de l'image afin de l'enregistrer en BDD
        string? filePath = Path.Combine("/images/livres/",
            Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(file.FileName)).ToString();

        using (var stream = System.IO.File.Create("wwwroot" + filePath))
        {
            file.CopyTo(stream);
        }
        return filePath;
    }

    [HttpGet]
    public IActionResult Nouveau()
    {
        EditeurLivreViewModel livreViewModel = new() { action = "Nouveau", titre = "Nouveau Livre" };
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
                livre.couverture = ManageCover(livre.couvertureFile!);
            }

            // enregistrement du livre en BDD
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
            EditeurLivreViewModel livreViewModel = new() { action = "Nouveau", titre = "Nouveau Livre" };
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
            EditeurLivreViewModel livreViewModel = new() { action = "Nouveau", titre = "Nouveau Livre" };
            livreViewModel.livre = livre;
            livreViewModel.categories = GetCategories();
            ViewData["ValidateMessage"] = e.Message;
            return View("Editeur", livreViewModel);
        }
    }

    [HttpGet]
    public IActionResult Editer(int id)
    {
        EditeurLivreViewModel livreViewModel = new() { action = "Editer", titre = "Modification livre", idLivre = id };
        try
        {
            using (var connexion = new NpgsqlConnection(_connexionString))
            {
                string query = "SELECT * FROM livres LEFT JOIN livre_categorie ON livres.id = livre_categorie.livre_id LEFT JOIN categories ON livre_categorie.categorie_id = categories.id where livres.id=@id";
                List<Livre> livres = connexion.Query<Livre, Categorie, Livre>(query, (livre, categorie) =>
                {
                    livre.categories.Add(categorie);
                    return livre;
                },
                new { id = id },
                splitOn: "id").ToList();
                livreViewModel.livre = livres.GroupBy(l => l.id).Select(g =>
                {
                    Livre groupedLivre = g.First();
                    groupedLivre.categories = g.Select(l => l.categories.First()).ToList();
                    return groupedLivre;
                }).First();
            }

        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (Exception)
        {
            // TODO : return error page
        }
        livreViewModel.categories = GetCategories(livreViewModel.livre!.categories);

        return View("Editeur", livreViewModel);
    }

    [HttpPost]
    public IActionResult Editer([FromRoute] int id, [FromForm] Livre livre)
    {
        if (id != livre.id)
        {
            return BadRequest();
        }
        try
        {
            // vérification de la validité du model (livre)
            if (!ModelState.IsValid)
            {
                throw new Exception("Les données rentrées ne sont pas correctes, veuillez réessayer.");
            }


            // gestion de la couverture si une image est fournie
            if (livre.couvertureFile != null && livre.couvertureFile.Length > 0)
            {
                if (System.IO.File.Exists("wwwroot" + livre.couverture))
                {
                    System.IO.File.Delete("wwwroot" + livre.couverture);
                }
                livre.couverture = ManageCover(livre.couvertureFile!);
            }

            // enregistrement du livre en BDD
            string queryLivre = "UPDATE livres SET titre=@titre, auteur=@auteur, isbn=@isbn, date_publication=@date_publication, couverture=@couverture WHERE id=@id";
            string queryRemoveCategories = "DELETE FROM livre_categorie WHERE livre_id=@id";
            string queryCategories = "INSERT INTO livre_categorie (livre_id, categorie_id) VALUES (@livre_id,@categorie_id)";

            using (var connexion = new NpgsqlConnection(_connexionString))
            {
                connexion.Open();
                using (var transaction = connexion.BeginTransaction())
                {
                    // modification du livre 
                    int res = connexion.Execute(queryLivre, livre);
                    if (res != 1)
                    {
                        transaction.Rollback();
                        throw new Exception("Erreur pendant la mise à jour du livre. Veuillez réessayer plus tard. Si le problème persiste merci de contacter l'administrateur.");
                    }
                    else
                    {
                        // suppression des anciennes catégories 
                        connexion.Execute(queryRemoveCategories, new { id = id });
                        // TODO : controller bien supprimer
                        // ajout des associations avec les catégories
                        List<object> list = new List<object>();
                        foreach (int categorie_id in livre.categoriesIDs)
                        {
                            list.Add(new { livre_id = id, categorie_id = categorie_id });
                        }
                        res = connexion.Execute(queryCategories, list);
                        if (res != livre.categoriesIDs.Count)
                        {
                            transaction.Rollback();
                            throw new Exception("Erreur pendant la mise à jour du livre. Veuillez réessayer plus tard. Si le problème persiste merci de contacter l'administrateur.");
                        }
                        transaction.Commit();
                    }
                }

            }

            ViewData["ValidateMessage"] = "Livre mis à jour";
            EditeurLivreViewModel livreViewModel = new() { action = "Editer", titre = "Modification Livre", idLivre = id };
            livreViewModel.livre = livre;
            livreViewModel.categories = GetCategories(null, livre.categoriesIDs);

            return View("Editeur", livreViewModel);


        }
        catch (Exception e)
        {
            // suppresion de la couverture dans le système de fichier si il y en a une
            if (livre.couvertureFile != null && System.IO.File.Exists("wwwroot" + livre.couverture))
            {
                System.IO.File.Delete("wwwroot" + livre.couverture);
            }
            EditeurLivreViewModel livreViewModel = new() { action = "Editer", titre = "Modification livre", idLivre = id };
            livreViewModel.livre = livre;
            livreViewModel.categories = GetCategories(null, livre.categoriesIDs);
            ViewData["ValidateMessage"] = e.Message;
            return View("Editeur", livreViewModel);
        }
    }
}
