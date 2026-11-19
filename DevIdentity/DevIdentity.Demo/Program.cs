using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// =====================================================
// DEMO TEMPORAL - IDENTIDAD DE DESARROLLO (NO ES KEYCLOAK, NO ES PRODUCCIÓN)
// ESTADO: TEMPORAL. REEMPLAZAR POR EL KEYCLOAK DEL GRUPO 2.
//
// QUÉ HACE ESTE ARCHIVO:
//   Mini proveedor OpenID Connect local que expone lo mismo que necesita JwtBearer de la API:
//     GET  /realms/proveedores-dev/.well-known/openid-configuration   (descubrimiento)
//     GET  /realms/proveedores-dev/protocol/openid-connect/certs      (JWKS: solo clave PÚBLICA RSA)
//   y emite JWT firmados con RS256 verificables por la API para DOS identidades fijas:
//     POST /token  { "perfil": "Administrador" | "Cajero" }
//   No existen contraseñas, usuarios ni registro. No se pueden pedir roles arbitrarios: el perfil
//   solo elige una de las dos identidades de demostración predefinidas en este archivo.
//   La clave RSA se genera en memoria al arrancar (no hay secretos en el repositorio).
//
// AISLAMIENTO: se niega a arrancar si ASPNETCORE_ENVIRONMENT no es Development.
// CÓMO RETIRARLO: quitar el servicio "dev-identity" de docker-compose.yml y la ruta "dev-auth" del
//   Gateway; apuntar Authentication__Jwt__Authority/Audience al Keycloak real. No requiere tocar
//   Domain, Application ni la lógica CRUD (ver INTEGRACION_GRUPO2.md, sección 18).
// =====================================================
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

if (!app.Environment.IsDevelopment())
    throw new InvalidOperationException(
        "DevIdentity.Demo es solo para desarrollo/demostración y no puede ejecutarse fuera de Development.");

var config = app.Configuration;
var issuer = config["DevIdentity:Issuer"] ?? "http://localhost:5300/realms/proveedores-dev";
var audience = config["DevIdentity:Audience"] ?? "proveedores-api";
var minutosVigencia = config.GetValue("DevIdentity:TokenMinutes", 60);

using var rsa = RSA.Create(2048);
var kid = Guid.NewGuid().ToString("N");
var firma = new SigningCredentials(new RsaSecurityKey(rsa) { KeyId = kid }, SecurityAlgorithms.RsaSha256);

// Identidades de demostración FIJAS (el claim "sub" es una cadena, como en Keycloak).
var identidades = new Dictionary<string, (string Sub, string Usuario)>(StringComparer.OrdinalIgnoreCase)
{
    ["Administrador"] = ("a1111111-1111-4111-8111-111111111111", "admin.demo"),
    ["Cajero"] = ("c2222222-2222-4222-8222-222222222222", "cajero.demo")
};

var rutaRealm = new Uri(issuer).AbsolutePath.TrimEnd('/');

app.MapGet($"{rutaRealm}/.well-known/openid-configuration", () => Results.Json(new
{
    issuer,
    jwks_uri = $"{issuer}/protocol/openid-connect/certs",
    id_token_signing_alg_values_supported = new[] { "RS256" },
    subject_types_supported = new[] { "public" },
    response_types_supported = new[] { "token" }
}));

app.MapGet($"{rutaRealm}/protocol/openid-connect/certs", () =>
{
    var p = rsa.ExportParameters(includePrivateParameters: false); // solo parte pública
    return Results.Json(new
    {
        keys = new[]
        {
            new { kty = "RSA", use = "sig", alg = "RS256", kid, n = Base64UrlEncoder.Encode(p.Modulus), e = Base64UrlEncoder.Encode(p.Exponent) }
        }
    });
});

app.MapPost("/token", (TokenRequest solicitud) =>
{
    if (string.IsNullOrWhiteSpace(solicitud.Perfil) || !identidades.TryGetValue(solicitud.Perfil, out var identidad))
        return Results.BadRequest(new { error = "Perfil no válido. Use 'Administrador' o 'Cajero'." });

    var rol = identidades.Keys.First(k => string.Equals(k, solicitud.Perfil, StringComparison.OrdinalIgnoreCase));
    var ahora = DateTime.UtcNow;
    var descriptor = new SecurityTokenDescriptor
    {
        Issuer = issuer,
        Audience = audience,
        NotBefore = ahora.AddSeconds(-5),
        Expires = ahora.AddMinutes(minutosVigencia),
        SigningCredentials = firma,
        Claims = new Dictionary<string, object>
        {
            ["sub"] = identidad.Sub,
            ["preferred_username"] = identidad.Usuario,
            // Misma forma que Keycloak: los roles de realm viajan dentro de realm_access.roles
            ["realm_access"] = new Dictionary<string, object> { ["roles"] = new[] { rol } }
        }
    };
    var token = new JsonWebTokenHandler().CreateToken(descriptor);
    return Results.Ok(new { access_token = token, token_type = "Bearer", expires_in = minutosVigencia * 60, perfil = rol });
});

app.MapGet("/health", () => Results.Ok("ok"));

app.Run();

internal sealed record TokenRequest(string? Perfil);
