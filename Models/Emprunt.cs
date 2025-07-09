public class Emprunt
{
    public int id { get; set; }
    public DateTime date_emprunt { get; set; }
    public DateTime date_retour { get; set; }

    public Livre? livre { get; set; }
    
    public Utilisateur? utilisateur { get; set; }
}