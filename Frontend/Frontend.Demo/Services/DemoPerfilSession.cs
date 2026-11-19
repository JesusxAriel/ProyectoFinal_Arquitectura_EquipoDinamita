namespace Frontend.Demo.Services;

// =====================================================
// DEMO TEMPORAL - SELECTOR DE PERFIL
// REEMPLAZAR EN FRONTEND DEFINITIVO
// INTEGRACIÓN GRUPO 2
//
// QUÉ HACE: recuerda (en sesión del servidor) qué identidad de demostración usa el selector visual.
//   Solo acepta los valores "Administrador" y "Cajero"; cualquier otro valor cae en "Cajero" (mínimo privilegio).
//   El perfil NO concede permisos por sí mismo: determina qué token pide el frontend a la identidad de
//   desarrollo, y es la API la que valida la firma y el rol de ese token.
// QUÉ DEBE HACER EL GRUPO 2: eliminar esta clase y usar el usuario autenticado real.
// =====================================================
public sealed class DemoPerfilSession(IHttpContextAccessor accesor)
{
    public const string Administrador = "Administrador";
    public const string Cajero = "Cajero";
    private const string Clave = "demo_perfil";

    public string Perfil
    {
        get
        {
            var valor = accesor.HttpContext?.Session.GetString(Clave);
            return valor == Administrador ? Administrador : Cajero;
        }
        set
        {
            var seguro = value == Administrador ? Administrador : Cajero;
            accesor.HttpContext?.Session.SetString(Clave, seguro);
        }
    }

    public bool TienePerfil => accesor.HttpContext?.Session.GetString(Clave) is Administrador or Cajero;
    public void CerrarSesion() => accesor.HttpContext?.Session.Remove(Clave);
    public bool EsAdministrador => TienePerfil && Perfil == Administrador;
}
