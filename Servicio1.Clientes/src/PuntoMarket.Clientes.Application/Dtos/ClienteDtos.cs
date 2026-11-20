using PuntoMarket.Clientes.Domain.Entities;

namespace PuntoMarket.Clientes.Application.Dtos;

public sealed record GuardarClienteRequest(string NitCi, string RazonSocial, string? Correo, string? Telefono);
public sealed record ClienteDto(int Id, string NitCi, string RazonSocial, string? Correo, string? Telefono,
    bool EstaActivo, bool EsGenerico, string IdUsuarioCreacion, string? IdUsuarioActualizacion,
    DateTime FechaCreacion, DateTime? FechaActualizacion)
{
    public static ClienteDto Desde(Cliente c) => new(c.Id, c.Datos.NitCi, c.Datos.RazonSocial, c.Datos.Correo,
        c.Datos.Telefono, c.EstaActivo, c.EsGenerico, c.IdUsuarioCreacion, c.IdUsuarioActualizacion,
        c.FechaCreacion, c.FechaActualizacion);
}
public sealed record PaginaResultado<T>(IReadOnlyList<T> Items, int Pagina, int TamanoPagina, long Total)
{
    public int TotalPaginas => TamanoPagina <= 0 || Total <= 0 ? 0
        : (int)Math.Min(int.MaxValue, Total / TamanoPagina + (Total % TamanoPagina == 0 ? 0 : 1));
}
