namespace PuntoMarket.Productos.Adapters.In.Api.Security;

/// <summary>Roles internos del microservicio (independientes de cómo se llamen en Keycloak: ver RoleMapping).</summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Cajero = "Cajero";
}

/// <summary>
/// Políticas de autorización de Productos:
///   Lectura  = Administrador o Cajero (listar, buscar, consultar).
///   Gestion  = solo Administrador (crear y editar).
///   Baja     = solo Administrador (dar de baja).
/// </summary>
public static class Policies
{
    public const string Lectura = "ProductosLectura";
    public const string Gestion = "ProductosGestion";
    public const string Baja = "ProductosBaja";
}
