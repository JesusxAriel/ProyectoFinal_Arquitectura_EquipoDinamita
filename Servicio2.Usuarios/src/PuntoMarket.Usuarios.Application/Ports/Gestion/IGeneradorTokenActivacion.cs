namespace PuntoMarket.Usuarios.Application.Ports.Gestion;

public interface IGeneradorTokenActivacion
{
    TokenActivacion Generar(DateTime ahoraUtc, TimeSpan vigencia);

    string CalcularHash(string token);
}
