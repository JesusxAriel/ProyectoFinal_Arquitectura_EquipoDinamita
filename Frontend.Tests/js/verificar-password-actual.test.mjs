import test from 'node:test';
import assert from 'node:assert/strict';
import {
  crearVerificador,
  solicitudDeVerificacion,
  iniciarVerificacionDePasswordActual
} from '../../Frontend/wwwroot/js/verificar-password-actual.mjs';

function controlable() {
  const pendientes = [];
  return {
    esperar: () => new Promise(resolver => pendientes.push(resolver)),
    liberar: () => pendientes.splice(0).forEach(resolver => resolver()),
    cantidad: () => pendientes.length
  };
}

function creadorDeControladores() {
  const creados = [];
  return {
    crear: () => {
      const controlador = { signal: { abortada: false }, abort() { this.signal.abortada = true; } };
      creados.push(controlador);
      return controlador;
    },
    creados
  };
}

async function vaciar() {
  for (let i = 0; i < 5; i += 1) await Promise.resolve();
}

test('un valor vacío vuelve a sin verificar y no consulta al servidor', async () => {
  const llamadas = [];
  const estados = [];
  const v = crearVerificador({
    solicitar: async valor => { llamadas.push(valor); return { estado: 'correcta' }; },
    aplicar: (estado, mensaje) => estados.push([estado, mensaje]),
    esperar: async () => {}
  });

  await v.verificar('');

  assert.deepEqual(llamadas, []);
  assert.deepEqual(estados, [['sin-verificar', '']]);
});

test('una contraseña correcta pasa por verificando y termina en correcta', async () => {
  const estados = [];
  const v = crearVerificador({
    solicitar: async () => ({ estado: 'correcta', mensaje: 'Contraseña actual correcta.' }),
    aplicar: (estado, mensaje) => estados.push([estado, mensaje]),
    esperar: async () => {}
  });

  await v.verificar('Actual#123');

  assert.deepEqual(estados, [['verificando', ''], ['correcta', 'Contraseña actual correcta.']]);
});

test('una contraseña incorrecta termina en incorrecta con el mensaje del servidor', async () => {
  const estados = [];
  const v = crearVerificador({
    solicitar: async () => ({ estado: 'incorrecta', mensaje: 'La contraseña actual no es correcta.' }),
    aplicar: (estado, mensaje) => estados.push([estado, mensaje]),
    esperar: async () => {}
  });

  await v.verificar('Mal#1234');

  assert.deepEqual(estados.at(-1), ['incorrecta', 'La contraseña actual no es correcta.']);
});

test('un fallo de red deja el estado en no disponible para no bloquear al usuario', async () => {
  const estados = [];
  const v = crearVerificador({
    solicitar: async () => { throw new Error('red'); },
    aplicar: estado => estados.push(estado),
    esperar: async () => {}
  });

  await v.verificar('Actual#123');

  assert.deepEqual(estados, ['verificando', 'no-disponible']);
});

test('el debounce descarta una verificación si llega otra antes de terminar la espera', async () => {
  const espera = controlable();
  const llamadas = [];
  const v = crearVerificador({
    solicitar: async valor => { llamadas.push(valor); return { estado: 'correcta' }; },
    aplicar: () => {},
    esperar: espera.esperar
  });

  const primera = v.verificar('Primera#1');
  const segunda = v.verificar('Segunda#2');
  espera.liberar();
  await Promise.all([primera, segunda]);

  assert.deepEqual(llamadas, ['Segunda#2']);
});

test('una petición anterior se cancela cuando llega una nueva', async () => {
  const controladores = creadorDeControladores();
  const resoluciones = [];
  const v = crearVerificador({
    solicitar: (valor, signal) => new Promise(resolver => resoluciones.push({ valor, signal, resolver })),
    aplicar: () => {},
    esperar: async () => {},
    crearControlador: controladores.crear
  });

  const primera = v.verificar('Primera#1');
  await vaciar();
  const segunda = v.verificar('Segunda#2');
  await vaciar();

  assert.equal(controladores.creados[0].signal.abortada, true);
  assert.equal(controladores.creados[1].signal.abortada, false);
  resoluciones[0].resolver({ estado: 'correcta' });
  resoluciones[1].resolver({ estado: 'incorrecta', mensaje: 'x' });
  await Promise.all([primera, segunda]);
});

test('la respuesta de una petición cancelada no cambia el estado', async () => {
  const estados = [];
  const resoluciones = [];
  const v = crearVerificador({
    solicitar: valor => new Promise(resolver => resoluciones.push({ valor, resolver })),
    aplicar: (estado, mensaje) => estados.push([estado, mensaje]),
    esperar: async () => {}
  });

  const primera = v.verificar('Primera#1');
  await vaciar();
  const segunda = v.verificar('Segunda#2');
  await vaciar();
  resoluciones[0].resolver({ estado: 'correcta', mensaje: 'vieja' });
  resoluciones[1].resolver({ estado: 'incorrecta', mensaje: 'nueva' });
  await Promise.all([primera, segunda]);

  assert.deepEqual(estados.at(-1), ['incorrecta', 'nueva']);
  assert.equal(estados.some(([, mensaje]) => mensaje === 'vieja'), false);
});

test('un AbortError no se trata como servicio no disponible', async () => {
  const estados = [];
  const v = crearVerificador({
    solicitar: async () => { const error = new Error('abortada'); error.name = 'AbortError'; throw error; },
    aplicar: estado => estados.push(estado),
    esperar: async () => {}
  });

  await v.verificar('Actual#123');

  assert.deepEqual(estados, ['verificando']);
});

test('cancelar impide que una verificación pendiente aplique su resultado', async () => {
  const espera = controlable();
  const estados = [];
  const v = crearVerificador({
    solicitar: async () => ({ estado: 'correcta' }),
    aplicar: estado => estados.push(estado),
    esperar: espera.esperar
  });

  const pendiente = v.verificar('Actual#123');
  v.cancelar();
  espera.liberar();
  await pendiente;

  assert.deepEqual(estados, []);
});

function formularioConToken(token = 'token-123') {
  return { querySelector: selector => (selector === 'input[name="__RequestVerificationToken"]' ? { value: token } : null) };
}

test('la solicitud envía la contraseña y el token antifalsificación por POST', async () => {
  let recibido;
  const fetchFn = async (url, opciones) => {
    recibido = { url, opciones };
    return { ok: true, json: async () => ({ estado: 'correcta', mensaje: 'ok' }) };
  };

  const respuesta = await solicitudDeVerificacion({
    url: '/Auth/CambiarPassword?handler=VerificarActual',
    formulario: formularioConToken(),
    fetchFn
  })('Actual#123', 'senal');

  assert.equal(recibido.url, '/Auth/CambiarPassword?handler=VerificarActual');
  assert.equal(recibido.opciones.method, 'POST');
  assert.equal(recibido.opciones.signal, 'senal');
  assert.equal(recibido.opciones.body.get('PasswordActual'), 'Actual#123');
  assert.equal(recibido.opciones.body.get('__RequestVerificationToken'), 'token-123');
  assert.equal(respuesta.estado, 'correcta');
});

test('un 503 o una respuesta inesperada se trata como fallo', async () => {
  const sin503 = solicitudDeVerificacion({
    url: '/x',
    formulario: formularioConToken(),
    fetchFn: async () => ({ ok: false, json: async () => ({ estado: 'no-disponible' }) })
  });
  const invalida = solicitudDeVerificacion({
    url: '/x',
    formulario: formularioConToken(),
    fetchFn: async () => ({ ok: true, json: async () => ({ estado: 'otra-cosa' }) })
  });

  await assert.rejects(() => sin503('a', null));
  await assert.rejects(() => invalida('a', null));
});

function clases() {
  const conjunto = new Set();
  return {
    add: c => conjunto.add(c),
    remove: c => conjunto.delete(c),
    contains: c => conjunto.has(c)
  };
}

function campo(nombre, valor = '') {
  const atributos = new Map();
  const oyentes = {};
  return {
    name: nombre,
    value: valor,
    disabled: false,
    classList: clases(),
    dataset: {},
    setAttribute: (n, v) => atributos.set(n, v),
    removeAttribute: n => atributos.delete(n),
    tieneAtributo: n => atributos.has(n),
    addEventListener: (evento, funcion) => { oyentes[evento] = funcion; },
    disparar: (evento, extra = {}) => oyentes[evento]({ target: this, ...extra }),
    oyentes,
    enfocado: false,
    focus() { this.enfocado = true; }
  };
}

function pantalla(valorActual = '') {
  const actual = campo('PasswordActual', valorActual);
  actual.dataset = {
    verificarActualUrl: '/Auth/CambiarPassword?handler=VerificarActual',
    textoVerificando: 'Verificando…',
    textoCorrecta: 'Contraseña actual correcta.'
  };
  const nueva = campo('PasswordNueva');
  const repetir = campo('RepetirPasswordNueva');
  const error = { dataset: { valmsgFor: 'PasswordActual' }, textContent: '', classList: clases() };
  const estado = { textContent: '', classList: clases() };
  const oyentes = {};
  const formulario = {
    querySelector: selector => {
      const nombre = /\[name="([^"]+)"\]/.exec(selector)?.[1];
      if (selector === 'input[name="__RequestVerificationToken"]') return { value: 'tok' };
      return [actual, nueva, repetir].find(c => c.name === nombre) ?? null;
    },
    querySelectorAll: selector => (selector === '[data-valmsg-for]' ? [error] : []),
    addEventListener: (evento, funcion) => { oyentes[evento] = funcion; },
    disparar: (evento, extra = {}) => {
      const e = { prevented: false, preventDefault() { this.prevented = true; }, ...extra };
      oyentes[evento](e);
      return e;
    }
  };
  actual.form = formulario;
  const doc = {
    querySelector: selector => (selector === '[data-verificar-actual-url]' ? actual : null),
    getElementById: id => (id === 'estado-password-actual' ? estado : null)
  };
  return { actual, nueva, repetir, error, estado, formulario, doc };
}

function respuestas(...cola) {
  const llamadas = [];
  const fetchFn = async (url, opciones) => {
    llamadas.push(opciones.body.get('PasswordActual'));
    const siguiente = cola.shift();
    if (siguiente instanceof Error) throw siguiente;
    return siguiente;
  };
  return { fetchFn, llamadas };
}

const correcta = { ok: true, json: async () => ({ estado: 'correcta', mensaje: 'Contraseña actual correcta.' }) };
const incorrecta = { ok: true, json: async () => ({ estado: 'incorrecta', mensaje: 'La contraseña actual no es correcta.' }) };
const sinServicio = { ok: false, json: async () => ({ estado: 'no-disponible' }) };

async function esperarVerificacion() {
  await new Promise(resolver => setTimeout(resolver, 380));
}

test('al cargar, Nueva contraseña y Repetir quedan deshabilitadas', () => {
  const p = pantalla();

  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: respuestas().fetchFn });

  assert.equal(p.nueva.disabled, true);
  assert.equal(p.repetir.disabled, true);
});

test('sin el campo de contraseña actual no hace nada', () => {
  assert.equal(iniciarVerificacionDePasswordActual({ doc: { querySelector: () => null } }), null);
});

test('al salir del campo con una contraseña correcta se habilitan los demás campos', async () => {
  const p = pantalla('Actual#123');
  const r = respuestas(correcta);
  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: r.fetchFn });

  await esperarVerificacion();

  assert.deepEqual(r.llamadas, ['Actual#123']);
  assert.equal(p.nueva.disabled, false);
  assert.equal(p.repetir.disabled, false);
  assert.equal(p.estado.textContent, 'Contraseña actual correcta.');
  assert.equal(p.estado.classList.contains('text-success'), true);
});

test('mientras verifica muestra Verificando… y mantiene deshabilitados los campos', async () => {
  const p = pantalla();
  let liberar;
  const fetchFn = () => new Promise(resolver => { liberar = () => resolver(correcta); });
  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn });
  p.actual.value = 'Actual#123';

  p.formulario.disparar('focusout', { target: p.actual });
  await esperarVerificacion();

  assert.equal(p.estado.textContent, 'Verificando…');
  assert.equal(p.nueva.disabled, true);
  liberar();
  await vaciar();
  assert.equal(p.nueva.disabled, false);
});

test('una contraseña incorrecta muestra el mensaje bajo el campo y mantiene deshabilitados los demás', async () => {
  const p = pantalla('Mal#1234');
  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: respuestas(incorrecta).fetchFn });

  await esperarVerificacion();

  assert.equal(p.error.textContent, 'La contraseña actual no es correcta.');
  assert.equal(p.actual.classList.contains('is-invalid'), true);
  assert.equal(p.nueva.disabled, true);
  assert.equal(p.repetir.disabled, true);
});

test('al corregir la contraseña actual se vuelve a verificar y se habilitan los campos', async () => {
  const p = pantalla('Mal#1234');
  const r = respuestas(incorrecta, correcta);
  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: r.fetchFn });
  await esperarVerificacion();

  p.actual.oyentes.input();
  p.actual.value = 'Actual#123';
  p.formulario.disparar('focusout', { target: p.actual });
  await esperarVerificacion();

  assert.deepEqual(r.llamadas, ['Mal#1234', 'Actual#123']);
  assert.equal(p.nueva.disabled, false);
  assert.equal(p.actual.classList.contains('is-invalid'), false);
});

test('al escribir en la contraseña actual los demás campos vuelven a deshabilitarse', async () => {
  const p = pantalla('Actual#123');
  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: respuestas(correcta).fetchFn });
  await esperarVerificacion();
  assert.equal(p.nueva.disabled, false);

  p.actual.oyentes.input();

  assert.equal(p.nueva.disabled, true);
  assert.equal(p.estado.textContent, '');
});

test('un fallo de red o un 503 habilita los campos para que mande la validación del servidor', async () => {
  for (const respuesta of [sinServicio, new Error('red')]) {
    const p = pantalla('Actual#123');
    iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: respuestas(respuesta).fetchFn });

    await esperarVerificacion();

    assert.equal(p.nueva.disabled, false);
    assert.equal(p.repetir.disabled, false);
    assert.equal(p.error.textContent, '');
  }
});

test('Enter verifica en lugar de enviar el formulario y lleva el foco a Nueva contraseña', async () => {
  const p = pantalla('Actual#123');
  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: respuestas(correcta).fetchFn });
  const evento = { key: 'Enter', prevented: false, preventDefault() { this.prevented = true; } };

  p.actual.oyentes.keydown(evento);
  await esperarVerificacion();

  assert.equal(evento.prevented, true);
  assert.equal(p.nueva.enfocado, true);
});

test('otras teclas no disparan la verificación', () => {
  const p = pantalla('Actual#123');
  const r = respuestas(correcta);
  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: r.fetchFn });
  const evento = { key: 'a', prevented: false, preventDefault() { this.prevented = true; } };

  p.actual.oyentes.keydown(evento);

  assert.equal(evento.prevented, false);
  assert.deepEqual(r.llamadas, []);
});

test('enviar sin haber verificado cancela el envío y verifica primero', async () => {
  const p = pantalla('Actual#123');
  p.actual.value = 'Actual#123';
  const r = respuestas(correcta);
  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: r.fetchFn });
  p.actual.value = 'Otra#1234';
  p.actual.oyentes.input();

  const evento = p.formulario.disparar('submit');
  await esperarVerificacion();

  assert.equal(evento.prevented, true);
  assert.deepEqual(r.llamadas.at(-1), 'Otra#1234');
});

test('enviar con la contraseña ya verificada no se interrumpe', async () => {
  const p = pantalla('Actual#123');
  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: respuestas(correcta).fetchFn });
  await esperarVerificacion();

  const evento = p.formulario.disparar('submit');

  assert.equal(evento.prevented, false);
});

test('volver a salir del campo con el mismo valor incorrecto vuelve a mostrar el error sin otra consulta', async () => {
  const p = pantalla('Mal#1234');
  const r = respuestas(incorrecta);
  iniciarVerificacionDePasswordActual({ doc: p.doc, fetchFn: r.fetchFn });
  await esperarVerificacion();
  p.error.textContent = '';
  p.actual.classList.remove('is-invalid');

  p.formulario.disparar('focusout', { target: p.actual });

  assert.equal(r.llamadas.length, 1);
  assert.equal(p.error.textContent, 'La contraseña actual no es correcta.');
});
