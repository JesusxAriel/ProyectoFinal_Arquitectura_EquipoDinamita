using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Proveedores.Domain.Entities;

/// <summary>
/// Normaliza texto para comparar nombres: recorta, colapsa espacios, quita tildes y pasa a minúsculas.
/// "  Distribuidora   ÁNDINA " y "distribuidora andina" producen la misma clave.
/// </summary>
public static class NombreNormalizador
{
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var colapsado = Regex.Replace(texto.Trim(), @"\s+", " ");
        var descompuesto = colapsado.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>Recorta y colapsa espacios conservando mayúsculas y tildes (para mostrar).</summary>
    public static string Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? string.Empty : Regex.Replace(texto.Trim(), @"\s+", " ");
}
