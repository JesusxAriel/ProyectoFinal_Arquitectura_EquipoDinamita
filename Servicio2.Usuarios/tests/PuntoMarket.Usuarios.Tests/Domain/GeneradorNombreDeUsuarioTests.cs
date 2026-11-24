using PuntoMarket.Usuarios.Domain.Entities;
using PuntoMarket.Usuarios.Domain.Services;
using Xunit;

namespace PuntoMarket.Usuarios.Tests.Domain;

public class GeneradorNombreDeUsuarioTests
{
    [Theory]
    [InlineData("Juan Carlos", "Pérez", "jperez")]
    [InlineData("  juan  ", "  perez  ", "jperez")]
    [InlineData("Ángel", "Íñiguez", "ainiguez")]
    [InlineData("María José", "Núñez", "mnunez")]
    [InlineData("Juan", "De la Cruz", "jdelacruz")]
    public void GenerarBase_toma_la_inicial_del_nombre_y_el_apellido_paterno(
        string nombres, string apellidos, string esperado)
    {
        var resultado = GeneradorNombreDeUsuario.GenerarBase(nombres, apellidos);

        Assert.True(resultado.IsOk);
        Assert.Equal(esperado, resultado.Value);
    }

    [Theory]
    [InlineData(null, "Pérez")]
    [InlineData("", "Pérez")]
    [InlineData("Juan", "   ")]
    [InlineData("123", "456")]
    public void GenerarBase_falla_sin_lanzar_cuando_faltan_letras(string? nombres, string? apellidos)
    {
        var resultado = GeneradorNombreDeUsuario.GenerarBase(nombres, apellidos);

        Assert.True(resultado.IsFail);
        Assert.NotEmpty(resultado.Error);
    }

    [Fact]
    public void Siguiente_numera_desde_el_segundo_intento()
    {
        Assert.Equal("jperez", GeneradorNombreDeUsuario.Siguiente("jperez", GeneradorNombreDeUsuario.PrimerIntento));
        Assert.Equal("jperez2", GeneradorNombreDeUsuario.Siguiente("jperez", 2));
        Assert.Equal("jperez10", GeneradorNombreDeUsuario.Siguiente("jperez", 10));
    }

    [Fact]
    public void Siguiente_recorta_el_nombre_base_para_respetar_el_largo_de_la_columna()
    {
        var baseLarga = new string('a', Usuario.LongitudMaximaUsername + 10);

        var username = GeneradorNombreDeUsuario.Siguiente(baseLarga, 2);

        Assert.Equal(Usuario.LongitudMaximaUsername, username.Length);
        Assert.EndsWith("2", username);
    }

    [Fact]
    public void Siguiente_produce_la_secuencia_del_protocolo_ante_duplicados()
    {
        var nombreBase = GeneradorNombreDeUsuario.GenerarBase("Juan", "Pérez").Value;
        var tomados = new HashSet<string> { "jperez", "jperez2" };

        var intento = GeneradorNombreDeUsuario.PrimerIntento;
        while (tomados.Contains(GeneradorNombreDeUsuario.Siguiente(nombreBase, intento)))
            intento++;

        Assert.Equal("jperez3", GeneradorNombreDeUsuario.Siguiente(nombreBase, intento));
    }
}
