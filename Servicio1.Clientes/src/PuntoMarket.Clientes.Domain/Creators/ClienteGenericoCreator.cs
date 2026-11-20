using PuntoMarket.Clientes.Domain.Entities;

namespace PuntoMarket.Clientes.Domain.Creators;

/// <summary>
/// Crea el cliente genérico ("Sin Nombre") con el NIT reservado "0". Se usa solo al preparar la base de datos;
/// la API pública nunca lo utiliza, por lo que ningún usuario puede crear otro cliente genérico.
/// </summary>
public sealed class ClienteGenericoCreator : ClienteCreator
{
    public static readonly DatosCliente DatosPorDefecto =
        new(Cliente.NitCiGenerico, "Sin Nombre / Cliente Genérico", null, null);

    protected override bool PermiteNitGenerico => true;

    protected override Cliente InstanciarCliente(DatosCliente datosValidados, string idUsuario, DateTime ahoraUtc) =>
        Cliente.Nuevo(datosValidados with { NitCi = Cliente.NitCiGenerico }, idUsuario, ahoraUtc);
}
