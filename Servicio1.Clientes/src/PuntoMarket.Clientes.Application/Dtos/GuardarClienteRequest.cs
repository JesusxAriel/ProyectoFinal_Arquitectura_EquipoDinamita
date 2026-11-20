namespace PuntoMarket.Clientes.Application.Dtos;

/// <summary>Cuerpo de POST y PUT. No incluye id, estado ni auditoría: los fija el servidor.</summary>
public sealed class GuardarClienteRequest
{
    public string? NitCi { get; set; }
    public string? RazonSocial { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
}
