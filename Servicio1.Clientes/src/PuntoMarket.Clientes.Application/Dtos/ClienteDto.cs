using PuntoMarket.Clientes.Domain.Entities;

namespace PuntoMarket.Clientes.Application.Dtos;

public sealed record ClienteDto(
    int Id,
    string NitCi,
    string RazonSocial,
    string? Correo,
    string? Telefono,
    bool EstaActivo,
    bool EsGenerico,
    string IdUsuarioCreacion,
    string? IdUsuarioActualizacion,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion);

public static class ClienteMapper
{
    public static ClienteDto ToDto(this Cliente c) => new(
        c.Id, c.NitCi, c.RazonSocial, c.Correo, c.Telefono, c.EstaActivo, c.EsGenerico,
        c.IdUsuarioCreacion, c.IdUsuarioActualizacion, c.FechaCreacion, c.FechaActualizacion);
}
