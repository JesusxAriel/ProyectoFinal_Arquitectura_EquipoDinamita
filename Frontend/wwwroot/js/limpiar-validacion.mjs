export function limpiarCampo(formulario, campo) {
  if (!campo || !campo.name) return;
  campo.classList.remove('is-invalid');
  campo.removeAttribute('aria-invalid');
  formulario.querySelectorAll('[data-valmsg-for]').forEach(mensaje => {
    if (mensaje.dataset.valmsgFor !== campo.name) return;
    mensaje.textContent = '';
    mensaje.classList.remove('field-validation-error');
    mensaje.classList.add('field-validation-valid');
  });
}

export function iniciarLimpiezaDeValidacion({ doc = document } = {}) {
  doc.querySelectorAll('form').forEach(formulario => {
    for (const evento of ['input', 'change'])
      formulario.addEventListener(evento, e => limpiarCampo(formulario, e.target));
  });
}

if (typeof document !== 'undefined')
  document.addEventListener('DOMContentLoaded', () => iniciarLimpiezaDeValidacion());
