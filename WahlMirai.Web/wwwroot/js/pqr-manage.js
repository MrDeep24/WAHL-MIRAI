/**
 * PQR — Gestión y Resolución (RF-M08-02)
 * Conecta el listado y el modal de respuesta con los endpoints reales
 * de PqrController: GET /Pqr/List?status=, POST /Pqr/Resolve/{id}
 *
 * No depende de clases Tailwind específicas — usa atributos data-*
 * para que funcione con cualquier estilo que tenga la vista real.
 *
 * Markup mínimo requerido en Manage.cshtml (agregar estos atributos
 * al markup que ya existe, sin cambiar tus clases/estilos):
 *
 *   Tabs de filtro:
 *     <button data-pqr-filter="TODAS">Todas</button>
 *     <button data-pqr-filter="ABIERTO">Abiertas</button>
 *     <button data-pqr-filter="RESUELTO">Resueltas</button>
 *
 *   Buscador:
 *     <input data-pqr-search type="text" placeholder="Buscar...">
 *
 *   Contenedor donde se insertan las filas (reemplaza su innerHTML):
 *     <div data-pqr-list></div>
 *
 *   Modal (oculto por defecto, ej. con la clase "hidden" de Tailwind
 *   o display:none — el script solo alterna esa clase):
 *     <div data-pqr-modal class="hidden"> ... </div>
 *     <button data-pqr-close>X</button>          (botón X del header)
 *     <button data-pqr-cancel>Cancelar</button>
 *     <button data-pqr-submit>Enviar Respuesta</button>
 *     <textarea data-pqr-response-input></textarea>
 *     <span data-pqr-field="elector-name"></span>
 *     <span data-pqr-field="elector-id"></span>
 *     <span data-pqr-field="fecha"></span>
 *     <span data-pqr-field="asunto"></span>
 *     <span data-pqr-field="mensaje"></span>
 *
 *   Antiforgery token (agregar en algún lugar de la vista si no existe):
 *     @Html.AntiForgeryToken()
 *
 *   Fila individual (generada dinámicamente por este script, pero cada
 *   botón "Ver Detalle" debe llevar data-pqr-view="{id del ticket}" —
 *   eso ya lo genera renderRow() más abajo, no hace falta tocarlo).
 */
(function () {
  'use strict';

  const state = {
    filter: 'ABIERTO', // coincide con la pestaña activa por defecto del mockup
    search: '',
    tickets: [],
  };

  const els = {};

  function cacheEls() {
    els.list = document.querySelector('[data-pqr-list]');
    els.filterButtons = document.querySelectorAll('[data-pqr-filter]');
    els.search = document.querySelector('[data-pqr-search]');
    els.modal = document.querySelector('[data-pqr-modal]');
    els.close = document.querySelector('[data-pqr-close]');
    els.cancel = document.querySelector('[data-pqr-cancel]');
    els.submit = document.querySelector('[data-pqr-submit]');
    els.responseInput = document.querySelector('[data-pqr-response-input]');
    els.fields = {
      electorName: document.querySelector('[data-pqr-field="elector-name"]'),
      electorId: document.querySelector('[data-pqr-field="elector-id"]'),
      fecha: document.querySelector('[data-pqr-field="fecha"]'),
      asunto: document.querySelector('[data-pqr-field="asunto"]'),
      mensaje: document.querySelector('[data-pqr-field="mensaje"]'),
    };
  }

  function getCsrfToken() {
    const input = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!input) {
      console.error('[PQR] No se encontró el antiforgery token. Agrega @Html.AntiForgeryToken() en la vista.');
      return null;
    }
    return input.value;
  }

  function formatDate(iso) {
    if (!iso) return '—';
    const d = new Date(iso);
    return d.toLocaleDateString('es-CO', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  function initials(name) {
    if (!name) return '??';
    const parts = name.trim().split(/\s+/);
    return ((parts[0]?.[0] || '') + (parts[1]?.[0] || '')).toUpperCase();
  }

  async function fetchTickets() {
    const params = new URLSearchParams();
    if (state.filter !== 'TODAS') params.set('status', state.filter);

    try {
      const res = await fetch(`/Pqr/List?${params.toString()}`, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
      });
      const data = await res.json();
      if (!data.ok) {
        console.error('[PQR] Error al listar:', data.message);
        state.tickets = [];
      } else {
        state.tickets = data.tickets || [];
      }
    } catch (err) {
      console.error('[PQR] Fallo de red al listar tickets:', err);
      state.tickets = [];
    }
    renderList();
  }

  function filteredTickets() {
    if (!state.search) return state.tickets;
    const q = state.search.toLowerCase();
    return state.tickets.filter(
      (t) => (t.userName ?? t.voterName)?.toLowerCase().includes(q) || t.subject?.toLowerCase().includes(q)
    );
  }

  function renderList() {
    if (!els.list) return;
    const tickets = filteredTickets();

    if (tickets.length === 0) {
      els.list.innerHTML = '<p data-pqr-empty>No hay PQR para mostrar.</p>';
      return;
    }

    els.list.innerHTML = tickets.map(renderRow).join('');

    els.list.querySelectorAll('[data-pqr-view]').forEach((btn) => {
      btn.addEventListener('click', () => openModal(btn.getAttribute('data-pqr-view')));
    });
  }

  function renderRow(t) {
    const statusLabel = t.status === 'ABIERTO' ? 'Abierta' : 'Resuelta';
    const userName = t.userName ?? t.voterName ?? '';
    return `
      <div data-pqr-row data-status="${t.status}"
           class="grid grid-cols-12 gap-2 items-center px-2 py-3 border-b text-sm">
        <div class="col-span-1 flex items-center">
          <span data-pqr-avatar
                class="w-8 h-8 rounded-full bg-primary-container text-on-primary-container flex items-center justify-center text-xs font-semibold">
            ${initials(userName)}
          </span>
        </div>
        <span data-pqr-col="elector" class="col-span-3 truncate">${escapeHtml(userName)}</span>
        <span data-pqr-col="asunto" class="col-span-4 truncate">${escapeHtml(t.subject || '')}</span>
        <span data-pqr-col="estado" data-status-value="${t.status}" class="col-span-2">
          <span class="inline-block px-2 py-1 rounded-full text-xs font-medium ${
            t.status === 'ABIERTO' ? 'bg-status-pending/10 text-status-pending' : 'bg-status-graduated/20 text-status-graduated'
          }">${statusLabel}</span>
        </span>
        <div class="col-span-2">
          <button data-pqr-view="${t.id}"
                  class="px-3 py-1.5 rounded-lg bg-primary text-on-primary text-xs font-medium hover:opacity-90 transition-opacity">
            Ver Detalle
          </button>
        </div>
      </div>
    `;
  }

  function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
  }

  function openModal(ticketId) {
    const ticket = state.tickets.find((t) => String(t.id) === String(ticketId));
    if (!ticket || !els.modal) return;

    const userName = ticket.userName ?? ticket.voterName ?? '';
    const userId = ticket.userId ?? ticket.voterId ?? '';

    els.modal.dataset.currentTicketId = ticket.id;
    els.fields.electorName.textContent = userName;
    els.fields.electorId.textContent = `ID: ${userId}`;
    els.fields.fecha.textContent = formatDate(ticket.createdAt);
    els.fields.asunto.textContent = ticket.subject || '';
    els.fields.mensaje.textContent = ticket.message || '';

    if (ticket.status === 'RESUELTO') {
      els.responseInput.value = ticket.adminResponse || '';
      els.responseInput.disabled = true;
      els.submit.disabled = true;
      els.submit.textContent = 'Ya resuelta';
    } else {
      els.responseInput.value = '';
      els.responseInput.disabled = false;
      els.submit.disabled = false;
      els.submit.textContent = 'Enviar Respuesta';
    }

    els.modal.classList.remove('hidden');
  }

  function closeModal() {
    if (!els.modal) return;
    els.modal.classList.add('hidden');
    delete els.modal.dataset.currentTicketId;
  }

  async function submitResponse() {
    const ticketId = els.modal?.dataset.currentTicketId;
    const adminResponse = els.responseInput?.value?.trim();

    if (!ticketId) return;
    if (!adminResponse) {
      alert('Escribe una respuesta antes de enviar.');
      return;
    }

    const token = getCsrfToken();
    if (!token) {
      alert('No se pudo verificar el token de seguridad. Recarga la página.');
      return;
    }

    els.submit.disabled = true;
    els.submit.textContent = 'Enviando...';

    try {
      const res = await fetch(`/Pqr/Resolve/${ticketId}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          RequestVerificationToken: token,
        },
        body: JSON.stringify({ adminResponse }),
      });
      const data = await res.json();

      if (!data.ok) {
        alert(data.message || 'No se pudo enviar la respuesta.');
        els.submit.disabled = false;
        els.submit.textContent = 'Enviar Respuesta';
        return;
      }

      closeModal();
      await fetchTickets(); // refresca la lista para reflejar el nuevo estado
    } catch (err) {
      console.error('[PQR] Fallo de red al resolver ticket:', err);
      alert('Error de red. Intenta de nuevo.');
      els.submit.disabled = false;
      els.submit.textContent = 'Enviar Respuesta';
    }
  }

  function updateFilterStyles() {
    els.filterButtons.forEach((b) => {
      const isActive = b.getAttribute('data-active') === 'true';
      if (isActive) {
        b.classList.add('bg-primary', 'text-on-primary');
        b.classList.remove('border');
      } else {
        b.classList.remove('bg-primary', 'text-on-primary');
        b.classList.add('border');
      }
    });
  }

  function bindEvents() {
    els.filterButtons.forEach((btn) => {
      btn.addEventListener('click', () => {
        state.filter = btn.getAttribute('data-pqr-filter');
        els.filterButtons.forEach((b) => b.removeAttribute('data-active'));
        btn.setAttribute('data-active', 'true');
        updateFilterStyles();
        fetchTickets();
      });
    });

    if (els.search) {
      let debounce;
      els.search.addEventListener('input', (e) => {
        clearTimeout(debounce);
        debounce = setTimeout(() => {
          state.search = e.target.value;
          renderList();
        }, 200);
      });
    }

    els.close?.addEventListener('click', closeModal);
    els.cancel?.addEventListener('click', closeModal);
    els.submit?.addEventListener('click', submitResponse);
  }

  document.addEventListener('DOMContentLoaded', () => {
    cacheEls();
    if (!els.list) {
      console.warn('[PQR] No se encontró [data-pqr-list] — este script no aplica en esta vista.');
      return;
    }
    updateFilterStyles();
    bindEvents();
    fetchTickets();
  });
})();
