using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PuntoMarket.Frontend.Compartido;

[AttributeUsage(AttributeTargets.Property)]
public sealed class PoliticaPasswordAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool RequiresValidationContext => false;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string texto && string.IsNullOrWhiteSpace(texto))
            return ValidationResult.Success;

        var error = ValidadorPassword.ValidarNueva(value as string);
        return error is null || error == MensajesPassword.NuevaObligatoria
            ? ValidationResult.Success
            : new ValidationResult(error);
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        Agregar(context, "data-val", "true");
        Agregar(context, "data-val-longitudminima", MensajesPassword.NuevaCorta);
        Agregar(context, "data-val-longitudminima-min", MensajesPassword.LongitudMinima.ToString());
        Agregar(context, "data-val-mayuscula", MensajesPassword.NuevaSinMayuscula);
        Agregar(context, "data-val-minuscula", MensajesPassword.NuevaSinMinuscula);
        Agregar(context, "data-val-numero", MensajesPassword.NuevaSinNumero);
        Agregar(context, "data-val-especial", MensajesPassword.NuevaSinEspecial);
    }

    private static void Agregar(ClientModelValidationContext context, string clave, string valor) =>
        context.Attributes.TryAdd(clave, valor);
}
