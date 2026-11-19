using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Frontend.Demo.Services;

// DEMO TEMPORAL: sustituir por el login real al integrar Keycloak.
// INTEGRACIÓN: La caché es compartida por perfil, no por persona ni audiencia. Sirve para la demo;
//   la identidad real necesitará tokens asociados al usuario y, si corresponde, al recurso solicitado.
//   Renovar aquí significa pedir otro token demo, no ejecutar un flujo OAuth de refresh token.
public sealed class DemoTokenProvider(IHttpClientFactory fabrica, IMemoryCache cache)
{
    private readonly SemaphoreSlim administrador = new(1, 1);
    private readonly SemaphoreSlim cajero = new(1, 1);

    public Task<string?> ObtenerAsync(string perfil, CancellationToken ct) => ObtenerAsync(perfil, null, ct);
    public Task<string?> RenovarAsync(string perfil, string tokenRechazado, CancellationToken ct) =>
        ObtenerAsync(perfil, tokenRechazado, ct);

    public async Task InvalidarAsync(string perfil, string tokenRechazado, CancellationToken ct)
    {
        var cerrojo = perfil == DemoPerfilSession.Administrador ? administrador : cajero;
        await cerrojo.WaitAsync(ct);
        try
        {
            var clave = $"demo-token:{perfil}";
            if (cache.TryGetValue(clave, out string? existente) && existente == tokenRechazado)
                cache.Remove(clave);
        }
        finally { cerrojo.Release(); }
    }

    private async Task<string?> ObtenerAsync(string perfil, string? tokenRechazado, CancellationToken ct)
    {
        var cerrojo = perfil == DemoPerfilSession.Administrador ? administrador : cajero;
        await cerrojo.WaitAsync(ct);
        try
        {
            var clave = $"demo-token:{perfil}";
            if (cache.TryGetValue(clave, out string? existente) && existente is not null)
            {
                // Otra solicitud puede haber renovado ya el token rechazado.
                if (existente != tokenRechazado) return existente;
                cache.Remove(clave);
            }
            using var http = fabrica.CreateClient("ApiGateway");
            using var respuesta = await http.PostAsJsonAsync("dev-auth/token", new { perfil }, ct);
            if (!respuesta.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                !doc.RootElement.TryGetProperty("access_token", out var t) || t.ValueKind != JsonValueKind.String)
                return null;
            var token = t.GetString();
            var segundos = 600;
            if (doc.RootElement.TryGetProperty("expires_in", out var e) &&
                (e.ValueKind != JsonValueKind.Number || !e.TryGetInt32(out segundos))) return null;
            if (string.IsNullOrWhiteSpace(token) || segundos <= 0) return null;
            if (segundos > 60) cache.Set(clave, token, TimeSpan.FromSeconds(segundos - 60));
            return token;
        }
        finally { cerrojo.Release(); }
    }
}
