using InventarioPro.Frontend.Models;
using InventarioPro.Frontend.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace InventarioPro.Frontend.Controllers
{
    public class ProductosController : Controller
    {

        private readonly HttpClient _httpClient;
        private readonly IServicioLista _servicioLista;
        private readonly IServicioImagen _servicioImagen;

        public ProductosController(IHttpClientFactory httpClientFactory, IServicioLista servicioLista, IServicioImagen servicioImagen)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("https://localhost:7013/");
            _servicioLista = servicioLista;
            _servicioImagen = servicioImagen;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _httpClient.GetAsync("/api/Productos");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var productos = JsonConvert.DeserializeObject<IEnumerable<Producto>>(content);
                return View("Index", productos);
            }

            return View(new List<Producto>());
        }

        public async Task<IActionResult> Create()
        {
            Producto producto = new()
            {
                Categorias = await _servicioLista.GetListaCategorias(),
            };
            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Producto producto, IFormFile Imagen)
        {
            if (ModelState.IsValid)
            {
                Stream image = Imagen.OpenReadStream();
                string urlimagen = await _servicioImagen.SubirImagen(image, Imagen.FileName);
                producto.Foto = urlimagen;
                var json = JsonConvert.SerializeObject(producto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Productos/", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Error al crear el producto.");
                }
            }
            producto.Categorias = await _servicioLista.GetListaCategorias();
            return View(producto);
        }
    }
}
