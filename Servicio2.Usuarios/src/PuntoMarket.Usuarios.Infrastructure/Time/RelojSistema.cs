using PuntoMarket.Usuarios.Application.Ports.Comunes;

namespace PuntoMarket.Usuarios.Infrastructure.Time;

public sealed class RelojSistema : IReloj
{
    public DateTime Ahora => DateTime.UtcNow;
}
