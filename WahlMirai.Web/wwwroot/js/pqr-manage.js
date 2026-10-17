// Archivo:       pqr-manage.js
// Módulo:        M08 — Ayuda, Tutorial, PQR y Chatbot
// Desarrollador: Kevin
// Descripción:   Carga, filtra y representa tickets PQR para su gestión
//                administrativa, y controla el modal de respuesta.
//                Se integra con los endpoints de consulta y resolución.
// Requerimientos: RF-M08-02.
// PQR — Gestión y Resolución (RF-M08-02)
// Conecta el listado y el modal de respuesta con los endpoints reales
// de PqrController: GET /Pqr/List?status=, POST /Pqr/Resolve/{id}
//
// No depende de clases Tailwind específicas — usa atributos data-*
// para que funcione con cualquier estilo que tenga la vista real.
//
// Markup mínimo requerido en Manage.cshtml (agregar estos atributos
// al markup que ya existe, sin cambiar tus clases/estilos):
//
//   Tabs de filtro:
//     <button data-pqr-filter="TODAS">Todas</button>
//     <button data-pqr-filter="ABIERTO">Abiertas</button>
//     <button data-pqr-filter="RESUELTO">Resueltas</button>
//
//   Buscador:
//     <input data-pqr-search type="text" placeholder="Buscar...">
//
//   Contenedor donde se insertan las filas (reemplaza su innerHTML):
//     <div data-pqr-list></div>
//
//   Modal (oculto por defecto, ej. con la clase "hidden" de Tailwind
//   o display:none — el script solo alterna esa clase):
//     <div data-pqr-modal class="hidden"> ... </div>
//     <button data-pqr-close>X</button>          (botón X del header)
//     <button data-pqr-cancel>Cancelar</button>
//     <button data-pqr-submit>Enviar Respuesta</button>
//     <textarea data-pqr-response-input></textarea>
//     <span data-pqr-field="elector-name"></span>
//     <span data-pqr-field="elector-id"></span>
//     <span data-pqr-field="fecha"></span>
//     <span data-pqr-field="asunto"></span>
//     <span data-pqr-field="mensaje"></span>
//
//   Antiforgery token (agregar en algún lugar de la vista si no existe):
//     @Html.AntiForgeryToken()
//
//   Fila individual (generada dinámicamente por este script, pero cada
//   botón "Ver Detalle" debe llevar data-pqr-view="{id del ticket}" —
//   eso ya lo genera renderRow() más abajo, no hace falta tocarlo).
//
(function () {
  'use strict';

  function _t(text) {
    return (window.translateText && typeof window.translateText === 'function')
      ? window.translateText(text)
      : text;
  }

  const state = {
    filter: 'ABIERTO', // coincide con la pestaña activa por defecto del mockup
    search: '',
    page: 1,
    pageSize: 20,
    totalPages: 1,
    tickets: [],
  };

  let lastFocusedElement = null;
  let currentRequestId = 0;

  const els = {};

  function cacheEls() {
    // NOTA: la integración con el DOM (IDs/estructura esperada de la
    // vista Manage.cshtml) no se verificó en esta pasada de
    // documentación — la vista está fuera del alcance de este archivo.
    els.list = document.querySelector('[data-pqr-list]');
    els.filterButtons = document.querySelectorAll('[data-pqr-filter]');
    els.search = document.querySelector('[data-pqr-search]');
    els.pagination = document.querySelector('[data-pqr-pagination]');
    els.prev = document.querySelector('[data-pqr-prev]');
    els.next = document.querySelector('[data-pqr-next]');
    els.pageInfo = document.querySelector('[data-pqr-page-info]');
    els.modal = document.querySelector('[data-pqr-modal]');
    els.modalTicketId = document.querySelector('[data-pqr-modal-ticket-id]');
    els.close = document.querySelector('[data-pqr-close]');
    els.cancel = document.querySelector('[data-pqr-cancel]');
    els.submit = document.querySelector('[data-pqr-submit]');
    els.responseInput = document.querySelector('[data-pqr-response-input]');
    els.fields = {
      electorName: document.querySelector('[data-pqr-field="elector-name"]'),
      fecha: document.querySelector('[data-pqr-field="fecha"]'),
      asunto: document.querySelector('[data-pqr-field="asunto"]'),
      mensaje: document.querySelector('[data-pqr-field="mensaje"]'),
    };
  }

  // Obtiene el token antiforgery que se envía en la resolución del ticket.
  function getCsrfToken() {
    const input = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!input) {
      console.error('[PQR] No se encontró el antiforgery token. Agrega @Html.AntiForgeryToken() en la vista.');
      return null;
    }
    return input.value;
  }

  // Formatea una fecha ISO para presentarla en la interfaz en español de Colombia.
  function formatDate(iso) {
    if (!iso) return '—';
    const d = new Date(iso);
    return d.toLocaleDateString('es-CO', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  // Calcula las iniciales del nombre para el avatar de la fila.
  function initials(name) {
    if (!name) return '??';
    const parts = name.trim().split(/\s+/);
    return ((parts[0]?.[0] || '') + (parts[1]?.[0] || '')).toUpperCase();
  }

  // Consulta los tickets según el filtro, búsqueda y paginación activos.
  async function fetchTickets() {
    const requestId = ++currentRequestId;
    const params = new URLSearchParams();
    if (state.filter !== 'TODAS') params.set('status', state.filter);
    if (state.search.trim()) params.set('search', state.search.trim());
    params.set('page', state.page);
    params.set('pageSize', state.pageSize);

    try {
      const res = await fetch(`/Pqr/List?${params.toString()}`, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
      });
      const data = await res.json();
      if (requestId !== currentRequestId) {
        return; // Descartar respuesta obsoleta fuera de orden
      }

      if (!data.ok) {
        console.error('[PQR] Error al listar:', data.message);
        state.tickets = [];
        state.totalPages = 0;
      } else {
        state.tickets = data.items || [];
        state.totalPages = typeof data.totalPages === 'number' ? data.totalPages : 0;
        state.page = typeof data.pageNumber === 'number' ? data.pageNumber : 1;
      }
    } catch (err) {
      if (requestId !== currentRequestId) {
        return;
      }
      console.error('[PQR] Fallo de red al listar tickets:', err);
      state.tickets = [];
      state.totalPages = 0;
    }
    renderList();
    renderPagination();
  }

  // Actualiza los controles del paginador y los oculta si totalPages <= 1.
  function renderPagination() {
    if (!els.pagination) return;
    if (state.totalPages <= 1) {
      els.pagination.classList.add('hidden');
      return;
    }

    els.pagination.classList.remove('hidden');
    if (els.pageInfo) {
      els.pageInfo.textContent = `Página ${state.page} de ${state.totalPages}`;
    }
    if (els.prev) {
      els.prev.disabled = state.page <= 1;
    }
    if (els.next) {
      els.next.disabled = state.page >= state.totalPages;
    }
  }

  // Representa la lista y conecta sus botones de detalle.
  function renderList() {
    if (!els.list) return;

    if (state.tickets.length === 0) {
      els.list.innerHTML = `<p data-pqr-empty class="py-6 text-center text-sm text-on-surface-variant">${_t('No hay PQR para mostrar.')}</p>`;
      return;
    }

    els.list.innerHTML = state.tickets.map(renderRow).join('');

    els.list.querySelectorAll('[data-pqr-view]').forEach((btn) => {
      btn.addEventListener('click', () => openModal(btn.getAttribute('data-pqr-view')));
    });
  }

  // Genera el marcado de una fila/tarjeta PQR con sus datos visibles.
  function renderRow(t) {
    const statusLabel = t.status === 'ABIERTO' ? _t('Abierta') : _t('Resuelta');
    const userName = t.userName || '';
    const safeName    = escapeHtml(userName);
    const safeSubject = escapeHtml(t.subject || '');
    const statusBadgeClass = t.status === 'ABIERTO'
      ? 'bg-status-pending/10 text-status-pending'
      : 'bg-status-graduated/20 text-status-graduated';

    return `
      <div data-pqr-row data-status="${t.status}"
           class="pqr-grid-row flex flex-col gap-3 rounded-xl border border-outline-variant bg-surface p-4 text-sm shadow-sm
                  md:rounded-none md:border-x-0 md:border-t-0 md:border-b md:border-border-subtle md:bg-transparent md:px-2 md:py-3 md:shadow-none">

        <!-- CABECERA MÓVIL: ID a la izquierda y Estado a la derecha. En desktop se desempaqueta en el grid -->
        <div class="pqr-desktop-contents flex items-center justify-between gap-2 pb-2.5 border-b border-outline-variant/30 md:border-b-0 md:pb-0">
          <div class="pqr-col-id flex items-center gap-1.5 shrink-0">
            <span class="text-[11px] font-semibold text-on-surface-variant uppercase md:hidden">ID</span>
            <span data-pqr-col="id"
                  class="font-mono text-xs font-semibold text-primary px-1.5 py-0.5 rounded bg-primary/10"
                  aria-label="Ticket: #${t.id}">
              #${t.id}
            </span>
          </div>

          <div class="pqr-col-status flex items-center"
               data-pqr-col="estado"
               data-status-value="${t.status}"
               aria-label="Estado: ${escapeHtml(statusLabel)}">
            <span class="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium ${statusBadgeClass}">
              ${statusLabel}
            </span>
          </div>
        </div>

        <!-- VOTANTE: Avatar circular + Nombre con etiqueta visible en móvil -->
        <div class="pqr-col-voter flex items-center gap-2.5 min-w-0">
          <span data-pqr-avatar
                class="w-8 h-8 rounded-full bg-primary-container text-on-primary-container flex items-center justify-center text-xs font-semibold shrink-0"
                aria-hidden="true">
            ${initials(userName)}
          </span>
          <div class="flex flex-col min-w-0 md:contents">
            <span class="text-[10px] font-semibold uppercase tracking-wider text-on-surface-variant md:hidden">Votante</span>
            <span data-pqr-col="elector"
                  class="font-medium text-on-surface md:truncate md:font-normal"
                  aria-label="Votante: ${safeName}">
              ${safeName}
            </span>
          </div>
        </div>

        <!-- ASUNTO: En móvil bloque completo con etiqueta; en desktop columna truncable -->
        <div class="pqr-col-subject flex flex-col gap-0.5 min-w-0 md:block">
          <span class="text-[10px] font-semibold uppercase tracking-wider text-on-surface-variant md:hidden">Asunto</span>
          <p data-pqr-col="asunto"
             class="text-on-surface-variant md:text-on-surface text-sm break-words md:truncate"
             aria-label="Asunto: ${safeSubject}">
            ${safeSubject}
          </p>
        </div>

        <!-- ACCIONES: En móvil pie de tarjeta con etiqueta y botón; en desktop botón alineado a la derecha -->
        <div class="pqr-col-actions flex items-center justify-between sm:justify-end gap-2 pt-2 border-t border-outline-variant/30 md:border-t-0 md:pt-0">
          <span class="text-[10px] font-semibold uppercase tracking-wider text-on-surface-variant md:hidden">Acciones</span>
          <button data-pqr-view="${t.id}"
                  class="w-full sm:w-auto px-4 py-2 min-h-[40px] rounded-lg bg-primary text-on-primary text-xs font-medium hover:opacity-90 transition-opacity flex items-center justify-center shadow-sm"
                  aria-label="Ver detalle de PQR: ${safeSubject}">
            ${_t('Ver Detalle')}
          </button>
        </div>

      </div>
    `;
  }

  // Escapa texto antes de incluir datos del ticket dentro del marcado generado.
  function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
  }

  // Carga el detalle del ticket seleccionado y abre el modal de respuesta.
  function openModal(ticketId) {
    const ticket = state.tickets.find((t) => String(t.id) === String(ticketId));
    if (!ticket || !els.modal) return;

    lastFocusedElement = document.activeElement;
    els.modal.dataset.currentTicketId = ticket.id;
    if (els.modalTicketId) els.modalTicketId.textContent = `#${ticket.id}`;
    if (els.fields.electorName) els.fields.electorName.textContent = ticket.userName || '';
    if (els.fields.fecha) els.fields.fecha.textContent = formatDate(ticket.createdAt);
    if (els.fields.asunto) els.fields.asunto.textContent = ticket.subject || '';
    if (els.fields.mensaje) els.fields.mensaje.textContent = ticket.message || '';

    if (ticket.status === 'RESUELTO') {
      els.responseInput.value = ticket.adminResponse || '';
      els.responseInput.disabled = true;
      els.submit.disabled = true;
      els.submit.textContent = _t('Ya resuelta');
    } else {
      els.responseInput.value = '';
      els.responseInput.disabled = false;
      els.submit.disabled = false;
      els.submit.textContent = _t('Enviar Respuesta');
    }

    els.modal.classList.remove('hidden');
    // Mover el foco al modal para accesibilidad accesible con teclado
    if (els.close) {
      els.close.focus();
    } else {
      els.modal.focus();
    }
  }

  // Cierra el modal, elimina el identificador del ticket activo y restaura el foco.
  // Si el botón activador fue destruido tras re-renderizar la grilla, se transfiere al buscador o contenedor.
  function closeModal() {
    if (!els.modal) return;
    els.modal.classList.add('hidden');
    delete els.modal.dataset.currentTicketId;
    if (lastFocusedElement && typeof lastFocusedElement.focus === 'function' && lastFocusedElement.isConnected) {
      lastFocusedElement.focus();
    } else if (els.search && typeof els.search.focus === 'function') {
      els.search.focus();
    } else if (els.list && typeof els.list.focus === 'function') {
      els.list.focus();
    }
    lastFocusedElement = null;
  }

  // Valida y envía la respuesta administrativa, luego actualiza el listado.
  async function submitResponse() {
    const ticketId = els.modal?.dataset.currentTicketId;
    const adminResponse = els.responseInput?.value?.trim();

    if (!ticketId) return;
    if (!adminResponse) {
      alert(_t('Escribe una respuesta antes de enviar.'));
      return;
    }

    const token = getCsrfToken();
    if (!token) {
      alert(_t('No se pudo verificar el token de seguridad. Recarga la página.'));
      return;
    }

    els.submit.disabled = true;
    els.submit.textContent = _t('Enviando...');

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
        alert(_t(data.message) || _t('No se pudo enviar la respuesta.'));
        els.submit.disabled = false;
        els.submit.textContent = _t('Enviar Respuesta');
        return;
      }

      closeModal();
      await fetchTickets(); // refresca la lista para reflejar el nuevo estado
    } catch (err) {
      console.error('[PQR] Fallo de red al resolver ticket:', err);
      alert(_t('Error de red. Intenta de nuevo.'));
      els.submit.disabled = false;
      els.submit.textContent = _t('Enviar Respuesta');
    }
  }

  // Sincroniza el estilo visual de los filtros con su atributo data-active.
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

  // Conecta filtros, búsqueda, paginación, controles del modal y envío de la respuesta.
  function bindEvents() {
    els.filterButtons.forEach((btn) => {
      btn.addEventListener('click', () => {
        state.filter = btn.getAttribute('data-pqr-filter');
        state.page = 1;
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
          state.page = 1;
          fetchTickets();
        }, 200);
      });
    }

    if (els.prev) {
      els.prev.addEventListener('click', () => {
        if (state.page > 1) {
          state.page--;
          fetchTickets();
        }
      });
    }

    if (els.next) {
      els.next.addEventListener('click', () => {
        if (state.page < state.totalPages) {
          state.page++;
          fetchTickets();
        }
      });
    }

    els.close?.addEventListener('click', closeModal);
    els.cancel?.addEventListener('click', closeModal);
    els.submit?.addEventListener('click', submitResponse);

    // Cierra el modal al pulsar la tecla Escape si se encuentra abierto
    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape' && els.modal && !els.modal.classList.contains('hidden')) {
        closeModal();
      }
    });
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
