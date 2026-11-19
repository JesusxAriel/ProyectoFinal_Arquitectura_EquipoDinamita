#!/usr/bin/env bash
# DEMO TEMPORAL: prueba de humo vía Gateway demo; no usar con el Gateway común. Requiere los cuatro servicios, curl y python3.
# Realiza escrituras: usar exclusivamente una API conectada a una base temporal de pruebas.
set -euo pipefail
GW="${GATEWAY_URL:-http://localhost:15000}"
tok() { curl -fsS -X POST "$GW/dev-auth/token" -H 'Content-Type: application/json' -d "{\"perfil\":\"$1\"}" | python3 -c 'import sys,json;print(json.load(sys.stdin)["access_token"])'; }
ADMIN=$(tok Administrador); CAJERO=$(tok Cajero)
code() { curl -s -o /dev/null -w '%{http_code}' "$@"; }
echo "Sin token (401):      $(code "$GW/api/proveedores")"
echo "Cajero lista (200):   $(code -H "Authorization: Bearer $CAJERO" "$GW/api/proveedores")"
echo "Cajero crea (403):    $(code -X POST -H "Authorization: Bearer $CAJERO" -H 'Content-Type: application/json' -d '{"nombreEmpresa":"X","numeroEmpresa":"1"}' "$GW/api/proveedores")"
NOMBRE="Smoke $(date +%s)"
RESP=$(curl -s -X POST -H "Authorization: Bearer $ADMIN" -H 'Content-Type: application/json' -d "{\"nombreEmpresa\":\"$NOMBRE\",\"numeroEmpresa\":\"71234567\"}" "$GW/api/proveedores")
ID=$(echo "$RESP" | python3 -c 'import sys,json;print(json.load(sys.stdin)["id"])')
echo "Admin crea (id):      $ID"
echo "Duplicado (409):      $(code -X POST -H "Authorization: Bearer $ADMIN" -H 'Content-Type: application/json' -d "{\"nombreEmpresa\":\"$NOMBRE\",\"numeroEmpresa\":\"71234567\"}" "$GW/api/proveedores")"
echo "Desactivar (204):     $(code -X DELETE -H "Authorization: Bearer $ADMIN" "$GW/api/proveedores/$ID")"
echo "Reactivar (200):      $(code -X PATCH -H "Authorization: Bearer $ADMIN" "$GW/api/proveedores/$ID/reactivar")"
