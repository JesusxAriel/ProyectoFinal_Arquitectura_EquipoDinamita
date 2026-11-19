using Proveedores.Domain.Common;

namespace Proveedores.Domain.Errors;

/// <summary>Catálogo central de errores de negocio de Proveedores (código estable + mensaje en español).</summary>
public static class ProveedorErrors
{
    public static readonly Error NombreInvalido = Error.Validation("Proveedor.NombreInvalido",
        "El nombre debe contener letras o números y puntuación comercial; no admite HTML, código ni caracteres de control o invisibles.");
    public static readonly Error NumeroInvalido = Error.Validation("Proveedor.NumeroInvalido",
        "El teléfono debe tener 8 dígitos y comenzar por 4, 6 o 7, sin prefijos ni espacios interiores.");
    public static readonly Error CambioConcurrente = Error.Conflict(
        "Proveedor.CambioConcurrente", "El proveedor fue modificado por otra solicitud. Actualice los datos y vuelva a intentar.");

    public static readonly Error NombreRequerido =
        Error.Validation("Proveedor.NombreRequerido", "El nombre de la empresa es obligatorio.");

    public static readonly Error NombreMuyLargo =
        Error.Validation("Proveedor.NombreMuyLargo", "El nombre de la empresa no puede superar los 150 caracteres.");

    public static readonly Error NumeroRequerido =
        Error.Validation("Proveedor.NumeroRequerido", "El número de la empresa es obligatorio.");

    public static readonly Error NumeroMuyLargo =
        Error.Validation("Proveedor.NumeroMuyLargo", "El número de la empresa no puede superar los 20 caracteres.");

    public static readonly Error CorreoInvalido =
        Error.Validation("Proveedor.CorreoInvalido", "El correo de referencia no tiene un formato válido.");

    public static readonly Error CorreoMuyLargo =
        Error.Validation("Proveedor.CorreoMuyLargo", "El correo de referencia no puede superar los 150 caracteres.");

    public static readonly Error NombreDuplicado =
        Error.Conflict("Proveedor.NombreDuplicado", "Ya existe un proveedor activo con ese nombre de empresa.");

    public static readonly Error NoEncontrado =
        Error.NotFound("Proveedor.NoEncontrado", "El proveedor solicitado no existe.");

    public static readonly Error YaDesactivado =
        Error.Conflict("Proveedor.YaDesactivado", "El proveedor ya se encuentra desactivado.");

    public static readonly Error YaActivo =
        Error.Conflict("Proveedor.YaActivo", "El proveedor ya se encuentra activo.");

    public static readonly Error EdicionDeInactivo =
        Error.Conflict("Proveedor.EdicionDeInactivo", "No se puede modificar un proveedor desactivado. Reactívelo primero.");

    public static readonly Error ReactivacionConflictiva =
        Error.Conflict("Proveedor.ReactivacionConflictiva", "No se puede reactivar: ya existe otro proveedor activo con el mismo nombre de empresa.");

    public static readonly Error UsuarioNoIdentificado =
        Error.Unauthorized("Proveedor.UsuarioNoIdentificado", "No se pudo identificar al usuario autenticado (falta el identificador del token).");
}
