using System.Globalization;
using PuntoMarket.Usuarios.Domain.Entities;

namespace PuntoMarket.Usuarios.Domain.Services;

public static class GeneradorNombreDeUsuario
{
    public const int PrimerIntento = 1;

    public static Result<string> GenerarBase(string? nombres, string? apellidoPaterno)
    {
        var letrasDelNombre = Texto.SoloLetrasSinTildes(Texto.NormalizarEspacios(nombres));
        var letrasDelApellido = Texto.SoloLetrasSinTildes(apellidoPaterno);

        if (letrasDelNombre.Length == 0 || letrasDelApellido.Length == 0)
        {
            return Result<string>.Fail(
                "El nombre y el apellido paterno deben tener letras para generar el nombre de usuario.");
        }

        return Result<string>.Ok(letrasDelNombre[..1] + letrasDelApellido);
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
}
