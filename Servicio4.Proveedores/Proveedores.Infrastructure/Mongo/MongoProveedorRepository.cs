using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Proveedores.Application.Abstractions;
using Proveedores.Application.Dtos;
using Proveedores.Domain.Common;
using Proveedores.Domain.Entities;
using Proveedores.Domain.Errors;

namespace Proveedores.Infrastructure.Mongo;

// =====================================================
// PATRÓN DE DISEÑO 2/4: ADAPTER (persistencia)
// Adapta la API de MongoDB.Driver (IMongoCollection<ProveedorDocument>) al puerto
// IProveedorRepository definido por Application. Application nunca ve MongoDB.
// Diferencia con Repository: el repositorio es el rol (colección de entidades de dominio);
// el Adapter es la técnica que traduce una interfaz incompatible (la del driver) a la esperada.
// =====================================================
public sealed class MongoProveedorRepository(IMongoCollection<ProveedorDocument> coleccion, ILogger<MongoProveedorRepository> logger)
    : IProveedorRepository
{
    public async Task<Proveedor?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var objectId)) return null; // id mal formado => "no existe" (404)

        var documento = await coleccion.Find(x => x.Id == objectId).FirstOrDefaultAsync(cancellationToken);
        return documento is null ? null : ProveedorDocumentMapper.ToDomain(documento);
    }

    public async Task<PaginaProveedores> ListarAsync(ListarProveedoresQuery consulta, CancellationToken cancellationToken)
    {
        consulta = consulta.Normalizada();
        var f = Builders<ProveedorDocument>.Filter;
        var filtros = new List<FilterDefinition<ProveedorDocument>>();

        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
        {
            var termino = Regex.Escape(NombreNormalizador.Normalizar(consulta.Busqueda));
            filtros.Add(f.Or(
                f.Regex(x => x.NombreNormalizado, new BsonRegularExpression(termino)),
                f.Regex(x => x.NumeroEmpresa, new BsonRegularExpression(Regex.Escape(consulta.Busqueda.Trim()), "i")),
                f.Regex(x => x.CorreoReferencia, new BsonRegularExpression(Regex.Escape(consulta.Busqueda.Trim()), "i"))));
        }

        switch (consulta.Estado)
        {
            case EstadoFiltro.Activos: filtros.Add(f.Eq(x => x.EstaActivo, true)); break;
            case EstadoFiltro.Inactivos: filtros.Add(f.Eq(x => x.EstaActivo, false)); break;
        }

        var filtro = filtros.Count == 0 ? FilterDefinition<ProveedorDocument>.Empty : f.And(filtros);

        var s = Builders<ProveedorDocument>.Sort;
        var orden = (consulta.OrdenarPor, consulta.Descendente) switch
        {
            (CampoOrden.Numero, false) => s.Ascending(x => x.NumeroEmpresa),
            (CampoOrden.Numero, true) => s.Descending(x => x.NumeroEmpresa),
            (CampoOrden.FechaCreacion, false) => s.Ascending(x => x.FechaCreacion),
            (CampoOrden.FechaCreacion, true) => s.Descending(x => x.FechaCreacion),
            (CampoOrden.FechaActualizacion, false) => s.Ascending(x => x.FechaActualizacion),
            (CampoOrden.FechaActualizacion, true) => s.Descending(x => x.FechaActualizacion),
            (_, true) => s.Descending(x => x.NombreNormalizado),
            _ => s.Ascending(x => x.NombreNormalizado)
        };
        // Desempate estable para que la paginación sea determinista.
        orden = s.Combine(orden, s.Ascending(x => x.Id));

        var total = await coleccion.CountDocumentsAsync(filtro, cancellationToken: cancellationToken);
        List<ProveedorDocument> documentos;
        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
        {
            // Expresiones de infraestructura con operadores fijos: la entrada solo es un literal escapado.
            var termino = Regex.Escape(NombreNormalizador.Normalizar(consulta.Busqueda));
            BsonDocument Coincide(string regex) => new("$regexMatch", new BsonDocument
            {
                { "input", new BsonDocument("$ifNull", new BsonArray { "$nombreNormalizado", "" }) },
                { "regex", regex }
            });
            var prioridad = new BsonDocument("$set", new BsonDocument("__prioridadBusqueda",
                new BsonDocument("$cond", new BsonArray { Coincide("^" + termino), 0,
                    new BsonDocument("$cond", new BsonArray { Coincide(termino), 1, 2 }) })));
            documentos = await coleccion.Aggregate().Match(filtro)
                .AppendStage<ProveedorDocument>(prioridad)
                .Sort(s.Combine(s.Ascending("__prioridadBusqueda"), orden))
                .Skip(consulta.Desplazamiento).Limit(consulta.TamanoPagina)
                .AppendStage<ProveedorDocument>(new BsonDocument("$unset", "__prioridadBusqueda"))
                .ToListAsync(cancellationToken);
        }
        else
        {
            documentos = await coleccion.Find(filtro).Sort(orden)
                .Skip(consulta.Desplazamiento).Limit(consulta.TamanoPagina).ToListAsync(cancellationToken);
        }

        return new PaginaProveedores(documentos.Select(ProveedorDocumentMapper.ToDomain).ToList(), total);
    }

    public async Task<bool> ExisteActivoConNombreAsync(string nombreNormalizado, string? excluirId, CancellationToken cancellationToken)
    {
        var f = Builders<ProveedorDocument>.Filter;
        var filtro = f.And(f.Eq(x => x.NombreNormalizado, nombreNormalizado), f.Eq(x => x.EstaActivo, true));

        if (excluirId is not null && ObjectId.TryParse(excluirId, out var excluido))
            filtro = f.And(filtro, f.Ne(x => x.Id, excluido));

        return await coleccion.Find(filtro).Limit(1).AnyAsync(cancellationToken);
    }

    public async Task<Result<Proveedor>> AgregarAsync(Proveedor proveedor, CancellationToken cancellationToken)
    {
        var id = ObjectId.GenerateNewId();
        var documento = ProveedorDocumentMapper.ToDocument(proveedor, id);
        try
        {
            await coleccion.InsertOneAsync(documento, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Violación ESPERADA del índice único parcial (p. ej. dos solicitudes concurrentes): error de negocio.
            logger.LogInformation("Nombre activo duplicado detectado por el índice único al insertar.");
            return Result.Failure<Proveedor>(ProveedorErrors.NombreDuplicado);
        }

        proveedor.AsignarIdentificador(id.ToString());
        return Result.Success(proveedor);
    }

    public async Task<Result> ActualizarAsync(Proveedor proveedor, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(proveedor.Id, out var objectId))
            return Result.Failure(ProveedorErrors.NoEncontrado);

        var documento = ProveedorDocumentMapper.ToDocument(proveedor, objectId);
        try
        {
            // Se conserva origenSeed si existía: se reemplaza campo a campo con $set en lugar de reemplazar el documento.
            var u = Builders<ProveedorDocument>.Update;
            var actualizacion = u.Combine(
                u.Inc(x => x.Version, 1),
                u.Set(x => x.NombreEmpresa, documento.NombreEmpresa),
                u.Set(x => x.NombreNormalizado, documento.NombreNormalizado),
                u.Set(x => x.NumeroEmpresa, documento.NumeroEmpresa),
                u.Set(x => x.CorreoReferencia, documento.CorreoReferencia),
                u.Set(x => x.EsAutogestionado, documento.EsAutogestionado),
                u.Set(x => x.EstaActivo, documento.EstaActivo),
                u.Set(x => x.IdUsuarioActualizacion, documento.IdUsuarioActualizacion),
                u.Set(x => x.FechaActualizacion, documento.FechaActualizacion));

            var f = Builders<ProveedorDocument>.Filter;
            // Documentos anteriores sin version equivalen a version 0; no requieren migración.
            var version = proveedor.Version == 0
                ? f.Or(f.Eq(x => x.Version, 0), f.Exists(x => x.Version, false))
                : f.Eq(x => x.Version, proveedor.Version);
            // INTEGRACIÓN: Comparación de versión e incremento ocurren en una escritura atómica.
            //   Todo escritor debe respetar este protocolo; una actualización manual que no incremente
            //   version puede eludir la detección de concurrencia. No cambiar esto por ReplaceOne.
            var resultado = await coleccion.UpdateOneAsync(
                f.And(f.Eq(x => x.Id, objectId), version), actualizacion, cancellationToken: cancellationToken);
            if (resultado.MatchedCount == 0)
            {
                var existe = await coleccion.Find(x => x.Id == objectId).AnyAsync(cancellationToken);
                return Result.Failure(existe ? ProveedorErrors.CambioConcurrente : ProveedorErrors.NoEncontrado);
            }
            proveedor.ConfirmarEscritura();
            return Result.Success();
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            logger.LogInformation("Nombre activo duplicado detectado por el índice único al actualizar {Id}.", proveedor.Id);
            return Result.Failure(ProveedorErrors.NombreDuplicado);
        }
    }
}
