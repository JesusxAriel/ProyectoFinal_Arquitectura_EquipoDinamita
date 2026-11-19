using Proveedores.Domain.Entities;

namespace Proveedores.Application.Dtos;

public static class ProveedorMapper
{
    public static ProveedorDto ToDto(this Proveedor p) => new(
        p.Id, p.NombreEmpresa, p.NumeroEmpresa, p.CorreoReferencia, p.EsAutogestionado,
        p.EstaActivo, p.IdUsuarioAdmin, p.IdUsuarioActualizacion, p.FechaCreacion, p.FechaActualizacion);
}
