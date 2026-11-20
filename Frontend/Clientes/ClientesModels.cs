namespace PuntoMarket.Frontend.Clientes;

public sealed record ClienteFrontend(int Id, string NitCi, string RazonSocial, string? Correo, string? Telefono,
    bool EstaActivo, bool EsGenerico, string IdUsuarioCreacion, string? IdUsuarioActualizacion,
    DateTime FechaCreacion, DateTime? FechaActualizacion);

public sealed record GuardarClienteFrontend(string NitCi, string RazonSocial, string? Correo, string? Telefono);

public sealed record PaginaClientes(IReadOnlyList<ClienteFrontend> Items, int Pagina, int TamanoPagina, long Total, int TotalPaginas);

public sealed class ResultadoCliente<T>
{
    private ResultadoCliente(bool exitoso, T? valor, string? codigo, string? mensaje, IReadOnlyDictionary<string, string[]>? errores)
    {
        Exitoso = exitoso; Valor = valor; Codigo = codigo; Mensaje = mensaje; Errores = errores ?? new Dictionary<string, string[]>();
    }
    public bool Exitoso { get; }
    public T? Valor { get; }
    public string? Codigo { get; }
    public string? Mensaje { get; }
    public IReadOnlyDictionary<string, string[]> Errores { get; }
    public static ResultadoCliente<T> Ok(T valor) => new(true, valor, null, null, null);
    public static ResultadoCliente<T> Fallo(string? codigo, string? mensaje, int estado, IReadOnlyDictionary<string, string[]>? errores = null) =>
        new(false, default, codigo, estado == 401 ? "La sesión venció o no está autorizada. Inicie sesión nuevamente."
            : estado == 403 ? "No tiene permisos para realizar esta operación."
            : estado == 503 ? "El servicio de clientes no está disponible temporalmente."
            : mensaje ?? "No se pudo completar la operación.", errores);
}
