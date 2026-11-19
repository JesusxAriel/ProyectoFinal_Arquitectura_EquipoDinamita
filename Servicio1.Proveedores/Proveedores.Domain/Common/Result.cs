namespace Proveedores.Domain.Common;

// =====================================================
// PATRÓN DE DISEÑO 1/4: RESULT
// Representa éxito o fallo (con Error) sin usar excepciones para
// validaciones normales de negocio. Las excepciones quedan reservadas
// para fallos técnicos inesperados (red, disco, bugs).
// =====================================================

/// <summary>Resultado de una operación sin valor de retorno.</summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Un resultado exitoso no puede contener un error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Un resultado fallido debe contener un error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>Resultado de una operación que devuelve un valor cuando tiene éxito.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>Valor devuelto. Leerlo en un resultado fallido es un error de programación.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("No se puede leer Value de un resultado fallido. Compruebe IsSuccess primero.");
}
