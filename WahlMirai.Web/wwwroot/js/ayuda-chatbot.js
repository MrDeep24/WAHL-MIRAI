/**
 * Ayuda — Chatbot Asistente Basado en Reglas (RF-M08-03)
 * Motor de reglas 100% cliente guiado por palabras clave y menú de opciones.
 * Sin llamadas a servicios externos, sin IA generativa y sin persistencia en BD.
 *
 * Convenciones:
 * - IIFE auto-ejecutable en modo estricto.
 * - Objeto centralizado `state`.
 * - Selectores en caché mediante `cacheEls()`.
 * - Enlace exclusivo a atributos `data-*` (desacoplado de clases de diseño).
 * - Saneamiento previo con `escapeHtml()`.
 * - Escalamiento a PQR mediante `sessionStorage` ('pqr_draft_escalation') y redirección.
 */
(function () {
  'use strict';

  const TOPICS = {
    registro: {
      key: 'registro',
      label: 'Crear cuenta',
      title: '¿Cómo creo mi cuenta?',
      image: '/img/ayuda/ayuda-registro.svg',
      text: 'Ingresa tu número de documento en Crear mi cuenta. Si el colegio ya te habilitó en la lista de estudiantes autorizados, verás tu nombre y grado precargados; solo debes definir tu correo de contacto y una contraseña (mínimo 8 caracteres, una mayúscula y un símbolo especial). Si el sistema no reconoce tu documento, contacta al Administrador del colegio.',
      keywords: [
        'registro', 'registrar', 'registrarse', 'crear cuenta', 'crear mi cuenta',
        'nueva cuenta', 'cuenta nueva', 'habilitar cuenta', 'lista blanca',
        'censo', 'inscribirme', 'inscribir', 'nuevo estudiante', 'primer ingreso'
      ]
    },
    login: {
      key: 'login',
      label: 'Iniciar sesión',
      title: '¿Cómo inicio sesión?',
      image: '/img/ayuda/ayuda-login.svg',
      text: 'Ingresa tu número de documento y la contraseña que tú mismo definiste al crear tu cuenta. El sistema nunca te envía la contraseña por correo al registrarte: solo lo hace si usas la opción Recuperar acceso. Si los datos no coinciden, revisa que no haya espacios de más en el documento.',
      keywords: [
        'login', 'iniciar sesion', 'inicio de sesion', 'iniciar', 'ingresar',
        'ingreso', 'entrar', 'acceder', 'acceso', 'autenticar', 'credenciales'
      ]
    },
    recuperar: {
      key: 'recuperar',
      label: 'Recuperar contraseña',
      title: '¿Olvidé mi contraseña, qué hago?',
      image: '/img/ayuda/ayuda-recuperar.svg',
      text: 'Ve a Recuperar acceso e ingresa tu documento. Te enviaremos una nueva contraseña al correo de contacto que registraste al crear tu cuenta.',
      keywords: [
        'contrasena', 'clave', 'password', 'olvide', 'olvide mi clave',
        'olvide mi contrasena', 'recuperar', 'recuperacion', 'restablecer',
        'restaurar', 'cambiar clave', 'nueva clave', 'perdi mi clave'
      ]
    },
    postulacion: {
      key: 'postulacion',
      label: 'Postularme a candidato',
      title: '¿Cómo me postulo como candidato?',
      image: null,
      text: 'Durante la etapa de Inscripción de una elección, entra a Postularme como candidato, carga tu foto, tus propuestas y tu plan de gobierno, además de los documentos que exija el cargo al que aspiras. Tu postulación queda pendiente hasta que el Administrador la revise y apruebe; solo entonces aparecerás en el tarjetón.',
      keywords: [
        'candidato', 'candidatos', 'candidatura', 'postular', 'postularme',
        'postulacion', 'aspirar', 'aspirante', 'propuestas', 'plan de gobierno',
        'ser candidato', 'inscribirse como candidato', 'cargo'
      ]
    },
    votar: {
      key: 'votar',
      label: 'Cómo votar',
      title: '¿Cómo voto?',
      image: '/img/ayuda/ayuda-votar.svg',
      text: 'Cuando la elección esté en su etapa de Votación, elige un candidato en el tarjetón, revisa sus propuestas en la ventana emergente y confirma tu voto. Una vez confirmado, no se puede deshacer.',
      keywords: [
        'votar', 'voto', 'votacion', 'sufragio', 'tarjeton', 'urna',
        'eleccion', 'elecciones', 'como votar', 'emitir voto', 'sufragar'
      ]
    },
    perfil: {
      key: 'perfil',
      label: 'Editar perfil',
      title: '¿Cómo edito mi perfil?',
      image: '/img/ayuda/ayuda-perfil.svg',
      text: 'Puedes cambiar tu correo de contacto o tu contraseña desde Mi perfil. Tu documento, nombre y grado los administra el colegio y no se pueden editar.',
      keywords: [
        'perfil', 'mi perfil', 'correo', 'email', 'datos personales',
        'editar perfil', 'actualizar datos', 'modificar datos', 'editar mi correo'
      ]
    },
    resultados: {
      key: 'resultados',
      label: 'Ver resultados',
      title: '¿Cuándo puedo ver los resultados?',
      image: '/img/ayuda/ayuda-resultados.svg',
      text: 'Mientras la elección está activa, solo ves resultados si ya votaste. Al finalizar, los resultados se abren para todos los electores del grado habilitado.',
      keywords: [
        'resultados', 'escrutinio', 'ganador', 'ganadores', 'estadisticas',
        'conteo', 'votos totales', 'quien va ganando', 'quien gano', 'ver resultados',
        'graficas'
      ]
    }
  };

  const state = {
    isAuthenticated: false,
    isElector: false,
    isAdmin: false,
    currentTopic: null,
    lastUserQuery: '',
    transcript: [],
    waitingResolution: false
  };

  const els = {};

  function cacheEls() {
    els.panel = document.querySelector('[data-chatbot-panel]');
    if (!els.panel) return;

    els.messages = els.panel.querySelector('[data-chatbot-messages]');
    els.menu = els.panel.querySelector('[data-chatbot-menu]');
    els.form = els.panel.querySelector('[data-chatbot-form]');
    els.input = els.panel.querySelector('[data-chatbot-input]');
    els.resetBtn = els.panel.querySelector('[data-chatbot-reset]');

    state.isAuthenticated = els.panel.getAttribute('data-user-authenticated') === 'true';
    state.isElector = els.panel.getAttribute('data-user-is-elector') === 'true';
    state.isAdmin = els.panel.getAttribute('data-user-is-admin') === 'true';
  }

  function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str ?? '';
    return div.innerHTML;
  }

  function normalizeText(text) {
    return (text || '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .trim();
  }

  function findTopicByKeyword(input) {
    const normInput = normalizeText(input);
    if (!normInput) return null;

    // 1. Coincidencia directa por clave o etiqueta de tema
    for (const key of Object.keys(TOPICS)) {
      const topic = TOPICS[key];
      if (normInput === key || normInput === normalizeText(topic.label)) {
        return topic;
      }
    }

    // 2. Coincidencia exacta con alguna palabra clave
    for (const key of Object.keys(TOPICS)) {
      const topic = TOPICS[key];
      for (const kw of topic.keywords) {
        if (normInput === normalizeText(kw)) {
          return topic;
        }
      }
    }

    // 3. Subcadena: la entrada incluye una palabra clave completa (prioriza la más larga)
    let bestMatch = null;
    let longestLen = 0;
    for (const key of Object.keys(TOPICS)) {
      const topic = TOPICS[key];
      for (const kw of topic.keywords) {
        const normKw = normalizeText(kw);
        if (normInput.includes(normKw) && normKw.length > longestLen) {
          longestLen = normKw.length;
          bestMatch = topic;
        }
      }
    }
    if (bestMatch) return bestMatch;

    // 4. Coincidencia a nivel de palabras (tokens significativos > 2 caracteres)
    const inputWords = normInput.split(/\s+/).filter(w => w.length > 2);
    for (const key of Object.keys(TOPICS)) {
      const topic = TOPICS[key];
      for (const kw of topic.keywords) {
        const kwWords = normalizeText(kw).split(/\s+/).filter(w => w.length > 2);
        for (const iw of inputWords) {
          if (kwWords.includes(iw)) {
            return topic;
          }
        }
      }
    }

    return null;
  }

  function scrollMessagesToBottom() {
    if (els.messages) {
      els.messages.scrollTop = els.messages.scrollHeight;
    }
  }

  function addMessage(sender, htmlContent, plainTextForTranscript) {
    state.transcript.push({
      sender: sender,
      text: plainTextForTranscript || htmlContent.replace(/<[^>]+>/g, ' ').trim()
    });

    const wrapper = document.createElement('div');
    if (sender === 'user') {
      wrapper.className = 'flex justify-end';
      wrapper.innerHTML = `
        <div class="bg-primary text-on-primary rounded-lg p-3 text-sm max-w-[85%] shadow-sm">
          ${htmlContent}
        </div>
      `;
    } else {
      wrapper.className = 'flex justify-start';
      wrapper.innerHTML = `
        <div class="bg-surface-container text-on-surface rounded-lg p-3 text-sm max-w-[90%] shadow-sm space-y-2">
          ${htmlContent}
        </div>
      `;
    }

    els.messages.appendChild(wrapper);
    scrollMessagesToBottom();
  }

  function renderInitialGreeting() {
    state.transcript = [];
    state.currentTopic = null;
    state.lastUserQuery = '';
    state.waitingResolution = false;

    if (!els.messages) return;
    els.messages.innerHTML = '';

    const greetingHtml = `
      <p class="font-medium text-primary">¡Hola! Soy tu asistente de Ayuda.</p>
      <p class="text-on-surface-variant text-xs mt-1">
        Escribe una palabra clave (ej. <em>'clave'</em>, <em>'votar'</em>, <em>'registro'</em>) o elige un tema rápido a continuación:
      </p>
    `;
    addMessage('bot', greetingHtml, '¡Hola! Soy tu asistente de Ayuda. Escribe una palabra clave o elige un tema rápido.');
  }

  function renderTopicResponse(topic) {
    state.currentTopic = topic;
    state.waitingResolution = true;

    let mediaHtml = '';
    if (topic.image) {
      mediaHtml = `
        <div class="rounded-md overflow-hidden border border-outline/20 bg-surface-container-lowest p-2 my-2">
          <img src="${topic.image}" alt="${escapeHtml(topic.title)}" class="w-full max-h-36 object-contain" />
        </div>
      `;
    }

    const botHtml = `
      <div>
        <p class="font-semibold text-primary">${escapeHtml(topic.title)}</p>
        ${mediaHtml}
        <p class="text-xs text-on-surface-variant leading-relaxed mt-1">${escapeHtml(topic.text)}</p>
      </div>
      <div class="pt-2 border-t border-outline/20 mt-2" data-chatbot-resolution-block>
        <p class="text-xs font-medium text-on-surface-variant mb-1.5">¿Esto resolvió tu duda?</p>
        <div class="flex gap-2">
          <button type="button" data-chatbot-resolve-yes class="px-2.5 py-1 rounded bg-secondary text-on-secondary text-xs font-medium hover:opacity-90 transition-opacity">
            Sí
          </button>
          <button type="button" data-chatbot-resolve-no class="px-2.5 py-1 rounded bg-surface-container-high text-on-surface text-xs font-medium hover:bg-surface-container-highest transition-colors">
            No
          </button>
        </div>
      </div>
    `;

    addMessage('bot', botHtml, `${topic.title}: ${topic.text}`);
    bindResolutionButtons();
  }

  function renderFallback(userReason) {
    state.waitingResolution = false;

    let escalationActionHtml = '';
    if (state.isAdmin) {
      escalationActionHtml = `
        <p class="text-xs text-on-surface-variant italic mt-2">
          Nota: La radicación de solicitudes PQR está reservada para cuentas de electores.
        </p>
      `;
    } else {
      escalationActionHtml = `
        <div class="mt-2.5">
          <button type="button" data-chatbot-escalate class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-primary text-on-primary hover:opacity-90 transition-opacity text-xs font-medium shadow-sm">
            <span class="material-symbols-outlined text-[16px]">support_agent</span>
            Crear PQR con esta conversación
          </button>
        </div>
      `;
    }

    const msg = userReason === 'unresolved'
      ? 'Entiendo que la información no fue suficiente para resolver tu duda. Puedes formular otra pregunta o radicar una solicitud formal:'
      : 'No encontré una respuesta directa para tu búsqueda. Puedes elegir un tema del menú o radicar una solicitud formal para que un administrador te responda:';

    const botHtml = `
      <p class="text-xs text-on-surface-variant leading-relaxed">${msg}</p>
      ${escalationActionHtml}
    `;

    addMessage('bot', botHtml, `${msg} [Opción de escalamiento a PQR ofrecida]`);
    bindEscalationButton();
  }

  function bindResolutionButtons() {
    const yesBtn = els.messages.querySelector('button[data-chatbot-resolve-yes]:not([data-bound])');
    const noBtn = els.messages.querySelector('button[data-chatbot-resolve-no]:not([data-bound])');

    if (yesBtn) {
      yesBtn.setAttribute('data-bound', 'true');
      yesBtn.addEventListener('click', () => {
        disableResolutionBlock();
        addMessage('user', 'Sí, resolvió mi duda.', 'Sí, resolvió mi duda.');
        const botAck = `
          <p class="text-xs text-secondary font-medium">¡Me alegra haberte ayudado!</p>
          <p class="text-xs text-on-surface-variant mt-1">Si tienes otra consulta, escribe una palabra clave o selecciona un tema del menú inferior.</p>
        `;
        addMessage('bot', botAck, '¡Me alegra haberte ayudado! Si tienes otra consulta, escribe una palabra clave o selecciona un tema del menú.');
        state.waitingResolution = false;
        state.currentTopic = null;
      });
    }

    if (noBtn) {
      noBtn.setAttribute('data-bound', 'true');
      noBtn.addEventListener('click', () => {
        disableResolutionBlock();
        addMessage('user', 'No resolvió mi duda.', 'No resolvió mi duda.');
        renderFallback('unresolved');
      });
    }
  }

  function disableResolutionBlock() {
    const blocks = els.messages.querySelectorAll('[data-chatbot-resolution-block]');
    blocks.forEach(b => {
      b.classList.add('opacity-50', 'pointer-events-none');
    });
  }

  function bindEscalationButton() {
    const escalateBtn = els.messages.querySelector('button[data-chatbot-escalate]:not([data-bound])');
    if (!escalateBtn) return;

    escalateBtn.setAttribute('data-bound', 'true');
    escalateBtn.addEventListener('click', escalateToPqr);
  }

  function escalateToPqr() {
    const querySubjectContext = state.currentTopic
      ? state.currentTopic.title
      : (state.lastUserQuery || 'Consulta de Asistencia');

    const subject = `Consulta desde el asistente de Ayuda: ${querySubjectContext}`.substring(0, 200);

    let messageBody = '=== Solicitud escalada desde el Asistente de Ayuda ===\n';
    if (state.lastUserQuery) {
      messageBody += `Consulta ingresada: "${state.lastUserQuery}"\n`;
    }
    if (state.currentTopic) {
      messageBody += `Tema consultado: ${state.currentTopic.title}\n`;
    }
    messageBody += '\nHistorial de interacción:\n';
    state.transcript.forEach(t => {
      messageBody += `[${t.sender === 'bot' ? 'Asistente' : 'Usuario'}]: ${t.text}\n`;
    });
    messageBody += '\nDetalle adicional del usuario:\n(Por favor escribe aquí detalles complementarios de tu solicitud)';

    try {
      sessionStorage.setItem('pqr_draft_escalation', JSON.stringify({
        subject: subject,
        message: messageBody
      }));
    } catch (err) {
      console.warn('[Chatbot] No se pudo guardar el borrador en sessionStorage:', err);
    }

    if (state.isAuthenticated && state.isElector) {
      window.location.href = '/Pqr/Create';
    } else if (!state.isAuthenticated) {
      window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent('/Pqr/Create');
    }
  }

  function handleUserSubmit(text) {
    const raw = (text || '').trim();
    if (!raw) return;

    state.lastUserQuery = raw;
    addMessage('user', escapeHtml(raw), raw);

    const matchedTopic = findTopicByKeyword(raw);
    if (matchedTopic) {
      renderTopicResponse(matchedTopic);
    } else {
      renderFallback('not_found');
    }
  }

  function bindEvents() {
    if (els.form) {
      els.form.addEventListener('submit', (e) => {
        e.preventDefault();
        const value = els.input ? els.input.value : '';
        if (els.input) els.input.value = '';
        handleUserSubmit(value);
      });
    }

    if (els.menu) {
      els.menu.querySelectorAll('[data-chatbot-topic]').forEach(btn => {
        btn.addEventListener('click', () => {
          const key = btn.getAttribute('data-chatbot-topic');
          const topic = TOPICS[key];
          if (topic) {
            state.lastUserQuery = topic.label;
            addMessage('user', escapeHtml(topic.label), topic.label);
            renderTopicResponse(topic);
          }
        });
      });
    }

    if (els.resetBtn) {
      els.resetBtn.addEventListener('click', () => {
        renderInitialGreeting();
      });
    }
  }

  document.addEventListener('DOMContentLoaded', () => {
    cacheEls();
    if (!els.panel) return;
    renderInitialGreeting();
    bindEvents();
  });
})();
