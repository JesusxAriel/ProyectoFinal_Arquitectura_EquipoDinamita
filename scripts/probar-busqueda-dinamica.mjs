import assert from 'node:assert/strict';
import { crearBusquedaDinamica } from '../Frontend/Frontend.Demo/wwwroot/js/proveedores-busqueda.mjs';

const pendientes = [], mostrados = [], errores = [], ocupados = [];
let reloj = 0, secuencia = 0;
const timers = new Map();
const buscar = crearBusquedaDinamica({
  esperar(fn, ms) { const id = ++secuencia; timers.set(id, { fn, tiempo: reloj + ms }); return id; },
  cancelarEspera(id) { timers.delete(id); },
  consultar(texto, pagina, signal) {
    return new Promise((resolve, reject) => pendientes.push({ texto, pagina, signal, resolve, reject }));
  },
  mostrar(r) { mostrados.push(r); }, error() { errores.push(true); }, ocupado(v) { ocupados.push(v); }
});
async function avanzar(ms) {
  reloj += ms;
  for (const [id,t] of [...timers]) if (t.tiempo <= reloj) { timers.delete(id); t.fn(); }
  await Promise.resolve();
}
buscar.programar('c'); await avanzar(200); buscar.programar('co'); await avanzar(200);
assert.equal(pendientes.length,0,'No consulta durante el debounce');
buscar.programar('com'); await avanzar(299); assert.equal(pendientes.length,0);
await avanzar(1); assert.equal(pendientes.length,1);
assert.equal(pendientes[0].texto,'com'); assert.equal(pendientes[0].pagina,1);
buscar.programar('come');
assert.equal(pendientes[0].signal.aborted,true,'Aborta inmediatamente la consulta anterior');
pendientes[0].resolve('respuesta vieja'); await avanzar(0);
assert.deepEqual(mostrados,[],'No muestra respuestas viejas durante el debounce');
await avanzar(300);
const limpiar = buscar.buscar(''); // Vaciar inicia inmediatamente la página 1.
assert.equal(pendientes[1].signal.aborted,true);
assert.equal(pendientes[2].texto,''); assert.equal(pendientes[2].pagina,1);
pendientes[2].resolve('todos de la pestaña'); await limpiar;
pendientes[1].resolve('búsqueda pendiente'); await avanzar(0);
assert.deepEqual(mostrados,['todos de la pestaña']);
buscar.programar('cancelada'); const segundaLimpieza = buscar.buscar('');
await avanzar(300); assert.equal(pendientes.length,4,'Limpiar cancela también el temporizador');
pendientes[3].resolve('todos nuevamente'); await segundaLimpieza;
const pagina = buscar.buscar('com',3);
assert.equal(pendientes[4].pagina,3,'Navegación conserva la página solicitada');
pendientes[4].resolve('página 3'); await pagina;
const fallo = buscar.buscar('error'); pendientes[5].reject(new Error('fallo')); await fallo;
assert.equal(errores.length,1); assert.equal(ocupados.at(-1),false);
buscar.programar('abandono'); buscar.cancelar(); await avanzar(300);
assert.equal(pendientes.length,6);
console.log('Búsqueda dinámica: debounce 300 ms, página 1, limpiar, abortos, respuestas obsoletas, paginación y errores: correctos.');
