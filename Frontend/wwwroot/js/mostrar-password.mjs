const ETIQUETA_MOSTRAR = 'Mostrar contraseña';
const ETIQUETA_OCULTAR = 'Ocultar contraseña';

export function alternarVisibilidad(campo, boton, icono) {
  const mostrar = campo.type === 'password';
  campo.type = mostrar ? 'text' : 'password';
  boton.setAttribute('aria-pressed', String(mostrar));
  boton.setAttribute('aria-label', mostrar ? ETIQUETA_OCULTAR : ETIQUETA_MOSTRAR);
  icono.className = mostrar ? 'bi bi-eye-slash' : 'bi bi-eye';
}

export function agregarBotonDeVisibilidad(campo, doc = document) {
  const envoltura = doc.createElement('div');
  envoltura.className = 'campo-password';
  campo.parentNode.insertBefore(envoltura, campo);
  envoltura.appendChild(campo);

  const icono = doc.createElement('i');
  icono.className = 'bi bi-eye';
  icono.setAttribute('aria-hidden', 'true');

  const boton = doc.createElement('button');
  boton.type = 'button';
  boton.className = 'campo-password-boton';
  boton.setAttribute('aria-label', ETIQUETA_MOSTRAR);
  boton.setAttribute('aria-pressed', 'false');
  boton.appendChild(icono);
  boton.addEventListener('click', () => alternarVisibilidad(campo, boton, icono));
  envoltura.appendChild(boton);

  return boton;
}

export function iniciarMostrarPassword({ doc = document } = {}) {
  doc.querySelectorAll('input[type="password"]').forEach(campo => agregarBotonDeVisibilidad(campo, doc));
}

if (typeof document !== 'undefined')
  document.addEventListener('DOMContentLoaded', () => iniciarMostrarPassword());
