using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProjetExempleCDA10.ViewModels
{
    public class EditeurLivreViewModel
    {
        public Livre? livre { get; set; }

        public List<SelectListItem> categories { get; set; } = new List<SelectListItem>();

        public required string action { get; init; }

        public required string titre { get; init; }

        public int? idLivre { get; set; } = null;
        
    }
}