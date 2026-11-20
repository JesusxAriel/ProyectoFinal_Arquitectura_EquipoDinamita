namespace PuntoMarket.Clientes.Domain.Creators;

public enum TipoCliente
{
    Estandar = 0,
    Generico = 1
}

/// <summary>Elige el creador concreto según el tipo de cliente. Un tipo nuevo exige un creador nuevo y revisar este selector.</summary>
public static class ClienteCreatorSelector
{
    private static readonly ClienteEstandarCreator Estandar = new();
    private static readonly ClienteGenericoCreator Generico = new();

    public static ClienteCreator Para(TipoCliente tipo) => tipo switch
    {
        TipoCliente.Generico => Generico,
        _ => Estandar
    };
}
