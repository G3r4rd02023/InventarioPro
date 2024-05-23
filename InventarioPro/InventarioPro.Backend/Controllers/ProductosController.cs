using InventarioPro.Backend.Data;
using InventarioPro.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventarioPro.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly DataContext _context;

        public ProductosController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            return Ok(await _context.Productos.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(Producto producto)
        {
            _context.Add(producto);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAsync(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .SingleOrDefaultAsync(p => p.Id == id);
            if (producto == null)
            {
                return NotFound();
            }
            return Ok(producto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(int id, [FromBody] Producto producto)
        {
            if (id != producto.Id)
            {
                return BadRequest();
            }

            _context.Update(producto);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }
            _context.Remove(producto);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Método para registrar una entrada o salida de inventario
        [HttpPost("{id}/operacion")]
        public async Task<IActionResult> RegistrarTransaccion(int id, [FromBody] Operacion operacion)
        {
            var producto = await _context.Productos.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id);
            if (producto == null)
            {
                return NotFound();
            }

            if (operacion.Tipo == "Entrada")
            {
                producto.Cantidad += operacion.Cantidad;
            }
            else if (operacion.Tipo == "Salida")
            {
                if (producto.Cantidad < operacion.Cantidad)
                {
                    return BadRequest("Stock insuficiente.");
                }
                producto.Cantidad -= operacion.Cantidad;
            }
            else
            {
                return BadRequest("Tipo de transacción inválido.");
            }

            // Desvincular el producto de la operación para evitar inserciones no deseadas
            operacion.Producto = null;
            operacion.ProductoId = id;
            operacion.Fecha = DateTime.UtcNow;

            // No asignar manualmente el Id de la operación
            _context.Operaciones.Add(operacion);

            // Asegurarse de que solo el producto se actualice sin modificar la categoría
            _context.Entry(producto).State = EntityState.Modified;
            _context.Entry(producto).Property(p => p.CategoriaId).IsModified = false;

            await _context.SaveChangesAsync();

            return NoContent();
        }


    }
}
