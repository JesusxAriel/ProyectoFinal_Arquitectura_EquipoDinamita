namespace PuntoMarket.Clientes.Domain.Entities;

public sealed record DatosCliente(string NitCi, string RazonSocial, string? Correo, string? Telefono);

public sealed class Cliente
{
    private Cliente(int id, DatosCliente datos, bool estaActivo, bool esGenerico, string idUsuarioCreacion,
        string? idUsuarioActualizacion, DateTime fechaCreacion, DateTime? fechaActualizacion)
    {
        Id = id;
        Datos = datos;
        EstaActivo = estaActivo;
        EsGenerico = esGenerico;
        IdUsuarioCreacion = idUsuarioCreacion;
        IdUsuarioActualizacion = idUsuarioActualizacion;
        FechaCreacion = fechaCreacion;
        FechaActualizacion = fechaActualizacion;
    }

    public int Id { get; }
    public DatosCliente Datos { get; private set; }
    public bool EstaActivo { get; private set; }
    public bool EsGenerico { get; }
    public string IdUsuarioCreacion { get; }
    public string? IdUsuarioActualizacion { get; private set; }
    public DateTime FechaCreacion { get; }
    public DateTime? FechaActualizacion { get; private set; }

    public static Cliente Crear(int id, DatosCliente datos, bool estaActivo, bool esGenerico, string usuario,
        string? usuarioActualizacion, DateTime fechaCreacion, DateTime? fechaActualizacion) =>
        new(id, datos, estaActivo, esGenerico, usuario, usuarioActualizacion, fechaCreacion, fechaActualizacion);

    public void Actualizar(DatosCliente datos, string usuario, DateTime fecha)
    {
        if (EsGenerico) throw new InvalidOperationException("El cliente genérico está protegido.");
        if (!EstaActivo) throw new InvalidOperationException("No se puede editar un cliente inactivo.");
        Datos = datos;
        IdUsuarioActualizacion = usuario;
        FechaActualizacion = fecha;
    }

    public void Desactivar(string usuario, DateTime fecha)
    {
        if (EsGenerico) throw new InvalidOperationException("El cliente genérico está protegido.");
        EstaActivo = false;
        IdUsuarioActualizacion = usuario;
        FechaActualizacion = fecha;
    }

    public void Reactivar(string usuario, DateTime fecha)
    {
        if (EsGenerico) throw new InvalidOperationException("El cliente genérico está protegido.");
        EstaActivo = true;
        IdUsuarioActualizacion = usuario;
        FechaActualizacion = fecha;
    }
}
