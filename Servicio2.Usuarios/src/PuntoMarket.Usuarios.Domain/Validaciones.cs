using System.Text.RegularExpressions;

namespace PuntoMarket.Usuarios.Domain;

public static class Validaciones
{
    private static readonly Regex FormatoCorreo = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static Result NoVacio(string? valor, string campo) =>
        string.IsNullOrWhiteSpace(valor)
            ? Result.Fail($"{campo} es obligatorio.")
            : Result.Ok();

    public static Result LongitudMaxima(string? valor, int maximo, string campo) =>
        Texto.NormalizarEspacios(valor).Length > maximo
            ? Result.Fail($"{campo} admite hasta {maximo} caracteres.")
            : Result.Ok();

    public static Result SinCaracteresDeControl(string? valor, string campo) =>
        (valor ?? "").Any(char.IsControl)
            ? Result.Fail($"{campo} contiene caracteres no permitidos.")
            : Result.Ok();

    public static Result Correo(string? valor, string campo) =>
        FormatoCorreo.IsMatch(Texto.NormalizarEspacios(valor))
            ? Result.Ok()
            : Result.Fail($"{campo} no tiene un formato válido.");

    public static Result Positivo(int valor, string campo) =>
        valor <= 0
            ? Result.Fail($"{campo} es obligatorio.")
            : Result.Ok();

    public static Result PrimerError(params Result[] resultados)
    {
        foreach (var resultado in resultados)
        {
            if (resultado.IsFail)
                return resultado;
        }

        return Result.Ok();
    }
}
