public class Categorie
{
    public int id { get; set; }
    public string? nom { get; set; }
    public List<Livre> livres { get; } = new List<Livre>();

    public override bool Equals(object? obj)
    {
        return obj is Categorie categorie &&
               id == categorie.id;
    }
}