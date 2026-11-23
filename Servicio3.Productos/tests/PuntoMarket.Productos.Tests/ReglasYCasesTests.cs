using PuntoMarket.Productos.Domain;
using PuntoMarket.Productos.Application;
using PuntoMarket.Productos.Application.Ports.Out;
using PuntoMarket.Productos.Application.UseCases;
using PuntoMarket.Productos.Adapters.Out.Persistence;
using Npgsql;
namespace PuntoMarket.Productos.Tests;

public class ReglasYCasesTests
{
    internal static DatosProducto Datos => new("SKU-1","Producto",10,15,1,1,"proveedor-externo");
    [Theory]
    [InlineData(0,15)] [InlineData(-1,15)] [InlineData(10,9)] [InlineData(10.001,15)] [InlineData(10,15.001)]
    public void PrecioInvalido(double costo,double venta) =>
        Assert.Equal(ErrorType.Validation,ProductoReglas.Validar(Datos with{Costo=(decimal)costo,PrecioVenta=(decimal)venta}).Error.Type);
    [Fact] public void PrecioIgualAlCostoEsValido() => Assert.True(ProductoReglas.Validar(Datos with{PrecioVenta=10}).IsSuccess);
    [Fact] public void CambioPrecioExigeMotivo() {
        Assert.Equal("Producto.MotivoCambioPrecioInvalido",ProductoReglas.Validar(Datos,14).Error.Code);
        Assert.True(ProductoReglas.Validar(Datos with{MotivoCambioPrecio="Ajuste comercial"},14).IsSuccess);
    }
    [Theory] [InlineData("", "Nombre")] [InlineData("SKU 1","Nombre")] [InlineData("SKU","<script>")]
    public void TextoInvalido(string codigo,string nombre) => Assert.True(ProductoReglas.Validar(Datos with{Codigo=codigo,Nombre=nombre}).IsFailure);
    [Fact] public void ReferenciasRequeridas() {
        Assert.True(ProductoReglas.Validar(Datos with{CategoriaId=0}).IsFailure);
        Assert.True(ProductoReglas.Validar(Datos with{EmpaqueId=0}).IsFailure);
        Assert.True(ProductoReglas.Validar(Datos with{ProveedorId=""}).IsFailure);
    }
    [Fact] public void ResultNoPermiteLeerFallo() {
        var error=Error.Conflict("Producto.CodigoDuplicado","Ya existe.");
        var r=Result.Failure<Producto>(error);
        Assert.True(r.IsFail); Assert.Equal(error,r.Error);
        Assert.Throws<InvalidOperationException>(()=>r.Value);
        Assert.Equal(4,Result.Success(4).Value);
        Assert.Throws<InvalidOperationException>(()=>Result.Failure(Error.None));
    }
    [Fact] public void PaginacionYOrdenProtegidos() {
        var q=new ListarProductosQuery(Pagina:int.MaxValue,TamanoPagina:1000);
        var r=q.Normalizar(); Assert.True(r.IsSuccess);
        Assert.InRange((long)(r.Value.Pagina-1)*r.Value.TamanoPagina,0,int.MaxValue);
        Assert.True((q with{OrdenarPor="nombre;DROP TABLE producto"}).Normalizar().IsFailure);
    }
    [Fact] public async Task CrearNoPersisteCuandoProveedorFalla() {
        var repo=new Repo(); var p=new Proveedor {Respuesta=Result.Failure(Error.Unavailable("Proveedor.NoDisponible","No disponible."))};
        var r=await new CrearProducto(repo,p,new Usuario()).EjecutarAsync(Datos,default);
        Assert.Equal(ErrorType.Unavailable,r.Error.Type); Assert.Equal(0,repo.Escrituras);
    }
    [Fact] public async Task CrearValidaAntesDeHttpYUsaIdentidad() {
        var repo=new Repo(); var p=new Proveedor();
        var use=new CrearProducto(repo,p,new Usuario());
        Assert.True((await use.EjecutarAsync(Datos with{Costo=0},default)).IsFailure);
        Assert.Equal(0,p.Llamadas);
        Assert.True((await use.EjecutarAsync(Datos,default)).IsSuccess);
        Assert.Equal("sub-de-prueba",repo.Actor); Assert.Equal(1,p.Llamadas);
        var sinIdentidad=await new CrearProducto(repo,p,new Usuario{UserId=null}).EjecutarAsync(Datos,default);
        Assert.Equal(ErrorType.Unauthorized,sinIdentidad.Error.Type); Assert.Equal(1,repo.Escrituras);
    }
    [Fact] public async Task EditarNoSaltaReglasNiEstado() {
        var repo=new Repo(); var p=new Proveedor();
        var use=new ActualizarProducto(repo,p,new Usuario());
        Assert.True((await use.EjecutarAsync(1,Datos with{PrecioVenta=16},default)).IsFailure);
        Assert.Equal(0,repo.Escrituras); Assert.Equal(0,p.Llamadas);
        Assert.True((await use.EjecutarAsync(1,Datos with{PrecioVenta=16,MotivoCambioPrecio="Ajuste"},default)).IsSuccess);
        repo.Activo=false;
        Assert.Equal("Producto.Inactivo",(await use.EjecutarAsync(1,Datos,default)).Error.Code);
    }
    [Fact] public async Task ConsultasYBajaDeleganEnPuertos() {
        var repo=new Repo();
        Assert.True((await new ListarProductos(repo).EjecutarAsync(new(),default)).IsSuccess);
        Assert.True((await new ObtenerProducto(repo).EjecutarAsync(1,default)).IsSuccess);
        Assert.True((await new ConsultarCatalogos(repo).EjecutarAsync(default)).IsSuccess);
        Assert.Equal(ErrorType.NotFound,(await new ObtenerProducto(repo).EjecutarAsync(0,default)).Error.Type);
        Assert.True((await new DesactivarProducto(repo,new Usuario()).EjecutarAsync(1,default)).IsSuccess);
        Assert.False(repo.Activo);
    }
    [Fact] public async Task FactoryMethodCreaAdaptadorReal() {
        await using var ds=NpgsqlDataSource.Create("Host=localhost;Database=no_conectar;Username=test");
        ProductoRepositoryCreator creator=new PostgresProductoRepositoryCreator(ds);
        Assert.IsType<PostgresProductoRepository>(creator.Crear());
        Assert.NotSame(creator.Crear(),creator.Crear());
    }
    internal sealed class Usuario : ICurrentUser { public string? UserId {get;set;}="sub-de-prueba"; }
    internal sealed class Proveedor : IProveedorApiClient {
        public int Llamadas; public Result Respuesta=Result.Success();
        public Task<Result> ValidarActivoAsync(string id,CancellationToken ct) {Llamadas++;return Task.FromResult(Respuesta);}
    }
    internal sealed class Repo : IProductoRepository {
        public int Escrituras; public string? Actor; public bool Activo=true;
        private Producto Producto => new(1,"SKU-1","Producto",10,15,1,1,"proveedor-externo",Activo,"creador",null,DateTime.UtcNow,null);
        public Task<Result<Producto>> ObtenerAsync(int id,CancellationToken ct)=>Task.FromResult(Result.Success(Producto));
        public Task<Result<PaginaResultado<Producto>>> ListarAsync(ListarProductosQuery q,CancellationToken ct)=>Task.FromResult(Result.Success(new PaginaResultado<Producto>([Producto],1,10,1,1)));
        public Task<Result<CatalogosDto>> CatalogosAsync(CancellationToken ct)=>Task.FromResult(Result.Success(new CatalogosDto([],[])));
        public Task<Result<Producto>> CrearAsync(DatosProducto d,string u,CancellationToken ct) {Escrituras++;Actor=u;return Task.FromResult(Result.Success(Producto));}
        public Task<Result<Producto>> ActualizarAsync(int id,DatosProducto d,string u,CancellationToken ct)=>CrearAsync(d,u,ct);
        public Task<Result> DesactivarAsync(int id,string u,CancellationToken ct) {Activo=false;Actor=u;return Task.FromResult(Result.Success());}
    }
}
