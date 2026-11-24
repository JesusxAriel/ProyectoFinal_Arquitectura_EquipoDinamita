import { limpiarCampo } from './limpiar-validacion.mjs';
import { mostrarError } from './validacion-cliente.mjs';

export const ESPERA_MS = 300;

export function crearVerificador({
  solicitar,
  aplicar,
  esperar = milisegundos => new Promise(resolver => setTimeout(resolver, milisegundos)),
  crearControlador = () => new AbortController(),
  espera = ESPERA_MS
}) {
  let numero = 0;
  let controlador = null;

  function cancelar() {
    numero += 1;
    if (controlador) controlador.abort();
    controlador = null;
  }

  async function verificar(valor) {
    cancelar();
    const mio = numero;

    if (!valor) {
      aplicar('sin-verificar', '');
      return;
    }

    await esperar(espera);
    if (mio !== numero) return;

    aplicar('verificando', '');
    const actual = crearControlador();
    controlador = actual;

    try {
      const respuesta = await solicitar(valor, actual.signal);
      if (mio !== numero) return;
      aplicar(respuesta.estado, respuesta.mensaje ?? '');
    } catch (error) {
      if (mio !== numero || (error && error.name === 'AbortError')) return;
      aplicar('no-disponible', '');
    } finally {
      if (controlador === actual) controlador = null;
    }
  }

  return { verificar, cancelar, get estaVerificando() { return controlador !== null; } };
}

export function solicitudDeVerificacion({ url, formulario, fetchFn = fetch }) {
  return async (valor, signal) => {
    const cuerpo = new URLSearchParams();
    cuerpo.set('PasswordActual', valor);
    const token = formulario.querySelector('input[name="__RequestVerificationToken"]');
    if (token) cuerpo.set('__RequestVerificationToken', token.value);

    const respuesta = await fetchFn(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded', Accept: 'application/json' },
      body: cuerpo,
      credentials: 'same-origin',
      signal
    });
    if (!respuesta.ok) throw new Error('verificacion-no-disponible');

    const datos = await respuesta.json();
    if (datos.estado !== 'correcta' && datos.estado !== 'incorrecta') throw new Error('respuesta-invalida');
    return datos;
  };
}

export function iniciarVerificacionDePasswordActual({ doc = document, fetchFn } = {}) {
  const actual = doc.querySelector('[data-verificar-actual-url]');
  if (!actual) return null;

  const formulario = actual.form ?? actual.closest('form');
  const dependientes = ['PasswordNueva', 'RepetirPasswordNueva']
    .map(nombre => formulario.querySelector(`[name="${nombre}"]`))
    .filter(Boolean);
  const estado = doc.getElementById('estado-password-actual');
  let ultimo = 'sin-verificar';
  let ultimoMensaje = '';
  let valorVerificado = null;
  let enfocarAlCorregir = false;

  function habilitar(habilitados) {
    dependientes.forEach(campo => { campo.disabled = !habilitados; });
  }

  function escribirEstado(texto, clase) {
    if (!estado) return;
    estado.textContent = texto;
    estado.classList.remove('text-success', 'text-body-secondary');
    if (clase) estado.classList.add(clase);
  }

  function aplicar(nuevo, mensaje) {
    ultimo = nuevo;
    ultimoMensaje = mensaje;
    if (nuevo === 'verificando') {
      limpiarCampo(formulario, actual);
      habilitar(false);
      escribirEstado(actual.dataset.textoVerificando, 'text-body-secondary');
    } else if (nuevo === 'correcta') {
      limpiarCampo(formulario, actual);
      habilitar(true);
      escribirEstado(mensaje || actual.dataset.textoCorrecta, 'text-success');
      if (enfocarAlCorregir && dependientes[0]) dependientes[0].focus();
      enfocarAlCorregir = false;
    } else if (nuevo === 'incorrecta') {
      habilitar(false);
      escribirEstado('', null);
      mostrarError(formulario, actual, mensaje);
    } else if (nuevo === 'no-disponible') {
      habilitar(true);
      escribirEstado('', null);
    } else {
      habilitar(false);
      escribirEstado('', null);
    }
  }

  const verificador = crearVerificador({
    solicitar: solicitudDeVerificacion({ url: actual.dataset.verificarActualUrl, formulario, fetchFn }),
    aplicar
  });

  function pedir() {
    const valor = actual.value;
    if (valor && valor === valorVerificado && ultimo !== 'sin-verificar') {
      if (ultimo !== 'verificando') aplicar(ultimo, ultimoMensaje);
      return;
    }
    valorVerificado = valor || null;
    verificador.verificar(valor);
  }

  aplicar('sin-verificar', '');

  actual.addEventListener('input', () => {
    verificador.cancelar();
    valorVerificado = null;
    enfocarAlCorregir = false;
    aplicar('sin-verificar', '');
  });

  formulario.addEventListener('focusout', evento => {
    if (evento.target === actual) pedir();
  });

  actual.addEventListener('keydown', evento => {
    if (evento.key !== 'Enter') return;
    evento.preventDefault();
    enfocarAlCorregir = true;
    pedir();
  });

  formulario.addEventListener('submit', evento => {
    if (!actual.value || ultimo === 'correcta' || ultimo === 'no-disponible') return;
    evento.preventDefault();
    pedir();
  });

  if (actual.value) pedir();

  return { verificador, estadoActual: () => ultimo };
}

if (typeof document !== 'undefined')
  document.addEventListener('DOMContentLoaded', () => iniciarVerificacionDePasswordActual());
