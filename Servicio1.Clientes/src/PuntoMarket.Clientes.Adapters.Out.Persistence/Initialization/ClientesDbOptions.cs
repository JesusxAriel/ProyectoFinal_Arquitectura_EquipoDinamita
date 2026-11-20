namespace PuntoMarket.Clientes.Adapters.Out.Persistence.Initialization;

public sealed class ClientesDbOptions
{
    public const string SectionName = "Clientes";

    /// <summary>Crea la tabla si no existe al iniciar. Poner false si la base la administra otro proceso.</summary>
    public bool InitializeOnStartup { get; set; } = true;

    /// <summary>Inserta el cliente genérico y dos clientes de ejemplo SOLO si la tabla está vacía.</summary>
    public bool SeedEnabled { get; set; } = true;

    /// <summary>Incluye el genérico en la semilla de una tabla vacía. No modifica registros existentes.</summary>
    public bool SeedGenericoEnabled { get; set; } = true;
}
