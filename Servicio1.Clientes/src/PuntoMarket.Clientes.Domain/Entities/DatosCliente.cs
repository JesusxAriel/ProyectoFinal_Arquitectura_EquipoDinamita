namespace PuntoMarket.Clientes.Domain.Entities;

/// <summary>Datos comerciales de un cliente tal como llegan (o ya limpios, tras validar).</summary>
public sealed record DatosCliente(string NitCi, string RazonSocial, string? Correo, string? Telefono);
