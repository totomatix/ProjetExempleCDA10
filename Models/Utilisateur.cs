public class Utilisateur
{
    public int id { get; set; }
    public string? nom { get; set; }
    public string? prenom { get; set; }
    public string? email { get; set; }
    public DateTime date_inscription { get; set; }

    // public int role_id { get; set; }
    public List<Emprunt> emprunts { get; } = new List<Emprunt>();

    public Role? role { get; set; }
}