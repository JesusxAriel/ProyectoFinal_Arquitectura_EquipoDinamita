using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Proveedores.Infrastructure.Mongo;

/// <summary>
/// Documento de persistencia (detalle de infraestructura; nunca sale de esta capa).
/// Los nombres de campo conservan los del SQL original (PROVEEDOR). No contiene contraseñas ni datos de autenticación.
/// </summary>
public sealed class ProveedorDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }

    [BsonElement("nombreEmpresa")]
    public string NombreEmpresa { get; set; } = string.Empty;

    /// <summary>Clave normalizada (minúsculas, sin tildes, espacios colapsados) sobre la que se aplica la unicidad.</summary>
    [BsonElement("nombreNormalizado")]
    public string NombreNormalizado { get; set; } = string.Empty;

    [BsonElement("numeroEmpresa")]
    public string NumeroEmpresa { get; set; } = string.Empty;

    [BsonElement("correoReferencia")]
    [BsonIgnoreIfNull]
    public string? CorreoReferencia { get; set; }

    [BsonElement("esAutogestionado")]
    public bool EsAutogestionado { get; set; }

    [BsonElement("estaActivo")]
    public bool EstaActivo { get; set; }

    [BsonElement("idUsuarioAdmin")]
    public string IdUsuarioAdmin { get; set; } = string.Empty;

    [BsonElement("idUsuarioActualizacion")]
    [BsonIgnoreIfNull]
    public string? IdUsuarioActualizacion { get; set; }

    [BsonElement("fechaCreacion")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime FechaCreacion { get; set; }

    [BsonElement("fechaActualizacion")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [BsonIgnoreIfNull]
    public DateTime? FechaActualizacion { get; set; }

    /// <summary>Marca de los documentos creados por el seed (permite idempotencia sin pisar cambios de usuarios).</summary>
    [BsonElement("origenSeed")]
    [BsonIgnoreIfNull]
    public string? OrigenSeed { get; set; }
}
