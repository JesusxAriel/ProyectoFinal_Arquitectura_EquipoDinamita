using PuntoMarket.Clientes.Domain.Common;
using PuntoMarket.Clientes.Domain.Entities;
using PuntoMarket.Clientes.Domain.Errors;
using PuntoMarket.Clientes.Domain.Validaciones;

namespace PuntoMarket.Clientes.Domain.Creators;

// PATRÓN DE DISEÑO 2/4: Factory Method
public abstract class ClienteCreator
{
    public Result<DatosCliente> Crear(DatosCliente datos)
    {
        var validacion = ClienteValidador.Validar(datos);
        if (validacion.IsFailure) return validacion;
        return InstanciarCliente(validacion.Value);
    }
    protected abstract Result<DatosCliente> InstanciarCliente(DatosCliente datos);
}

public sealed class ClienteEstandarCreator : ClienteCreator
{
    protected override Result<DatosCliente> InstanciarCliente(DatosCliente datos) =>
        datos.NitCi == "0" ? Result.Failure<DatosCliente>(ClienteErrors.NitCiReservado) : Result.Success(datos);
}

public sealed class ClienteGenericoCreator : ClienteCreator
{
    protected override Result<DatosCliente> InstanciarCliente(DatosCliente datos) =>
        Result.Success(new DatosCliente("0", "Sin Nombre", null, null));
}

public enum TipoCliente { Estandar, Generico }

public static class ClienteCreatorSelector
{
    public static ClienteCreator Para(TipoCliente tipo) => tipo switch
    {
        TipoCliente.Generico => new ClienteGenericoCreator(),
        _ => new ClienteEstandarCreator()
    };
}
