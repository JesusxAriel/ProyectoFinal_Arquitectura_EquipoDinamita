using System.Net;
using System.Text;
using PuntoMarket.Usuarios.Infrastructure.Identity;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Keycloak;

public sealed class KeycloakGestorDeIdentidadTests
{
    private const string TokenAdminJson = """{"access_token":"token-admin"}""";
    private const string RolJson = """{"id":"rol-1","name":"Cajero"}""";
    private const string UsuariosJson = """[{"id":"usuario-1","username":"jperez"}]""";

    private sealed record Llamada(HttpMethod Metodo, string Ruta, string Cuerpo);

    private sealed class ManejadorGuionado(Func<Llamada, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<Llamada> Llamadas { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage solicitud, CancellationToken cancellationToken)
        {
            var cuerpo = solicitud.Content is null ? "" : await solicitud.Content.ReadAsStringAsync(cancellationToken);
            var llamada = new Llamada(solicitud.Method, solicitud.RequestUri!.PathAndQuery, cuerpo);
            Llamadas.Add(llamada);
            return responder(llamada);
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode estado, string json = "") =>
        new(estado) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static KeycloakOpciones Opciones() => new()
    {
        BaseUrl = "http://keycloak.test:8080/",
        Realm = "puntomarket",
        AdminUserName = "admin-de-prueba",
        AdminPassword = "clave-de-prueba",
        LoginClientId = "cliente-web"
    };

    private static KeycloakGestorDeIdentidad Crear(ManejadorGuionado manejador) =>
        new(new HttpClient(manejador), new Lazy<KeycloakOpciones>(Opciones));

    private static HttpResponseMessage RutasHabituales(Llamada llamada)
    {
        var ruta = llamada.Ruta;
        if (ruta.StartsWith("/realms/master/protocol/openid-connect/token")) return Json(HttpStatusCode.OK, TokenAdminJson);
        if (ruta.Contains("/roles/")) return Json(HttpStatusCode.OK, RolJson);
        if (ruta.Contains("/users?username=")) return Json(HttpStatusCode.OK, UsuariosJson);
        if (llamada.Metodo == HttpMethod.Post && ruta.EndsWith("/users"))
        {
            var respuesta = Json(HttpStatusCode.Created);
            respuesta.Headers.Location = new Uri("http://keycloak.test:8080/admin/realms/puntomarket/users/usuario-1");
            return respuesta;
        }

        return Json(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CrearUsuario_Correcto_CreaConPasswordDefinitivaYAsignaElRol()
    {
        var manejador = new ManejadorGuionado(RutasHabituales);

        var resultado = await Crear(manejador).CrearUsuario("jperez", "j@correo.com", "Juan Pérez Rojas", "Cajero", "Temporal#1");

        Assert.True(resultado.IsOk, resultado.Error);
        var creacion = Assert.Single(manejador.Llamadas, l => l.Metodo == HttpMethod.Post && l.Ruta.EndsWith("/users"));
        Assert.Contains("\"username\":\"jperez\"", creacion.Cuerpo);
        Assert.Contains("\"firstName\":\"Juan\"", creacion.Cuerpo);
        Assert.Contains("rez Rojas\"", creacion.Cuerpo);
        Assert.Contains("\"temporary\":false", creacion.Cuerpo);
        var asignacion = Assert.Single(manejador.Llamadas, l => l.Ruta.EndsWith("/users/usuario-1/role-mappings/realm"));
        Assert.Contains("\"name\":\"Cajero\"", asignacion.Cuerpo);
    }

    [Fact]
    public async Task CrearUsuario_ObtieneElTokenDeAdministradorConAdminCli()
    {
        var manejador = new ManejadorGuionado(RutasHabituales);

        await Crear(manejador).CrearUsuario("jperez", "j@correo.com", "Juan Pérez", "Cajero", "Temporal#1");

        var tokenAdmin = manejador.Llamadas.First();
        Assert.Equal("/realms/master/protocol/openid-connect/token", tokenAdmin.Ruta);
        Assert.Contains("client_id=admin-cli", tokenAdmin.Cuerpo);
        Assert.Contains("grant_type=password", tokenAdmin.Cuerpo);
    }

    [Fact]
    public async Task CrearUsuario_NombreDeUnaSolaPalabra_UsaLaMismaPalabraComoApellido()
    {
        var manejador = new ManejadorGuionado(RutasHabituales);

        await Crear(manejador).CrearUsuario("madonna", "m@correo.com", "Madonna", "Cajero", "Temporal#1");

        var creacion = manejador.Llamadas.Single(l => l.Metodo == HttpMethod.Post && l.Ruta.EndsWith("/users"));
        Assert.Contains("\"lastName\":\"Madonna\"", creacion.Cuerpo);
    }

    [Fact]
    public async Task CrearUsuario_UsuarioExistente_DevuelveFailSinAsignarRol()
    {
        var manejador = new ManejadorGuionado(llamada =>
            llamada.Metodo == HttpMethod.Post && llamada.Ruta.EndsWith("/users")
                ? Json(HttpStatusCode.Conflict)
                : RutasHabituales(llamada));

        var resultado = await Crear(manejador).CrearUsuario("jperez", "j@correo.com", "Juan Pérez", "Cajero", "Temporal#1");

        Assert.True(resultado.IsFail);
        Assert.Equal(KeycloakMensajes.UsuarioExistente, resultado.Error);
        Assert.DoesNotContain(manejador.Llamadas, l => l.Ruta.Contains("role-mappings"));
    }

    [Fact]
    public async Task CrearUsuario_RolInexistente_DevuelveFailSinCrearElUsuario()
    {
        var manejador = new ManejadorGuionado(llamada =>
            llamada.Ruta.Contains("/roles/") ? Json(HttpStatusCode.NotFound) : RutasHabituales(llamada));

        var resultado = await Crear(manejador).CrearUsuario("jperez", "j@correo.com", "Juan Pérez", "Inexistente", "Temporal#1");

        Assert.Equal(KeycloakMensajes.RolNoEncontrado, resultado.Error);
        Assert.DoesNotContain(manejador.Llamadas, l => l.Metodo == HttpMethod.Post && l.Ruta.EndsWith("/users"));
    }

    [Fact]
    public async Task CrearUsuario_FallaLaAsignacionDelRol_EliminaElUsuarioCreado()
    {
        var manejador = new ManejadorGuionado(llamada =>
            llamada.Ruta.Contains("role-mappings") ? Json(HttpStatusCode.InternalServerError) : RutasHabituales(llamada));

        var resultado = await Crear(manejador).CrearUsuario("jperez", "j@correo.com", "Juan Pérez", "Cajero", "Temporal#1");

        Assert.True(resultado.IsFail);
        Assert.Contains(manejador.Llamadas, l => l.Metodo == HttpMethod.Delete && l.Ruta.EndsWith("/users/usuario-1"));
    }

    [Fact]
    public async Task CrearUsuario_PasswordNoCumplePolitica_DevuelveMensajeEnEspanol()
    {
        var manejador = new ManejadorGuionado(llamada =>
            llamada.Metodo == HttpMethod.Post && llamada.Ruta.EndsWith("/users")
                ? Json(HttpStatusCode.BadRequest, """{"error":"invalidPasswordMinLengthMessage"}""")
                : RutasHabituales(llamada));

        var resultado = await Crear(manejador).CrearUsuario("jperez", "j@correo.com", "Juan Pérez", "Cajero", "corta");

        Assert.Equal(KeycloakMensajes.PasswordNoCumplePolitica, resultado.Error);
    }

    [Fact]
    public async Task CualquierOperacion_AdministradorRechazado_DevuelveServicioNoDisponible()
    {
        var manejador = new ManejadorGuionado(_ => Json(HttpStatusCode.Unauthorized));

        var resultado = await Crear(manejador).CambiarPassword("jperez", "Nueva#123");

        Assert.Equal(KeycloakMensajes.ServicioNoDisponible, resultado.Error);
    }

    [Fact]
    public async Task CualquierOperacion_KeycloakCaido_NoLanzaYDevuelveFail()
    {
        var manejador = new ManejadorGuionado(_ => throw new HttpRequestException("sin conexión"));

        var resultado = await Crear(manejador).CambiarEstado("jperez", false);

        Assert.True(resultado.IsFail);
        Assert.Equal(KeycloakMensajes.ServicioNoDisponible, resultado.Error);
    }

    [Fact]
    public async Task CambiarPassword_Correcto_RestableceSinMarcarlaTemporal()
    {
        var manejador = new ManejadorGuionado(RutasHabituales);

        var resultado = await Crear(manejador).CambiarPassword("jperez", "Nueva#123");

        Assert.True(resultado.IsOk, resultado.Error);
        var cambio = Assert.Single(manejador.Llamadas, l => l.Metodo == HttpMethod.Put);
        Assert.EndsWith("/users/usuario-1/reset-password", cambio.Ruta);
        Assert.Contains("\"temporary\":false", cambio.Cuerpo);
        Assert.Contains("\"value\":\"Nueva#123\"", cambio.Cuerpo);
    }

    [Fact]
    public async Task CambiarPassword_RechazadaPorPolitica_DevuelveMensajeDePolitica()
    {
        var manejador = new ManejadorGuionado(llamada =>
            llamada.Metodo == HttpMethod.Put ? Json(HttpStatusCode.BadRequest) : RutasHabituales(llamada));

        var resultado = await Crear(manejador).CambiarPassword("jperez", "corta");

        Assert.Equal(KeycloakMensajes.PasswordNoCumplePolitica, resultado.Error);
    }

    [Fact]
    public async Task CambiarPassword_UsuarioInexistente_DevuelveUsuarioNoEncontrado()
    {
        var manejador = new ManejadorGuionado(llamada =>
            llamada.Ruta.Contains("/users?username=") ? Json(HttpStatusCode.OK, "[]") : RutasHabituales(llamada));

        var resultado = await Crear(manejador).CambiarPassword("nadie", "Nueva#123");

        Assert.Equal(KeycloakMensajes.UsuarioNoEncontrado, resultado.Error);
    }

    [Fact]
    public async Task VerificarPassword_Correcta_UsaPasswordGrantConElClienteDeLogin()
    {
        var manejador = new ManejadorGuionado(_ => Json(HttpStatusCode.OK, TokenAdminJson));

        var resultado = await Crear(manejador).VerificarPassword("jperez", "Actual#123");

        Assert.True(resultado.IsOk);
        var llamada = Assert.Single(manejador.Llamadas);
        Assert.Equal("/realms/puntomarket/protocol/openid-connect/token", llamada.Ruta);
        Assert.Contains("client_id=cliente-web", llamada.Cuerpo);
        Assert.Contains("grant_type=password", llamada.Cuerpo);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task VerificarPassword_Incorrecta_DevuelveContrasenaActualIncorrecta(HttpStatusCode estado)
    {
        var manejador = new ManejadorGuionado(_ => Json(estado));

        var resultado = await Crear(manejador).VerificarPassword("jperez", "mala");

        Assert.Equal(KeycloakMensajes.PasswordActualIncorrecta, resultado.Error);
    }

    [Fact]
    public async Task VerificarPassword_KeycloakFallando_DevuelveServicioNoDisponible()
    {
        var manejador = new ManejadorGuionado(_ => Json(HttpStatusCode.InternalServerError));

        var resultado = await Crear(manejador).VerificarPassword("jperez", "Actual#123");

        Assert.Equal(KeycloakMensajes.ServicioNoDisponible, resultado.Error);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CambiarEstado_ActualizaElCampoEnabled(bool activo)
    {
        var manejador = new ManejadorGuionado(RutasHabituales);

        var resultado = await Crear(manejador).CambiarEstado("jperez", activo);

        Assert.True(resultado.IsOk, resultado.Error);
        var cambio = Assert.Single(manejador.Llamadas, l => l.Metodo == HttpMethod.Put);
        Assert.EndsWith("/users/usuario-1", cambio.Ruta);
        Assert.Contains($"\"enabled\":{activo.ToString().ToLowerInvariant()}", cambio.Cuerpo);
    }

    [Fact]
    public void Opciones_SinConfiguracionObligatoria_FallanAlValidarNombrandoLosCampos()
    {
        var excepcion = Assert.Throws<InvalidOperationException>(() => new KeycloakOpciones().Validar());

        Assert.Contains("Keycloak:BaseUrl", excepcion.Message);
        Assert.Contains("Keycloak:AdminPassword", excepcion.Message);
    }

    [Fact]
    public void Opciones_ConstruyenLasUrlsDelRealm()
    {
        var opciones = Opciones();

        Assert.Equal("http://keycloak.test:8080/realms/master/protocol/openid-connect/token", opciones.UrlToken("master").ToString());
        Assert.Equal("http://keycloak.test:8080/admin/realms/puntomarket/users", opciones.UrlAdmin("users").ToString());
    }
}
