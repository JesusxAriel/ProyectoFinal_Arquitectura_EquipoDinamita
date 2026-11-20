using PuntoMarket.Clientes.Application.Dtos;
using PuntoMarket.Clientes.Application.Ports.Out;
using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Creators;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Errors;

namespace PuntoMarket.Clientes.Tests;

internal static class Datos
{
    public static readonly DateTime Ahora = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

    public static DatosCliente Valido(string nitCi = "4545451226", string razon = "Juan Pérez") =>
        new(nitCi, razon, "jperez@gmail.com", "77123456");

    public static Cliente NuevoCliente(string nitCi = "4545451226", string razon = "Juan Pérez") =>
        ClienteCreatorSelector.Para(TipoCliente.Estandar).Crear(Valido(nitCi, razon), "usuario-1", Ahora).Value;

    public static Cliente Generico() =>
        ClienteCreatorSelector.Para(TipoCliente.Generico).Crear(ClienteGenericoCreator.DatosPorDefecto, "sistema", Ahora).Value;
}

internal sealed class UsuarioFalso(string? userId = "usuario-1") : ICurrentUser
{
    public string? UserId { get; } = userId;
}

/// <summary>Repositorio en memoria que imita las reglas del adaptador MySQL (unicidad del NIT/CI entre activos).</summary>
internal sealed class RepositorioEnMemoria : IClienteRepository
{
    private readonly List<Cliente> _clientes = [];
    private int _siguienteId = 1;

    public IReadOnlyList<Cliente> Todos => _clientes;

    public Cliente Sembrar(Cliente cliente)
    {
        cliente.AsignarId(_siguienteId++);
        _clientes.Add(cliente);
        return cliente;
    }

    public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(_clientes.FirstOrDefault(c => c.Id == id));

    public Task<PaginaClientes> ListarAsync(ListarClientesQuery consulta, CancellationToken cancellationToken)
    {
        IEnumerable<Cliente> q = _clientes;
        if (consulta.Estado == EstadoFiltro.Activos) q = q.Where(c => c.EstaActivo);
        if (consulta.Estado == EstadoFiltro.Inactivos) q = q.Where(c => !c.EstaActivo);
        if (!string.IsNullOrWhiteSpace(consulta.Busqueda))
            q = q.Where(c => c.RazonSocial.Contains(consulta.Busqueda, StringComparison.OrdinalIgnoreCase)
                             || c.NitCi.Contains(consulta.Busqueda, StringComparison.OrdinalIgnoreCase));

        var lista = q.OrderBy(c => c.RazonSocial).ToList();
        var pagina = lista.Skip(consulta.Desplazamiento).Take(consulta.TamanoPagina).ToList();
        return Task.FromResult(new PaginaClientes(pagina, lista.Count));
    }

    public Task<bool> ExisteActivoConNitCiAsync(string nitCi, int? excluirId, CancellationToken cancellationToken) =>
        Task.FromResult(_clientes.Any(c => c.EstaActivo && c.NitCi == nitCi && c.Id != excluirId));

    public Task<Result<Cliente>> AgregarAsync(Cliente cliente, CancellationToken cancellationToken)
    {
        if (_clientes.Any(c => c.EstaActivo && c.NitCi == cliente.NitCi))
            return Task.FromResult(Result.Failure<Cliente>(ClienteErrors.NitCiDuplicado));
        return Task.FromResult(Result.Success(Sembrar(cliente)));
    }

    public Task<Result> ActualizarAsync(Cliente cliente, CancellationToken cancellationToken) =>
        Task.FromResult(_clientes.Contains(cliente) ? Result.Success() : Result.Failure(ClienteErrors.NoEncontrado(cliente.Id)));
}
