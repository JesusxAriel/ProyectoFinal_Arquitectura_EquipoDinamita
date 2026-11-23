using System.Text.RegularExpressions;
namespace PuntoMarket.Productos.Domain;

public sealed record DatosProducto(string? Codigo, string? Nombre, decimal Costo, decimal PrecioVenta,
    int CategoriaId, int EmpaqueId, string? ProveedorId, string? MotivoCambioPrecio = null)
{
    public DatosProducto Normalizar() => this with {
        Codigo = Codigo?.Trim().ToUpperInvariant(), Nombre = Nombre?.Trim(),
        ProveedorId = ProveedorId?.Trim(), MotivoCambioPrecio = MotivoCambioPrecio?.Trim()
    };
}
public static class ProductoReglas
{
    public static Result Validar(DatosProducto datos, decimal? precioAnterior = null)
    {
        var d = datos.Normalizar();
        if (!Regex.IsMatch(d.Codigo ?? "", "^[A-Z0-9][A-Z0-9._-]{0,49}$"))
            return Invalido("Codigo", "El código es obligatorio: hasta 50 letras ASCII, números, puntos, guiones o guiones bajos.");
        if (string.IsNullOrWhiteSpace(d.Nombre) || d.Nombre.Length > 150 || d.Nombre.Any(c => char.IsControl(c) || c is '<' or '>'))
            return Invalido("Nombre", "El nombre es obligatorio, admite hasta 150 caracteres sin controles ni HTML.");
        if (d.Costo <= 0 || d.Costo > 9999999999.99m || decimal.Round(d.Costo, 2) != d.Costo)
            return Invalido("Costo", "El costo debe ser positivo, con hasta 10 enteros y 2 decimales.");
        if (d.PrecioVenta < d.Costo || d.PrecioVenta > 9999999999.99m || decimal.Round(d.PrecioVenta, 2) != d.PrecioVenta)
            return Invalido("PrecioVenta", "El precio de venta debe ser mayor o igual al costo, con hasta 10 enteros y 2 decimales.");
        if (d.CategoriaId <= 0) return Invalido("CategoriaId", "Seleccione una categoría válida.");
        if (d.EmpaqueId <= 0) return Invalido("EmpaqueId", "Seleccione un empaque válido.");
        if (string.IsNullOrWhiteSpace(d.ProveedorId) || d.ProveedorId.Length > 100 || d.ProveedorId.Any(char.IsControl))
            return Invalido("ProveedorId", "Seleccione un proveedor válido.");
        if (d.MotivoCambioPrecio is { Length: > 250 } || d.MotivoCambioPrecio?.Any(c => char.IsControl(c) || c is '<' or '>') == true)
            return Invalido("MotivoCambioPrecio", "El motivo admite hasta 250 caracteres sin controles ni HTML.");
        if (precioAnterior.HasValue && precioAnterior != d.PrecioVenta && string.IsNullOrWhiteSpace(d.MotivoCambioPrecio))
            return Invalido("MotivoCambioPrecio", "Indique el motivo del cambio de precio de venta.");
        return Result.Success();
    }
    private static Result Invalido(string campo, string mensaje) => Result.Failure(
        Error.Validation("Producto." + campo + "Invalido", mensaje) with {
            Fields = new Dictionary<string, string[]> { [char.ToLowerInvariant(campo[0]) + campo[1..]] = [mensaje] }
        });
}
public sealed record Producto(int Id, string Codigo, string Nombre, decimal Costo, decimal PrecioVenta,
    int CategoriaId, int EmpaqueId, string ProveedorId, bool EstaActivo,
    string IdUsuarioCreacion, string? IdUsuarioActualizacion, DateTime FechaCreacion, DateTime? FechaActualizacion);
