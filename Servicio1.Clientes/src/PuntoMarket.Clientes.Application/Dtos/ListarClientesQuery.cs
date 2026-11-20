using PuntoMarket.Clientes.Domain.Common;

namespace PuntoMarket.Clientes.Application.Dtos;

public enum EstadoFiltro { Todos = 0, Activos = 1, Inactivos = 2 }

public enum CampoOrden { RazonSocial = 0, NitCi = 1, FechaCreacion = 2, FechaActualizacion = 3 }

/// <summary>Parámetros de GET /api/clientes: búsqueda, filtro de estado, ordenación y paginación.</summary>
public sealed class ListarClientesQuery
{
    public const int TamanoPaginaMaximo = 100;
    public const int BusquedaMaxLength = 150;

    public string? Busqueda { get; set; }
    public EstadoFiltro Estado { get; set; } = EstadoFiltro.Todos;
    public CampoOrden OrdenarPor { get; set; } = CampoOrden.RazonSocial;
    public bool Descendente { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 10;

    public int Desplazamiento => checked((Pagina - 1) * TamanoPagina);

    public Result Validar()
    {
        var busqueda = Busqueda?.Trim() ?? string.Empty;
        if (busqueda.Length > BusquedaMaxLength || busqueda.Any(char.IsControl))
            return Result.Failure(Error.Validation("Consulta.BusquedaInvalida",
                $"La búsqueda admite hasta {BusquedaMaxLength} caracteres y no permite caracteres de control."));

        if (!Enum.IsDefined(Estado) || !Enum.IsDefined(OrdenarPor))
            return Result.Failure(Error.Validation("Consulta.FiltroInvalido",
                "El estado o el criterio de ordenación no es válido."));

        return Result.Success();
    }

    /// <summary>Limita página y tamaño para que el desplazamiento quepa en un entero de 32 bits.</summary>
    public ListarClientesQuery Normalizada()
    {
        var tamano = Math.Clamp(TamanoPagina, 1, TamanoPaginaMaximo);
        var paginaMaxima = (int)Math.Min(int.MaxValue, (long)int.MaxValue / tamano + 1);
        return new ListarClientesQuery
        {
            Busqueda = string.IsNullOrWhiteSpace(Busqueda) ? null : Busqueda.Trim(),
            Estado = Estado,
            OrdenarPor = OrdenarPor,
            Descendente = Descendente,
            Pagina = Math.Clamp(Pagina, 1, paginaMaxima),
            TamanoPagina = tamano
        };
    }
}
