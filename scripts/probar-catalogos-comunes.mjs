import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {execFileSync} from 'node:child_process';
import {iniciarModalesCatalogo} from '../Frontend/wwwroot/js/catalogos-modales.mjs';

// Reutiliza los mismos escenarios de regresión con los módulos comunes reales, sin copiar las pruebas.
for(const archivo of ['probar-clientes-demo.mjs','probar-interfaz-clientes.mjs','probar-busqueda-dinamica.mjs']) {
  const source=readFileSync(new URL('./'+archivo,import.meta.url),'utf8')
    .replaceAll('../Frontend/Frontend.Demo/wwwroot/js/','./Frontend/wwwroot/js/');
  process.stdout.write(execFileSync(process.execPath,['--input-type=module','-e',source],{cwd:process.cwd(),encoding:'utf8'}));
}
const validaciones=readFileSync(new URL('./probar-validaciones-frontend.cjs',import.meta.url),'utf8')
  .replace('Frontend/Frontend.Demo/wwwroot/js/site.js','Frontend/wwwroot/js/proveedores-formulario.js');
process.stdout.write(execFileSync(process.execPath,['-e',validaciones],{cwd:process.cwd(),encoding:'utf8'}));
const listeners={};const modal={dataset:{autoOpen:'true'},addEventListener(k,fn){listeners[k]=fn;}};
let abierto=0,destino;
iniciarModalesCatalogo({doc:{querySelectorAll(){return [modal];}},win:{location:{href:'http://comun/Proveedores?handler=Detalle&id=abc&Pagina=2&Estado=Inactivos',assign(url){destino=String(url);}}},modales:{getOrCreateInstance(){return {show(){abierto++;}};}}});
assert.equal(abierto,1);listeners['hidden.bs.modal']();const url=new URL(destino);
assert.equal(url.pathname,'/Proveedores');assert.equal(url.searchParams.get('Estado'),'Inactivos');assert.equal(url.searchParams.get('Pagina'),'2');assert.equal(url.searchParams.has('id'),false);assert.equal(url.searchParams.has('handler'),false);
console.log('Frontend común: modales POST, cierre sin IDs y preservación del listado: correctos.');
