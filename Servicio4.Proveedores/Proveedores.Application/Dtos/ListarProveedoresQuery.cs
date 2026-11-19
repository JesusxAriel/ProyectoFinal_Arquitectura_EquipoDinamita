using Proveedores.Domain.Common;
using Proveedores.Domain.Entities;

namespace Proveedores.Application.Dtos;

public enum EstadoFiltro { Todos = 0, Activos = 1, Inactivos = 2 }

public enum CampoOrden { Nombre = 0, Numero = 1, FechaCreacion = 2, FechaActualizacion = 3 }

/// <summary>Parámetros de consulta de GET /api/proveedores (búsqueda, filtro, orden y paginación).</summary>
public sealed class ListarProveedoresQuery
{
    public const int TamanoPaginaMaximo = 100;
    public const int BusquedaMaxLength = 150;

    public Result Validar()
    {
        if (TextoSeguro.ContieneCaracteresProhibidos(Busqueda) || (Busqueda?.Trim().Length ?? 0) > BusquedaMaxLength)
            return Result.Failure(Error.Validation("Consulta.BusquedaInvalida", "La búsqueda admite hasta 150 caracteres y no permite caracteres de control o invisibles."));
        if (!Enum.IsDefined(Estado) || !Enum.IsDefined(OrdenarPor))
            return Result.Failure(Error.Validation("Consulta.FiltroInvalido", "El estado o criterio de ordenación no es válido."));
        return Result.Success();
    }

    public string? Busqueda { get; set; }
    public EstadoFiltro Estado { get; set; } = EstadoFiltro.Todos;
    public CampoOrden OrdenarPor { get; set; } = CampoOrden.Nombre;
    public bool Descendente { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 10;

    public int Desplazamiento => checked((Pagina - 1) * TamanoPagina);

    /// <summary>Limita página y tamaño para que el desplazamiento quepa en un entero de 32 bits.</summary>
    public ListarProveedoresQuery Normalizada()
    {
        var tamano = Math.Clamp(TamanoPagina, 1, TamanoPaginaMaximo);
        var paginaMaxima = (int)Math.Min(int.MaxValue, (long)int.MaxValue / tamano + 1);
        return new()
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
