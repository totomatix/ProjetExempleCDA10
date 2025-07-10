public class Livre
{
    public int id { get; }
    public string? titre { get; set; }
    public string? auteur { get; set; }
    public string? isbn { get; set; }
    public DateTime date_publication { get; set; }
    public bool disponible { get; set; }

    public List<Categorie> categories { get; set; } = new List<Categorie>();

    public List<Emprunt> emprunts { get; } = new List<Emprunt>();
 }