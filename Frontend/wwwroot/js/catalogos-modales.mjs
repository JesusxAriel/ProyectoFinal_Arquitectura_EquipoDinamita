// Los IDs viajan por POST. Cerrar vuelve al GET del listado y evita reutilizar una edición como alta.
export function iniciarModalesCatalogo({doc=document,win=window,modales=bootstrap.Modal}={}) {
  doc.querySelectorAll('.catalogo .modal').forEach(modal=>{
    if(modal.dataset.autoOpen==='true') modales.getOrCreateInstance(modal).show();
    modal.addEventListener('hidden.bs.modal',()=>{
      const url=new URL(win.location.href);
      for(const key of ['handler','Nuevo','EditarId','DetalleId','id']) url.searchParams.delete(key);
      win.location.assign(url);
    });
  });
}
if(typeof document!=='undefined') document.addEventListener('DOMContentLoaded',()=>iniciarModalesCatalogo());
