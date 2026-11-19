using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Proveedores.Domain.Entities;

namespace Proveedores.Infrastructure.Mongo;

/// <summary>
/// Siete proveedores del INSERT de PROVEEDOR de bdMicroMercadoArqui.sql.
/// IDEMPOTENTE: usa upsert con $setOnInsert sobre la marca "origenSeed". Si el documento ya existe
/// (aunque el usuario lo haya modificado o desactivado) NO se toca. Se desactiva con Mongo__SeedEnabled=false.
/// </summary>
public static class ProveedorSeeder
{
    public const string UsuarioSistema = "sistema-seed";

    // El número SQL solo identifica la marca de seed existente; no sustituye al ObjectId.
    public static IReadOnlyList<(int N, DatosProveedor Datos)> Datos { get; } = Array.AsReadOnly(new (int, DatosProveedor)[]
    {
        (1, new("PIL Andina S.A.", "44112233", "ventas.cbba@pilandina.com.bo", false)),
        (2, new("EMBOL S.A. (Coca-Cola)", "44556677", "pedidos.cbba@embol.com.bo", false)),
        (3, new("Sofía SHE S.A.", "44889900", "contacto.cbba@sofia.com.bo", true)),
        (4, new("CBN (Cervecería Boliviana Nacional)", "44224466", "distribucion@cbn.com.bo", false)),
        (5, new("Unilever Bolivia", "44335577", "atencion@unilever.com", false)),
        (6, new("Industrias Venado S.A. (Kris)", "44118899", "ventas@venado.com.bo", false)),
        (7, new("Arcor Bolivia", "44778811", "pedidos@arcor.com.bo", false))
    });

    internal static async Task SembrarAsync(IMongoCollection<ProveedorDocument> coleccion, ILogger logger, CancellationToken ct)
    {
        var insertados = 0;
        foreach (var (n, datos) in Datos)
        {
            var clave = $"seed-proveedor-{n}";
            var validacion = ProveedorValidador.Validar(datos);
            if (validacion.IsFailure)
                throw new InvalidOperationException($"Datos de seed inválidos ({clave}): {validacion.Error.Message}");
            var valido = validacion.Value;
            var nombre = valido.NombreEmpresa;
            var u = Builders<ProveedorDocument>.Update;
            var update = u.Combine(
                u.SetOnInsert(x => x.NombreEmpresa, nombre),
                u.SetOnInsert(x => x.NombreNormalizado, NombreNormalizador.Normalizar(nombre)),
                u.SetOnInsert(x => x.NumeroEmpresa, valido.NumeroEmpresa),
                u.SetOnInsert(x => x.CorreoReferencia, valido.CorreoReferencia),
                u.SetOnInsert(x => x.EsAutogestionado, valido.EsAutogestionado),
                u.SetOnInsert(x => x.OrigenSeed, clave),
                u.SetOnInsert(x => x.Version, 0),
                u.SetOnInsert(x => x.EstaActivo, true),
                u.SetOnInsert(x => x.IdUsuarioAdmin, UsuarioSistema),
                u.SetOnInsert(x => x.FechaCreacion, DateTime.UtcNow));

            try
            {
                var r = await coleccion.UpdateOneAsync(
                    x => x.OrigenSeed == clave, update,
                    new UpdateOptions { IsUpsert = true }, ct);
                if (r.UpsertedId is not null) insertados++;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                logger.LogWarning("Seed omitido para '{Nombre}': ya existe un proveedor activo con ese nombre.", nombre);
            }
        }
        logger.LogInformation("Seed de proveedores completado. Documentos nuevos: {Insertados}.", insertados);
    }
}
