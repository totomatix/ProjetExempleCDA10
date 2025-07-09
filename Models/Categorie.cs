public class Categorie
{
    public int id { get; set; }
    public string? nom { get; set; }
    public List<Livre> livres { get; } = new List<Livre>();
}