using System.ComponentModel.DataAnnotations;

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

    public int nbCat { get; set; }
    public List<Categorie> categories { get; set; } = new List<Categorie>();

    public List<Emprunt> emprunts { get; } = new List<Emprunt>();


    public string? couverture { get; set; } // pour l'affichage

    public IFormFile? couvertureFile { get; set; } // pour le formulaire
}