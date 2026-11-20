using System.Security.Claims;
using System.Text.Json;

namespace PuntoMarket.Clientes.Adapters.In.Api.Security;

// =====================================================
// INTEGRACIÓN GRUPO 2 - KEYCLOAK  (mapeo de roles)
// ESTADO: PREPARADO
//
// QUÉ HACE ESTE ARCHIVO:
//   Keycloak NO envía los roles como ClaimTypes.Role. Los coloca en claims JSON, p. ej.
//     "realm_access": { "roles": ["admin"] }
//     "resource_access": { "clientes-api": { "roles": ["admin"] } }
//   Este mapper lee las rutas configuradas en AuthSettings.RoleClaimPaths, traduce los
//   nombres externos con AuthSettings.RoleMapping y agrega claims ClaimTypes.Role
//   ("Administrador"/"Cajero") que usan las políticas de autorización.
//
// QUÉ DEBE MODIFICAR EL GRUPO 2: normalmente NADA aquí; solo appsettings (RoleClaimPaths/RoleMapping).
//   Si Keycloak usa otra estructura de claims, extender ExtraerValores().
// QUÉ NO DEBE MODIFICARSE: los nombres internos de rol.
// CÓMO COMPROBAR:  decodificar un token real en jwt.io.
// =====================================================
public static class KeycloakRolesClaimsMapper
{
    public static void Aplicar(ClaimsIdentity identidad, AuthSettings settings)
    {
        var rolesExternos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ruta in settings.RoleClaimPaths)
            foreach (var valor in ExtraerValores(identidad.Claims, ruta))
                rolesExternos.Add(valor);

        foreach (var (rolInterno, nombresExternos) in settings.RoleMapping)
        {
            var coincide = rolesExternos.Contains(rolInterno) || nombresExternos.Any(rolesExternos.Contains);
            if (coincide && !identidad.HasClaim(ClaimTypes.Role, rolInterno))
                identidad.AddClaim(new Claim(ClaimTypes.Role, rolInterno));
        }
    }

    public static List<string> ExtraerValores(IEnumerable<Claim> claims, string ruta)
    {
        var resultado = new List<string>();
        var segmentos = ruta.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segmentos.Length == 0) return resultado;

        foreach (var claim in claims.Where(c => c.Type == segmentos[0]))
        {
            if (segmentos.Length == 1)
            {
                AgregarTexto(claim.Value, resultado);
                continue;
            }

            JsonDocument? documento = null;
            try
            {
                documento = JsonDocument.Parse(claim.Value);
                var actual = documento.RootElement;
                var encontrado = true;
                foreach (var segmento in segmentos.Skip(1))
                {
                    if (actual.ValueKind == JsonValueKind.Object && actual.TryGetProperty(segmento, out var siguiente))
                        actual = siguiente;
                    else { encontrado = false; break; }
                }
                if (encontrado) AgregarElemento(actual, resultado);
            }
            catch (JsonException)
            {
                // El claim no es JSON: se ignora (no aporta roles por esa ruta).
            }
            finally
            {
                documento?.Dispose();
            }
        }
        return resultado;
    }

    private static void AgregarTexto(string valor, List<string> destino)
    {
        var texto = valor.Trim();
        if (texto.StartsWith('['))
        {
            try
            {
                using var doc = JsonDocument.Parse(texto);
                AgregarElemento(doc.RootElement, destino);
                return;
            }
            catch (JsonException) { /* se trata como texto simple */ }
        }
        if (texto.Length > 0) destino.Add(texto);
    }

    private static void AgregarElemento(JsonElement elemento, List<string> destino)
    {
        switch (elemento.ValueKind)
        {
            case JsonValueKind.String:
                var s = elemento.GetString();
                if (!string.IsNullOrWhiteSpace(s)) destino.Add(s);
                break;
            case JsonValueKind.Array:
                foreach (var e in elemento.EnumerateArray()) AgregarElemento(e, destino);
                break;
        }
    }
}
