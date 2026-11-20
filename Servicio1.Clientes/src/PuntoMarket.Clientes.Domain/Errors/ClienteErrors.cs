using PuntoMarket.Clientes.Domain.Common;

namespace PuntoMarket.Clientes.Domain.Errors;

/// <summary>Catálogo de errores esperados del negocio de Clientes (cada uno es un Error, no una excepción).</summary>
public static class ClienteErrors
{
    public static Error NoEncontrado(int id) =>
        Error.NotFound("Cliente.NoEncontrado", $"No existe un cliente con el identificador {id}.");

    public static readonly Error NitCiDuplicado =
        Error.Conflict("Cliente.NitCiDuplicado", "Ya existe un cliente activo con ese NIT o CI.");

    public static readonly Error GenericoProtegido =
        Error.Conflict("Cliente.GenericoProtegido", "El cliente genérico no puede modificarse ni darse de baja.");

    public static readonly Error Inactivo =
        Error.Conflict("Cliente.Inactivo", "No se puede editar un cliente inactivo. Reactívelo primero.");

    public static readonly Error YaInactivo =
        Error.Conflict("Cliente.YaInactivo", "El cliente ya está inactivo.");

    public static readonly Error YaActivo =
        Error.Conflict("Cliente.YaActivo", "El cliente ya está activo.");

    public static readonly Error UsuarioNoIdentificado =
        Error.Unauthorized("Cliente.UsuarioNoIdentificado", "El token no contiene el identificador del usuario.");

    public static Error DatosInvalidos(IReadOnlyDictionary<string, string[]> campos) =>
        new("Cliente.DatosInvalidos", "Los datos del cliente no son válidos.", ErrorType.Validation) { Fields = campos };
}
