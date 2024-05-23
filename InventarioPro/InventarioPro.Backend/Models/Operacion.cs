namespace InventarioPro.Backend.Models
{
    public class Operacion
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }
        public DateTime Fecha { get; set; }
        public int Cantidad { get; set; } // Positivo para entrada, negativo para salida
        public string Tipo { get; set; } // "Entrada" o "Salida"
    }
}
