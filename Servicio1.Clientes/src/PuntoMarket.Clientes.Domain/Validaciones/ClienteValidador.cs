using System.Text.RegularExpressions;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Domain.Validaciones;

/// <summary>
/// Autoridad de las reglas de validación de un cliente. Devuelve un Result (patrón RESULT):
/// éxito con los datos ya limpios, o fallo con todos los errores por campo. Nunca lanza excepciones.
/// </summary>
public static partial class ClienteValidador
{
    public const int NitCiMinimo = 5;
    public const int NitCiMaximo = 15;
    public const int RazonSocialMaximo = 150;
    public const int CorreoMaximo = 150;

    [GeneratedRegex("^[0-9]+$")] private static partial Regex SoloDigitos();
    [GeneratedRegex("^[467][0-9]{7}$")] private static partial Regex TelefonoBoliviano();
    [GeneratedRegex(@"\s+")] private static partial Regex EspaciosRepetidos();
    [GeneratedRegex(@"^[A-Za-z0-9._%+\-]{1,64}@([A-Za-z0-9]([A-Za-z0-9\-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$")]
    private static partial Regex CorreoBasico();

    /// <param name="permitirGenerico">Solo el creador del cliente genérico acepta el NIT reservado "0".</param>
    public static Result<DatosCliente> Validar(DatosCliente datos, bool permitirGenerico = false)
    {
        var errores = new Dictionary<string, List<string>>();

        var nitCi = (datos.NitCi ?? string.Empty).Trim();
        var razon = EspaciosRepetidos().Replace((datos.RazonSocial ?? string.Empty).Trim(), " ");
        var correo = string.IsNullOrWhiteSpace(datos.Correo) ? null : datos.Correo.Trim();
        var telefono = string.IsNullOrWhiteSpace(datos.Telefono) ? null : datos.Telefono.Trim();

        ValidarNitCi(nitCi, permitirGenerico, errores);
        ValidarRazonSocial(razon, errores);

        if (correo is not null && (correo.Length > CorreoMaximo || !CorreoBasico().IsMatch(correo)))
            Agregar(errores, "correo", "El correo electrónico no es válido.");

        if (telefono is not null && !TelefonoBoliviano().IsMatch(telefono))
            Agregar(errores, "telefono", "El teléfono debe tener 8 dígitos y comenzar con 4, 6 o 7.");

        if (errores.Count > 0)
            return Result.Failure<DatosCliente>(ClienteErrors.DatosInvalidos(
                errores.ToDictionary(e => e.Key, e => e.Value.ToArray())));

        return Result.Success(new DatosCliente(nitCi, razon, correo, telefono));
    }

    private static void ValidarNitCi(string nitCi, bool permitirGenerico, Dictionary<string, List<string>> errores)
    {
        if (nitCi.Length == 0)
        {
            Agregar(errores, "nitCi", "El NIT o CI es obligatorio.");
            return;
        }

        if (nitCi == Cliente.NitCiGenerico)
        {
            if (!permitirGenerico)
                Agregar(errores, "nitCi", "El NIT o CI 0 está reservado para el cliente genérico.");
            return;
        }

        if (!SoloDigitos().IsMatch(nitCi) || nitCi.Length < NitCiMinimo || nitCi.Length > NitCiMaximo)
            Agregar(errores, "nitCi", $"El NIT o CI debe tener entre {NitCiMinimo} y {NitCiMaximo} dígitos numéricos.");
    }

    private static void ValidarRazonSocial(string razon, Dictionary<string, List<string>> errores)
    {
        if (razon.Length == 0)
            Agregar(errores, "razonSocial", "La razón social es obligatoria.");
        else if (razon.Length > RazonSocialMaximo)
            Agregar(errores, "razonSocial", $"La razón social no puede superar los {RazonSocialMaximo} caracteres.");
        else if (razon.Any(c => char.IsControl(c) || c is '<' or '>'))
            Agregar(errores, "razonSocial", "La razón social no puede contener caracteres de control ni los símbolos < y >.");
    }

    private static void Agregar(Dictionary<string, List<string>> errores, string campo, string mensaje)
    {
        if (!errores.TryGetValue(campo, out var lista)) errores[campo] = lista = [];
        lista.Add(mensaje);
    }
}
