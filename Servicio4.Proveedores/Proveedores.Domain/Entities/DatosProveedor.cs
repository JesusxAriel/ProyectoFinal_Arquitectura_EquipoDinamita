namespace Proveedores.Domain.Entities;

/// <summary>Datos de entrada (sin validar) para crear o modificar un proveedor.</summary>
public sealed record DatosProveedor(
    string? NombreEmpresa,
    string? NumeroEmpresa,
    string? CorreoReferencia,
    bool EsAutogestionado);

/// <summary>Datos ya validados y normalizados. Solo los produce <see cref="ProveedorValidador"/>.</summary>
public sealed record DatosProveedorValidados(
    string NombreEmpresa,
    string NumeroEmpresa,
    string? CorreoReferencia,
    bool EsAutogestionado)
{
    public string NombreNormalizado => NombreNormalizador.Normalizar(NombreEmpresa);
}
