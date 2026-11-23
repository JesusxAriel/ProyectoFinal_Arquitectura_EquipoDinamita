using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace PuntoMarket.Productos.Adapters.In.Api.Security;

// =====================================================
// INTEGRACIÓN GRUPO 2 - KEYCLOAK  (validación del JWT)
// ESTADO: PREPARADO
//
// QUÉ HACE: configura JwtBearer para validar FIRMA (por JWKS descubierto desde Authority),
//   ISSUER, AUDIENCE y EXPIRACIÓN, y tras validar el token añade los roles mapeados.
//   NO emite tokens ni gestiona usuarios.
// QUÉ DEBE MODIFICAR EL GRUPO 2: solo valores de configuración (ver AuthSettings.cs).
// QUÉ NO DEBE MODIFICARSE: ValidateIssuer/ValidateAudience/ValidateLifetime deben seguir en true.
// CÓMO COMPROBAR: GET /api/productos sin token => 401; con token válido de Administrador => 200.
// =====================================================
public sealed class ConfigureJwtBearerOptions(IOptions<AuthSettings> opciones) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;
        var s = opciones.Value;

        options.Authority = s.Authority;
        // El emisor público se conserva; solo el transporte de metadatos puede usar DNS interno.
        if (!string.IsNullOrWhiteSpace(s.MetadataAddress)) options.MetadataAddress = s.MetadataAddress;
        options.Audience = s.Audience;
        options.RequireHttpsMetadata = s.RequireHttpsMetadata;
        options.MapInboundClaims = false; // conserva "sub", "realm_access", etc. tal como vienen en el token

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = s.UserIdClaim,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = contexto =>
            {
                if (contexto.Principal?.Identity is ClaimsIdentity identidad)
                    KeycloakRolesClaimsMapper.Aplicar(identidad, s);
                return Task.CompletedTask;
            }
        };
    }

    public void Configure(JwtBearerOptions options) => Configure(JwtBearerDefaults.AuthenticationScheme, options);
}
