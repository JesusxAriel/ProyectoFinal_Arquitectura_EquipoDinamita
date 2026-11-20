using MySql.Data.MySqlClient;

namespace PuntoMarket.Usuarios.Infrastructure.Persistence;

public sealed class ConexionMySql
{
    private readonly Lazy<string> cadenaConexion;

    public ConexionMySql(Func<string?> obtenerCadenaConexion)
    {
        ArgumentNullException.ThrowIfNull(obtenerCadenaConexion);
        cadenaConexion = new Lazy<string>(() => Normalizar(obtenerCadenaConexion()));
    }

    public MySqlConnection CrearConexion() => new(cadenaConexion.Value);

    private static string Normalizar(string? cadena)
    {
        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:MySqlConnection para la persistencia de usuarios.");
        }

        return new MySqlConnectionStringBuilder(cadena) { AllowUserVariables = true }.ConnectionString;
    }
}
