using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Validaciones;

namespace PuntoMarket.Clientes.Domain.Creators;

// =====================================================
// PATRÓN DE DISEÑO 2/4: FACTORY METHOD
// ClienteCreator define el algoritmo común (validar y luego instanciar) y delega la
// instanciación concreta en InstanciarCliente, que cada creador derivado implementa.
// El código cliente (casos de uso) depende de la abstracción, no de la clase concreta.
// =====================================================
public abstract class ClienteCreator
{
    /// <summary>Indica si este creador acepta el NIT reservado del cliente genérico.</summary>
    protected abstract bool PermiteNitGenerico { get; }

    public Result<Cliente> Crear(DatosCliente datos, string idUsuario, DateTime ahoraUtc)
    {
        var validado = ClienteValidador.Validar(datos, PermiteNitGenerico);
        if (validado.IsFailure) return Result.Failure<Cliente>(validado.Error);

        return Result.Success(InstanciarCliente(validado.Value, idUsuario, ahoraUtc));
    }

    /// <summary>Factory Method: cada creador concreto decide cómo construir el cliente.</summary>
    protected abstract Cliente InstanciarCliente(DatosCliente datosValidados, string idUsuario, DateTime ahoraUtc);
}
