namespace Proveedores.API.Security;

/// <summary>Roles internos del microservicio (independientes de cómo se llamen en Keycloak: ver RoleMapping).</summary>
public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Cajero = "Cajero";
}

/// <summary>Políticas de autorización. Lectura: Administrador y Cajero. Escritura: solo Administrador.</summary>
public static class Policies
{
    public const string Lectura = "ProveedoresLectura";
    public const string Escritura = "ProveedoresEscritura";
}
