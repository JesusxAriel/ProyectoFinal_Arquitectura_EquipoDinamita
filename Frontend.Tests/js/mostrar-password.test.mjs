import test from 'node:test';
import assert from 'node:assert/strict';
import { alternarVisibilidad, agregarBotonDeVisibilidad, iniciarMostrarPassword } from '../../Frontend/wwwroot/js/mostrar-password.mjs';

function elemento(etiqueta) {
  const atributos = new Map();
  const oyentes = {};
  return {
    etiqueta,
    className: '',
    type: '',
    hijos: [],
    parentNode: null,
    setAttribute: (n, v) => atributos.set(n, v),
    getAttribute: n => atributos.get(n),
    appendChild(hijo) {
      if (hijo.parentNode) hijo.parentNode.hijos.splice(hijo.parentNode.hijos.indexOf(hijo), 1);
      hijo.parentNode = this;
      this.hijos.push(hijo);
      return hijo;
    },
    insertBefore(nuevo, referencia) {
      nuevo.parentNode = this;
      this.hijos.splice(this.hijos.indexOf(referencia), 0, nuevo);
    },
    addEventListener: (evento, funcion) => { oyentes[evento] = funcion; },
    disparar: evento => oyentes[evento]()
  };
}

function documentoCon(campos) {
  const padre = elemento('div');
  campos.forEach(c => { c.parentNode = padre; padre.hijos.push(c); });
  return {
    padre,
    createElement: elemento,
    querySelectorAll: selector => (selector === 'input[type="password"]' ? campos : [])
  };
}

function campoPassword() {
  const c = elemento('input');
  c.type = 'password';
  return c;
}

test('el botón nace oculto, con aria-label y aria-pressed en falso', () => {
  const campo = campoPassword();
  const doc = documentoCon([campo]);

  const boton = agregarBotonDeVisibilidad(campo, doc);

  assert.equal(boton.getAttribute('aria-label'), 'Mostrar contraseña');
  assert.equal(boton.getAttribute('aria-pressed'), 'false');
  assert.equal(boton.type, 'button');
  assert.equal(boton.hijos[0].className, 'bi bi-eye');
});

test('el campo queda dentro de una envoltura junto con su botón', () => {
  const campo = campoPassword();
  const doc = documentoCon([campo]);

  const boton = agregarBotonDeVisibilidad(campo, doc);

  const envoltura = doc.padre.hijos[0];
  assert.equal(envoltura.className, 'campo-password');
  assert.deepEqual(envoltura.hijos, [campo, boton]);
});

test('al pulsar el botón se muestra la contraseña y se actualiza su estado accesible', () => {
  const campo = campoPassword();
  const boton = agregarBotonDeVisibilidad(campo, documentoCon([campo]));

  boton.disparar('click');

  assert.equal(campo.type, 'text');
  assert.equal(boton.getAttribute('aria-pressed'), 'true');
  assert.equal(boton.getAttribute('aria-label'), 'Ocultar contraseña');
  assert.equal(boton.hijos[0].className, 'bi bi-eye-slash');
});

test('al pulsar de nuevo se vuelve a ocultar', () => {
  const campo = campoPassword();
  const boton = agregarBotonDeVisibilidad(campo, documentoCon([campo]));

  boton.disparar('click');
  boton.disparar('click');

  assert.equal(campo.type, 'password');
  assert.equal(boton.getAttribute('aria-pressed'), 'false');
  assert.equal(boton.getAttribute('aria-label'), 'Mostrar contraseña');
  assert.equal(boton.hijos[0].className, 'bi bi-eye');
});

test('cada campo conserva su propio estado aunque se cambie de casilla', () => {
  const primero = campoPassword();
  const segundo = campoPassword();
  const doc = documentoCon([primero, segundo]);
  iniciarMostrarPassword({ doc });
  const botones = doc.padre.hijos.map(envoltura => envoltura.hijos[1]);

  botones[0].disparar('click');

  assert.equal(primero.type, 'text');
  assert.equal(segundo.type, 'password');
  assert.equal(botones[0].getAttribute('aria-pressed'), 'true');
  assert.equal(botones[1].getAttribute('aria-pressed'), 'false');
});

test('iniciarMostrarPassword agrega un botón a cada campo de contraseña', () => {
  const campos = [campoPassword(), campoPassword(), campoPassword()];
  const doc = documentoCon(campos);

  iniciarMostrarPassword({ doc });

  assert.equal(doc.padre.hijos.length, 3);
  assert.ok(doc.padre.hijos.every(e => e.className === 'campo-password' && e.hijos.length === 2));
});

test('alternarVisibilidad funciona directamente sobre un campo, botón e ícono', () => {
  const campo = campoPassword();
  const boton = elemento('button');
  const icono = elemento('i');

  alternarVisibilidad(campo, boton, icono);

  assert.equal(campo.type, 'text');
  assert.equal(icono.className, 'bi bi-eye-slash');
});
