import test from 'node:test';
import assert from 'node:assert/strict';
import { limpiarCampo, iniciarLimpiezaDeValidacion } from '../../Frontend/wwwroot/js/limpiar-validacion.mjs';
import { iniciarAvisosTemporales } from '../../Frontend/wwwroot/js/avisos-temporales.mjs';

function clases(...iniciales) {
  const conjunto = new Set(iniciales);
  return {
    add: c => conjunto.add(c),
    remove: c => conjunto.delete(c),
    contains: c => conjunto.has(c)
  };
}

function campo(nombre, ...iniciales) {
  const atributos = new Map([['aria-invalid', 'true']]);
  return {
    name: nombre,
    classList: clases(...iniciales),
    removeAttribute: n => atributos.delete(n),
    tieneAtributo: n => atributos.has(n)
  };
}

function mensaje(nombre, texto) {
  return { dataset: { valmsgFor: nombre }, textContent: texto, classList: clases('invalid-feedback', 'field-validation-error') };
}

function formulario(mensajes) {
  const oyentes = {};
  return {
    querySelectorAll: selector => (selector === '[data-valmsg-for]' ? mensajes : []),
    addEventListener: (evento, funcion) => { oyentes[evento] = funcion; },
    disparar: (evento, objetivo) => oyentes[evento]({ target: objetivo })
  };
}

test('al escribir se quita el estilo de error y el mensaje solo de ese campo', () => {
  const nombres = campo('Nombres', 'form-control', 'is-invalid');
  const propio = mensaje('Nombres', 'El campo Nombres es obligatorio.');
  const ajeno = mensaje('ApellidoPaterno', 'El campo Apellido paterno es obligatorio.');

  limpiarCampo(formulario([propio, ajeno]), nombres);

  assert.equal(nombres.classList.contains('is-invalid'), false);
  assert.equal(nombres.classList.contains('form-control'), true);
  assert.equal(nombres.tieneAtributo('aria-invalid'), false);
  assert.equal(propio.textContent, '');
  assert.equal(propio.classList.contains('field-validation-error'), false);
  assert.equal(propio.classList.contains('field-validation-valid'), true);
  assert.equal(ajeno.textContent, 'El campo Apellido paterno es obligatorio.');
  assert.equal(ajeno.classList.contains('field-validation-error'), true);
});

test('un objetivo sin nombre no hace nada', () => {
  const propio = mensaje('Nombres', 'Error');

  limpiarCampo(formulario([propio]), { classList: clases('is-invalid') });
  limpiarCampo(formulario([propio]), null);

  assert.equal(propio.textContent, 'Error');
});

test('los eventos input y change del formulario limpian el campo, también el select de rol', () => {
  const rol = campo('RolId', 'form-select', 'is-invalid');
  const texto = mensaje('RolId', 'Seleccione un rol válido.');
  const envoltorio = formulario([texto]);
  iniciarLimpiezaDeValidacion({ doc: { querySelectorAll: () => [envoltorio] } });

  envoltorio.disparar('change', rol);

  assert.equal(rol.classList.contains('is-invalid'), false);
  assert.equal(texto.textContent, '');

  const correo = campo('Correo', 'form-control', 'is-invalid');
  const textoCorreo = mensaje('Correo', 'El campo Correo no es una dirección de correo válida.');
  const otro = formulario([textoCorreo]);
  iniciarLimpiezaDeValidacion({ doc: { querySelectorAll: () => [otro] } });

  otro.disparar('input', correo);

  assert.equal(correo.classList.contains('is-invalid'), false);
  assert.equal(textoCorreo.textContent, '');
});

function aviso(milisegundos, conectado = true) {
  return { dataset: { avisoMs: milisegundos }, isConnected: conectado };
}

function entorno(avisos) {
  const programados = [];
  const cerrados = [];
  return {
    programados,
    cerrados,
    opciones: {
      doc: { querySelectorAll: selector => (selector === '[data-aviso-ms]' ? avisos : []) },
      alertas: { getOrCreateInstance: elemento => ({ close: () => cerrados.push(elemento) }) },
      temporizador: (funcion, milisegundos) => programados.push({ funcion, milisegundos })
    }
  };
}

test('cada aviso se programa con su propio tiempo y se cierra con el fundido de Bootstrap', () => {
  const exito = aviso('6000');
  const error = aviso('10000');
  const contexto = entorno([exito, error]);

  iniciarAvisosTemporales(contexto.opciones);

  assert.deepEqual(contexto.programados.map(p => p.milisegundos), [6000, 10000]);
  contexto.programados[0].funcion();
  assert.deepEqual(contexto.cerrados, [exito]);
  contexto.programados[1].funcion();
  assert.deepEqual(contexto.cerrados, [exito, error]);
});

test('un aviso que el usuario ya cerró no se vuelve a cerrar', () => {
  const cerrado = aviso('6000', false);
  const contexto = entorno([cerrado]);

  iniciarAvisosTemporales(contexto.opciones);
  contexto.programados[0].funcion();

  assert.deepEqual(contexto.cerrados, []);
});

test('un tiempo inválido no programa nada', () => {
  const contexto = entorno([aviso('abc'), aviso('0'), aviso('-5')]);

  iniciarAvisosTemporales(contexto.opciones);

  assert.equal(contexto.programados.length, 0);
});
