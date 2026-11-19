namespace Proveedores.Application.Dtos;

/// <summary>Cuerpo de POST /api/proveedores y PUT /api/proveedores/{id}. La validación real la hace el dominio.</summary>
public sealed record GuardarProveedorRequest(
    string? NombreEmpresa,
    string? NumeroEmpresa,
    string? CorreoReferencia,
    bool EsAutogestionado);
