using System.Globalization;
using PuntoMarket.Usuarios.Domain.Entities;

namespace PuntoMarket.Usuarios.Domain.Services;

public static class GeneradorNombreDeUsuario
{
    public const int PrimerIntento = 1;

    public static Result<string> GenerarBase(string? nombres, string? apellidos)
    {
        var letrasDelNombre = Texto.SoloLetrasSinTildes(Texto.NormalizarEspacios(nombres));
        var primerApellido = PrimerApellido(apellidos);

        if (letrasDelNombre.Length == 0 || primerApellido.Length == 0)
        {
            return Result<string>.Fail(
                "El nombre y el primer apellido deben tener letras para generar el nombre de usuario.");
        }

        return Result<string>.Ok(letrasDelNombre[..1] + primerApellido);
    }

    public static string Siguiente(string nombreBase, int intento)
    {
        var sufijo = intento <= PrimerIntento
            ? ""
            : intento.ToString(CultureInfo.InvariantCulture);
        var espacioDisponible = Math.Min(
            nombreBase.Length,
            Usuario.LongitudMaximaUsername - sufijo.Length);

        return nombreBase[..espacioDisponible] + sufijo;
    }

    private static string PrimerApellido(string? apellidos)
    {
        var normalizados = Texto.NormalizarEspacios(apellidos);

        return normalizados.Length == 0
            ? ""
            : Texto.SoloLetrasSinTildes(normalizados.Split(' ')[0]);
    }
}
