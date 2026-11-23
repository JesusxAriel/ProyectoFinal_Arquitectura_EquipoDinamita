document.addEventListener("DOMContentLoaded", () => {
  const precio = document.querySelector('[name="Form.PrecioVenta"]');
  const motivo = document.querySelector('[name="Form.MotivoCambioPrecio"]');
  if (precio && motivo && motivo.dataset.precioOriginal) {
    const actualizar = () => { motivo.required = Number(precio.value) !== Number(motivo.dataset.precioOriginal); };
    precio.addEventListener('input', actualizar); actualizar();
  }
  document.querySelectorAll(".catalogo .modal").forEach(modal => modal.addEventListener("show.bs.modal", event => {
    if (document.querySelector(".catalogo .modal.show")) event.preventDefault();
  }));
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

  }));
  const eliminar = document.getElementById("eliminarModal");
  eliminar?.addEventListener("show.bs.modal", event => {
    const button = event.relatedTarget;
    eliminar.querySelector("[data-delete-id]").value = button?.dataset.id ?? "";
    eliminar.querySelector("[data-delete-name]").textContent = button?.dataset.nombre ?? "este producto";
  });
  document.querySelectorAll(".modal").forEach(modal => modal.addEventListener("shown.bs.modal", () => {
    const objetivo = modal.querySelector(".alert[role=alert], .validation-summary-errors") ?? modal.querySelector("input:not([type=hidden])") ?? modal.querySelector(".btn-light");
    if (objetivo?.getAttribute("role") === "alert") objetivo.tabIndex = -1;
    objetivo?.focus();
  }));

});
window.addEventListener("pageshow", () => {

  document.querySelectorAll("form[data-loading]").forEach(form => { delete form.dataset.submitting; form.querySelectorAll("button[type=submit]").forEach(b => b.disabled = false); form.querySelectorAll("[data-submitter-copy]").forEach(e => e.remove()); });
});
