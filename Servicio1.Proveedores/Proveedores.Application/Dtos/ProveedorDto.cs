namespace Proveedores.Application.Dtos;

/// <summary>Representación pública de un proveedor (contrato HTTP). Nunca se expone la entidad ni el documento de Mongo.</summary>
public sealed record ProveedorDto(
    string Id,
    string NombreEmpresa,
    string NumeroEmpresa,
    string? CorreoReferencia,
    bool EsAutogestionado,
    bool EstaActivo,
    string IdUsuarioAdmin,
    string? IdUsuarioActualizacion,
    DateTime FechaCreacion,
    DateTime? FechaActualizacion);
