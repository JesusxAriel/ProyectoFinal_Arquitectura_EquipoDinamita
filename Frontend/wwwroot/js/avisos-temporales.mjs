export function iniciarAvisosTemporales({ doc = document, alertas = bootstrap.Alert, temporizador = setTimeout } = {}) {
  doc.querySelectorAll('[data-aviso-ms]').forEach(aviso => {
    const milisegundos = Number(aviso.dataset.avisoMs);
    if (!Number.isFinite(milisegundos) || milisegundos <= 0) return;
    temporizador(() => {
      if (aviso.isConnected) alertas.getOrCreateInstance(aviso).close();
    }, milisegundos);
  });
}

if (typeof document !== 'undefined')
  document.addEventListener('DOMContentLoaded', () => iniciarAvisosTemporales());
