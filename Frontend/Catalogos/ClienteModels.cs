using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PuntoMarket.Frontend.Catalogos;

// Presentación DEMO: contratos HTTP propios de Clientes; nunca se referencia su persistencia.
public sealed record ClienteVm(int Id, string NitCi, string RazonSocial, string? Correo, string? Telefono,
    bool EstaActivo, bool EsGenerico, string IdUsuarioCreacion, string? IdUsuarioActualizacion,
    DateTime FechaCreacion, DateTime? FechaActualizacion);
public sealed record PaginaClientesVm(IReadOnlyList<ClienteVm> Items, int Pagina, int TamanoPagina, long Total, int TotalPaginas);
public sealed record ConsultaClientesVm(string? Busqueda, string Estado, string OrdenarPor, bool Descendente, int Pagina, int TamanoPagina);

public sealed class ClienteFormVm : IValidatableObject
{
    public string? NitCi { get; set; }
    private string? razonSocial;
    // El mismo modelo se enlaza en Crear/Editar. Normalizar al asignar garantiza que MVC
    // valide y serialice el valor definitivo, incluso sin JavaScript.
    public string? RazonSocial
    {
        get => razonSocial;
        set => razonSocial = NormalizarRazonSocial(value);
    }
    public static string NormalizarRazonSocial(string? value) =>
        Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ").ToUpperInvariant();
    public string? Correo { get; set; }
    public string? Telefono { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        var nit = NitCi?.Trim() ?? "";
        var razon = RazonSocial ?? "";
        var correo = Correo?.Trim() ?? "";
        var telefono = Telefono?.Trim() ?? "";
        if (!Regex.IsMatch(nit, "^[0-9]{5,15}$"))
            yield return new("El NIT o CI debe tener entre 5 y 15 dígitos. El 0 está reservado a Sin Nombre.", [nameof(NitCi)]);
        if (razon.Length == 0 || razon.Length > 150 || razon.Any(c => char.IsControl(c) || c is '<' or '>'))
            yield return new("La razón social es obligatoria, admite hasta 150 caracteres y no permite HTML ni controles.", [nameof(RazonSocial)]);
        if (correo.Length > 0 && (correo.Length > 150 || !Regex.IsMatch(correo,
            @"^[A-Za-z0-9._%+\-]{1,64}@([A-Za-z0-9]([A-Za-z0-9\-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$")))
            yield return new("El correo electrónico no es válido (máximo 150 caracteres).", [nameof(Correo)]);
        if (telefono.Length > 0 && !Regex.IsMatch(telefono, "^[467][0-9]{7}$"))
            yield return new("El teléfono debe tener 8 dígitos y comenzar con 4, 6 o 7.", [nameof(Telefono)]);
    }
}
