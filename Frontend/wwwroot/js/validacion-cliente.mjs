import { limpiarCampo } from './limpiar-validacion.mjs';

const MAYUSCULA = /\p{Lu}/u;
const MINUSCULA = /\p{Ll}/u;
const DIGITO = /\p{Nd}/u;
const ESPECIAL = /[^\p{L}\p{Nd}]/u;

const REGLAS = [
  ['valRequired', (valor, datos) => (valor.trim() === '' ? datos.valRequired : null)],
  ['valLength', (valor, datos) => {
    if (valor === '') return null;
    const maximo = Number(datos.valLengthMax);
    const minimo = Number(datos.valLengthMin);
    if (Number.isFinite(maximo) && valor.length > maximo) return datos.valLength;
    if (Number.isFinite(minimo) && valor.length < minimo) return datos.valLength;
    return null;
  }],
  ['valRegex', (valor, datos) => {
    if (valor === '') return null;
    return new RegExp(datos.valRegexPattern).test(valor) ? null : datos.valRegex;
  }],
  ['valRange', (valor, datos) => {
    if (valor === '') return null;
    const numero = Number(valor);
    const minimo = Number(datos.valRangeMin);
    const maximo = Number(datos.valRangeMax);
    if (!Number.isFinite(numero)) return datos.valRange;
    return numero < minimo || numero > maximo ? datos.valRange : null;
  }],
  ['valLongitudminima', (valor, datos) =>
    valor !== '' && valor.length < Number(datos.valLongitudminimaMin) ? datos.valLongitudminima : null],
  ['valMayuscula', (valor, datos) => (valor !== '' && !MAYUSCULA.test(valor) ? datos.valMayuscula : null)],
  ['valMinuscula', (valor, datos) => (valor !== '' && !MINUSCULA.test(valor) ? datos.valMinuscula : null)],
  ['valNumero', (valor, datos) => (valor !== '' && !DIGITO.test(valor) ? datos.valNumero : null)],
  ['valEspecial', (valor, datos) => (valor !== '' && !ESPECIAL.test(valor) ? datos.valEspecial : null)],
  ['valEqualto', (valor, datos, otro) =>
    valor !== '' && otro !== undefined && valor !== otro ? datos.valEqualto : null]
];

export function nombreDelOtroCampo(referencia) {
  return referencia.replace(/^\*\./, '');
}

export function evaluarCampo(campo, formulario) {
  const datos = campo.dataset ?? {};
  for (const [clave, regla] of REGLAS) {
    if (datos[clave] === undefined) continue;
    let otro;
    if (clave === 'valEqualto') {
      const relacionado = formulario.querySelector(`[name="${nombreDelOtroCampo(datos.valEqualtoOther)}"]`);
      otro = relacionado ? relacionado.value : undefined;
    }
    const mensaje = regla(campo.value ?? '', datos, otro);
    if (mensaje) return mensaje;
  }
  return null;
}

export function mostrarError(formulario, campo, mensaje) {
  campo.classList.add('is-invalid');
  campo.setAttribute('aria-invalid', 'true');
  formulario.querySelectorAll('[data-valmsg-for]').forEach(contenedor => {
    if (contenedor.dataset.valmsgFor !== campo.name) return;
    contenedor.textContent = mensaje;
    contenedor.classList.remove('field-validation-valid');
    contenedor.classList.add('field-validation-error');
  });
}

export function validarCampo(formulario, campo) {
  const mensaje = evaluarCampo(campo, formulario);
  if (mensaje) mostrarError(formulario, campo, mensaje);
  else limpiarCampo(formulario, campo);
  return mensaje === null;
}

export function camposValidables(formulario) {
  return Array.from(formulario.querySelectorAll('[data-val="true"]')).filter(campo => campo.name && !campo.disabled);
}

export function validarFormulario(formulario) {
  const invalidos = camposValidables(formulario).filter(campo => !validarCampo(formulario, campo));
  return invalidos;
}

function saleHaciaCancelar(evento) {
  const destino = evento.relatedTarget;
  return Boolean(destino && destino.closest && destino.closest('[data-bs-dismiss]'));
}

export function iniciarValidacionCliente({ doc = document } = {}) {
  doc.querySelectorAll('form').forEach(formulario => {
    if (camposValidables(formulario).length === 0) return;

    formulario.addEventListener('focusout', evento => {
      const campo = evento.target;
      if (!campo || campo.dataset?.val !== 'true' || saleHaciaCancelar(evento)) return;
      validarCampo(formulario, campo);
    });

    formulario.addEventListener('change', evento => {
      const campo = evento.target;
      if (campo && campo.tagName === 'SELECT' && campo.dataset?.val === 'true') validarCampo(formulario, campo);
    });

    formulario.addEventListener('submit', evento => {
      const invalidos = validarFormulario(formulario);
      if (invalidos.length === 0) return;
      evento.preventDefault();
      invalidos[0].focus();
    });
  });
}

if (typeof document !== 'undefined')
  document.addEventListener('DOMContentLoaded', () => iniciarValidacionCliente());
