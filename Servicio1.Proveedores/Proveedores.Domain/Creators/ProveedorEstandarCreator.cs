using Proveedores.Domain.Entities;

namespace Proveedores.Domain.Creators;

/// <summary>Creador concreto: proveedor cuya reposición gestiona el micromercado (esAutogestionado = false).</summary>
public sealed class ProveedorEstandarCreator : ProveedorCreator
{
    protected override Proveedor InstanciarProveedor(DatosProveedorValidados datos, string idUsuario, DateTime ahoraUtc) =>
        new(datos with { EsAutogestionado = false }, idUsuario, ahoraUtc);
}
