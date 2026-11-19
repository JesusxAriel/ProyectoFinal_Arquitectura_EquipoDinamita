using Proveedores.Domain.Common;
using Proveedores.Domain.Errors;

namespace Proveedores.Domain.Entities;

/// <summary>
/// Entidad de dominio Proveedor. Producto del patrón Factory Method
/// (ver Proveedores.Domain/Creators). El constructor es internal: la única forma
/// de crear un proveedor NUEVO es mediante un <see cref="Creators.ProveedorCreator"/>,
/// que garantiza las invariantes. <see cref="Rehidratar"/> restaura datos ya persistidos.
/// </summary>
public sealed class Proveedor
{
    public const int NombreMaxLength = 150;   // VARCHAR(150) en PROVEEDOR.nombreEmpresa
    public const int NumeroMaxLength = 20;    // VARCHAR(20)  en PROVEEDOR.numeroEmpresa
    public const int CorreoMaxLength = 150;   // VARCHAR(150) en PROVEEDOR.correoReferencia

    internal Proveedor(DatosProveedorValidados datos, string idUsuarioAdmin, DateTime fechaCreacionUtc)
    {
        Id = string.Empty;
        NombreEmpresa = datos.NombreEmpresa;
        NumeroEmpresa = datos.NumeroEmpresa;
        CorreoReferencia = datos.CorreoReferencia;
        EsAutogestionado = datos.EsAutogestionado;
        EstaActivo = true;
        IdUsuarioAdmin = idUsuarioAdmin;
        FechaCreacion = fechaCreacionUtc;
    }

    private Proveedor()
    {
        Id = string.Empty;
        NombreEmpresa = string.Empty;
        NumeroEmpresa = string.Empty;
        IdUsuarioAdmin = string.Empty;
    }

    public string Id { get; private set; }
    // INTEGRACIÓN: Versión interna del servidor; no viaja en los DTOs. Protege escrituras solapadas,
    //   no detecta por sí sola un formulario antiguo enviado después de una actualización ya terminada.
    public long Version { get; private set; }

    /// <summary>Confirma una escritura aceptada mediante control optimista de concurrencia.</summary>
    public void ConfirmarEscritura() => Version = checked(Version + 1);
    public string NombreEmpresa { get; private set; }
    public string NumeroEmpresa { get; private set; }
    public string? CorreoReferencia { get; private set; }
    public bool EsAutogestionado { get; private set; }
    public bool EstaActivo { get; private set; }

    // Auditoría. Los identificadores de usuario son cadenas porque coinciden con el claim "sub" (Keycloak).
    // INTEGRACIÓN: IdUsuarioAdmin significa creador, no propietario ni permiso sobre el documento.
    //   Puede ser un actor de sistema del seed; no debe exigirse que sea un UUID ni un ID entero.
    public string IdUsuarioAdmin { get; private set; }
    public string? IdUsuarioActualizacion { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaActualizacion { get; private set; }

    public string NombreNormalizado => NombreNormalizador.Normalizar(NombreEmpresa);

    /// <summary>Restaura una entidad desde persistencia sin revalidar (los datos ya fueron validados al guardarse).</summary>
    public static Proveedor Rehidratar(
        string id, string nombreEmpresa, string numeroEmpresa, string? correoReferencia,
        bool esAutogestionado, bool estaActivo, string idUsuarioAdmin, string? idUsuarioActualizacion,
        DateTime fechaCreacion, DateTime? fechaActualizacion, long version = 0) =>
        new()
        {
            Id = id,
            Version = version,
            NombreEmpresa = nombreEmpresa,
            NumeroEmpresa = numeroEmpresa,
            CorreoReferencia = correoReferencia,
            EsAutogestionado = esAutogestionado,
            EstaActivo = estaActivo,
            IdUsuarioAdmin = idUsuarioAdmin,
            IdUsuarioActualizacion = idUsuarioActualizacion,
            FechaCreacion = fechaCreacion,
            FechaActualizacion = fechaActualizacion
        };

    /// <summary>Asigna el identificador generado por la persistencia (una sola vez).</summary>
    public void AsignarIdentificador(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("El identificador no puede estar vacío.", nameof(id));
        if (Id.Length != 0) throw new InvalidOperationException("El proveedor ya tiene identificador asignado.");
        Id = id;
    }

    public Result Actualizar(DatosProveedor datos, string idUsuario, DateTime ahoraUtc)
    {
        if (!EstaActivo) return Result.Failure(ProveedorErrors.EdicionDeInactivo);

        var validacion = ProveedorValidador.Validar(datos);
        if (validacion.IsFailure) return Result.Failure(validacion.Error);

        var v = validacion.Value;
        NombreEmpresa = v.NombreEmpresa;
        NumeroEmpresa = v.NumeroEmpresa;
        CorreoReferencia = v.CorreoReferencia;
        EsAutogestionado = v.EsAutogestionado;
        RegistrarAuditoria(idUsuario, ahoraUtc);
        return Result.Success();
    }

    /// <summary>Eliminación lógica: estaActivo = false.</summary>
    public Result Desactivar(string idUsuario, DateTime ahoraUtc)
    {
        if (!EstaActivo) return Result.Failure(ProveedorErrors.YaDesactivado);
        EstaActivo = false;
        RegistrarAuditoria(idUsuario, ahoraUtc);
        return Result.Success();
    }

    public Result Reactivar(string idUsuario, DateTime ahoraUtc)
    {
        if (EstaActivo) return Result.Failure(ProveedorErrors.YaActivo);
        EstaActivo = true;
        RegistrarAuditoria(idUsuario, ahoraUtc);
        return Result.Success();
    }

    private void RegistrarAuditoria(string idUsuario, DateTime ahoraUtc)
    {
        IdUsuarioActualizacion = idUsuario;
        FechaActualizacion = ahoraUtc;
    }
}
