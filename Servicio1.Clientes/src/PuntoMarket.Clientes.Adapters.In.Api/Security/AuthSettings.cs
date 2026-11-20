namespace PuntoMarket.Clientes.Adapters.In.Api.Security;

// =====================================================
// INTEGRACIÓN GRUPO 2 - KEYCLOAK
// ESTADO: PREPARADO
//
// QUÉ HACE ESTE ARCHIVO:
//   Define la configuración de autenticación (sección "Authentication:Jwt" de
//   Adapters.In.Api/appsettings.json o variables Authentication__Jwt__*).
//
// QUÉ DEBE MODIFICAR EL GRUPO 2 (solo configuración):
//   - Authority: URL del realm, p. ej. https://<host>/realms/<realm>
//   - Audience: valor del claim "aud" que emite Keycloak para esta API
//   - RequireHttpsMetadata: true en producción
//   - RoleClaimPaths: dónde viajan los roles en el token (realm_access.roles o resource_access.<cliente>.roles)
//   - RoleMapping: nombres reales de roles de Keycloak que equivalen a Administrador y Cajero
//   - UserIdClaim: "sub" (por defecto)
//
// QUÉ NO DEBE MODIFICARSE:
//   Los nombres internos de rol (Roles.Administrador / Roles.Cajero) ni las políticas de autorización.
//
// CÓMO COMPROBAR LA INTEGRACIÓN:
//   Ver INTEGRACION_GRUPO2.md y GUIA_INTEGRACION_MICROSERVICIOS.md.
// =====================================================
public sealed class AuthSettings
{
    public const string SectionName = "Authentication:Jwt";

    public string Authority { get; set; } = string.Empty;
    /// <summary>Opcional: URL de descubrimiento accesible por el servidor (DNS interno de Docker).</summary>
    public string? MetadataAddress { get; set; }
    public string Audience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Claim que identifica al usuario para la auditoría. En Keycloak es "sub".</summary>
    public string UserIdClaim { get; set; } = "sub";

    /// <summary>Rutas (separadas por punto) de los claims donde viajan los roles del token.</summary>
    public string[] RoleClaimPaths { get; set; } = ["realm_access.roles", "roles"];

    /// <summary>Rol interno -> nombres de rol del proveedor de identidad que le corresponden.</summary>
    public Dictionary<string, string[]> RoleMapping { get; set; } = new()
    {
        [Roles.Administrador] = [Roles.Administrador],
        [Roles.Cajero] = [Roles.Cajero]
    };
}
