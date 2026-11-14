// Interfaz web mínima de NEXUS (HTML + Bootstrap + JavaScript). El token vive solo en memoria.
let token = null;
let rol = null;
let recursos = [];

const $ = (id) => document.getElementById(id);
const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

async function api(ruta, opciones = {}) {
  const r = await fetch('/api' + ruta, {
    ...opciones,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: 'Bearer ' + token } : {}) },
  });
  const cuerpo = r.status === 204 ? null : await r.json().catch(() => null);
  if (!r.ok) throw new Error(cuerpo?.error || cuerpo?.title || 'Error ' + r.status);
  return cuerpo;
}

function aviso(texto, tipo = 'info') {
  const a = $('alerta');
  a.className = 'alert alert-' + tipo;
  a.innerHTML = texto;
}

// ---------- Sesión ----------
$('form-login').addEventListener('submit', async (e) => {
  e.preventDefault();
  $('error-login').textContent = '';
  try {
    const r = await api('/auth/login', { method: 'POST', body: JSON.stringify({ correo: $('correo').value, contrasena: $('clave').value }) });
    token = r.token; rol = r.rol;
    $('sesion').textContent = `${r.nombre} · ${r.rol}`;
    $('vista-login').classList.add('d-none');
    $('vista-app').classList.remove('d-none');
    await iniciar();
  } catch (err) { $('error-login').textContent = err.message; }
});

$('salir').addEventListener('click', () => location.reload());

document.querySelectorAll('#tabs [data-tab]').forEach((b) =>
  b.addEventListener('click', () => {
    document.querySelectorAll('#tabs .nav-link').forEach((x) => x.classList.remove('active'));
    b.classList.add('active');
    ['incidentes', 'panel', 'simulador'].forEach((t) => $('tab-' + t).classList.toggle('d-none', t !== b.dataset.tab));
    if (b.dataset.tab === 'panel') cargarPanel();
    if (b.dataset.tab === 'simulador') cargarRecursosSim();
  }));

async function iniciar() {
  // El operador solo trabaja con incidentes; panel y simulador son de coordinación (validado también en el servidor).
  if (rol === 'Operador') document.querySelectorAll('[data-tab="panel"],[data-tab="simulador"]').forEach((b) => b.parentElement.classList.add('d-none'));
  await cargarIncidentes();
}

// ---------- Incidentes ----------
const NIVELES = { Critica: 'danger', Alta: 'warning', Media: 'info', Baja: 'secondary' };

async function cargarIncidentes() {
  try {
    const lista = await api('/incidentes');
    if (!$('categoria').options.length) {
      (await api('/categorias')).forEach((c) => $('categoria').add(new Option(c.nombre, c.id)));
    }
    $('tabla-incidentes').innerHTML = lista.map((i) => `
      <tr>
        <td>${i.id}</td><td>${esc(i.titulo)}</td><td>${esc(i.categoria)}</td><td>${esc(i.areaAfectada)}</td>
        <td><span class="badge text-bg-light border">${esc(i.estado)}</span></td>
        <td><span class="badge text-bg-${NIVELES[i.prioridad.nivel]}" title="${esc(i.prioridad.factores.map((f) => `${f.nombre}: ${f.aporte.toFixed(1)} pts`).join(' | '))}">
          ${esc(i.prioridad.nivel)} · ${i.prioridad.puntaje}</span></td>
        <td class="text-nowrap">${acciones(i)}</td>
      </tr>`).join('');
  } catch (err) { aviso(esc(err.message), 'danger'); }
}

function acciones(i) {
  const b = [];
  if (i.estado === 'Nuevo' && rol === 'Coordinador')
    b.push(`<button class="btn btn-outline-primary btn-sm" onclick="recomendar(${i.id})">Recomendar</button>`);
  if (i.estado === 'Asignado') b.push(`<button class="btn btn-outline-secondary btn-sm" onclick="estado(${i.id}, 'EnProceso')">Iniciar</button>`);
  if (i.estado === 'EnProceso') b.push(`<button class="btn btn-outline-success btn-sm" onclick="estado(${i.id}, 'Resuelto')">Resolver</button>`);
  if (i.estado === 'Resuelto') b.push(`<button class="btn btn-outline-dark btn-sm" onclick="estado(${i.id}, 'Cerrado')">Cerrar</button>`);
  return b.join(' ');
}

$('form-incidente').addEventListener('submit', async (e) => {
  e.preventDefault();
  try {
    const r = await api('/incidentes', {
      method: 'POST',
      body: JSON.stringify({
        titulo: $('titulo').value, descripcion: $('descripcion').value, idCategoria: +$('categoria').value,
        areaAfectada: $('area').value, impacto: +$('impacto').value, urgencia: +$('urgencia').value,
      }),
    });
    const c = r.coincidencias;
    aviso(c.length
      ? `Incidente registrado. <b>Posibles coincidencias</b> (revisión humana, no se fusionan): ` +
        c.map((x) => `#${x.idIncidente} ${esc(x.titulo)} (${esc(x.criterios.join(', '))})`).join('; ')
      : 'Incidente registrado.', c.length ? 'warning' : 'success');
    e.target.reset();
    cargarIncidentes();
  } catch (err) { aviso(esc(err.message), 'danger'); }
});

async function recomendar(id) {
  try {
    const r = await api(`/incidentes/${id}/recomendaciones`);
    if (!r.recomendaciones.length) return aviso(esc(r.mensaje), 'warning');
    const mejor = r.recomendaciones[0];
    if (confirm(`Recomendado: ${mejor.nombre}\n- ${mejor.criterios.join('\n- ')}\n\n¿Asignar el incidente #${id}?`)) {
      await api(`/incidentes/${id}/asignar`, { method: 'POST', body: JSON.stringify({ idRecurso: mejor.idRecurso, observacion: 'Recomendación aceptada' }) });
      aviso('Incidente asignado.', 'success');
      cargarIncidentes();
    }
  } catch (err) { aviso(esc(err.message), 'danger'); }
}

async function estado(id, nuevo) {
  try {
    await api(`/incidentes/${id}/estado`, { method: 'PUT', body: JSON.stringify({ estado: nuevo }) });
    cargarIncidentes();
  } catch (err) { aviso(esc(err.message), 'danger'); }
}

// ---------- Panel ----------
async function cargarPanel() {
  try {
    const p = await api('/panel');
    const tarjeta = (t, v) => `<div class="col-6 col-md-3"><div class="card shadow-sm"><div class="card-body"><div class="text-muted small">${esc(t)}</div><div class="fs-3">${v}</div></div></div></div>`;
    $('panel-tarjetas').innerHTML = tarjeta('Total de incidentes', p.total)
      + Object.entries(p.porEstado).map(([k, v]) => tarjeta('Estado: ' + k, v)).join('')
      + Object.entries(p.porNivel).map(([k, v]) => tarjeta('Prioridad: ' + k, v)).join('');
    $('panel-recursos').innerHTML = p.cargaPorRecurso.map((r) => {
      const pct = Math.round(r.cargaActual * 100 / r.capacidadMaxima);
      return `<div class="mb-2"><div class="small">${esc(r.nombre)} · ${r.cargaActual}/${r.capacidadMaxima}</div>
        <div class="progress"><div class="progress-bar" style="width:${pct}%"></div></div></div>`;
    }).join('');
  } catch (err) { aviso(esc(err.message), 'danger'); }
}

// ---------- Simulador ----------
async function cargarRecursosSim() {
  try {
    recursos = await api('/recursos');
    $('sim-recursos').innerHTML = recursos.map((r) =>
      `<div class="form-check form-check-inline"><input class="form-check-input sim-rec" type="checkbox" id="rec-${r.id}" data-id="${r.id}" ${r.disponible ? 'checked' : ''}>
       <label class="form-check-label" for="rec-${r.id}">${esc(r.nombre)} disponible</label></div>`).join('');
  } catch (err) { aviso(esc(err.message), 'danger'); }
}

$('sim-comparar').addEventListener('click', async () => {
  try {
    const disp = {};
    document.querySelectorAll('.sim-rec').forEach((c) => { disp[c.dataset.id] = c.checked; });
    const base = { nombre: 'Base (datos actuales)', volumenAdicionalPorcentaje: 0, horasPorIncidente: +$('sim-horas').value };
    const esc2 = { nombre: $('sim-nombre').value, disponibilidadRecursos: disp, volumenAdicionalPorcentaje: +$('sim-volumen').value, horasPorIncidente: +$('sim-horas').value };
    const r = await api('/simulaciones/comparar', { method: 'POST', body: JSON.stringify({ escenarios: [base, esc2] }) });
    $('sim-head').innerHTML = '<tr><th>Indicador</th>' + r.map((x) => `<th>${esc(x.nombre)}</th>`).join('') + '</tr>';
    const filas = [
      ['Incidentes atendidos', (x) => x.resultado.incidentesAtendidos],
      ['Incidentes pendientes', (x) => x.resultado.incidentesPendientes],
      ['Carga estimada (%)', (x) => x.resultado.cargaEstimadaPorcentaje],
      ['Tiempo total estimado (h)', (x) => x.resultado.tiempoTotalEstimadoHoras],
      ['Supuestos aplicados', (x) => x.resultado.supuestosAplicados.map(esc).join('<br>')],
    ];
    $('sim-body').innerHTML = filas.map(([n, f]) => `<tr><th>${n}</th>${r.map((x) => `<td>${f(x)}</td>`).join('')}</tr>`).join('')
      + `<tr><td colspan="${r.length + 1}" class="text-muted small">${esc(r[0].aviso)}</td></tr>`;
  } catch (err) { aviso(esc(err.message), 'danger'); }
});
