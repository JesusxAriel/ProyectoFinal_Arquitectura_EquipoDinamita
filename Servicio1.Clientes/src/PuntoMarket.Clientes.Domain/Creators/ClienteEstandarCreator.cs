using PuntoMarket.Clientes.Domain.Entities;

namespace PuntoMarket.Clientes.Domain.Creators;

/// <summary>Crea clientes normales. Rechaza el NIT reservado "0".</summary>
public sealed class ClienteEstandarCreator : ClienteCreator
{
    protected override bool PermiteNitGenerico => false;

    protected override Cliente InstanciarCliente(DatosCliente datosValidados, string idUsuario, DateTime ahoraUtc) =>
        Cliente.Nuevo(datosValidados, idUsuario, ahoraUtc);
}
