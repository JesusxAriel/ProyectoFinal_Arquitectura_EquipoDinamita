using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PuntoMarket.Frontend.Auth;
using PuntoMarket.Frontend.Compartido;

namespace PuntoMarket.Frontend.Usuarios;

public sealed class UsuariosApiAdapter(HttpClient httpClient) : IUsuariosApiAdapter
{
    public async Task<Result<IReadOnlyList<UsuarioResponse>>> Listar()
    {
        try
        {
            using var response = await httpClient.GetAsync("api/usuarios");
            if (!response.IsSuccessStatusCode)
            {
                return Result<IReadOnlyList<UsuarioResponse>>.Fail(await MapearErrorAsync(response));
            }

            var usuarios = await response.Content.ReadFromJsonAsync<List<UsuarioResponse>>();
            return Result<IReadOnlyList<UsuarioResponse>>.Ok(usuarios ?? []);
        }
        catch (HttpRequestException)
        {
            return Result<IReadOnlyList<UsuarioResponse>>.Fail(MensajesAuth.SinConexion);
        }
        catch (OperationCanceledException)
        {
            return Result<IReadOnlyList<UsuarioResponse>>.Fail("El servidor tardó demasiado en responder. Intente nuevamente.");
        }
    }

    public async Task<Result<UsuarioResponse>> Obtener(int id)
    {
        try
        {
            using var response = await httpClient.GetAsync($"api/usuarios/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return Result<UsuarioResponse>.Fail(await MapearErrorAsync(response));
            }

            var usuario = await response.Content.ReadFromJsonAsync<UsuarioResponse>();
            return usuario is null
                ? Result<UsuarioResponse>.Fail("No se pudo leer la información del usuario.")
                : Result<UsuarioResponse>.Ok(usuario);
        }
        catch (HttpRequestException)
        {
            return Result<UsuarioResponse>.Fail(MensajesAuth.SinConexion);
        }
        catch (OperationCanceledException)
        {
            return Result<UsuarioResponse>.Fail("El servidor tardó demasiado en responder. Intente nuevamente.");
        }
    }

    public async Task<Result<IReadOnlyList<RolResponse>>> ListarRoles()
    {
        try
        {
            using var response = await httpClient.GetAsync("api/usuarios/roles");
            if (!response.IsSuccessStatusCode)
            {
                return Result<IReadOnlyList<RolResponse>>.Fail(await MapearErrorAsync(response));
            }

            var roles = await response.Content.ReadFromJsonAsync<List<RolResponse>>();
            return Result<IReadOnlyList<RolResponse>>.Ok(roles ?? []);
        }
        catch (HttpRequestException)
        {
            return Result<IReadOnlyList<RolResponse>>.Fail(MensajesAuth.SinConexion);
        }
        catch (OperationCanceledException)
        {
            return Result<IReadOnlyList<RolResponse>>.Fail("El servidor tardó demasiado en responder. Intente nuevamente.");
        }
    }

    public async Task<Result> Crear(CreateUsuarioRequest request)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync("api/usuarios", request);
            return response.IsSuccessStatusCode
                ? Result.Ok()
                : Result.Fail(await MapearErrorAsync(response));
        }
        catch (HttpRequestException)
        {
            return Result.Fail(MensajesAuth.SinConexion);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("El servidor tardó demasiado en responder. Intente nuevamente.");
        }
    }

    public async Task<Result> Actualizar(int id, UpdateUsuarioRequest request)
    {
        try
        {
            using var response = await httpClient.PutAsJsonAsync($"api/usuarios/{id}", request);
            return response.IsSuccessStatusCode
                ? Result.Ok()
                : Result.Fail(await MapearErrorAsync(response));
        }
        catch (HttpRequestException)
        {
            return Result.Fail(MensajesAuth.SinConexion);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("El servidor tardó demasiado en responder. Intente nuevamente.");
        }
    }

    public async Task<Result> CambiarEstado(int id, ChangeStatusRequest request)
    {
        try
        {
            using var response = await httpClient.PatchAsJsonAsync($"api/usuarios/{id}/estado", request);
            return response.IsSuccessStatusCode
                ? Result.Ok()
                : Result.Fail(await MapearErrorAsync(response));
        }
        catch (HttpRequestException)
        {
            return Result.Fail(MensajesAuth.SinConexion);
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("El servidor tardó demasiado en responder. Intente nuevamente.");
        }
    }

    private static async Task<string> MapearErrorAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var mensaje = await LeerMensajeErrorAsync(response);
            return mensaje ?? "Revise los datos ingresados.";
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Su sesión expiró o no está autenticado. Inicie sesión nuevamente.",
            HttpStatusCode.Forbidden => MensajesAuth.SinPermisos,
            HttpStatusCode.NotFound => "No se encontró el usuario solicitado.",
            HttpStatusCode.ServiceUnavailable => MensajesAuth.ServicioNoDisponible,
            _ => MensajesAuth.ErrorInesperado
        };
    }

    private static async Task<string?> LeerMensajeErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorApi>();
            return string.IsNullOrWhiteSpace(error?.Error) ? null : error.Error;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return null;
        }
    }
}