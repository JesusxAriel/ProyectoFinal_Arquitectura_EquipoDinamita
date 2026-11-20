using System.Net.Mail;
using System.Text.RegularExpressions;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Domain.Validaciones;

public static partial class ClienteValidador
{
    [GeneratedRegex(@"^[0-9]{5,15}$", RegexOptions.CultureInvariant)]
    private static partial Regex NitCiRegex();
    [GeneratedRegex(@"^[467][0-9]{7}$", RegexOptions.CultureInvariant)]
    private static partial Regex TelefonoRegex();

    public static Result<DatosCliente> Validar(DatosCliente entrada)
    {
        var campos = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        void ErrorCampo(string campo, string mensaje) {
            if (!campos.TryGetValue(campo, out var mensajes)) campos[campo] = mensajes = [];
            mensajes.Add(mensaje);
        }

        var nit = entrada.NitCi?.Trim() ?? string.Empty;
        if (nit.Length == 0) ErrorCampo(nameof(entrada.NitCi), "El NIT/CI es obligatorio.");
        else if (nit == "0") ErrorCampo(nameof(entrada.NitCi), "El NIT/CI 0 está reservado al cliente genérico.");
        else if (!NitCiRegex().IsMatch(nit)) ErrorCampo(nameof(entrada.NitCi), "El NIT/CI debe tener entre 5 y 15 dígitos.");

        var razon = ColapsarEspacios(entrada.RazonSocial);
        if (razon.Length == 0) ErrorCampo(nameof(entrada.RazonSocial), "La razón social es obligatoria.");
        else if (razon.Length > 150 || razon.IndexOfAny(['<', '>', '\r', '\n', '\t']) >= 0)
            ErrorCampo(nameof(entrada.RazonSocial), "La razón social no supera los 150 caracteres ni puede contener caracteres de control o < >.");

        var correo = string.IsNullOrWhiteSpace(entrada.Correo) ? null : entrada.Correo.Trim();
        if (correo is not null) {
            if (correo.Length > 150 || !CorreoValido(correo))
                ErrorCampo(nameof(entrada.Correo), "El correo electrónico no tiene un formato válido.");
        }

        var telefono = string.IsNullOrWhiteSpace(entrada.Telefono) ? null : entrada.Telefono.Trim();
        if (telefono is not null && !TelefonoRegex().IsMatch(telefono))
            ErrorCampo(nameof(entrada.Telefono), "El teléfono debe tener 8 dígitos y comenzar con 4, 6 o 7.");

        if (campos.Count > 0)
            return Result.Failure<DatosCliente>(ClienteErrors.Validacion(
                campos.ToDictionary(x => x.Key, x => (string[])x.Value.ToArray())));
        return Result.Success(new DatosCliente(nit, razon, correo, telefono));
    }

    public static string ColapsarEspacios(string? texto) =>
        string.Join(' ', (texto ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static bool CorreoValido(string correo)
    {
        try { var a = new MailAddress(correo); return string.Equals(a.Address, correo, StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }
}
