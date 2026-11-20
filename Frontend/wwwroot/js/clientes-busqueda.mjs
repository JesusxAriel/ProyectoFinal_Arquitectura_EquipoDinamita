// El motor compartido exportado por Proveedores permanece sin cambios.
// El formulario de Clientes usa otro atributo, por lo que no activa su integración DOM específica.
import { crearBusquedaDinamica } from "./proveedores-busqueda.mjs";

export function urlBusquedaClientes(base, datos, texto, pagina = 1) {
  const url = new URL(base);
  for (const k of ["handler", "EditarId", "DetalleId", "Nuevo", "Busqueda", "Pagina", "OrdenarPor", "Descendente", "TamanoPagina", "Estado"]) url.searchParams.delete(k);
  for (const [k,v] of datos) url.searchParams.set(k,v);
  if (texto.trim()) url.searchParams.set("Busqueda", texto.trim()); else url.searchParams.delete("Busqueda");
  url.searchParams.set("Pagina", String(pagina));
  url.hash = "";
  return url;
}
export function iniciarBusquedaClientes({ doc = document, win = window, fetcher = fetch } = {}) {
  const form = doc.querySelector("form[data-clientes-search]");
  if (!form) return;
  const input = form.querySelector("[name=Busqueda]");
  const limpiar = form.querySelector("[data-clientes-clear]");
  const card = form.closest(".client-directory");
  const alerta = card.querySelector("[data-search-error]");
  const estado = card.querySelector("[data-search-status]");
  let componiendo = false;
  const buscador = crearBusquedaDinamica({
    async consultar(texto,pagina,signal) {
      const url = urlBusquedaClientes(win.location.href,new FormData(form),texto,pagina);
      const respuesta = await fetcher(url,{signal,credentials:"same-origin"});
      if (new URL(respuesta.url).pathname.startsWith("/Auth/")) { win.location.assign(new URL(respuesta.url).pathname); throw new Error("Sesión requerida"); }
      if (respuesta.status === 401) { win.location.assign("/Auth/Login"); throw new Error("Sesión rechazada"); }
      if (!respuesta.ok) throw new Error("Consulta fallida");
      const html = new DOMParser().parseFromString(await respuesta.text(),"text/html");
      if (!html.querySelector(".client-directory .table-responsive")) throw new Error("Respuesta inválida");
      return {html,url};
    },
    mostrar({html,url,login}) {
      if(login) {win.location.assign("/Auth/Login");return;}
      const error = html.querySelector('[role=alert]:not([hidden])');
      if(error) {alerta.textContent=error.textContent;alerta.hidden=false;return;}
      for(const selector of [".table-responsive",".directory-count"]) {
        const fuente=html.querySelector(".client-directory "+selector);
        const anterior=card.querySelector(selector);
        if(fuente && anterior) anterior.replaceWith(doc.importNode(fuente,true));
      }
      const pie=html.querySelector(".client-directory .table-footer"),anterior=card.querySelector(".table-footer");
      if(pie) {
        const copia=doc.importNode(pie,true);
        if(anterior) anterior.replaceWith(copia);else card.append(copia);
        const actual=pie.querySelector('[aria-current="page"]');
        if(actual) url.searchParams.set("Pagina",actual.textContent.trim());
      } else anterior?.remove();
      // Los POST posteriores conservan exactamente los filtros que se ven en la tabla.
      doc.querySelectorAll('#clienteModal form,#estadoClienteModal form').forEach(modal=>{
        const action=new URL(modal.action);
        for(const k of ["Busqueda","Pagina","Estado","Descendente","TamanoPagina","OrdenarPor"]) {
          if(url.searchParams.has(k)) action.searchParams.set(k,url.searchParams.get(k));else action.searchParams.delete(k);
        }
        modal.action=action.href;
      });
      win.history.replaceState(null,"",url);alerta.hidden=true;estado.textContent=card.querySelector('.directory-count').textContent;
    },
    error(){alerta.textContent="No se pudo actualizar la búsqueda de Clientes. Inténtalo de nuevo.";alerta.hidden=false;},
    ocupado(v){card.setAttribute("aria-busy",String(v));}
  });
  const escribir=()=>{limpiar.hidden=input.value.length===0;if(!componiendo)buscador.programar(input.value);};
  input.addEventListener("input",escribir);
  input.addEventListener("compositionstart",()=>{componiendo=true;buscador.cancelar();});
  input.addEventListener("compositionend",()=>{componiendo=false;escribir();});
  limpiar.addEventListener("click",()=>{input.value="";limpiar.hidden=true;input.focus();buscador.buscar("");});
  form.addEventListener("submit",e=>{e.preventDefault();buscador.buscar(input.value);});
  form.querySelectorAll("select").forEach(select=>select.addEventListener("change",()=>buscador.buscar(input.value)));
  card.addEventListener("click",e=>{
    const link=e.target.closest('.table-footer a.page-link');
    if(!link || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey)return;
    if(link.closest('.disabled')){e.preventDefault();return;}
    e.preventDefault();const pagina=Number(new URL(link.href).searchParams.get('Pagina')??1);buscador.buscar(input.value,pagina);
  });
  win.addEventListener("pagehide",()=>buscador.cancelar());
  return buscador;
}
if(typeof document!=="undefined") document.addEventListener("DOMContentLoaded",()=>iniciarBusquedaClientes());
