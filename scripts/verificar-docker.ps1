param(
    [int]$FrontendPort = 5100,
    [int]$GatewayPort = 5000,
    [int]$ApiPort = 5200,
    [int]$IdentityPort = 5300,
    [switch]$PermitirEscrituras,
    [switch]$ExigirSoloSeed,
    [switch]$ProbarCicloDeVida
)

# Solo la pila Compose puntomarket con Mongo DNS interno; nunca el MongoDB de Windows.
# Crea un registro identificable, comprueba CRUD y lo deja desactivado, sin borrarlo físicamente.
$ErrorActionPreference = 'Stop'
if (-not $PermitirEscrituras) { throw 'Esta prueba escribe en la demo Docker. Indique -PermitirEscrituras explícitamente.' }
function Exigir($Condicion, [string]$Mensaje) { if (-not $Condicion) { throw $Mensaje } }
function EjecutarDocker([Parameter(ValueFromRemainingArguments=$true)][string[]]$Argumentos) {
    $resultado = & docker @Argumentos
    if ($LASTEXITCODE -ne 0) { throw "Falló docker $($Argumentos -join ' ')" }
    return $resultado
}
function EsperarPila {
    $limite = [DateTime]::UtcNow.AddSeconds(150)
    do {
        $estado = @(EjecutarDocker @('compose','ps','--format','json') | ForEach-Object { $_ | ConvertFrom-Json })
        if ($estado.Count -eq 5 -and @($estado | Where-Object { $_.Health -ne 'healthy' -or $_.State -ne 'running' }).Count -eq 0) { return }
        Start-Sleep -Seconds 1
    } while ([DateTime]::UtcNow -lt $limite)
    throw 'La pila no alcanzó cinco servicios saludables.'
}
function InstantaneaDocker {
    return (EjecutarDocker @('compose','exec','-T','mongodb','mongosh','--quiet','--eval',
        'EJSON.stringify(db.getSiblingDB("proveedores_db").proveedores.find({}).sort({_id:1}).toArray())')) -join "`n"
}
function Token([string]$Perfil) {
    return (Invoke-RestMethod -Method Post -Uri "$gateway/dev-auth/token" -ContentType 'application/json' -Body (@{perfil=$Perfil} | ConvertTo-Json)).access_token
}
function Api([string]$Metodo,[string]$Ruta,[string]$Jwt,$Cuerpo=$null,[int]$Esperado=200) {
    $parametros = @{ Method=$Metodo; Uri="$gateway/api/proveedores$Ruta"; SkipHttpErrorCheck=$true; TimeoutSec=20 }
    if ($Jwt) { $parametros.Headers = @{Authorization="Bearer $Jwt"} }
    if ($null -ne $Cuerpo) { $parametros.ContentType='application/json'; $parametros.Body=($Cuerpo | ConvertTo-Json -Depth 8) }
    $r = Invoke-WebRequest @parametros
    Exigir ($r.StatusCode -eq $Esperado) "$Metodo $Ruta devolvió $($r.StatusCode), esperado $Esperado."
    if ($r.Content) { return $r.Content | ConvertFrom-Json }
}
function PaginaFrontend([string]$Perfil) {
    $sesion = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $login = Invoke-WebRequest "$frontend/Login" -WebSession $sesion
    $csrf = [System.Net.WebUtility]::HtmlDecode([regex]::Match($login.Content,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value)
    Exigir ($csrf.Length -gt 0) 'Falta antiforgery del login.'
    Invoke-WebRequest "$frontend/Login" -Method Post -WebSession $sesion -Body @{perfilElegido=$Perfil;__RequestVerificationToken=$csrf} | Out-Null
    $r = Invoke-WebRequest "$frontend/Proveedores?Busqueda=pil" -WebSession $sesion
    Exigir ($r.Content.Contains('PIL Andina S.A.')) 'El frontend no consulta Proveedores a través del Gateway.'
    Exigir ($r.Content.Contains('id="proveedorModal"') -eq ($Perfil -eq 'Administrador')) 'Las acciones visibles no corresponden al perfil.'
    Exigir ($r.Content.Contains('proveedores-busqueda.mjs')) 'No se sirve el buscador dinámico.'
}

$anteriores = @{}
foreach ($par in @{FRONTEND_PORT=$FrontendPort;GATEWAY_PORT=$GatewayPort;PROVEEDORES_API_PORT=$ApiPort;DEV_IDENTITY_PORT=$IdentityPort}.GetEnumerator()) {
    $anteriores[$par.Key] = [Environment]::GetEnvironmentVariable($par.Key,'Process')
    [Environment]::SetEnvironmentVariable($par.Key,[string]$par.Value,'Process')
}
$gateway="http://localhost:$GatewayPort"
$frontend="http://localhost:$FrontendPort"
try {
    $config = (EjecutarDocker @('compose','config','--format','json') | Out-String) | ConvertFrom-Json
    Exigir ($config.name -eq 'puntomarket') 'Proyecto distinto de puntomarket; no se ejecutarán escrituras.'
    $apiId = EjecutarDocker @('compose','ps','-q','proveedores-api')
    Exigir (-not [string]::IsNullOrWhiteSpace($apiId)) 'La API Docker no está iniciada.'
    $inspeccion = ((EjecutarDocker @('inspect',$apiId) | Out-String) | ConvertFrom-Json)[0]
    Exigir ($inspeccion.Config.Env -contains 'Mongo__ConnectionString=mongodb://mongodb:27017') 'MongoDB no apunta al servicio Docker; se rechazan las pruebas.'
    Exigir (((EjecutarDocker @('compose','port','gateway','8080')) -join '').EndsWith(":$GatewayPort")) 'El Gateway no corresponde al puerto solicitado.'
    EsperarPila
    foreach ($puerto in $FrontendPort,$GatewayPort,$ApiPort,$IdentityPort) {
        Exigir ((Invoke-WebRequest "http://localhost:$puerto/health").StatusCode -eq 200) "Health $puerto falló."
    }
    $admin=Token 'Administrador'; $cajero=Token 'Cajero'
    $oidc=Invoke-RestMethod "http://localhost:$IdentityPort/realms/proveedores-dev/.well-known/openid-configuration"
    Exigir ($oidc.issuer -eq "http://localhost:$IdentityPort/realms/proveedores-dev") 'Issuer público incorrecto.'
    $jwks=Invoke-RestMethod $oidc.jwks_uri
    Exigir ($jwks.keys[0].kty -eq 'RSA') 'JWKS no contiene clave RSA.'
    Exigir (-not ($jwks.keys[0].PSObject.Properties.Name -contains 'd')) 'JWKS expone material privado.'
    Api GET '' '' $null 401 | Out-Null
    $alterado=$admin.Substring(0,$admin.Length-4)+'AAAA'
    Api GET '' $alterado $null 401 | Out-Null
    $inicial=Api GET '?estado=Todos&tamanoPagina=100' $admin
    if ($ExigirSoloSeed) { Exigir ($inicial.total -eq 7) 'La base no contiene exclusivamente los siete registros iniciales; no se borrará nada.' }
    $esperados=@('PIL Andina S.A.','EMBOL S.A. (Coca-Cola)','Sofía SHE S.A.','CBN (Cervecería Boliviana Nacional)','Unilever Bolivia','Industrias Venado S.A. (Kris)','Arcor Bolivia')
    foreach($nombre in $esperados) { Exigir ($inicial.items.nombreEmpresa -contains $nombre) "Falta proveedor seed $nombre." }
    Api GET '' $cajero | Out-Null
    PaginaFrontend 'Administrador'; PaginaFrontend 'Cajero'
    $antes=InstantaneaDocker
    EjecutarDocker @('compose','restart','proveedores-api') | Out-Host
    EsperarPila
    Exigir ((InstantaneaDocker) -ceq $antes) 'Reiniciar la API alteró o duplicó documentos.'
    $cuerpo=@{nombreEmpresa="Verificación Docker $([guid]::NewGuid().ToString('N').Substring(0,8))";numeroEmpresa='71234567';correoReferencia='docker@prueba.example.invalid';esAutogestionado=$false}
    $creado=Api POST '' $admin $cuerpo 201
    $id=$creado.id
    $antesPermisos=InstantaneaDocker
    Api POST '' $cajero $cuerpo 403 | Out-Null
    Api PUT "/$id" $cajero $cuerpo 403 | Out-Null
    Api DELETE "/$id" $cajero $null 403 | Out-Null
    Api PATCH "/$id/reactivar" $cajero $null 403 | Out-Null
    Exigir ((InstantaneaDocker) -ceq $antesPermisos) 'Una solicitud de Cajero alteró documentos.'
    $cuerpo.nombreEmpresa += ' Editado'
    Api PUT "/$id" $admin $cuerpo | Out-Null
    Api DELETE "/$id" $admin $null 204 | Out-Null
    Exigir (-not (Api GET "/$id" $admin).estaActivo) 'La baja no es lógica.'
    Api PATCH "/$id/reactivar" $admin | Out-Null
    $persistencia=InstantaneaDocker
    if ($ProbarCicloDeVida) {
        EjecutarDocker @('compose','restart','mongodb') | Out-Host
        EsperarPila
        Exigir ((InstantaneaDocker) -ceq $persistencia) 'Cambió MongoDB al reiniciar.'
        EjecutarDocker @('compose','down') | Out-Host
        # Intencionadamente SIN -v: los volúmenes se conservan.
        EjecutarDocker @('compose','up','-d') | Out-Host
        EsperarPila
        Exigir ((InstantaneaDocker) -ceq $persistencia) 'Cambió MongoDB tras down/up.'
        $admin=Token 'Administrador'
        Exigir ((Api GET "/$id" $admin).nombreEmpresa -eq $cuerpo.nombreEmpresa) 'La API no recupera el proveedor persistido.'
    }
    # Conserva la evidencia de alta posterior, fuera de Activos; no elimina el documento ni volúmenes.
    Api DELETE "/$id" $admin $null 204 | Out-Null
    $final=Api GET '?estado=Todos&tamanoPagina=100' $admin
    Exigir ($final.total -eq $inicial.total+1) 'El total no conserva el registro de prueba.'
    EsperarPila
    New-Item -ItemType Directory -Force artifacts/docker | Out-Null
    @{totalInicial=$inicial.total;totalFinal=$final.total;seedSinDuplicados=$true;idPrueba=$id;registroPruebaDesactivado=$true;
      permisos=$true;jwt=$true;frontend=$true;persistenciaComprobada=[bool]$ProbarCicloDeVida;
      puertos=@{frontend=$FrontendPort;gateway=$GatewayPort;api=$ApiPort;identity=$IdentityPort}} |
      ConvertTo-Json -Depth 6 | Set-Content artifacts/docker/verificacion-pila.json -Encoding utf8
    'Correcto: JWT, Gateway, roles, frontend, seed, CRUD y persistencia. Registro de prueba conservado como inactivo.'
}
finally {
    foreach($par in $anteriores.GetEnumerator()) { [Environment]::SetEnvironmentVariable($par.Key,$par.Value,'Process') }
}
