using PuntoMarket.Usuarios.Application.Ports.Gestion;
using PuntoMarket.Usuarios.Domain.Services;

namespace PuntoMarket.Usuarios.Infrastructure.Security;

public sealed class GeneradorPasswordSeguro : IGeneradorPassword
{
    public string Generar() => GeneradorPasswordTemporal.Generar();
}
