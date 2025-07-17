using System.ComponentModel.DataAnnotations;

public class Utilisateur
{
    public int id { get; }

    [Required(ErrorMessage = "Le nom est requis.")]
    [MinLength(1)]
    [MaxLength(100)]
    [DataType(DataType.Text)]
    public string? nom { get; set; }

    [Required(ErrorMessage = "Le prénom est requis.")]
    [MinLength(1)]
    [MaxLength(100)]
    [DataType(DataType.Text)]
    public string? prenom { get; set; }

    [Required(ErrorMessage = "L'email est requis.")]
    [DataType(DataType.EmailAddress)]
    public string? email { get; set; }

    public DateTime date_inscription {get; set;}

    public Role? role {get; set;} // cette attribut sera utilisé seulement pour AFFICHER le rôle, pas dans la cas de la création d'un utilisateur

    [Required(ErrorMessage = "Le rôle est requis.")]
    public int role_id {get; set;}  // quand le formulaire sera soumis on récupèrera grâce à cette attribut l'id du rôle du nouvel utilisateur
    
}