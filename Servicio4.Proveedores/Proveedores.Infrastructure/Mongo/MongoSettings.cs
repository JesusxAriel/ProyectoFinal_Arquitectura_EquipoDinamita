namespace Proveedores.Infrastructure.Mongo;

// =====================================================
// CONFIGURACIÓN DE MONGODB
// Sección "Mongo" de appsettings.json / variables de entorno:
//   Mongo__ConnectionString, Mongo__DatabaseName, Mongo__ProveedoresCollection,
//   Mongo__InitializeOnStartup (crear índices al arrancar), Mongo__SeedEnabled (datos iniciales).
// En Docker el host de la cadena de conexión es el NOMBRE DEL SERVICIO ("mongodb"), nunca localhost.
// Para desactivar el seed: Mongo__SeedEnabled=false.
// =====================================================
public sealed class MongoSettings
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "proveedores_db";
    public string ProveedoresCollection { get; set; } = "proveedores";
    public bool InitializeOnStartup { get; set; } = true;
    public bool SeedEnabled { get; set; } = true;
}
