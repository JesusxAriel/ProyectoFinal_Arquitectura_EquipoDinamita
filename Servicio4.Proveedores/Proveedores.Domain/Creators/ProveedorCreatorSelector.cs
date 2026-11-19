namespace Proveedores.Domain.Creators;

/// <summary>Elige el creador concreto según el tipo de proveedor solicitado. Los creadores no tienen estado.</summary>
public static class ProveedorCreatorSelector
{
    private static readonly ProveedorCreator Estandar = new ProveedorEstandarCreator();
    private static readonly ProveedorCreator Autogestionado = new ProveedorAutogestionadoCreator();

    public static ProveedorCreator Para(bool esAutogestionado) =>
        esAutogestionado ? Autogestionado : Estandar;
}
