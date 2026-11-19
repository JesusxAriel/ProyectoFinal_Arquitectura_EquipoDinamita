namespace PuntoMarket.Gateway.Auth;

public enum ClientStatus
{
    Ok,
    Rejected,
    Unavailable
}

public sealed record ClientOutcome<T>(ClientStatus Status, T? Value)
{
    public static ClientOutcome<T> Success(T value) => new(ClientStatus.Ok, value);
    public static ClientOutcome<T> Rejected() => new(ClientStatus.Rejected, default);
    public static ClientOutcome<T> Unavailable() => new(ClientStatus.Unavailable, default);
}
