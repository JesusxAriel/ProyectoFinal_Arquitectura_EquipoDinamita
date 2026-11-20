namespace PuntoMarket.Clientes.Domain.Common;

// PATRÓN DE DISEÑO 1/4: Result
public sealed record Error(string Code, string Message, ErrorType Type, IReadOnlyDictionary<string, string[]>? Fields = null)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
}

public enum ErrorType { None, Validation, NotFound, Conflict, Unauthorized, Unavailable, Failure }

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
    public static Result<T> Success<T>(T value) => new Result<T>(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new Result<T>(default, false, error);
}

public sealed class Result<T> : Result
{
    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;
    private readonly T? _value;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("No se puede leer Value de un resultado fallido.");
}
