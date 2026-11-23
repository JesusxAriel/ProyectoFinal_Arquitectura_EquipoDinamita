namespace PuntoMarket.Productos.Domain;

public enum ErrorType { None, Validation, NotFound, Conflict, Unauthorized, Unavailable }
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public IReadOnlyDictionary<string, string[]>? Fields { get; init; }
    public static readonly Error None = new("", "", ErrorType.None);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Unavailable(string code, string message) => new(code, message, ErrorType.Unavailable);
}
public class Result
{
    protected Result(bool success, Error error)
    {
        if (success == (error != Error.None)) throw new InvalidOperationException("Estado de Result inconsistente.");
        IsSuccess = success; Error = error;
    }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public bool IsOk => IsSuccess;
    public bool IsFail => IsFailure;
    public Error Error { get; }
    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}
public sealed class Result<T> : Result
{
    private readonly T? value;
    internal Result(T? value, bool success, Error error) : base(success, error) { this.value = value; }
    public T Value => IsSuccess ? value! : throw new InvalidOperationException("Compruebe IsSuccess antes de leer Value.");
}
