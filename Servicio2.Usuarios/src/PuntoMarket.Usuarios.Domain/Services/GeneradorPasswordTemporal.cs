using System.Security.Cryptography;

namespace PuntoMarket.Usuarios.Domain.Services;

public static class GeneradorPasswordTemporal
{
    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnopqrstuvwxyz";
    private const string Digitos = "23456789";
    private const string Especiales = "!#$%&*+-?@";

    private static readonly string[] AlfabetosObligatorios =
        [Mayusculas, Minusculas, Digitos, Especiales];

    private static readonly string TodosLosCaracteres = string.Concat(AlfabetosObligatorios);

    private static readonly int Longitud = AlfabetosObligatorios.Length + PoliticaDePassword.LongitudMinima;

    public static string Generar()
    {
        var caracteres = new char[Longitud];

        for (var posicion = 0; posicion < AlfabetosObligatorios.Length; posicion++)
            caracteres[posicion] = Elegir(AlfabetosObligatorios[posicion]);

        for (var posicion = AlfabetosObligatorios.Length; posicion < Longitud; posicion++)
            caracteres[posicion] = Elegir(TodosLosCaracteres);

        RandomNumberGenerator.Shuffle<char>(caracteres);

        return new string(caracteres);
    }

    private static char Elegir(string alfabeto) =>
        alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];
}
