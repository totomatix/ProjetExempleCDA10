 public class Role
 {
     public int id { get; }
     public string? nom { get; set; }

    public List<Utilisateur> utilisateurs { get; } = new List<Utilisateur>();
 }