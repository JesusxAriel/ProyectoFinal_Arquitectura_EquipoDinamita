import assert from 'node:assert/strict';
import vm from 'node:vm';
import {readFileSync} from 'node:fs';
import {urlBusquedaProductos} from '../Frontend/wwwroot/js/productos-busqueda.mjs';

const url=urlBusquedaProductos('http://frontend/Productos?handler=Editar&EditarId=3', [['Estado','Inactivos'],['OrdenarPor','PrecioVenta'],['Descendente','true'],['TamanoPagina','25']], ' SKU ', 2);
for(const [key,value] of [['Estado','Inactivos'],['OrdenarPor','PrecioVenta'],['Descendente','true'],['TamanoPagina','25'],['Pagina','2'],['Busqueda','SKU']]) assert.equal(url.searchParams.get(key),value);
assert.equal(url.searchParams.has('handler'),false);
assert.equal(url.searchParams.has('EditarId'),false);

const element=(extra={})=>({listeners:{},addEventListener(name,fn){this.listeners[name]=fn;},...extra});
const precio=element({value:'15'}),motivo={dataset:{precioOriginal:'15'}};
const fields={'[data-delete-id]':{},'[data-delete-name]':{}};
const eliminar=element({querySelector(selector){return fields[selector];}});
const modal=element({querySelector(){return null;}});
let open=false;
const doc={
  addEventListener(name,fn){if(name==='DOMContentLoaded')fn();},
  getElementById(){return eliminar;},
  querySelector(selector){
    if(selector==='[name="Form.PrecioVenta"]')return precio;
    if(selector==='[name="Form.MotivoCambioPrecio"]')return motivo;
    if(selector==='.catalogo .modal.show')return open ? modal : null;
    return null;
  },
  querySelectorAll(selector){return selector==='.catalogo .modal'||selector==='.modal' ? [modal] : [];}
};
vm.runInNewContext(readFileSync(new URL('../Frontend/wwwroot/js/productos-formulario.js',import.meta.url),'utf8'),{document:doc,window:{addEventListener(){}}});
assert.equal(motivo.required,false);
precio.value='16';precio.listeners.input();assert.equal(motivo.required,true);
precio.value='15.00';precio.listeners.input();assert.equal(motivo.required,false);
const name='<img src=x onerror=alert(1)>';
eliminar.listeners['show.bs.modal']({relatedTarget:{dataset:{id:'17',nombre:name}}});
assert.equal(fields['[data-delete-id]'].value,'17');
assert.equal(fields['[data-delete-name]'].textContent,name);
assert.equal(fields['[data-delete-name]'].innerHTML,undefined);
let prevented=false;open=true;
modal.listeners['show.bs.modal']({preventDefault(){prevented=true;}});assert.equal(prevented,true);
console.log('Productos: filtros, motivo condicional, confirmacion segura y modal unico correctos (DOM simulado).');
import {iniciarModalesCatalogo} from '../Frontend/wwwroot/js/catalogos-modales.mjs';
const creation=element({dataset:{autoOpen:'true'}});
let shown=0, destination;
iniciarModalesCatalogo({
  doc:{querySelectorAll(){return [creation];}},
  win:{location:{href:'http://frontend/Productos?handler=Guardar&Estado=Activos&Busqueda=SKU',assign(url){destination=String(url);}}},
  modales:{getOrCreateInstance(){return {show(){shown++;}};}}
});
assert.equal(shown,1);
creation.listeners['hidden.bs.modal']();
const cleanUrl=new URL(destination);
assert.equal(cleanUrl.searchParams.has('handler'),false);
assert.equal(cleanUrl.searchParams.get('Estado'),'Activos');
assert.equal(cleanUrl.searchParams.get('Busqueda'),'SKU');
console.log('Alta: cerrar/cancelar vuelve al GET limpio conservando filtros (DOM simulado).');
