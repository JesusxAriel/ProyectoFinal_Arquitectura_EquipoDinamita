# Dockerización de PuntoMarket

Entorno de desarrollo con cinco servicios y bases independientes. Esta fase conserva el CRUD, el diseño del frontend y la ejecución local; no integra Keycloak ni Usuarios. No realizar commit/push hasta revisar el resultado.

## 1. Requisitos y Docker Desktop

En Windows 10 instalar Docker Desktop desde su sitio oficial, cumplir sus requisitos de Windows/virtualización/WSL 2 e iniciar Docker Desktop. Seleccionar contenedores Linux y esperar a que el motor esté disponible. La compatibilidad concreta depende de la edición/build y soporte de Windows instalado; no desactivar controles de seguridad para sortear errores.

```powershell
docker version
docker compose version
```

Ambos deben responder y docker version debe mostrar Client y Server. Si solo aparece Client, abrir Docker Desktop o ejecutar `docker desktop start` si la CLI instalada dispone de ese comando. El SDK .NET y MongoDB de Windows no son necesarios para ejecutar los contenedores; sí se necesitan herramientas locales para las pruebas .NET/JavaScript.

Fuentes oficiales: [Docker Desktop para Windows](https://docs.docker.com/desktop/setup/install/windows-install/), [imágenes .NET](https://github.com/dotnet/dotnet-docker), [imagen oficial MongoDB](https://hub.docker.com/_/mongo).

## 2. Clonar y levantar

```powershell
git clone https://github.com/JesusxAriel/ProyectoFinal_Arquitectura_EquipoDinamita.git
Set-Location ProyectoFinal_Arquitectura_EquipoDinamita
docker compose config --quiet
docker compose up --build -d
docker compose ps
```

La configuración de esta fase debe estar disponible en la copia revisada; mientras no se publique, un clon del commit inicial no incluye estos archivos Docker. Solo existe un Compose, `docker-compose.yml`, con nombre de proyecto `puntomarket`. No se fijan container_name.

No hace falta crear .env: los valores predeterminados permiten iniciar los cinco servicios. La primera construcción descarga imágenes oficiales y dependencias; puede tardar varios minutos.

## 3. Servicios, redes y puertos

| Servicio Compose | Aplicación | Puerto host predeterminado | Puerto contenedor |
|---|---|---|---|
| frontend | Frontend.Demo | 127.0.0.1:5100 | 8080 |
| gateway | ApiGateway.Demo | 127.0.0.1:5000 | 8080 |
| proveedores-api | Proveedores.API | 127.0.0.1:5200 | 8080 |
| identity-dev | DevIdentity.Demo | 127.0.0.1:5300 | 8080 |
| mongodb | MongoDB 8.0.38-noble | 127.0.0.1:27018 | 27017 |

Todos los puertos se publican solo en loopback. La red aplicaciones conecta frontend, gateway, API e identidad. La red datos-proveedores es interna y conecta únicamente la API con su MongoDB. Una tercera red, compass-local, contiene solo MongoDB y permite publicar su puerto loopback; sin ella Docker no publica el puerto de un contenedor conectado únicamente a una red internal. El servidor MongoDB de Windows en 27017 no se monta, copia ni utiliza desde Compose.

Cada Dockerfile usa SDK .NET 10, restauración por csproj antes de copiar fuentes, publicación Release y runtime ASP.NET Core 10 sin SDK ni fuentes. Los procesos .NET ejecutan como APP_UID/app, no root. Curl está instalado para health checks. Los .dockerignore excluyen binarios, secretos y datos locales.

## 4. Variables opcionales y conflictos de puertos

`.env.example` contiene únicamente puertos de ejemplo, sin contraseñas. Para cambiarlos:

```powershell
Copy-Item .env.example .env
# Editar .env antes de iniciar los contenedores.
```

| Variable | Predeterminado |
|---|---|
| FRONTEND_PORT | 5100 |
| GATEWAY_PORT | 5000 |
| PROVEEDORES_API_PORT | 5200 |
| DEV_IDENTITY_PORT | 5300 |
| MONGO_PORT | 27018 |

No sobrescribir un .env propio. Si ya se ejecutan los servicios con dotnet run, detenerlos manualmente o elegir puertos Docker alternativos. No terminar procesos ajenos. Cambiar DEV_IDENTITY_PORT actualiza simultáneamente el issuer del emisor y Authority de la API; no cambiar una sola parte de la identidad.

Las variables existentes en la terminal tienen precedencia sobre .env. Cambiar de proyecto con `-p` crea otro conjunto de redes/volúmenes; mantener el mismo nombre para recuperar sus datos. Revisar `docker compose config` antes de iniciar si hay dudas.

## 5. Acceso y login

Abrir **http://localhost:5100** o /Login. Seleccionar Administrador para crear/editar/desactivar/reactivar o Cajero para consulta. El frontend utiliza `http://gateway:8080/` internamente; no llama directamente a la API.

DevIdentity continúa siendo una demostración sin credenciales personales. Sus identidades son fijas, no hay registro real y solo arranca en Development. Su clave RSA se genera al iniciar; un reinicio puede invalidar tokens hasta renovar y refrescar JWKS. No utilizar este despliegue como autenticación de producción.

## 6. Los siete proveedores iniciales

En un volumen nuevo, el inicializador de Proveedores crea índices e inserta PIL Andina, EMBOL, Sofía, CBN, Unilever, Industrias Venado y Arcor mediante el seed del código. No se importan datos de Windows, SQL ni registros manuales del desarrollador.

Se conservan las marcas origenSeed y $setOnInsert: reiniciar la API no duplica ni sobrescribe registros ya sembrados. Bajas y ediciones persisten. Una instalación que ya tenga su volumen no se restablece a siete documentos al reiniciar: conserva las altas posteriores, que es el comportamiento esperado.

## 7. JWT público y transporte interno

- Issuer público predeterminado: `http://localhost:5300/realms/proveedores-dev`.
- Audience: `proveedores-api`.
- Firma: RS256; JWKS publica solo la clave pública.
- Authority de la API: el issuer público anterior.
- MetadataAddress opcional de la API: `http://identity-dev:8080/realms/proveedores-dev/.well-known/openid-configuration`.

MetadataAddress modifica solo la URL de transporte del descubrimiento, no desactiva comprobaciones. El documento mantiene issuer público y anuncia JWKS en el origen por el que se consulta: identity-dev:8080 dentro de Docker, localhost:5300 desde el host. El JWT siempre se firma con el issuer configurado, nunca con el Host recibido. Esta adaptación corresponde al emisor temporal, no a una implementación de Keycloak.

Se conservan ValidateIssuer, ValidateAudience, ValidateLifetime y ValidateIssuerSigningKey. El Gateway propaga Authorization y la API valida JWT/roles. No se incorpora ningún secreto de firma a las imágenes ni al repositorio.

Consultas de desarrollo sin escrituras:

```powershell
Invoke-RestMethod http://localhost:5300/realms/proveedores-dev/.well-known/openid-configuration
Invoke-RestMethod http://localhost:5300/realms/proveedores-dev/protocol/openid-connect/certs
$identidad = Invoke-RestMethod -Method Post -Uri http://localhost:5000/dev-auth/token `
  -ContentType 'application/json' -Body '{"perfil":"Administrador"}'
Invoke-RestMethod -Uri http://localhost:5000/api/proveedores `
  -Headers @{ Authorization = "Bearer $($identidad.access_token)" }
```

No imprimir ni compartir tokens. Swagger está en http://localhost:5200/swagger en esta configuración Development.

## 8. Estado, logs y disponibilidad

```powershell
docker compose ps
docker compose logs --tail 100
docker compose logs -f proveedores-api
docker compose logs -f gateway
docker compose exec -T mongodb mongosh --quiet --eval 'db.adminCommand({ping:1})'
```

Compose espera a MongoDB y DevIdentity saludables antes de iniciar la API; después inicia Gateway y frontend. Los health de aplicación comprueban el proceso HTTP, no la disponibilidad permanente de toda la cadena. depends_on no garantiza recuperación automática de todas las dependencias después del arranque; diagnosticar errores con logs.

## 9. Detener, reiniciar y reconstruir

```powershell
# Detener sin eliminar contenedores ni volúmenes.
docker compose stop
# Iniciar los contenedores detenidos.
docker compose start
# Reiniciar procesos; no reconstruye imágenes.
docker compose restart
# Reconstruir y recrear tras modificar código.
docker compose up --build -d
# Reconstruir solo un servicio y recrearlo.
docker compose build proveedores-api
docker compose up -d proveedores-api
# Retirar contenedores y redes conservando los volúmenes.
docker compose down
# Recuperarlos usando los mismos volúmenes.
docker compose up -d
```

`docker compose down` conserva datos. **`docker compose down -v` elimina volúmenes y sus datos**: no usarlo para reinicios, pruebas o mantenimiento habitual. No se ejecuta ese comando durante esta verificación. Tampoco usar volume prune/system prune con volúmenes para solucionar problemas de arranque.

## 10. Persistencia y Compass

El volumen nombrado mongodb_data se monta en /data/db; por defecto Docker lo identifica como puntomarket_mongodb_data. El volumen frontend_keys conserva las claves locales de Data Protection/antiforgery al recrear el frontend; las sesiones siguen siendo de memoria y puede ser necesario volver a elegir perfil.

Compass en el host conecta a **mongodb://localhost:27018**, base proveedores_db, colección proveedores. Es el MongoDB Docker, distinto de mongodb://localhost:27017 de Windows. No hay contraseña en esta demo: el puerto solo se publica en loopback y la red de datos está separada. No exponerlo en LAN ni usar esta configuración para producción. Para una entrega real se acordarán autenticación y gestión de secretos.

```powershell
docker volume ls
docker volume inspect puntomarket_mongodb_data
```

El volumen pertenece al proyecto; no cambiar su nombre ni eliminarlo para recuperar datos. Una copia de seguridad de desarrollo puede prepararse de forma explícita, fuera de Git; esta fase no importa ni exporta la base personal de Windows.

## 11. Pruebas

Con la pila Docker iniciada:

```powershell
docker compose config --quiet
docker compose build
docker compose up -d
docker compose ps
$env:PROVEEDORES_TEST_MONGO = 'mongodb://localhost:27018'
dotnet restore Proyecto_Microservicios.slnx
dotnet build Proyecto_Microservicios.slnx --no-restore
dotnet test Servicio1.Proveedores/Proveedores.slnx --no-restore
node scripts/probar-validaciones-frontend.cjs
node scripts/probar-busqueda-dinamica.mjs
```

Las pruebas Mongo crean y eliminan exclusivamente bases temporales con nombre único dentro del servidor Docker; no usan proveedores_db. DockerIdentityTests comprueba el descubrimiento desde origen público/interno y valida firma RS256, issuer, audience y vigencia. MongoSeedApiTests prueba seed/API sobre otra base temporal vacía.

Prueba reproducible de la pila desde PowerShell 7, con los puertos predeterminados:

```powershell
./scripts/verificar-docker.ps1 -PermitirEscrituras -ExigirSoloSeed -ProbarCicloDeVida
```

`-ExigirSoloSeed` exige que inicialmente haya solo siete documentos: omitirlo si ya hay altas posteriores, sin borrar nada. El script verifica que se trata del proyecto puntomarket, que el destino Mongo es DNS Docker y que el puerto corresponde al Gateway de esa pila antes de escribir. Para puertos alternativos admite -FrontendPort, -GatewayPort, -ApiPort e -IdentityPort. El ciclo de vida reinicia API y MongoDB y ejecuta down/up **sin -v**. Crea un proveedor de prueba, comprueba permisos/CRUD y lo conserva desactivado; el resultado local queda en artifacts/docker/verificacion-pila.json.

Las pruebas de CRUD/persistencia sobre la base de demostración Docker crean un proveedor de prueba identificable; deben conservarlo o desactivarlo lógicamente, sin borrar colecciones ni volúmenes. Los resultados realmente ejecutados se documentan al terminar esta fase, sin atribuir como comprobado lo pendiente.

## 12. Problemas frecuentes

| Problema | Diagnóstico/solución |
|---|---|
| No hay conexión al motor | Abrir Docker Desktop, esperar Linux engine y comprobar docker version |
| Puerto ocupado | Conservar proceso local y cambiar el puerto correspondiente en .env o terminal |
| 401 tras arrancar | Revisar issuer/Authority, MetadataAddress, JWKS, audience y hora; no desactivar validación |
| 401 tras reiniciar identidad | Su RSA cambia; renovar token y permitir refresco de JWKS. Reiniciar API/frontend si es necesario en demo |
| 403 | Token válido sin el rol requerido; Cajero no puede escribir |
| 502/503 | Revisar DNS gateway/proveedores-api/identity-dev, health y logs |
| Mongo no aparece en Compass | Usar puerto host 27018 (o MONGO_PORT), no el 27017 del contenedor |
| No aparecen exactamente siete | Un volumen existente conserva altas/bajas anteriores; no eliminarlo para forzar el seed |
| Cambió código pero no aplicación | restart no compila; usar up --build -d |
| Sesión/cookie dejó de funcionar | Volver a elegir perfil; revisar permisos de frontend_keys y logs |
| Fallan descargas | Revisar proxy/red/certificados del entorno; no incorporar credenciales a Dockerfiles |

## 13. Incorporar otros microservicios

Cuando exista el servicio real, añadir su servicio/build y su base/volumen/red propios al mismo Compose. Asignar puertos libres y health coherente; configurar su destino YARP por DNS del servicio. Mantener las tres rutas actuales de Proveedores y dev-auth. No añadir un segundo Compose de arranque que compita con el actual.

Los clusters de YARP se definen en ApiGateway/ApiGateway.Demo/appsettings.json. Compose sobrescribe solo las direcciones para Docker. El nombre de servicio puede diferir del identificador del cluster, pero ClusterId y la clave del cluster deben coincidir. Agregar transformaciones solo cuando lo exija el contrato real.

Usuarios y su base no se crean en esta fase. Acordar rutas, JSON, permisos, issuer, audiencias, claims y relación de IDs con el Grupo 2. Cada API conserva validaciones de seguridad y no se conecta a la base de otra. Ver [guía de integración](GUIA_INTEGRACION_MICROSERVICIOS.md).

## 14. Ejecución local sin Docker

Los appsettings y launchSettings locales mantienen localhost y 5100/5000/5200/5300. MetadataAddress es opcional y no se define en esos archivos; sin él se descubre desde Authority como antes.

Con MongoDB de Windows disponible, en terminales distintas:

```powershell
dotnet run --project DevIdentity/DevIdentity.Demo
dotnet run --project Servicio1.Proveedores/Proveedores.API
dotnet run --project ApiGateway/ApiGateway.Demo
dotnet run --project Frontend/Frontend.Demo
```

No ejecutar al mismo tiempo pilas local/Docker con los mismos puertos. Evitar dejar variables Mongo__*, ApiGateway__* o Authentication__Jwt__* de otro entorno en la terminal. Docker utiliza sus variables dentro de contenedores, sin modificar appsettings locales.

## 15. Mantenimiento y alcance

Revisar versiones oficiales de imágenes antes de actualizarlas; Mongo se fija a una versión explícita y .NET a la línea 10.0-noble. No se modifica lógica comercial, dominio ni contratos CRUD. Las únicas adaptaciones de código son MetadataAddress opcional y el origen de JWKS del emisor temporal, cubiertas por pruebas.

No hay commit ni push en esta fase. La guía y los archivos Docker quedan disponibles para revisión. El despliegue es local de desarrollo y no sustituye la preparación de seguridad/identidad de una instalación productiva.

## 16. Resultado real de esta verificación

- Estado inicial de Git: main limpio en b1f40ed. No se creó commit, no se ejecutó push ni se modificó el índice Git.
- Docker Desktop estaba detenido/no disponible inicialmente; se inició y se comprobó motor Linux 29.9.1.
- Compose anterior: YAML corrupto. Se reemplazó el contenido del mismo archivo, sin crear otro Compose.
- `docker compose config --quiet`: correcto. `docker compose build`: cuatro imágenes construidas correctamente con SDK/ASP.NET Core 10.0-noble.
- También se ejecutó realmente `docker compose up --build -d` al final, usando el .env local de revisión; reconstrucción/arranque correctos.
- Arranque: cinco servicios saludables; inspección final con RestartCount=0 en todos. Las cuatro aplicaciones .NET usan uid/gid 1654 (app), no root.
- Puertos predeterminados 5100/5000/5200/5300 estaban ocupados por procesos locales del usuario. No se detuvieron. La prueba usó frontend **15100**, gateway **15000**, API **15200**, identidad **15300** y Mongo **27018**. Se dejó un `.env` local ignorado por Git con esos puertos para facilitar la revisión. Un clon sin .env conserva los puertos predeterminados.
- URL de revisión actual: **http://localhost:15100**. Compass: **mongodb://localhost:27018**. `docker compose ps` muestra los puertos efectivos.
- MongoDB Docker inicial: exactamente siete proveedores seed. Reiniciar la API conservó BSON completo e IDs sin duplicaciones.
- JWT/Gateway: issuer público 15300, descubrimiento/JWKS interno por identity-dev:8080 y público por localhost:15300; consulta autorizada correcta. Token ausente o firma alterada -> 401. Cajero consulta y recibe 403 en POST/PUT/DELETE/PATCH.
- Frontend: login con antiforgery de ambos perfiles, consulta/búsqueda por Gateway y visibilidad de acciones correctos. Los estilos, páginas y JavaScript de la UI no se modificaron; sus pruebas existentes pasaron.
- CRUD: se creó y editó un proveedor de prueba, se comprobó baja lógica y reactivación. Tras reiniciar MongoDB y realizar down/up sin -v, la instantánea BSON permaneció idéntica y la API recuperó el registro.
- Al terminar se conservó ese proveedor **desactivado** como evidencia. Estado final: **8 documentos totales, 7 activos, 7 con origenSeed**. No se borraron documentos ni volúmenes.
- .NET: restore correcto, build completo con **0 errores y 0 advertencias**, **215 pruebas aprobadas, 0 fallidas y 0 omitidas**. Incluye 14 de MongoDB real dentro del servidor Docker y 4 nuevas de descubrimiento/seguridad JWT. TRX local: artifacts/docker/tests/docker.trx.
- JavaScript: 60 casos de campos, corrección de mensajes y doble envío, además de las pruebas de debounce/limpiar/cancelación/obsolescencia/paginación: correctos.
- Compatibilidad local: appsettings/launchSettings siguen intactos; pruebas de MetadataAddress ausente y descubrimiento público aprobadas. Se inició realmente DevIdentity mediante dotnet run en un puerto temporal 15310, se consultaron issuer/JWKS y se cerró exclusivamente ese proceso de prueba. No se reiniciaron los cuatro procesos locales existentes ni se afirma un segundo CRUD manual completo en ellos.
- No se conectó ni modificó el MongoDB de Windows en 27017. Las pruebas .NET usaron PROVEEDORES_TEST_MONGO=mongodb://localhost:27018 y bases temporales propias.
- La red internal por sí sola no publicó el puerto Mongo para Compass; se añadió compass-local exclusivamente para Mongo y su binding 127.0.0.1. La separación de datos de la API permanece en datos-proveedores.
- Límites: no se hizo una revisión visual exhaustiva de todos los navegadores dentro de Docker; se comprobó el circuito HTTP real de Razor/Gateway y los tests de UI/JS. El entorno sigue siendo Development, sin autenticación Mongo ni Keycloak real. No se verificó una nueva clonación remota porque esta fase aún no está publicada.

Evidencia reproducible de la pila: `artifacts/docker/verificacion-pila.json`, excluida de Git. Para repetir sobre estos puertos sin exigir una colección nueva:

```powershell
./scripts/verificar-docker.ps1 -FrontendPort 15100 -GatewayPort 15000 -ApiPort 15200 -IdentityPort 15300 -PermitirEscrituras -ProbarCicloDeVida
```

Cada repetición añade su propio registro de prueba y lo conserva inactivo; no ejecutar repetidamente si no se desea acumular esa evidencia. No usar -ExigirSoloSeed cuando el volumen ya tiene datos posteriores.

## 17. Inventario de archivos

| Estado | Archivo | Finalidad |
|---|---|---|
| Nuevo en Git, reemplaza el borrador local | docker-compose.yml | Pila única, servicios, redes, health, puertos, DNS, JWT y volúmenes |
| Nuevo en Git, actualizados desde borradores locales | Servicio1.Proveedores/Dockerfile, Frontend/Frontend.Demo/Dockerfile, ApiGateway/ApiGateway.Demo/Dockerfile, DevIdentity/DevIdentity.Demo/Dockerfile | Multi-stage Release, runtime .NET 10 y usuario app |
| Nuevos en Git | .dockerignore y los cuatro .dockerignore de contextos | Excluir binarios, artefactos, secretos y datos |
| Nuevo en Git, reemplaza plantilla local antigua | .env.example | Solo puertos opcionales, sin contraseñas |
| Local ignorado | .env | Puertos alternativos de revisión; no versionar |
| Modificado | .gitignore | Permitir Docker y la plantilla, mantener exclusión de secretos/datos |
| Modificados | Proveedores.API/Security/AuthSettings.cs y ConfigureJwtBearerOptions.cs | MetadataAddress opcional sin desactivar validación |
| Modificado | DevIdentity/DevIdentity.Demo/Program.cs | JWKS según origen solicitado y acceso a TestServer |
| Modificado | Proveedores.Tests/Proveedores.Tests.csproj | Referencia con alias al emisor solo para pruebas |
| Nuevo | Proveedores.Tests/DockerIdentityTests.cs | Cuatro comprobaciones de identidad pública/interna y RS256 |
| Nuevo | scripts/verificar-docker.ps1 | Verificación real, protegida, de JWT/roles/CRUD/persistencia en Compose |
| Modificados | README.md e INTEGRACION_GRUPO2.md | Inicio rápido y configuración de la fase Docker |
| Nuevo | GUIA_DOCKER_PUNTOMARKET.md | Operación, diagnóstico, seguridad, pruebas e integración futura |

Los paths de Proveedores.API y Proveedores.Tests del inventario pertenecen a Servicio1.Proveedores/. No se cambiaron contratos de negocio, Domain, Application, índices ni datos de Windows.
