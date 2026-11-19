using System.Buffers;
using System.Globalization;
using System.Text;

namespace Proveedores.Domain.Entities;

/// <summary>Comprueba el texto original antes de recortarlo o normalizarlo.</summary>
public static class TextoSeguro
{
    public static bool ContieneCaracteresProhibidos(string? texto)
    {
        if (texto is null) return false;
        var restante = texto.AsSpan();
        while (!restante.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(restante, out var rune, out var usados) != OperationStatus.Done) return true;
            var categoria = Rune.GetUnicodeCategory(rune);
            if (categoria is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate
                or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned
                or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator) return true;
            var valor = rune.Value;
            if (valor is 0x034F or 0x115F or 0x1160 or 0x3164 or 0xFFA0
                or >= 0xFE00 and <= 0xFE0F or >= 0xE0100 and <= 0xE01EF) return true;
            restante = restante[usados..];
        }
        return false;
    }
}
