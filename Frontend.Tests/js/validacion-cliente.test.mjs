import test from 'node:test';
import assert from 'node:assert/strict';
import {
  evaluarCampo,
  validarCampo,
  validarFormulario,
  iniciarValidacionCliente
} from '../../Frontend/wwwroot/js/validacion-cliente.mjs';

function clases(...iniciales) {
  const conjunto = new Set(iniciales);
  return {
    add: c => conjunto.add(c),
    remove: c => conjunto.delete(c),
    contains: c => conjunto.has(c)
  };
}

function campo(nombre, valor, datos = {}, etiqueta = 'INPUT') {
  const atributos = new Map();
  return {
    name: nombre,
    value: valor,
    tagName: etiqueta,
    dataset: { val: 'true', ...datos },
    classList: clases('form-control'),
    setAttribute: (n, v) => atributos.set(n, v),
    removeAttribute: n => atributos.delete(n),
    tieneAtributo: n => atributos.has(n),
    enfocado: false,
    focus() { this.enfocado = true; }
  };
}

function mensaje(nombre) {
  return { dataset: { valmsgFor: nombre }, textContent: '', classList: clases('invalid-feedback', 'field-validation-valid') };
}

function formulario(campos, mensajes) {
  const oyentes = {};
  return {
    querySelectorAll: selector => {
      if (selector === '[data-val="true"]') return campos;
      if (selector === '[data-valmsg-for]') return mensajes;
      return [];
    },
    querySelector: selector => {
      const nombre = /\[name="([^"]+)"\]/.exec(selector)?.[1];
      return campos.find(c => c.name === nombre) ?? null;
    },
    addEventListener: (evento, funcion) => { oyentes[evento] = funcion; },
    disparar: (evento, objetivo, extra = {}) => {
      const e = { target: objetivo, prevented: false, preventDefault() { this.prevented = true; }, ...extra };
      oyentes[evento](e);
      return e;
    }
  };
}

const REQUERIDO = { valRequired: 'El campo Nombres es obligatorio.' };
const LARGO = { valLength: 'El campo Nombres no puede superar los 100 caracteres.', valLengthMax: '100' };
const CORREO = {
  valRegex: 'Ingrese un correo válido, por ejemplo nombre@dominio.com.',
  valRegexPattern: '^[A-Za-z0-9._%+\\-]+@(?:[A-Za-z0-9](?:[A-Za-z0-9\\-]*[A-Za-z0-9])?\\.)+[A-Za-z]{2,}$'
};
const POLITICA = {
  valRequired: 'Ingrese la nueva contraseña.',
  valLongitudminima: 'La nueva contraseña debe tener al menos 8 caracteres.',
  valLongitudminimaMin: '8',
  valMayuscula: 'La nueva contraseña debe incluir al menos una letra mayúscula.',
  valMinuscula: 'La nueva contraseña debe incluir al menos una letra minúscula.',
  valNumero: 'La nueva contraseña debe incluir al menos un número.',
  valEspecial: 'La nueva contraseña debe incluir al menos un carácter especial.'
};

test('un campo obligatorio vacío o con solo espacios devuelve el mensaje del servidor', () => {
  assert.equal(evaluarCampo(campo('Nombres', '', REQUERIDO), formulario([], [])), REQUERIDO.valRequired);
  assert.equal(evaluarCampo(campo('Nombres', '   ', REQUERIDO), formulario([], [])), REQUERIDO.valRequired);
  assert.equal(evaluarCampo(campo('Nombres', 'Ana', REQUERIDO), formulario([], [])), null);
});

test('el largo máximo se comprueba con el mensaje y el límite que envía el servidor', () => {
  const f = formulario([], []);
  assert.equal(evaluarCampo(campo('Nombres', 'a'.repeat(100), LARGO), f), null);
  assert.equal(evaluarCampo(campo('Nombres', 'a'.repeat(101), LARGO), f), LARGO.valLength);
});

test('el correo exige dominio con punto', () => {
  const f = formulario([], []);
  for (const bueno of ['nombre@dominio.com', 'nombre@dominio.edu.bo', 'a.b+c@sub-dominio.example.org'])
    assert.equal(evaluarCampo(campo('Correo', bueno, CORREO), f), null, bueno);
  for (const malo of ['correo@gmail', 'correo@.com', 'correo@dominio..com', 'correo.dominio.com', 'a@b.c'])
    assert.equal(evaluarCampo(campo('Correo', malo, CORREO), f), CORREO.valRegex, malo);
});

test('un campo opcional vacío no dispara el largo ni el formato', () => {
  const f = formulario([], []);
  assert.equal(evaluarCampo(campo('Correo', '', CORREO), f), null);
  assert.equal(evaluarCampo(campo('Nombres', '', LARGO), f), null);
});

test('el rango de un rol rechaza el valor 0', () => {
  const datos = { valRange: 'Seleccione un rol válido.', valRangeMin: '1', valRangeMax: '2147483647' };
  const f = formulario([], []);
  assert.equal(evaluarCampo(campo('RolId', '0', datos, 'SELECT'), f), 'Seleccione un rol válido.');
  assert.equal(evaluarCampo(campo('RolId', '2', datos, 'SELECT'), f), null);
});

test('la política de contraseña devuelve el mensaje de la regla que falla, en el orden del servidor', () => {
  const f = formulario([], []);
  const caso = valor => evaluarCampo(campo('PasswordNueva', valor, POLITICA), f);

  assert.equal(caso(''), POLITICA.valRequired);
  assert.equal(caso('Abcd#1x'), POLITICA.valLongitudminima);
  assert.equal(caso('abcd#12x'), POLITICA.valMayuscula);
  assert.equal(caso('ABCD#12X'), POLITICA.valMinuscula);
  assert.equal(caso('Abcd#efg'), POLITICA.valNumero);
  assert.equal(caso('Abcd12efg'), POLITICA.valEspecial);
  assert.equal(caso('Abcd#123'), null);
});

test('exactamente 8 caracteres que cumplen todo no dan error de longitud', () => {
  assert.equal(evaluarCampo(campo('PasswordNueva', 'Abcd#123', POLITICA), formulario([], [])), null);
});

test('la repetición debe ser igual a la nueva contraseña', () => {
  const nueva = campo('PasswordNueva', 'Abcd#123');
  const datos = { valEqualto: 'La repetición no coincide con la nueva contraseña.', valEqualtoOther: '*.PasswordNueva' };
  const f = formulario([nueva], []);

  assert.equal(evaluarCampo(campo('RepetirPasswordNueva', 'Abcd#124', datos), f), datos.valEqualto);
  assert.equal(evaluarCampo(campo('RepetirPasswordNueva', 'Abcd#123', datos), f), null);
});

test('validarCampo marca el campo y escribe el mensaje bajo él', () => {
  const nombres = campo('Nombres', '', REQUERIDO);
  const propio = mensaje('Nombres');
  const ajeno = mensaje('Correo');

  const valido = validarCampo(formulario([nombres], [propio, ajeno]), nombres);

  assert.equal(valido, false);
  assert.equal(nombres.classList.contains('is-invalid'), true);
  assert.equal(nombres.tieneAtributo('aria-invalid'), true);
  assert.equal(propio.textContent, REQUERIDO.valRequired);
  assert.equal(propio.classList.contains('field-validation-error'), true);
  assert.equal(propio.classList.contains('field-validation-valid'), false);
  assert.equal(ajeno.textContent, '');
});

test('validarCampo quita el error cuando el valor ya es correcto', () => {
  const nombres = campo('Nombres', '', REQUERIDO);
  const propio = mensaje('Nombres');
  const f = formulario([nombres], [propio]);
  validarCampo(f, nombres);

  nombres.value = 'Ana';
  const valido = validarCampo(f, nombres);

  assert.equal(valido, true);
  assert.equal(nombres.classList.contains('is-invalid'), false);
  assert.equal(propio.textContent, '');
});

test('validarFormulario devuelve todos los campos inválidos', () => {
  const a = campo('Nombres', '', REQUERIDO);
  const b = campo('Correo', 'correo@gmail', CORREO);
  const c = campo('Otro', 'ok', REQUERIDO);

  const invalidos = validarFormulario(formulario([a, b, c], [mensaje('Nombres'), mensaje('Correo')]));

  assert.deepEqual(invalidos, [a, b]);
});

function documentoCon(...formularios) {
  return { querySelectorAll: selector => (selector === 'form' ? formularios : []) };
}

test('al salir de un campo se valida al instante', () => {
  const correo = campo('Correo', 'correo@gmail', CORREO);
  const texto = mensaje('Correo');
  const f = formulario([correo], [texto]);
  iniciarValidacionCliente({ doc: documentoCon(f) });

  f.disparar('focusout', correo, { relatedTarget: null });

  assert.equal(texto.textContent, CORREO.valRegex);
});

test('salir hacia un botón de cancelar no muestra errores', () => {
  const nombres = campo('Nombres', '', REQUERIDO);
  const texto = mensaje('Nombres');
  const f = formulario([nombres], [texto]);
  iniciarValidacionCliente({ doc: documentoCon(f) });

  f.disparar('focusout', nombres, { relatedTarget: { closest: s => (s === '[data-bs-dismiss]' ? {} : null) } });

  assert.equal(texto.textContent, '');
});

test('al cambiar un select se valida', () => {
  const rol = campo('RolId', '0', { valRange: 'Seleccione un rol válido.', valRangeMin: '1', valRangeMax: '9' }, 'SELECT');
  const texto = mensaje('RolId');
  const f = formulario([rol], [texto]);
  iniciarValidacionCliente({ doc: documentoCon(f) });

  f.disparar('change', rol);

  assert.equal(texto.textContent, 'Seleccione un rol válido.');
});

test('al enviar con errores se cancela el envío y se enfoca el primer campo inválido', () => {
  const a = campo('Nombres', '', REQUERIDO);
  const b = campo('Correo', 'x', CORREO);
  const f = formulario([a, b], [mensaje('Nombres'), mensaje('Correo')]);
  iniciarValidacionCliente({ doc: documentoCon(f) });

  const evento = f.disparar('submit', f);

  assert.equal(evento.prevented, true);
  assert.equal(a.enfocado, true);
  assert.equal(b.enfocado, false);
});

test('al enviar sin errores no se cancela el envío', () => {
  const a = campo('Nombres', 'Ana', REQUERIDO);
  const f = formulario([a], [mensaje('Nombres')]);
  iniciarValidacionCliente({ doc: documentoCon(f) });

  const evento = f.disparar('submit', f);

  assert.equal(evento.prevented, false);
});

test('los formularios sin campos validables no se tocan', () => {
  let registros = 0;
  const f = { ...formulario([], []), addEventListener: () => { registros += 1; } };
  iniciarValidacionCliente({ doc: documentoCon(f) });

  assert.equal(registros, 0);
});
