// DEMO TEMPORAL. Validación temprana equivalente al contrato actual de Clientes.
// No sustituye Domain ni los permisos de la API. No se modifica el JS de Proveedores.
// Mayúsculas Unicode reales: no se eliminan diacríticos ni se usa text-transform.
export function normalizarRazonSocial(texto) {
  return texto.trim().replace(/\s+/gu, " ").toUpperCase();
}
export function validarCampoCliente(nombre, texto) {
  const v = texto.trim();
  switch (nombre) {
    case "NitCi": return /^[0-9]{5,15}$/.test(v) ? "" : "El NIT o CI debe tener entre 5 y 15 dígitos. El 0 está reservado.";
    case "RazonSocial": {
      const razon = normalizarRazonSocial(texto);
      return !razon || razon.length > 150 || /[\p{Cc}<>]/u.test(razon) ? "La razón social es obligatoria, admite hasta 150 caracteres y no permite HTML ni controles." : "";
    }
    case "Correo": return !v || (v.length <= 150 && /^[A-Za-z0-9._%+\-]{1,64}@([A-Za-z0-9]([A-Za-z0-9\-]*[A-Za-z0-9])?\.)+[A-Za-z]{2,}$/.test(v)) ? "" : "El correo electrónico no es válido.";
    case "Telefono": return !v || /^[467][0-9]{7}$/.test(v) ? "" : "El teléfono debe tener 8 dígitos y comenzar con 4, 6 o 7.";
    default: return "";
  }
}
export function prepararFormularioCliente(form) {
  form.noValidate = true;
  let enviando = false;
  const inputs = [...form.querySelectorAll('input[name^="Form."]')];
  function validar(input) {
    const error = validarCampoCliente(input.name.substring(5), input.value);
    input.setCustomValidity(error); input.classList.toggle("is-invalid", !!error); input.setAttribute("aria-invalid", String(!!error));
    const feedback = input.closest(".mb-3")?.querySelector(".invalid-feedback");
    if (feedback) feedback.textContent = error;
    return !error;
  }
  function normalizar(input) {
    if (input.name === "Form.RazonSocial") input.value = normalizarRazonSocial(input.value);
  }
  inputs.forEach(i => {
    i.addEventListener("input", () => validar(i));
    if (i.name === "Form.RazonSocial") i.addEventListener("blur", () => { normalizar(i); validar(i); });
  });
  form.addEventListener("submit", e => {
    if (enviando) { e.preventDefault(); return; }
    inputs.forEach(normalizar);
    const validos = inputs.map(validar);
    if (validos.includes(false)) { e.preventDefault(); inputs[validos.indexOf(false)].focus(); return; }
    enviando = true; form.querySelectorAll('button[type="submit"]').forEach(b => { b.disabled = true; });
  });
}
export function iniciarModalesClientes({doc = document, win = window, modales = bootstrap.Modal} = {}) {
  const modal = doc.querySelector("#clienteModal");
  if (modal) {
    prepararFormularioCliente(modal.querySelector("form"));
    if (modal.dataset.clienteAutoOpen === "true") modales.getOrCreateInstance(modal).show();
  }
  const detalle = doc.querySelector("#clienteDetalleModal");
  if (detalle?.dataset.clienteDetalleAutoOpen === "true") modales.getOrCreateInstance(detalle).show();
  for (const [elemento, claves] of [[modal,["EditarId","Nuevo","handler"]],[detalle,["DetalleId"]]]) {
    elemento?.addEventListener("hidden.bs.modal", () => {
      // Cerrar no altera filtros ni escribe datos; evita reabrir un modal al refrescar la URL.
      const url = new URL(win.location.href);
      claves.forEach(k => url.searchParams.delete(k));
      win.history.replaceState(null,"",url);
    });
  }
  doc.querySelector("#estadoClienteModal")?.addEventListener("show.bs.modal", e => {
    const btn = e.relatedTarget;
    if (!btn) return;
    const accion = btn.dataset.clienteAccion;
    if (!["Desactivar", "Reactivar"].includes(accion)) return;
    const form = e.target.querySelector("form");
    const url = new URL(form.action); url.searchParams.set("handler", accion); form.action = url.href;
    form.querySelector("[data-cliente-id]").value = btn.dataset.clienteId;
    form.querySelector("[data-cliente-nombre]").textContent = btn.dataset.clienteNombre;
    form.querySelector("[data-cliente-operacion]").textContent = accion.toLowerCase();
  });
}
if (typeof document !== "undefined") document.addEventListener("DOMContentLoaded", () => iniciarModalesClientes());
