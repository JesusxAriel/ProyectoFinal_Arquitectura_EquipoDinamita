using Microsoft.Extensions.Configuration;

namespace PuntoMarket.Usuarios.Infrastructure.Activacion;

public sealed class ActivacionOpciones
{
    public const string Seccion = "Activacion";
    public const int VigenciaPredeterminadaMinutos = 60;
    public const int VigenciaMaximaMinutos = 10080;

    public string BaseUrl { get; set; } = string.Empty;

    public int VigenciaMinutos { get; set; } = VigenciaPredeterminadaMinutos;

    public TimeSpan Vigencia => TimeSpan.FromMinutes(VigenciaMinutos);

    public static ActivacionOpciones Desde(IConfiguration configuracion)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        var seccion = configuracion.GetSection(Seccion);

        var vigenciaValida = int.TryParse(seccion[nameof(VigenciaMinutos)], out var minutos)
            && minutos is >= 1 and <= VigenciaMaximaMinutos;

        return new ActivacionOpciones
        {
            BaseUrl = seccion[nameof(BaseUrl)]?.Trim() ?? string.Empty,
            VigenciaMinutos = vigenciaValida ? minutos : VigenciaPredeterminadaMinutos
        };
    }
}
