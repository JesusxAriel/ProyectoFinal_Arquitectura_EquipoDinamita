using Proveedores.Domain.Entities;

namespace Proveedores.Domain.Creators;

/// <summary>Creador concreto: proveedor que gestiona su propia reposición (esAutogestionado = true).</summary>
public sealed class ProveedorAutogestionadoCreator : ProveedorCreator
{
    protected override Proveedor InstanciarProveedor(DatosProveedorValidados datos, string idUsuario, DateTime ahoraUtc) =>
        new(datos with { EsAutogestionado = true }, idUsuario, ahoraUtc);
}
