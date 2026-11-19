using Proveedores.Domain.Common;
using Proveedores.Domain.Entities;

namespace Proveedores.Domain.Creators;

// =====================================================
// PATRÓN DE DISEÑO 3/4: FACTORY METHOD (GoF)
//
// Participantes:
//   Creator           -> ProveedorCreator (esta clase abstracta)
//   Factory Method    -> InstanciarProveedor(...) (abstracto: lo redefine cada subclase)
//   ConcreteCreator   -> ProveedorEstandarCreator, ProveedorAutogestionadoCreator
//   Product           -> Proveedor
//
// Por qué: la creación de un proveedor tiene un algoritmo común (validar, normalizar,
// registrar auditoría de creación) y un paso variable (qué tipo de proveedor se
// instancia). La plantilla Crear() fija el algoritmo y delega la instanciación en el
// método fábrica. Así, un nuevo tipo de proveedor (p. ej. "Internacional") se agrega
// con una subclase, sin modificar Crear() ni los casos de uso (Open/Closed).
// Se combina con Result: si los datos son inválidos devuelve Failure y NO se instancia nada.
// =====================================================
public abstract class ProveedorCreator
{
    /// <summary>Método plantilla: valida y, solo si es válido, invoca al método fábrica.</summary>
    public Result<Proveedor> Crear(DatosProveedor datos, string idUsuario, DateTime ahoraUtc)
    {
        var validacion = ProveedorValidador.Validar(datos);
        if (validacion.IsFailure)
            return Result.Failure<Proveedor>(validacion.Error);

        var proveedor = InstanciarProveedor(validacion.Value, idUsuario, ahoraUtc);
        return Result.Success(proveedor);
    }

    /// <summary>MÉTODO FÁBRICA: cada creador concreto decide cómo instanciar el producto.</summary>
    protected abstract Proveedor InstanciarProveedor(
        DatosProveedorValidados datos, string idUsuario, DateTime ahoraUtc);
}
