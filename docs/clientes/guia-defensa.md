# Guía de defensa

1. ¿Por qué Hexagonal? Domain aislado y puertos sustituibles.
2. ¿Qué hace Result? Representa éxito/fallo sin excepciones de negocio.
3. ¿Dónde se valida? `Domain/Validaciones/ClienteValidador.cs`.
4. ¿Qué protege al genérico? `Cliente` y el caso de uso verifican `EsGenerico`.
5. ¿Cómo se evita NIT duplicado? Índice generado y validación en Application/repositorio.
6. ¿Cómo se usa Factory Method? `ClienteCreator` delega la creación a creadores.
7. ¿Qué es el Adapter? `MySqlClienteRepository` traduce puerto a SQL.
8. ¿Cómo se implementa Singleton? `MySqlDataSource` se registra con `AddSingleton`.
9. ¿Quién fija el usuario? El claim `sub` mediante `ICurrentUser`.
10. ¿La baja es física? No, cambia `estaActivo` a cero.
11. ¿Cómo se autoriza? Políticas JWT para lectura, gestión y baja.
12. ¿Qué pasa al cambiar base? Se reemplaza el adaptador, no Domain/Application.
