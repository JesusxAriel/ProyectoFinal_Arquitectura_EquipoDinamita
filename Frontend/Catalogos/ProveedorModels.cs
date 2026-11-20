namespace PuntoMarket.Frontend.Catalogos;

/// <summary>Proveedor tal como lo devuelve la API (vía Gateway). Solo para presentación.</summary>
public sealed record ProveedorVm(
    string Id, string NombreEmpresa, string NumeroEmpresa, string? CorreoReferencia, bool EsAutogestionado,
    bool EstaActivo, string IdUsuarioAdmin, string? IdUsuarioActualizacion, DateTime FechaCreacion, DateTime? FechaActualizacion);

public sealed record PaginaVm(IReadOnlyList<ProveedorVm> Items, int Pagina, int TamanoPagina, long Total, int TotalPaginas);

/// <summary>Datos del formulario (sin reglas de negocio: las valida la API).</summary>
public sealed class ProveedorFormVm
{
    public string? NombreEmpresa { get; set; }
    public string? NumeroEmpresa { get; set; }
    public string? CorreoReferencia { get; set; }
    public bool EsAutogestionado { get; set; }
}

public sealed record ConsultaVm(string? Busqueda, string Estado, string OrdenarPor, bool Descendente, int Pagina, int TamanoPagina);
