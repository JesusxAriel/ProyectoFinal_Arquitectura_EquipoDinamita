using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.In;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Creators;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Errors;
using PuntoMarket.Clientes.Domain.Validaciones;

namespace PuntoMarket.Clientes.Application.UseCases;

public abstract class ClienteUseCase(IClienteRepository repositorio, ICurrentUser usuario)
{
    protected readonly IClienteRepository Repositorio = repositorio;
    protected readonly ICurrentUser Usuario = usuario;
    protected Result ValidarUsuario() => string.IsNullOrWhiteSpace(Usuario.Id)
        ? Result.Failure(ClienteErrors.NoAutenticado) : Result.Success();
    protected static Result<DatosCliente> ValidarDatos(GuardarClienteRequest r) =>
        ClienteValidador.Validar(new DatosCliente(r.NitCi, r.RazonSocial, r.Correo, r.Telefono));
}

public sealed class ListarClientes(IClienteRepository repo) : IListarClientes
{
    public async Task<Result<PaginaResultado<ClienteDto>>> EjecutarAsync(ListarClientesQuery q, CancellationToken ct)
    {
        var consulta = new ConsultaClientes(q.Busqueda, q.Estado, q.OrdenarPorSeguro(), q.Descendente,
            Math.Max(1, q.Pagina), Math.Clamp(q.TamanoPagina, 1, 100));
        var (items, total) = await repo.ListarAsync(consulta, ct);
        return Result.Success(new PaginaResultado<ClienteDto>(items.Select(ClienteDto.Desde).ToList(), consulta.Pagina, consulta.TamanoPagina, total));
    }
}

public sealed class ObtenerCliente(IClienteRepository repo) : IObtenerCliente
{
    public async Task<Result<ClienteDto>> EjecutarAsync(int id, CancellationToken ct)
    {
        var c = await repo.ObtenerAsync(id, ct);
        return c is null ? Result.Failure<ClienteDto>(ClienteErrors.NoEncontrado) : Result.Success(ClienteDto.Desde(c));
    }
}

public sealed class CrearCliente(IClienteRepository repo, ICurrentUser usuario) : ClienteUseCase(repo, usuario), ICrearCliente
{
    public async Task<Result<ClienteDto>> EjecutarAsync(GuardarClienteRequest r, CancellationToken ct)
    {
        var autenticado = ValidarUsuario(); if (autenticado.IsFailure) return Result.Failure<ClienteDto>(autenticado.Error);
        var datos = ValidarDatos(r); if (datos.IsFailure) return Result.Failure<ClienteDto>(datos.Error);
        if (await repo.ExisteNitCiActivoAsync(datos.Value.NitCi, null, ct))
            return Result.Failure<ClienteDto>(ClienteErrors.NitCiDuplicado);
        var ahora = DateTime.UtcNow;
        var cliente = Cliente.Crear(0, datos.Value, true, false, Usuario.Id!, null, ahora, null);
        var resultado = await repo.CrearAsync(cliente, ct);
        return resultado.IsSuccess ? Result.Success(ClienteDto.Desde(resultado.Value)) : Result.Failure<ClienteDto>(resultado.Error);
    }
}

public sealed class ActualizarCliente(IClienteRepository repo, ICurrentUser usuario) : ClienteUseCase(repo, usuario), IActualizarCliente
{
    public async Task<Result<ClienteDto>> EjecutarAsync(int id, GuardarClienteRequest r, CancellationToken ct)
    {
        var autenticado = ValidarUsuario(); if (autenticado.IsFailure) return Result.Failure<ClienteDto>(autenticado.Error);
        var actual = await repo.ObtenerAsync(id, ct);
        if (actual is null) return Result.Failure<ClienteDto>(ClienteErrors.NoEncontrado);
        if (actual.EsGenerico) return Result.Failure<ClienteDto>(ClienteErrors.GenericoProtegido);
        if (!actual.EstaActivo) return Result.Failure<ClienteDto>(ClienteErrors.Inactivo);
        var datos = ValidarDatos(r); if (datos.IsFailure) return Result.Failure<ClienteDto>(datos.Error);
        if (await repo.ExisteNitCiActivoAsync(datos.Value.NitCi, id, ct))
            return Result.Failure<ClienteDto>(ClienteErrors.NitCiDuplicado);
        actual.Actualizar(datos.Value, Usuario.Id!, DateTime.UtcNow);
        var resultado = await repo.ActualizarAsync(actual, ct);
        return resultado.IsSuccess ? Result.Success(ClienteDto.Desde(resultado.Value)) : Result.Failure<ClienteDto>(resultado.Error);
    }
}

public sealed class DesactivarCliente(IClienteRepository repo, ICurrentUser usuario) : ClienteUseCase(repo, usuario), IDesactivarCliente
{
    public async Task<Result> EjecutarAsync(int id, CancellationToken ct)
    {
        var autenticado = ValidarUsuario(); if (autenticado.IsFailure) return autenticado;
        var c = await repo.ObtenerAsync(id, ct);
        if (c is null) return Result.Failure(ClienteErrors.NoEncontrado);
        if (c.EsGenerico) return Result.Failure(ClienteErrors.GenericoProtegido);
        c.Desactivar(Usuario.Id!, DateTime.UtcNow);
        return await repo.DesactivarAsync(c, Usuario.Id!, DateTime.UtcNow, ct);
    }
}

public sealed class ReactivarCliente(IClienteRepository repo, ICurrentUser usuario) : ClienteUseCase(repo, usuario), IReactivarCliente
{
    public async Task<Result<ClienteDto>> EjecutarAsync(int id, CancellationToken ct)
    {
        var autenticado = ValidarUsuario(); if (autenticado.IsFailure) return Result.Failure<ClienteDto>(autenticado.Error);
        var c = await repo.ObtenerAsync(id, ct);
        if (c is null) return Result.Failure<ClienteDto>(ClienteErrors.NoEncontrado);
        if (c.EsGenerico) return Result.Failure<ClienteDto>(ClienteErrors.GenericoProtegido);
        if (await repo.ExisteNitCiActivoAsync(c.Datos.NitCi, id, ct))
            return Result.Failure<ClienteDto>(ClienteErrors.NoReactivar);
        c.Reactivar(Usuario.Id!, DateTime.UtcNow);
        var resultado = await repo.ReactivarAsync(c, Usuario.Id!, DateTime.UtcNow, ct);
        return resultado.IsSuccess ? Result.Success(ClienteDto.Desde(resultado.Value)) : Result.Failure<ClienteDto>(resultado.Error);
    }
}

internal static class ConsultaExtensions
{
    public static string OrdenarPorSeguro(this ListarClientesQuery q) =>
        q.OrdenarPor is "RazonSocial" or "NitCi" or "FechaCreacion" or "FechaActualizacion" ? q.OrdenarPor : "RazonSocial";
}
