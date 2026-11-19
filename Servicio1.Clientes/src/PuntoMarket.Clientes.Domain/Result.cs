namespace PuntoMarket.Clientes.Domain;

public class Result
{
    public bool IsOk { get; }
    public bool IsFail => !IsOk;
    public string Error { get; }
    protected Result(bool ok, string error) { IsOk = ok; Error = error; }
    public static Result Ok() => new(true, "");
    public static Result Fail(string error) => new(false, error);
}

public class Result<T> : Result
{
    public T Value { get; }
    private Result(bool ok, T value, string error) : base(ok, error) { Value = value; }
    public static Result<T> Ok(T value) => new(true, value, "");
    public static new Result<T> Fail(string error) => new(false, default!, error);
}
