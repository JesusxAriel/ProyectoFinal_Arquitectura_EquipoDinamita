using System.Text;

namespace PuntoMarket.Usuarios.Domain;

public static class Texto
{
    public static string NormalizarEspacios(string? valor) =>
        string.Join(" ", (valor ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static string SoloLetrasSinTildes(string? valor) =>
        new((valor ?? "").Normalize(NormalizationForm.FormD)
            .Where(caracter => caracter is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
            .Select(char.ToLowerInvariant)
            .ToArray());
}
