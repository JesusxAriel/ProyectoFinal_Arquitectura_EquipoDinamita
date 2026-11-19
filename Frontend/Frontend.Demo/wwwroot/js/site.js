document.addEventListener("DOMContentLoaded", () => {
  // Validación temprana de presentación; Domain vuelve a comprobar todos los datos.
  const prohibido = /[\p{Cc}\p{Cf}\p{Co}\p{Cn}\p{Cs}\p{Zl}\p{Zp}\u034f\u115f\u1160\u3164\uffa0\ufe00-\ufe0f\u{e0100}-\u{e01ef}]/u;
  const nombrePermitido = /^[\p{L}\p{N}\p{Mn}\p{Mc}\p{Zs}\p{Sc}\p{Pd}.\-()'/&,+:#%!?@"’‘“”°º®©™]+$/u;
  function validarProveedor(input) {
    const valor = input.value.trim();
    let error = "";
    if (input.name === "Form.NombreEmpresa") {
      if (!valor) error = "El nombre de la empresa es obligatorio.";
      else if (valor.replace(/\s+/gu, " ").length > 150) error = "El nombre no puede superar los 150 caracteres.";
      else if (prohibido.test(input.value) || !nombrePermitido.test(valor) || !/[\p{L}\p{N}]/u.test(valor) || /(?:javascript:|vbscript:|data:text\/|\b(?:alert|eval|prompt|confirm|setTimeout|setInterval|Function)\s*\()/iu.test(valor)) error = "Use un nombre comercial sin HTML, código ni caracteres de control o invisibles.";
    } else if (input.name === "Form.NumeroEmpresa") {
      if (!valor) error = "El número de la empresa es obligatorio.";
      else if (prohibido.test(input.value) || !/^[467][0-9]{7}$/u.test(valor)) error = "Use 8 dígitos que comiencen por 4, 6 o 7, sin prefijos ni espacios interiores.";
    } else if (input.name === "Form.CorreoReferencia") {
      const partes = valor.split("@");
      const local = partes[0];
      const dominio = partes[1]?.split(".") ?? [];
      if (prohibido.test(input.value) || (valor && (valor.length > 150 || partes.length !== 2 || local.length > 64 ||
          !/^[A-Za-z0-9!#$%&'*+/=?^_`{|}~.\-]+$/u.test(local) || local.startsWith(".") || local.endsWith(".") || local.includes("..") ||
          dominio.length < 2 || dominio.some(e => !/^[A-Za-z0-9](?:[A-Za-z0-9\-]{0,61}[A-Za-z0-9])?$/u.test(e)) || !/^[A-Za-z]{2,}$/u.test(dominio.at(-1))))) error = "El correo de referencia no tiene un formato válido.";
    }
    input.setCustomValidity(error);
    input.classList.toggle("is-invalid", !!error);
    input.setAttribute("aria-invalid", String(!!error));
    const mensaje = input.closest(".mb-3, .mb-4")?.querySelector(".invalid-feedback");
    if (mensaje) mensaje.textContent = error;
    return !error;
  }
  const formulario = document.querySelector("#proveedorModal form");
  if (formulario) {
    // Se recorta exclusivamente después de verificar el texto original.
    formulario.noValidate = true;
    const campos = [...formulario.querySelectorAll("input:not([type=hidden]):not([type=checkbox])")];
    campos.forEach(input => input.addEventListener("input", () => validarProveedor(input)));
    formulario.addEventListener("submit", event => {
      const validos = campos.map(validarProveedor);
      if (validos.some(v => !v) || !formulario.checkValidity()) {
        event.preventDefault(); formulario.querySelector(":invalid")?.focus(); return;
      }
      campos.forEach(input => { input.value = input.value.trim(); });
    });
  }
  document.querySelectorAll("[data-submit-on-change]").forEach(select => select.addEventListener("change", () => select.form.requestSubmit()));
  document.querySelectorAll("form[data-loading]").forEach(form => form.addEventListener("submit", event => {
    if (form.hasAttribute?.("data-live-search")) return; // El módulo de búsqueda actualiza solo el listado.
    if (event.defaultPrevented || !form.checkValidity()) return;
    if (form.dataset.submitting === "true") { event.preventDefault(); return; }
    form.dataset.submitting = "true";
    if (event.submitter?.name) {
      const hidden = document.createElement("input"); hidden.type = "hidden"; hidden.name = event.submitter.name; hidden.value = event.submitter.value; hidden.dataset.submitterCopy = "true"; form.append(hidden);
    }
    form.querySelectorAll("button[type=submit]").forEach(button => { button.disabled = true; });
    document.getElementById("page-loading").hidden = false;
  }));
  const eliminar = document.getElementById("eliminarModal");
  eliminar?.addEventListener("show.bs.modal", event => {
    const button = event.relatedTarget;
    eliminar.querySelector("[data-delete-id]").value = button?.dataset.id ?? "";
    eliminar.querySelector("[data-delete-name]").textContent = button?.dataset.nombre ?? "este proveedor";
  });
  document.querySelectorAll(".modal").forEach(modal => modal.addEventListener("shown.bs.modal", () => {
    const objetivo = modal.querySelector("[role=alert]") ?? modal.querySelector("input:not([type=hidden])") ?? modal.querySelector(".btn-light");
    if (objetivo?.getAttribute("role") === "alert") objetivo.tabIndex = -1;
    objetivo?.focus();
  }));
  document.querySelectorAll(".modal[data-auto-open=true]").forEach(modal => bootstrap.Modal.getOrCreateInstance(modal).show());
  const proveedor = document.getElementById("proveedorModal");
  proveedor?.addEventListener("hidden.bs.modal", () => {
    // Evita reutilizar el formulario de edición como alta al pulsar Añadir.
    if (new URL(location.href).searchParams.has("EditarId")) {
      const destino = new URL(location.href); destino.searchParams.delete("EditarId"); destino.searchParams.delete("Nuevo"); location.replace(destino);
    }
  });
});
window.addEventListener("pageshow", () => {
  document.getElementById("page-loading").hidden = true;
  document.querySelectorAll("form[data-loading]").forEach(form => { delete form.dataset.submitting; form.querySelectorAll("button[type=submit]").forEach(b => b.disabled = false); form.querySelectorAll("[data-submitter-copy]").forEach(e => e.remove()); });
});
