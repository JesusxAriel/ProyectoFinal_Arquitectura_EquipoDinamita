using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Proveedores.Domain.Common;
using Proveedores.Domain.Errors;

namespace Proveedores.Domain.Entities;

/// <summary>Reglas de validación del proveedor. Devuelve Result; nunca lanza por datos inválidos.</summary>
// INTEGRACIÓN: Autoridad de los datos comerciales, sin HTTP ni BSON. El frontend anticipa mensajes;
//   Factory Method y Actualizar reutilizan estas reglas y devuelven Result, no excepciones de negocio.
public static class ProveedorValidador
{
    private static readonly Regex FormatoNumero = new(@"\A[467][0-9]{7}\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex FormatoLocal = new(@"\A[A-Za-z0-9!#$%&'*+/=?^_`{|}~.-]+\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex EtiquetaDominio = new(@"\A[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\z", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex LlamadaEjecutable = new(@"\b(?:alert|eval|prompt|confirm|setTimeout|setInterval|Function)\s*\(", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public static Result<DatosProveedorValidados> Validar(DatosProveedor datos)
    {
        var errores = new List<Error>();
        var campos = new Dictionary<string, string[]>();
        void Agregar(string campo, Error error) { errores.Add(error); campos[campo] = [error.Message]; }

        var nombre = NombreNormalizador.Limpiar(datos.NombreEmpresa);
        if (TextoSeguro.ContieneCaracteresProhibidos(datos.NombreEmpresa)) Agregar("nombreEmpresa", ProveedorErrors.NombreInvalido);
        else if (nombre.Length == 0) Agregar("nombreEmpresa", ProveedorErrors.NombreRequerido);
        else if (nombre.Length > Proveedor.NombreMaxLength) Agregar("nombreEmpresa", ProveedorErrors.NombreMuyLargo);
        else if (!NombreEsValido(nombre)) Agregar("nombreEmpresa", ProveedorErrors.NombreInvalido);

        var numero = (datos.NumeroEmpresa ?? string.Empty).Trim();
        if (TextoSeguro.ContieneCaracteresProhibidos(datos.NumeroEmpresa)) Agregar("numeroEmpresa", ProveedorErrors.NumeroInvalido);
        else if (numero.Length == 0) Agregar("numeroEmpresa", ProveedorErrors.NumeroRequerido);
        else if (numero.Length > Proveedor.NumeroMaxLength) Agregar("numeroEmpresa", ProveedorErrors.NumeroMuyLargo);
        else if (!FormatoNumero.IsMatch(numero)) Agregar("numeroEmpresa", ProveedorErrors.NumeroInvalido);

        string? correo = null;
        if (TextoSeguro.ContieneCaracteresProhibidos(datos.CorreoReferencia)) Agregar("correoReferencia", ProveedorErrors.CorreoInvalido);
        else if (!string.IsNullOrWhiteSpace(datos.CorreoReferencia))
        {
            correo = datos.CorreoReferencia.Trim();
            if (correo.Length > Proveedor.CorreoMaxLength) Agregar("correoReferencia", ProveedorErrors.CorreoMuyLargo);
            else if (!CorreoEsValido(correo)) Agregar("correoReferencia", ProveedorErrors.CorreoInvalido);
        }

        if (errores.Count == 0)
            return Result.Success(new DatosProveedorValidados(nombre, numero, correo, datos.EsAutogestionado));

        if (errores.Count == 1)
            return Result.Failure<DatosProveedorValidados>(errores[0] with { Fields = campos });

        // Varias violaciones: se informan todas juntas en un solo error de validación.
        var mensaje = string.Join(" ", errores.Select(e => e.Message));
        return Result.Failure<DatosProveedorValidados>(
            Error.Validation("Proveedor.DatosInvalidos", mensaje) with { Fields = campos });
    }

    private static bool CorreoEsValido(string correo)
    {
        var partes = correo.Split('@');
        if (partes.Length != 2) return false;
        var local = partes[0];
        if (local.Length is 0 or > 64 || !FormatoLocal.IsMatch(local)
            || local.StartsWith('.') || local.EndsWith('.') || local.Contains("..")) return false;
        var etiquetas = partes[1].Split('.');
        return etiquetas.Length >= 2 && etiquetas.All(e => EtiquetaDominio.IsMatch(e))
            && (etiquetas[^1].Length >= 2 && etiquetas[^1].All(char.IsAsciiLetter));
    }

    private static bool NombreEsValido(string nombre)
    {
        var tieneLetraONumero = false;
        foreach (var rune in nombre.EnumerateRunes())
        {
            if (Rune.IsLetter(rune) || Rune.IsNumber(rune)) { tieneLetraONumero = true; continue; }
            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark) continue;
            if (Rune.IsWhiteSpace(rune)) continue;
            if (rune.IsAscii && ".-()'/&,+:#%!?@\"".Contains((char)rune.Value)) continue;
            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.CurrencySymbol or UnicodeCategory.DashPunctuation) continue;
            if (rune.Value is 0x2019 or 0x2018 or 0x201C or 0x201D or 0x00B0 or 0x00BA or 0x00AE or 0x00A9 or 0x2122) continue;
            return false;
        }
        // La gramática excluye marcado y delimitadores de código; no se elimina contenido.
        return tieneLetraONumero && !LlamadaEjecutable.IsMatch(nombre) && !nombre.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
            && !nombre.Contains("vbscript:", StringComparison.OrdinalIgnoreCase)
            && !nombre.Contains("data:text/", StringComparison.OrdinalIgnoreCase);
    }
}
