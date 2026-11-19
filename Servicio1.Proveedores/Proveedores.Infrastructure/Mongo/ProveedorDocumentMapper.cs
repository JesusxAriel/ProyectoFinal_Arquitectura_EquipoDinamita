using MongoDB.Bson;
using Proveedores.Domain.Entities;

namespace Proveedores.Infrastructure.Mongo;

// INTEGRACIÓN: Traducción BSON <-> entidad pura; origenSeed es metadato exclusivo de persistencia.
//   El repositorio lo conserva mediante $set de campos concretos, no reemplazando todo el documento.
internal static class ProveedorDocumentMapper
{
    public static ProveedorDocument ToDocument(Proveedor p, ObjectId? id = null) => new()
    {
        Id = id ?? (ObjectId.TryParse(p.Id, out var parsed) ? parsed : ObjectId.Empty),
        Version = p.Version,
        NombreEmpresa = p.NombreEmpresa,
        NombreNormalizado = p.NombreNormalizado,
        NumeroEmpresa = p.NumeroEmpresa,
        CorreoReferencia = p.CorreoReferencia,
        EsAutogestionado = p.EsAutogestionado,
        EstaActivo = p.EstaActivo,
        IdUsuarioAdmin = p.IdUsuarioAdmin,
        IdUsuarioActualizacion = p.IdUsuarioActualizacion,
        FechaCreacion = DateTime.SpecifyKind(p.FechaCreacion, DateTimeKind.Utc),
        FechaActualizacion = p.FechaActualizacion is null ? null : DateTime.SpecifyKind(p.FechaActualizacion.Value, DateTimeKind.Utc)
    };

    public static Proveedor ToDomain(ProveedorDocument d) => Proveedor.Rehidratar(
        d.Id.ToString(), d.NombreEmpresa, d.NumeroEmpresa, d.CorreoReferencia, d.EsAutogestionado,
        d.EstaActivo, d.IdUsuarioAdmin, d.IdUsuarioActualizacion,
        DateTime.SpecifyKind(d.FechaCreacion, DateTimeKind.Utc),
        d.FechaActualizacion is null ? null : DateTime.SpecifyKind(d.FechaActualizacion.Value, DateTimeKind.Utc), d.Version);
}
