// Prueba el JavaScript real en un DOM mínimo; sin navegador ni paquetes adicionales.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const codigo = fs.readFileSync('Frontend/Frontend.Demo/wwwroot/js/site.js', 'utf8');
function preparar() {
  const listeners = [];
  const campos = ['NombreEmpresa', 'NumeroEmpresa', 'CorreoReferencia'].map((nombre, i) => ({
    name: 'Form.' + nombre, value: ['Comercial Pérez & Hijos', '71234567', 'ventas+cbba@empresa.com.bo'][i],
    error: '', mensaje: {textContent: ''}, clases: new Set(), atributos: {},
    classList: {toggle(clase, activo) { activo ? campos[i].clases.add(clase) : campos[i].clases.delete(clase); }},
    setCustomValidity(error) { this.error = error; },
    setAttribute(k, v) { this.atributos[k] = v; },
    closest() { return {querySelector: () => this.mensaje}; },
    addEventListener(evento, fn) { this[evento] = fn; },
    focus() { this.enfocado = true; }
  }));
  const boton = {disabled: false};
  const carga = {hidden: true};
  const form = {dataset: {}, noValidate: false,
    querySelectorAll(selector) { return selector === 'button[type=submit]' ? [boton] : campos; },
    querySelector() { return campos.find(c => c.error); },
    checkValidity() { return campos.every(c => !c.error); },
    addEventListener(evento, fn) { if (evento === 'submit') listeners.push(fn); }
  };
  vm.runInNewContext(codigo, {
    document: {
      addEventListener(evento, fn) { if (evento === 'DOMContentLoaded') fn(); },
      querySelector: () => form,
      querySelectorAll(selector) { return selector === 'form[data-loading]' ? [form] : []; },
      getElementById(id) { return id === 'page-loading' ? carga : null; }
    }, window: {addEventListener() {}}
  });
  return {campos, form, boton, carga, enviar() {
    const evento = {defaultPrevented: false, preventDefault() { this.defaultPrevented = true; }};
    listeners.forEach(fn => fn(evento));
    return evento;
  }};
}
let comprobaciones = 0;
function caso(campo, valor, valido) {
  const p = preparar(); p.campos[campo].value = valor;
  const evento = p.enviar();
  assert.equal(!evento.defaultPrevented, valido, `${p.campos[campo].name}: ${JSON.stringify(valor)}`);
  assert.equal(p.campos[campo].clases.has('is-invalid'), !valido);
  assert.equal(p.campos[campo].atributos['aria-invalid'], String(!valido));
  if (!valido) {
    assert.equal(p.campos[campo].value, valor, 'Conserva la entrada rechazada');
    assert.ok(p.campos[campo].mensaje.textContent);
    assert.equal(p.boton.disabled, false);
    assert.equal(p.carga.hidden, true);
  }
  comprobaciones++;
}
for (const valor of ['44112233','61234567','71234567',' 71234567 ']) caso(1,valor,true);
for (const valor of ['+59144112233','+59161234567','+59171234567','59171234567','12345678','51234567','81234567','7123456','712345678','abc12345','7123abcd','+59112345678','+5917123456','71234567<script>','7123 4567','71234567\n','\t71234567','７１２３４５６７']) caso(1,valor,false);
for (const valor of ['PIL Andina S.A.','EMBOL S.A. (Coca-Cola)','Distribuidora 24/7',"O'Reilly",'Comercial Pérez & Hijos','商店 24','Script Comercial','Cafe\u0301','Marca® & Servicios™','Comercial Ⅳ']) caso(0,valor,true);
for (const valor of ['', '   ','@@@@@@','<script>alert(1)</script>','Empresa\tSur','Empresa\u200bSur','Empresa\u202eSur','Empresa\ufe0f','javascript:alert(1)','alert(1)']) caso(0,valor,false);
for (const valor of ['', '  ','ventas@empresa.com','ventas+cbba@empresa.com.bo','contacto@subdominio.empresa.com']) caso(2,valor,true);
for (const valor of ['ventas@@empresa.com','ventas empresa@gmail.com','ventas@empresa','ventas@empresa..com','@empresa.com','ventas@','<script>alert(1)</script>','ventas..cbba@empresa.com','.ventas@empresa.com','ventas@-empresa.com','ventas@empresa-.com','ventas@empresa.c','ventas@empresa.com\n']) caso(2,valor,false);
const doble = preparar();
assert.equal(doble.enviar().defaultPrevented, false);
assert.equal(doble.boton.disabled, true);
assert.equal(doble.enviar().defaultPrevented, true);
const corregido = preparar(); corregido.campos[1].value = 'adaded'; corregido.enviar();
corregido.campos[1].value = '71234567'; corregido.campos[1].input();
assert.equal(corregido.campos[1].error, '');
assert.equal(corregido.campos[1].mensaje.textContent, '');
console.log(`${comprobaciones} casos de campos, corrección de errores y bloqueo de doble envío: correctos.`);
