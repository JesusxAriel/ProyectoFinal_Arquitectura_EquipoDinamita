import assert from 'node:assert/strict';
import { normalizarRazonSocial, validarCampoCliente, prepararFormularioCliente } from '../Frontend/Frontend.Demo/wwwroot/js/clientes-formulario.mjs';
const casos = [
 ['NitCi','12345',true],['NitCi','123456789012345',true],['NitCi','0',false],['NitCi','1234',false],['NitCi','1234567890123456',false],['NitCi','12abc',false],['NitCi','12345<script>',false],
 ['RazonSocial','Comercial Pérez & Hijos',true],['RazonSocial','@@@',true],['RazonSocial',' ',false],['RazonSocial','<script>alert(1)</script>',false],['RazonSocial','A'.repeat(151),false],['RazonSocial','A\u0001B',false],
 ['Telefono','',true],['Telefono',' 44112233 ',true],['Telefono','61234567',true],['Telefono','71234567',true],['Telefono','51234567',false],['Telefono','7123456',false],['Telefono','712345678',false],['Telefono','+59171234567',false],['Telefono','7123abcd',false],
 ['Correo','',true],['Correo','ventas+demo@sub.empresa.com.bo',true],['Correo','a@@b.com',false],['Correo','a@empresa',false],['Correo','a@empresa..com',false],['Correo','a b@empresa.com',false]
];
for (const [k,v,ok] of casos) assert.equal(validarCampoCliente(k,v)==='',ok,k+': '+v);
function input(name,value) { const listeners={};const feedback={textContent:''};return {name:'Form.'+name,value,listeners,feedback,classList:{toggle(){}},setCustomValidity(x){this.error=x;},setAttribute(){},closest(){return {querySelector(){return feedback;}};},addEventListener(k,fn){listeners[k]=fn;},focus(){this.focused=true;}}; }
const campos=[input('NitCi','0'),input('RazonSocial','Cliente'),input('Correo',''),input('Telefono','71234567')];const handlers={};const button={disabled:false};
const form={querySelectorAll(q){return q.startsWith('input')?campos:[button];},addEventListener(k,f){handlers[k]=f;}};
prepararFormularioCliente(form);
const evento=()=>({prevented:false,preventDefault(){this.prevented=true;}});
let e=evento();handlers.submit(e);assert.equal(e.prevented,true);assert.equal(button.disabled,false);assert.equal(campos[0].focused,true);
campos[0].value='12345';campos[0].listeners.input();assert.equal(campos[0].feedback.textContent,'');
e=evento();handlers.submit(e);assert.equal(e.prevented,false);assert.equal(button.disabled,true);
e=evento();handlers.submit(e);assert.equal(e.prevented,true);
console.log(`${casos.length} casos de Clientes, corrección de errores, foco y doble envío: correctos.`);

const nombres = [
 ['jesus ariel mamani gutierrez','JESUS ARIEL MAMANI GUTIERREZ'],
 ['  juan   perez   vargas  ','JUAN PEREZ VARGAS'],
 ['empresa   los andes s.r.l.','EMPRESA LOS ANDES S.R.L.'],
 [' compañía pérez & hijos ñ ','COMPAÑÍA PÉREZ & HIJOS Ñ'],
 ['comercial\u00a0\u00a0peña','COMERCIAL PEÑA'],['pen\u0303a','PEN\u0303A'],['   ','']
];
for (const [entrada,esperado] of nombres) assert.equal(normalizarRazonSocial(entrada),esperado);
for (const modo of ['Crear','Editar']) {
 const razon=input('RazonSocial','  compañía   peña  ');
 const fields=[input('NitCi','12345'),razon,input('Correo',''),input('Telefono','')];
 const events={};const submit={disabled:false};
 prepararFormularioCliente({querySelectorAll(q){return q.startsWith('input')?fields:[submit];},addEventListener(k,f){events[k]=f;}});
 razon.listeners.input();assert.equal(razon.value,'  compañía   peña  ',modo);
 razon.listeners.blur();assert.equal(razon.value,'COMPAÑÍA PEÑA',modo);
 razon.value='   ';razon.listeners.blur();assert.match(razon.feedback.textContent,/obligatoria/);
 let evt=evento();events.submit(evt);assert.equal(evt.prevented,true);assert.equal(submit.disabled,false);
 razon.value='  empresa   los andes s.r.l.  ';
 evt=evento();events.submit(evt);assert.equal(evt.prevented,false);assert.equal(razon.value,'EMPRESA LOS ANDES S.R.L.');assert.equal(razon.feedback.textContent,'');
}
console.log(`${nombres.length} normalizaciones Unicode; blur, envío y conservación/corrección de errores en Crear/Editar: correctos.`);
