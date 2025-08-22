using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class Livre
{
    public int id { get; set; }
    [Required(ErrorMessage = "Le titre est obligatoire")]
    [StringLength(100, ErrorMessage = "Le titre doit contenir minimum 8 charactères et maximum 100.")]
    public string? titre { get; set; }
    public string? auteur { get; set; }
    public string? isbn { get; set; }
    [Display(Name = "Date de publication")]
    [DataType(DataType.Date)]
    public DateTime date_publication { get; set; }
    public bool disponible { get; set; }

    public List<Categorie> categories { get; set; } = new List<Categorie>(); // pour l'affichage

    public List<int> categoriesIDs { get; set; } // pour l'édition

    public List<Emprunt> emprunts { get; } = new List<Emprunt>();

    public string? couverture { get; set; } // pour l'affichage

    public IFormFile? couvertureFile { get; set; } // pour le formulaire
}