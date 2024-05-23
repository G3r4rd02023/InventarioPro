using Firebase.Storage;
using InventarioPro.Frontend.Models;
using InventarioPro.Frontend.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Reflection;
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

        public async Task<IActionResult> Edit(int id)
        {
            // Obtener el producto existente por su ID
            var producto = await _httpClient.GetFromJsonAsync<Producto>($"/api/Productos/{id}");

            // Obtener la lista de categorías
            producto.Categorias = await _servicioLista.GetListaCategorias();

            // Retornar la vista con el producto
            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Producto producto, IFormFile Imagen)
        {
            if (ModelState.IsValid)
            {
                // Si hay una nueva imagen, subirla y actualizar la URL de la foto en el producto
                if (Imagen != null)
                {
                    Stream image = Imagen.OpenReadStream();
                    string urlimagen = await _servicioImagen.SubirImagen(image, Imagen.FileName);
                    producto.Foto = urlimagen;
                }

                // Serializar el producto a JSON
                var json = JsonConvert.SerializeObject(producto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Enviar la petición de actualización al servidor
                var response = await _httpClient.PutAsync($"/api/Productos/{id}", content);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Error al editar el producto.");
                }
            }

            // Si hay un error en el modelo o la respuesta falla, obtener nuevamente las categorías
            producto.Categorias = await _servicioLista.GetListaCategorias();
            return View(producto);
        }
        
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _httpClient.DeleteAsync($"/api/Productos/{id}");

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = "Error al eliminar el producto.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Detalles(int id)
        {
            
            var response = await _httpClient.GetAsync($"/api/Productos/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var producto = await response.Content.ReadFromJsonAsync<Producto>();
            return View(producto);
        }

        public async Task<IActionResult> RegistrarTransaccion(int id)
        {           
            var response = await _httpClient.GetAsync($"/api/Productos/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound();
            }

            var producto = await response.Content.ReadFromJsonAsync<Producto>();

           

            var operacion = new Operacion
            {
                ProductoId = producto.Id,
                Fecha = DateTime.UtcNow,
                Producto = producto,                
            };

            return View(operacion);
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarTransaccion(int id, Operacion operacion)
        {
            if (ModelState.IsValid)
            {
                var response = await _httpClient.GetAsync($"/api/Productos/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var producto = await response.Content.ReadFromJsonAsync<Producto>();

                if (operacion.Tipo == "Entrada")
                {
                    producto.Cantidad += operacion.Cantidad;
                }
                else if (operacion.Tipo == "Salida")
                {
                    if (producto.Cantidad < operacion.Cantidad)
                    {
                        ModelState.AddModelError(string.Empty, "Stock insuficiente.");
                        return View(operacion);
                    }
                    producto.Cantidad -= operacion.Cantidad;
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Tipo de transacción inválido.");
                    return View(operacion);
                }

                // Asegúrate de no incluir la categoría en la operación para evitar conflictos
                var jsonOperacion = JsonConvert.SerializeObject(new
                {
                    operacion.Cantidad,
                    operacion.Fecha,
                    operacion.ProductoId,
                    operacion.Tipo
                });
                var contentOperacion = new StringContent(jsonOperacion, Encoding.UTF8, "application/json");

                var postResponse = await _httpClient.PostAsync($"/api/Productos/{id}/operacion", contentOperacion);
                if (postResponse.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Error al registrar la transacción.");
                }
            }

            return View(operacion);
        }


    }
}
