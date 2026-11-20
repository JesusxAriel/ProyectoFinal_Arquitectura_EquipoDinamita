namespace PuntoMarket.Usuarios.Domain.Entities;

public sealed class Rol
{
    private Rol(int id, string nombre, bool activo)
    {
        Id = id;
        Nombre = nombre;
        Activo = activo;
    }

    public int Id { get; }

    public string Nombre { get; }

    public bool Activo { get; }

    public static Rol Reconstruir(int id, string nombre, bool activo) => new(id, nombre, activo);
}
