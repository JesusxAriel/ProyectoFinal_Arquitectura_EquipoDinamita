namespace PuntoMarket.Clientes.Adapters.In.Api.Security;

/// <summary>Roles internos del microservicio (independientes de cómo se llamen en Keycloak: ver RoleMapping).</summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Cajero = "Cajero";
}

/// <summary>
/// Políticas de autorización de Clientes:
///   Lectura  = Administrador o Cajero (listar, buscar, consultar).
///   Gestion  = Administrador o Cajero (crear y editar: el cajero registra clientes en la caja).
///   Baja     = solo Administrador (dar de baja y reactivar).
/// </summary>
public static class Policies
{
    public const string Lectura = "ClientesLectura";
    public const string Gestion = "ClientesGestion";
    public const string Baja = "ClientesBaja";
}
