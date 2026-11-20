using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain;

namespace PuntoMarket.Usuarios.Infrastructure.Identity;

public sealed class KeycloakGestorDeIdentidad(HttpClient httpClient, Lazy<KeycloakOpciones> opcionesPerezosas)
    : IGestorDeIdentidad
{
    private const char SeparadorDeNombre = ' ';

    private KeycloakOpciones Opciones => opcionesPerezosas.Value;

    public Task<Result> CrearUsuario(
        string username, string correo, string nombreCompleto, string rol, string passwordTemporal) =>
        Ejecutar(async () =>
        {
            var token = await ObtenerTokenAdministrador();
            if (token is null)
            {
                return Result.Fail(KeycloakMensajes.ServicioNoDisponible);
            }

            var rolRepresentacion = await ObtenerRol(rol, token);
            if (rolRepresentacion is null)
            {
                return Result.Fail(KeycloakMensajes.RolNoEncontrado);
            }

            var creacion = await CrearEnKeycloak(username, correo, nombreCompleto, passwordTemporal, token);
            if (creacion.IsFail)
            {
                return Result.Fail(creacion.Error);
            }

            var asignacion = await AsignarRol(creacion.Value, rolRepresentacion, token);
            if (asignacion.IsFail)
            {
                await Enviar(HttpMethod.Delete, Opciones.UrlAdmin($"users/{creacion.Value}"), token);
            }

            return asignacion;
        });

    public Task<Result> CambiarPassword(string username, string passwordNueva) =>
        Ejecutar(() => ConUsuario(username, async (id, token) =>
        {
            var cuerpo = new CredencialRepresentacion("password", passwordNueva, false);
            using var respuesta = await Enviar(HttpMethod.Put, Opciones.UrlAdmin($"users/{id}/reset-password"), token, cuerpo);
            return Traducir(respuesta, KeycloakMensajes.PasswordNoCumplePolitica);
        }));

    public Task<Result> VerificarPassword(string username, string passwordActual) =>
        Ejecutar(async () =>
        {
            var formulario = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = Opciones.LoginClientId,
                ["username"] = username,
                ["password"] = passwordActual
            };
            using var respuesta = await httpClient.PostAsync(
                Opciones.UrlToken(Opciones.Realm), new FormUrlEncodedContent(formulario));

            if (respuesta.IsSuccessStatusCode)
            {
                return Result.Ok();
            }

            return respuesta.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized
                ? Result.Fail(KeycloakMensajes.PasswordActualIncorrecta)
                : Result.Fail(KeycloakMensajes.ServicioNoDisponible);
        });

    public Task<Result> CambiarEstado(string username, bool activo) =>
        Ejecutar(() => ConUsuario(username, async (id, token) =>
        {
            using var respuesta = await Enviar(
                HttpMethod.Put, Opciones.UrlAdmin($"users/{id}"), token, new EstadoRepresentacion(activo));
            return Traducir(respuesta, KeycloakMensajes.OperacionRechazada);
        }));

    private static async Task<Result> Ejecutar(Func<Task<Result>> operacion)
    {
        try
        {
            return await operacion();
        }
        catch (Exception excepcion) when (
            excepcion is HttpRequestException or TaskCanceledException or JsonException)
        {
            return Result.Fail(KeycloakMensajes.ServicioNoDisponible);
        }
    }

    private async Task<Result> ConUsuario(string username, Func<string, string, Task<Result>> operacion)
    {
        var token = await ObtenerTokenAdministrador();
        if (token is null)
        {
            return Result.Fail(KeycloakMensajes.ServicioNoDisponible);
        }

        var id = await BuscarIdPorUsername(username, token);
        return id is null ? Result.Fail(KeycloakMensajes.UsuarioNoEncontrado) : await operacion(id, token);
    }

    private async Task<string?> ObtenerTokenAdministrador()
    {
        var formulario = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = Opciones.AdminClientId,
            ["username"] = Opciones.AdminUserName,
            ["password"] = Opciones.AdminPassword
        };
        using var respuesta = await httpClient.PostAsync(
            Opciones.UrlToken(Opciones.AdminRealm), new FormUrlEncodedContent(formulario));

        if (!respuesta.IsSuccessStatusCode)
        {
            return null;
        }

        var token = await respuesta.Content.ReadFromJsonAsync<TokenRespuesta>();
        return string.IsNullOrEmpty(token?.AccessToken) ? null : token.AccessToken;
    }

    private async Task<RolRepresentacion?> ObtenerRol(string rol, string token)
    {
        using var respuesta = await Enviar(HttpMethod.Get, Opciones.UrlAdmin($"roles/{Uri.EscapeDataString(rol)}"), token);
        return respuesta.IsSuccessStatusCode
            ? await respuesta.Content.ReadFromJsonAsync<RolRepresentacion>()
            : null;
    }

    private async Task<Result<string>> CrearEnKeycloak(
        string username, string correo, string nombreCompleto, string passwordTemporal, string token)
    {
        var (nombre, apellido) = SepararNombre(nombreCompleto);
        var cuerpo = new UsuarioNuevo(
            username, correo, nombre, apellido, true, true,
            [new CredencialRepresentacion("password", passwordTemporal, false)]);

        using var respuesta = await Enviar(HttpMethod.Post, Opciones.UrlAdmin("users"), token, cuerpo);
        if (respuesta.StatusCode == HttpStatusCode.Created)
        {
            var id = respuesta.Headers.Location?.Segments.LastOrDefault();
            return string.IsNullOrEmpty(id)
                ? Result<string>.Fail(KeycloakMensajes.OperacionRechazada)
                : Result<string>.Ok(id);
        }

        var error = respuesta.StatusCode == HttpStatusCode.Conflict
            ? KeycloakMensajes.UsuarioExistente
            : Traducir(respuesta, KeycloakMensajes.PasswordNoCumplePolitica).Error;
        return Result<string>.Fail(error);
    }

    private async Task<Result> AsignarRol(string idUsuario, RolRepresentacion rol, string token)
    {
        using var respuesta = await Enviar(
            HttpMethod.Post, Opciones.UrlAdmin($"users/{idUsuario}/role-mappings/realm"), token, new[] { rol });
        return Traducir(respuesta, KeycloakMensajes.OperacionRechazada);
    }

    private async Task<string?> BuscarIdPorUsername(string username, string token)
    {
        var url = Opciones.UrlAdmin($"users?username={Uri.EscapeDataString(username)}&exact=true");
        using var respuesta = await Enviar(HttpMethod.Get, url, token);
        if (!respuesta.IsSuccessStatusCode)
        {
            return null;
        }

        var usuarios = await respuesta.Content.ReadFromJsonAsync<List<UsuarioIdentificador>>();
        return usuarios?.FirstOrDefault()?.Id;
    }

    private Task<HttpResponseMessage> Enviar(HttpMethod metodo, Uri url, string token, object? cuerpo = null)
    {
        var solicitud = new HttpRequestMessage(metodo, url);
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (cuerpo is not null)
        {
            solicitud.Content = JsonContent.Create(cuerpo);
        }

        return httpClient.SendAsync(solicitud);
    }

    private static Result Traducir(HttpResponseMessage respuesta, string mensajeSiRechaza)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return Result.Ok();
        }

        return respuesta.StatusCode is HttpStatusCode.BadRequest
            ? Result.Fail(mensajeSiRechaza)
            : Result.Fail(KeycloakMensajes.ServicioNoDisponible);
    }

    private static (string Nombre, string Apellido) SepararNombre(string nombreCompleto)
    {
        var partes = nombreCompleto.Trim().Split(SeparadorDeNombre, 2, StringSplitOptions.RemoveEmptyEntries);
        var nombre = partes.Length > 0 ? partes[0] : nombreCompleto;
        return (nombre, partes.Length > 1 ? partes[1] : nombre);
    }

    private sealed record TokenRespuesta([property: JsonPropertyName("access_token")] string AccessToken);

    private sealed record UsuarioIdentificador(string Id);

    private sealed record RolRepresentacion(string Id, string Name);

    private sealed record CredencialRepresentacion(string Type, string Value, bool Temporary);

    private sealed record EstadoRepresentacion(bool Enabled);

    private sealed record UsuarioNuevo(
        string Username,
        string Email,
        string FirstName,
        string LastName,
        bool Enabled,
        bool EmailVerified,
        IReadOnlyList<CredencialRepresentacion> Credentials);
}
