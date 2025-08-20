using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProjetExempleCDA10.ViewModels
{
    public class EditeurLivreViewModel
    {
        public Livre livre { get; set; }

        public  List<SelectListItem> categories { get; set; }
    }
}