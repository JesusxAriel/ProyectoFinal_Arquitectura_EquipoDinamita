// Control de solicitudes independiente del DOM, comprobable sin paquetes externos.
export function crearBusquedaDinamica({ consultar, mostrar, error, ocupado,
  esperar = setTimeout, cancelarEspera = clearTimeout }) {
  let revision = 0, temporizador, controlador;
  function invalidar() {
    revision++;
    cancelarEspera(temporizador);
    controlador?.abort();
    return revision;
  }
  async function ejecutar(texto, pagina, actual) {
    controlador = new AbortController();
    ocupado(true);
    try {
      const resultado = await consultar(texto, pagina, controlador.signal);
      if (actual === revision) mostrar(resultado);
    } catch (e) {
      if (actual === revision && e.name !== "AbortError") error();
    } finally {
      if (actual === revision) ocupado(false);
    }
  }
  return {
    programar(texto) {
      const actual = invalidar(); // Invalida respuestas anteriores incluso durante el debounce.
      temporizador = esperar(() => ejecutar(texto, 1, actual), 300);
    },
    buscar(texto, pagina = 1) { return ejecutar(texto, pagina, invalidar()); },
    cancelar() { invalidar(); ocupado(false); }
  };
}

if (typeof document !== "undefined") {
  const form = document.querySelector("form[data-live-search]");
  if (form) {
    const input = form.querySelector("[name=Busqueda]");
    const limpiar = form.querySelector("[data-search-clear]");
    const card = form.closest(".directory-card");
    const alerta = card.querySelector("[data-search-error]");
    const estado = card.querySelector("[data-search-status]");
    let componiendo = false;
    const buscador = crearBusquedaDinamica({
      async consultar(texto, pagina, signal) {
        const url = new URL(location.href);
        for (const clave of ["handler", "EditarId", "Nuevo", "Busqueda", "Pagina", "OrdenarPor", "Descendente", "TamanoPagina", "Estado"])
          url.searchParams.delete(clave);
        for (const [clave, valor] of new FormData(form)) url.searchParams.set(clave, valor);
        if (texto.trim()) url.searchParams.set("Busqueda", texto.trim()); else url.searchParams.delete("Busqueda");
        url.searchParams.set("Pagina", String(pagina));
        url.searchParams.set("OrdenarPor", "Nombre");
        const respuesta = await fetch(url, { signal, credentials: "same-origin" });
        if (!respuesta.ok) throw new Error("No se pudo consultar.");
        if (new URL(respuesta.url).pathname === "/Login") return { login: true };
        const html = new DOMParser().parseFromString(await respuesta.text(), "text/html");
        if (!html.querySelector(".directory-card .table-responsive")) throw new Error("Respuesta inválida.");
        return { html, url };
      },
      mostrar({ html, url, login }) {
        if (login) { location.assign("/Login"); return; }
        const errorServidor = html.querySelector('[role=alert]:not([hidden])');
        if (errorServidor) { alerta.textContent = errorServidor.textContent; alerta.hidden = false; return; }
        for (const selector of [".table-responsive", ".directory-count"]) {
          card.querySelector(selector).replaceWith(document.importNode(html.querySelector(".directory-card " + selector), true));
        }
        const pie = html.querySelector(".table-footer");
        const anterior = card.querySelector(".table-footer");
        if (pie) {
          const copia = document.importNode(pie, true);
          if (anterior) anterior.replaceWith(copia); else card.append(copia);
          const paginaActual = pie.querySelector('[aria-current="page"]');
          if (paginaActual) url.searchParams.set("Pagina", paginaActual.textContent.trim());
        } else anterior?.remove();
        // Las acciones de los modales conservan los filtros vigentes después de una búsqueda.
        document.querySelectorAll('#proveedorModal form, #eliminarModal form').forEach(modal => {
          const action = new URL(modal.action);
          for (const clave of ["Busqueda", "Pagina", "Estado", "Descendente", "TamanoPagina", "OrdenarPor"]) {
            if (url.searchParams.has(clave)) action.searchParams.set(clave, url.searchParams.get(clave));
            else action.searchParams.delete(clave);
          }
          modal.action = action.href;
        });
        history.replaceState(null, "", url);
        alerta.hidden = true;
        estado.textContent = card.querySelector(".directory-count").textContent;
      },
      error() { alerta.textContent = "No se pudo actualizar la búsqueda. Inténtalo de nuevo."; alerta.hidden = false; },
      ocupado(valor) { card.setAttribute("aria-busy", String(valor)); }
    });
    function escribir() {
      limpiar.hidden = input.value.length === 0;
      if (!componiendo) buscador.programar(input.value);
    }
    input.addEventListener("input", escribir);
    input.addEventListener("compositionstart", () => { componiendo = true; buscador.cancelar(); });
    input.addEventListener("compositionend", () => { componiendo = false; escribir(); });
    limpiar.addEventListener("click", () => {
      input.value = ""; limpiar.hidden = true; input.focus(); buscador.buscar("");
    });
    form.addEventListener("submit", event => {
      event.preventDefault(); buscador.buscar(input.value);
    });
    card.addEventListener("click", event => {
      const enlace = event.target.closest(".table-footer a.page-link");
      if (!enlace || enlace.closest(".disabled") || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
      event.preventDefault();
      const pagina = Number(new URL(enlace.href).searchParams.get("Pagina") ?? 1);
      buscador.buscar(input.value, pagina);
    });
    window.addEventListener("pagehide", () => buscador.cancelar());
  }
}
