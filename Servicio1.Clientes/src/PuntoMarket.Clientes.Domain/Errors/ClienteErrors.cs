using PuntoMarket.Clientes.Domain.Common;

namespace PuntoMarket.Clientes.Domain.Errors;

public static class ClienteErrors
{
    public static Error Validacion(IReadOnlyDictionary<string, string[]> fields, string message = "Los datos del cliente no son válidos.") =>
        new("Cliente.DatosInvalidos", message, ErrorType.Validation, fields);
    public static readonly Error NitCiRequerido = new("Cliente.NitCiRequerido", "El NIT/CI es obligatorio.", ErrorType.Validation);
    public static readonly Error NitCiFormato = new("Cliente.NitCiFormato", "El NIT/CI debe tener entre 5 y 15 dígitos.", ErrorType.Validation);
    public static readonly Error NitCiReservado = new("Cliente.NitCiReservado", "El NIT/CI 0 está reservado al cliente genérico.", ErrorType.Validation);
    public static readonly Error NitCiDuplicado = new("Cliente.NitCiDuplicado", "Ya existe un cliente activo con ese NIT/CI.", ErrorType.Conflict);
    public static readonly Error RazonSocialRequerida = new("Cliente.RazonSocialRequerida", "La razón social es obligatoria.", ErrorType.Validation);
    public static readonly Error RazonSocialLongitud = new("Cliente.RazonSocialLongitud", "La razón social no puede superar los 150 caracteres.", ErrorType.Validation);
    public static readonly Error CorreoFormato = new("Cliente.CorreoFormato", "El correo electrónico no tiene un formato válido.", ErrorType.Validation);
    public static readonly Error TelefonoFormato = new("Cliente.TelefonoFormato", "El teléfono debe tener 8 dígitos y comenzar con 4, 6 o 7.", ErrorType.Validation);
    public static readonly Error NoEncontrado = new("Cliente.NoEncontrado", "El cliente no existe.", ErrorType.NotFound);
    public static readonly Error GenericoProtegido = new("Cliente.GenericoProtegido", "El cliente genérico no puede modificarse ni darse de baja.", ErrorType.Conflict);
    public static readonly Error Inactivo = new("Cliente.Inactivo", "No se puede editar un cliente inactivo.", ErrorType.Conflict);
    public static readonly Error NoReactivar = new("Cliente.NitCiDuplicado", "No se puede reactivar porque otro cliente activo usa ese NIT/CI.", ErrorType.Conflict);
    public static readonly Error NoAutenticado = new("Cliente.NoAutenticado", "Se requiere un usuario autenticado.", ErrorType.Unauthorized);
}
