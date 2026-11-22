import assert from "node:assert/strict";
import { iniciarModalesClientes } from "../Frontend/Frontend.Demo/wwwroot/js/clientes-formulario.mjs";
import { urlBusquedaClientes, iniciarBusquedaClientes } from "../Frontend/Frontend.Demo/wwwroot/js/clientes-busqueda.mjs";

const elemento = (extra={}) => ({ listeners:{}, addEventListener(k,fn){this.listeners[k]=fn;}, ...extra });
const form=elemento({querySelectorAll(){return [];}});
const modal=elemento({dataset:{clienteAutoOpen:"true"},querySelector(){return form;}});
const detalle=elemento({dataset:{clienteDetalleAutoOpen:"true"}});
const campos={"[data-cliente-id]":{},"[data-cliente-nombre]":{},"[data-cliente-operacion]":{}};
const confirmForm={action:"http://demo/Clientes?handler=Desactivar&Estado=Activos",querySelector(k){return campos[k];}};
const confirm=elemento({querySelector(){return confirmForm;}});
const doc={querySelector(k){return {"#clienteModal":modal,"#clienteDetalleModal":detalle,"#estadoClienteModal":confirm}[k];}};
const win={location:{href:"http://demo/Clientes?EditarId=2&Nuevo=true&DetalleId=1&Busqueda=abc&Pagina=2"},history:{replaceState(a,b,url){win.location.href=String(url);}}};
const abiertos=[];
iniciarModalesClientes({doc,win,modales:{getOrCreateInstance(m){return {show(){abiertos.push(m);}};}}});
assert.deepEqual(abiertos,[modal,detalle]);
modal.listeners["hidden.bs.modal"]();
let url=new URL(win.location.href);assert.equal(url.searchParams.has("EditarId"),false);assert.equal(url.searchParams.has("Nuevo"),false);assert.equal(url.searchParams.get("Busqueda"),"abc");assert.equal(url.searchParams.get("Pagina"),"2");
detalle.listeners["hidden.bs.modal"]();assert.equal(new URL(win.location.href).searchParams.has("DetalleId"),false);
confirm.listeners["show.bs.modal"]({relatedTarget:{dataset:{clienteAccion:"Reactivar",clienteId:"2",clienteNombre:"<script>texto</script>"}},target:confirm});
assert.equal(new URL(confirmForm.action).searchParams.get("handler"),"Reactivar");assert.equal(campos["[data-cliente-id]"].value,"2");assert.equal(campos["[data-cliente-nombre]"].textContent,"<script>texto</script>");
assert.equal(campos["[data-cliente-nombre]"].innerHTML,undefined);

url=urlBusquedaClientes("http://demo/Clientes?EditarId=4&DetalleId=1&Nuevo=true&handler=Guardar&Pagina=8",new Map([["Estado","Activos"],["OrdenarPor","NitCi"],["Descendente","false"],["TamanoPagina","25"]])," A&B ",1);
assert.equal(url.searchParams.get("Busqueda"),"A&B");assert.equal(url.searchParams.get("Pagina"),"1");assert.equal(url.searchParams.get("OrdenarPor"),"NitCi");assert.equal(url.searchParams.has("handler"),false);assert.equal(url.searchParams.has("DetalleId"),false);
assert.equal(urlBusquedaClientes(url,new Map(),"",1).searchParams.has("Busqueda"),false);

// Eventos del DOM: sin backend, sin descargar filas ni iniciar un navegador.
const input=elemento({value:"",focus(){this.focused=true;}}),clear=elemento({hidden:true}),select=elemento({value:"RazonSocial"});
const alerta={},estado={};
const card=elemento({querySelector(k){return k==="[data-search-error]"?alerta:estado;},setAttribute(){}});
const search=elemento({querySelector(k){return k==="[name=Busqueda]"?input:clear;},closest(){return card;},querySelectorAll(){return [select];}});
const documento={querySelector(){return search;}};
const ventana=elemento({location:{href:"http://demo/Clientes?Pagina=5"}});
const original=globalThis.FormData;
globalThis.FormData=class { *[Symbol.iterator](){yield ["OrdenarPor",select.value];yield ["Estado","Activos"];yield ["TamanoPagina","10"];} };
const requests=[];
try {
 iniciarBusquedaClientes({doc:documento,win:ventana,fetcher(url,options){requests.push({url,options});return new Promise(()=>{});}});
 input.value="Perez";input.listeners.input();assert.equal(clear.hidden,false);
 await new Promise(resolve=>setTimeout(resolve,330));assert.equal(requests.length,1);assert.equal(requests[0].url.searchParams.get("Pagina"),"1");
 select.value="NitCi";select.listeners.change();assert.equal(requests[0].options.signal.aborted,true);assert.equal(requests[1].url.searchParams.get("OrdenarPor"),"NitCi");
 clear.listeners.click();assert.equal(input.value,"");assert.equal(clear.hidden,true);assert.equal(requests[1].options.signal.aborted,true);assert.equal(requests[2].url.searchParams.has("Busqueda"),false);assert.equal(requests[2].url.searchParams.get("Pagina"),"1");
 ventana.listeners.pagehide();assert.equal(requests[2].options.signal.aborted,true);
} finally {globalThis.FormData=original;}
console.log("Clientes: apertura/cierre de modales, confirmación segura, búsqueda al escribir, orden, limpiar, página 1 y abortos: correctos.");
