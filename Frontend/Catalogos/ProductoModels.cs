using System.ComponentModel.DataAnnotations;
namespace PuntoMarket.Frontend.Catalogos;

public sealed record ProductoVm(int Id,string Codigo,string Nombre,decimal Costo,decimal PrecioVenta,int CategoriaId,int EmpaqueId,
    string ProveedorId,bool EstaActivo,string IdUsuarioCreacion,string? IdUsuarioActualizacion,DateTime FechaCreacion,DateTime? FechaActualizacion);
public sealed record PaginaProductosVm(IReadOnlyList<ProductoVm> Items,int Pagina,int TamanoPagina,long Total,int TotalPaginas);
public sealed record CatalogoProductoVm(int Id,string Nombre);
public sealed record CatalogosProductosVm(IReadOnlyList<CatalogoProductoVm> Categorias,IReadOnlyList<CatalogoProductoVm> Empaques);
public sealed class ProductoFormVm
{
    [Required(ErrorMessage="Ingrese un código."),StringLength(50,ErrorMessage="El código admite hasta 50 caracteres.")]
    public string? Codigo {get;set;}
    [Required(ErrorMessage="Ingrese el nombre."),StringLength(150,ErrorMessage="El nombre admite hasta 150 caracteres.")]
    public string? Nombre {get;set;}
    public decimal Costo {get;set;}
    public decimal PrecioVenta {get;set;}
    [Range(1,int.MaxValue,ErrorMessage="Seleccione una categoría.")] public int CategoriaId {get;set;}
    [Range(1,int.MaxValue,ErrorMessage="Seleccione un empaque.")] public int EmpaqueId {get;set;}
    [Required(ErrorMessage="Seleccione un proveedor.")] public string? ProveedorId {get;set;}
    [StringLength(250,ErrorMessage="El motivo admite hasta 250 caracteres.")] public string? MotivoCambioPrecio {get;set;}
}
