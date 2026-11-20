using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Errors;
using PuntoMarket.Clientes.Domain.Validaciones;

namespace PuntoMarket.Clientes.Domain.Entities;

/// <summary>
/// Entidad Cliente. Contiene sus propias reglas de estado (editar, dar de baja, reactivar) y
/// devuelve Result en lugar de lanzar excepciones. No conoce MySQL, HTTP ni ASP.NET.
/// Los clientes nuevos se crean únicamente mediante los creadores (FACTORY METHOD).
/// </summary>
public sealed class Cliente
{
    /// <summary>NIT reservado del cliente genérico ("Sin Nombre"), usado en ventas sin identificar al comprador.</summary>
    public const string NitCiGenerico = "0";

    public int Id { get; private set; }
    public string NitCi { get; private set; } = string.Empty;
    public string RazonSocial { get; private set; } = string.Empty;
    public string? Correo { get; private set; }
    public string? Telefono { get; private set; }
    public bool EstaActivo { get; private set; }

    // Auditoría: identificadores (claim "sub" del JWT) y fechas en UTC. Los fija el servidor.
    public string IdUsuarioCreacion { get; private set; } = string.Empty;
    public string? IdUsuarioActualizacion { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaActualizacion { get; private set; }

    public bool EsGenerico => NitCi == NitCiGenerico;

    private Cliente() { }

    /// <summary>Solo los creadores del dominio (misma biblioteca) pueden crear clientes nuevos.</summary>
    internal static Cliente Nuevo(DatosCliente datos, string idUsuario, DateTime ahoraUtc) => new()
    {
        NitCi = datos.NitCi,
        RazonSocial = datos.RazonSocial,
        Correo = datos.Correo,
        Telefono = datos.Telefono,
        EstaActivo = true,
        IdUsuarioCreacion = idUsuario,
        FechaCreacion = ahoraUtc
    };

    /// <summary>Reconstruye un cliente leído de la base de datos. No revalida datos históricos.</summary>
    public static Cliente Rehidratar(
        int id, string nitCi, string razonSocial, string? correo, string? telefono, bool estaActivo,
        string idUsuarioCreacion, string? idUsuarioActualizacion, DateTime fechaCreacion, DateTime? fechaActualizacion) => new()
    {
        Id = id,
        NitCi = nitCi,
        RazonSocial = razonSocial,
        Correo = correo,
        Telefono = telefono,
        EstaActivo = estaActivo,
        IdUsuarioCreacion = idUsuarioCreacion,
        IdUsuarioActualizacion = idUsuarioActualizacion,
        FechaCreacion = fechaCreacion,
        FechaActualizacion = fechaActualizacion
    };

    /// <summary>El repositorio asigna el identificador autogenerado tras insertar.</summary>
    public void AsignarId(int id)
    {
        if (Id != 0) throw new InvalidOperationException("El cliente ya tiene un identificador asignado.");
        Id = id;
    }

    public Result Actualizar(DatosCliente datos, string idUsuario, DateTime ahoraUtc)
    {
        if (EsGenerico) return Result.Failure(ClienteErrors.GenericoProtegido);
        if (!EstaActivo) return Result.Failure(ClienteErrors.Inactivo);

        var validado = ClienteValidador.Validar(datos);
        if (validado.IsFailure) return Result.Failure(validado.Error);

        var limpio = validado.Value;
        NitCi = limpio.NitCi;
        RazonSocial = limpio.RazonSocial;
        Correo = limpio.Correo;
        Telefono = limpio.Telefono;
        RegistrarModificacion(idUsuario, ahoraUtc);
        return Result.Success();
    }

    /// <summary>Baja lógica: el cliente se conserva (historial de ventas) pero deja de estar activo.</summary>
    public Result DarDeBaja(string idUsuario, DateTime ahoraUtc)
    {
        if (EsGenerico) return Result.Failure(ClienteErrors.GenericoProtegido);
        if (!EstaActivo) return Result.Failure(ClienteErrors.YaInactivo);

        EstaActivo = false;
        RegistrarModificacion(idUsuario, ahoraUtc);
        return Result.Success();
    }

    public Result Reactivar(string idUsuario, DateTime ahoraUtc)
    {
        if (EstaActivo) return Result.Failure(ClienteErrors.YaActivo);

        EstaActivo = true;
        RegistrarModificacion(idUsuario, ahoraUtc);
        return Result.Success();
    }

    private void RegistrarModificacion(string idUsuario, DateTime ahoraUtc)
    {
        IdUsuarioActualizacion = idUsuario;
        FechaActualizacion = ahoraUtc;
    }
}
