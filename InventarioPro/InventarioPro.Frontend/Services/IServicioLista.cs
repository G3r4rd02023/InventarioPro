using Microsoft.AspNetCore.Mvc.Rendering;

namespace InventarioPro.Frontend.Services
{
    public interface IServicioLista
    {
        Task<IEnumerable<SelectListItem>> GetListaCategorias();
    }
}
