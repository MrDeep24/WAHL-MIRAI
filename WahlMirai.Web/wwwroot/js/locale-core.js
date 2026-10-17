// wwwroot/js/locale-core.js
// Modular client-side i18n engine: DOM translation, reactive mutation handling, and localized APIs

(function () {
  const COOKIE_NAME = 'CurrentCulture';
  const ASPNET_COOKIE = '.AspNetCore.Culture';
  const SUPPORTED_LANGS = ['es', 'en', 'de', 'fr', 'ja', 'zh'];

  // 1. Limpiar cualquier residuo previo de Google Translate
  try {
    document.cookie = "googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
    document.cookie = "googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=" + window.location.hostname;
    document.cookie = "googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=." + window.location.hostname;
  } catch (e) {}

  // 2. Determinación de la cultura activa a partir del atributo <html lang="...">
  function getCulture() {
    const docLang = (document.documentElement.lang || document.documentElement.getAttribute('lang') || '').toLowerCase().trim();
    if (docLang && SUPPORTED_LANGS.includes(docLang)) {
      return docLang;
    }
    return 'es';
  }

  function setCultureCookie(culture) {
    const value = `c=${culture}|uic=${culture}`;
    const days = 365;
    const d = new Date();
    d.setTime(d.getTime() + (days * 24 * 60 * 60 * 1000));
    const expires = "expires=" + d.toUTCString();

    document.cookie = `${COOKIE_NAME}=${value};${expires};path=/;SameSite=Lax`;
    document.cookie = `${ASPNET_COOKIE}=${value};${expires};path=/;SameSite=Lax`;
  }

  function syncHtmlLang(lang) {
    const normalized = SUPPORTED_LANGS.includes(lang) ? lang : 'es';
    document.documentElement.lang = normalized;
    document.documentElement.setAttribute('lang', normalized);
  }

  // 3. Carga diferida (lazy) de los datos del idioma replicando la precedencia exacta del monolito
  const languageDataCache = Object.create(null);

  function getLanguageData(lang) {
    if (languageDataCache[lang]) {
      return languageDataCache[lang];
    }

    const root = window.WahlMiraiLocaleData && window.WahlMiraiLocaleData[lang];
    if (!root) {
      return null;
    }

    // Precedencia exacta replicada del monolito:
    // dict: DICTIONARY
    // extra: Object.assign({}, EXTRA_TRANSLATIONS, chatbotDraftTranslations, ajaxAndModalTranslations)
    const dict = root.DICTIONARY || {};
    const extra = Object.assign({}, root.EXTRA_TRANSLATIONS, root.chatbotDraftTranslations, root.ajaxAndModalTranslations);

    const lMap = Object.create(null);
    for (const k of Object.keys(dict)) {
      lMap[k.toLowerCase()] = dict[k];
    }
    const extraLower = Object.create(null);
    for (const k of Object.keys(extra)) {
      extraLower[k.toLowerCase()] = extra[k];
    }

    const data = { dict, extra, lMap, extraLower };
    languageDataCache[lang] = data;
    return data;
  }

  // 4. Funciones de traducción y reglas dinámicas (computeTranslation)
  const translationCache = new Map();

  function computeTranslation(t, lang) {
    const data = getLanguageData(lang);
    if (!data) return null;

    const extra = data.extra;
    const dict = data.dict;
    const extraLower = data.extraLower;
    const lMap = data.lMap;

    if (extra[t]) return extra[t];
    if (dict[t]) return dict[t];

    const lower = t.toLowerCase();
    if (extraLower[lower]) return extraLower[lower];
    if (lMap[lower]) return lMap[lower];

    // Reemplazos dinámicos con expresiones regulares idénticos al monolito
    // 1. Saludo: "¡Hola, Juan!" o "¡Hola, Ana! 👋"
    const greetingMatch = t.match(/^¡?Hola,?\s+([^!👋]+)[!👋\s]*$/i);
    if (greetingMatch) {
      const name = greetingMatch[1].trim();
      if (lang === 'en') return `Hello, ${name}! 👋`;
      if (lang === 'de') return `Hallo, ${name}! 👋`;
      if (lang === 'fr') return `Bonjour, ${name} ! 👋`;
      if (lang === 'ja') return `こんにちは、${name}さん! 👋`;
      if (lang === 'zh') return `你好，${name}！ 👋`;
    }

    // 2. Grado: "Grado 11-A", "Curso 10"
    const gradeMatch = t.match(/^(?:Grado|Curso)\s+(\d+.*)$/i);
    if (gradeMatch) {
      const val = gradeMatch[1].trim();
      if (lang === 'en') return `Grade ${val}`;
      if (lang === 'de') return `Klasse ${val}`;
      if (lang === 'fr') return `Classe ${val}`;
      if (lang === 'ja') return `学年 ${val}`;
      if (lang === 'zh') return `年级 ${val}`;
    }

    // 3. "X opciones"
    const optionsMatch = t.match(/^(\d+)\s+opciones$/i);
    if (optionsMatch) {
      const count = optionsMatch[1];
      if (lang === 'en') return `${count} options`;
      if (lang === 'de') return `${count} Optionen`;
      if (lang === 'fr') return `${count} options`;
      if (lang === 'ja') return `${count} 選択肢`;
      if (lang === 'zh') return `${count} 选项`;
    }

    // 4. "Candidato X"
    const candMatch = t.match(/^Candidato\s+(\d+)$/i);
    if (candMatch) {
      const num = candMatch[1];
      if (lang === 'en') return `Candidate ${num}`;
      if (lang === 'de') return `Kandidat ${num}`;
      if (lang === 'fr') return `Candidat ${num}`;
      if (lang === 'ja') return `候補者 ${num}`;
      if (lang === 'zh') return `候选人 ${num}`;
    }

    // 5. "Todos (3)" y "Título del Proceso *"
    const suffixCountMatch = t.match(/^(.+?)\s*\((\d+)\)$/);
    if (suffixCountMatch) {
      const base = suffixCountMatch[1].trim();
      const count = suffixCountMatch[2];
      const translatedBase = getTranslation(base, lang);
      if (translatedBase) return `${translatedBase} (${count})`;
    }

    const requiredFieldMatch = t.match(/^(.*?)(\s*\*)$/);
    if (requiredFieldMatch) {
      const base = requiredFieldMatch[1].trim();
      const suffix = requiredFieldMatch[2];
      const translatedBase = getTranslation(base, lang);
      if (translatedBase) return `${translatedBase}${suffix}`;
    }

    // 6. "5 votos"
    const voteCountMatch = t.match(/^(\d+)\s+votos?$/i);
    if (voteCountMatch) {
      const count = voteCountMatch[1];
      if (lang === 'en') return `${count} votes`;
      if (lang === 'de') return `${count} Stimmen`;
      if (lang === 'fr') return `${count} votes`;
      if (lang === 'ja') return `${count} 票`;
      if (lang === 'zh') return `${count} 票`;
    }

    // 6.b "0 procesos", "1 proceso"
    const processCountMatch = t.match(/^(\d+)\s+procesos?$/i);
    if (processCountMatch) {
      const count = processCountMatch[1];
      if (lang === 'en') return `${count} process${count === '1' ? '' : 'es'}`;
      if (lang === 'de') return `${count} Prozess${count === '1' ? '' : 'e'}`;
      if (lang === 'fr') return `${count} processus`;
      if (lang === 'ja') return `${count} 件のプロセス`;
      if (lang === 'zh') return `${count} 个流程`;
    }

    // 7. "Inscripción inicia el ..." / "Votación inicia el ..."
    const regStartMatch = t.match(/^(?:Inscripción|Votación)\s+inicia\s+el\s+(.*)$/i);
    if (regStartMatch) {
      const date = regStartMatch[1];
      if (lang === 'en') return `Registration starts on ${date}`;
      if (lang === 'de') return `Anmeldung beginnt am ${date}`;
      if (lang === 'fr') return `Inscription débute le ${date}`;
      if (lang === 'ja') return `登録開始日: ${date}`;
      if (lang === 'zh') return `报名开始时间：${date}`;
    }

    // 8. Bloqueo temporal por intentos fallidos
    const lockoutMatch = t.match(/^Demasiados intentos fallidos\.\s+Por seguridad,\s+la operación está bloqueada temporalmente\.\s+Intenta de nuevo en\s+(\d+)\s+minuto\(s\)\.$/i);
    if (lockoutMatch) {
      const mins = lockoutMatch[1];
      if (lang === 'en') return `Too many failed attempts. For security, this operation is temporarily blocked. Try again in ${mins} minute(s).`;
      if (lang === 'de') return `Zu viele fehlgeschlagene Versuche. Aus Sicherheitsgründen ist dieser Vorgang vorübergehend gesperrt. Versuche es in ${mins} Minute(n) erneut.`;
      if (lang === 'fr') return `Trop de tentatives échouées. Par sécurité, cette operación est temporairement bloquée. Réessaie dans ${mins} minute(s).`;
      if (lang === 'ja') return `失敗した試行が多すぎます。セキュリティのため、この操作は一時的にブロックされています。${mins}分後にもう一度お試しください。`;
      if (lang === 'zh') return `失败尝试次数过多。出于安全考虑，此操作已被暂时锁定。请在 ${mins} 分钟后重试。`;
    }

    // 9. Paginación: "Página X de Y"
    const pageMatch = t.match(/^Página\s+(\d+)\s+de\s+(\d+)$/i);
    if (pageMatch) {
      const page = pageMatch[1];
      const total = pageMatch[2];
      if (lang === 'en') return `Page ${page} of ${total}`;
      if (lang === 'de') return `Seite ${page} von ${total}`;
      if (lang === 'fr') return `Page ${page} sur ${total}`;
      if (lang === 'ja') return `${page} / ${total} ページ`;
      if (lang === 'zh') return `第 ${page} / ${total} 页`;
    }

    return null;
  }

  function getTranslation(text, lang) {
    if (!text) return null;
    const t = text.trim();
    if (!t) return null;

    const cacheKey = `${lang}:${t}`;
    if (translationCache.has(cacheKey)) {
      return translationCache.get(cacheKey);
    }

    let res = computeTranslation(t, lang);
    translationCache.set(cacheKey, res);
    return res;
  }

  // 5. Traducción nativa y limpia del DOM inicial
  const processedNodes = new WeakSet();

  function queryWithSelf(el, selector) {
    const list = Array.prototype.slice.call(el.querySelectorAll(selector));
    if (el.matches && el.matches(selector)) {
      list.unshift(el);
    }
    return list;
  }

  function translateDOM(lang) {
    if (lang === 'es') return;
    const data = getLanguageData(lang);
    if (!data) return;

    const dateEl = document.getElementById('dashboard-today-date');
    if (dateEl) {
      const locMap = { es: 'es-ES', en: 'en-US', de: 'de-DE', fr: 'fr-FR', ja: 'ja-JP', zh: 'zh-CN' };
      const dateStr = new Date().toLocaleDateString(locMap[lang] || 'es-ES', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
      dateEl.textContent = dateStr.charAt(0).toUpperCase() + dateStr.slice(1);
    }

    // 1. TextNodes con filtrado idéntico al motor original
    const walker = document.createTreeWalker(
      document.body,
      NodeFilter.SHOW_TEXT,
      {
        acceptNode: function (node) {
          if (processedNodes.has(node)) return NodeFilter.FILTER_REJECT;
          if (!node.nodeValue || !node.nodeValue.trim()) return NodeFilter.FILTER_REJECT;
          const parent = node.parentElement;
          if (!parent) return NodeFilter.FILTER_REJECT;
          const tag = parent.tagName.toLowerCase();
          if (tag === 'script' || tag === 'style' || tag === 'textarea' || parent.classList.contains('material-symbols-outlined')) {
            return NodeFilter.FILTER_REJECT;
          }
          return NodeFilter.FILTER_ACCEPT;
        }
      }
    );

    let node;
    while ((node = walker.nextNode())) {
      processedNodes.add(node);
      const trimmed = node.nodeValue.trim();
      const translated = getTranslation(trimmed, lang);
      if (translated) {
        const leading = node.nodeValue.match(/^\s*/)[0];
        const trailing = node.nodeValue.match(/\s*$/)[0];
        node.nodeValue = leading + translated + trailing;
      }
    }

    // 2. Input botones
    document.querySelectorAll('input[type="submit"], input[type="button"]').forEach(btn => {
      if (processedNodes.has(btn)) return;
      processedNodes.add(btn);
      const val = btn.value;
      const trans = getTranslation(val, lang);
      if (trans) {
        btn.value = trans;
      }
    });

    // 3. Placeholders
    document.querySelectorAll('input[placeholder], textarea[placeholder]').forEach(el => {
      const ph = el.getAttribute('placeholder');
      const trans = getTranslation(ph, lang);
      if (trans) {
        el.setAttribute('placeholder', trans);
      }
    });

    // 4. Attributes title y aria-label
    document.querySelectorAll('[title], [aria-label]').forEach(el => {
      const t = el.getAttribute('title') || el.getAttribute('aria-label');
      const trans = getTranslation(t, lang);
      if (trans) {
        if (el.hasAttribute('title')) {
          el.setAttribute('title', trans);
        }
        if (el.hasAttribute('aria-label')) {
          el.setAttribute('aria-label', trans);
        }
      }
    });
  }

  // 6. MutationObserver optimizado: procesa solo mutation.addedNodes, Set de pendientes y rAF
  const pendingRoots = new Set();
  let rafHandle = null;
  let activeObserver = null;

  function processAddedSubtree(rootNode, lang) {
    if (!rootNode || processedNodes.has(rootNode)) return;

    if (rootNode.nodeType === Node.TEXT_NODE) {
      if (!rootNode.nodeValue || !rootNode.nodeValue.trim()) return;
      const parent = rootNode.parentElement;
      if (!parent) return;
      const tag = parent.tagName ? parent.tagName.toLowerCase() : '';
      if (tag === 'script' || tag === 'style' || tag === 'textarea' || (parent.classList && parent.classList.contains('material-symbols-outlined'))) {
        return;
      }
      processedNodes.add(rootNode);
      const trans = getTranslation(rootNode.nodeValue.trim(), lang);
      if (trans) {
        const leading = rootNode.nodeValue.match(/^\s*/)[0];
        const trailing = rootNode.nodeValue.match(/\s*$/)[0];
        rootNode.nodeValue = leading + trans + trailing;
      }
      return;
    }

    if (rootNode.nodeType === Node.ELEMENT_NODE) {
      // 1. TextNodes en el subárbol
      const walker = document.createTreeWalker(
        rootNode,
        NodeFilter.SHOW_TEXT,
        {
          acceptNode: function (node) {
            if (processedNodes.has(node)) return NodeFilter.FILTER_REJECT;
            if (!node.nodeValue || !node.nodeValue.trim()) return NodeFilter.FILTER_REJECT;
            const parent = node.parentElement;
            if (!parent) return NodeFilter.FILTER_REJECT;
            const tag = parent.tagName.toLowerCase();
            if (tag === 'script' || tag === 'style' || tag === 'textarea' || parent.classList.contains('material-symbols-outlined')) {
              return NodeFilter.FILTER_REJECT;
            }
            return NodeFilter.FILTER_ACCEPT;
          }
        }
      );

      let textNode;
      while ((textNode = walker.nextNode())) {
        processedNodes.add(textNode);
        const trimmed = textNode.nodeValue.trim();
        const trans = getTranslation(trimmed, lang);
        if (trans) {
          const leading = textNode.nodeValue.match(/^\s*/)[0];
          const trailing = textNode.nodeValue.match(/\s*$/)[0];
          textNode.nodeValue = leading + trans + trailing;
        }
      }

      // 2. Input botones
      queryWithSelf(rootNode, 'input[type="submit"], input[type="button"]').forEach(btn => {
        if (processedNodes.has(btn)) return;
        processedNodes.add(btn);
        const val = btn.value;
        const trans = getTranslation(val, lang);
        if (trans) {
          btn.value = trans;
        }
      });

      // 3. Placeholders
      queryWithSelf(rootNode, 'input[placeholder], textarea[placeholder]').forEach(el => {
        const ph = el.getAttribute('placeholder');
        const trans = getTranslation(ph, lang);
        if (trans) {
          el.setAttribute('placeholder', trans);
        }
      });

      // 4. Attributes title y aria-label
      queryWithSelf(rootNode, '[title], [aria-label]').forEach(el => {
        const t = el.getAttribute('title') || el.getAttribute('aria-label');
        const trans = getTranslation(t, lang);
        if (trans) {
          if (el.hasAttribute('title')) {
            el.setAttribute('title', trans);
          }
          if (el.hasAttribute('aria-label')) {
            el.setAttribute('aria-label', trans);
          }
        }
      });

      processedNodes.add(rootNode);
    }
  }

  let fallbackTimer = null;

  function flushPendingNodes(lang) {
    if (!activeObserver) return;
    activeObserver.disconnect();
    try {
      for (const node of pendingRoots) {
        if (!node.isConnected) continue;
        processAddedSubtree(node, lang);
      }
    } finally {
      pendingRoots.clear();
      rafHandle = null;
      if (fallbackTimer) {
        clearTimeout(fallbackTimer);
        fallbackTimer = null;
      }
      activeObserver.observe(document.body, { childList: true, subtree: true });
    }
  }

  function setupMutationObserver(lang) {
    if (activeObserver) activeObserver.disconnect();
    activeObserver = new MutationObserver(function (mutations) {
      for (let i = 0; i < mutations.length; i++) {
        const m = mutations[i];
        if (m.type === 'childList' && m.addedNodes) {
          for (let j = 0; j < m.addedNodes.length; j++) {
            pendingRoots.add(m.addedNodes[j]);
          }
        }
      }

      if (pendingRoots.size > 0 && !rafHandle) {
        let fired = false;
        const doFlush = function () {
          if (fired) return;
          fired = true;
          flushPendingNodes(lang);
        };
        rafHandle = requestAnimationFrame(doFlush);
        fallbackTimer = setTimeout(doFlush, 16);
      }
    });

    activeObserver.observe(document.body, { childList: true, subtree: true });
  }

  // 7. Labels de tema localizado
  function applyLocalizedThemeLabels() {
    const labels = {
      dark: {
        es: 'Cambiar a modo claro',
        en: 'Switch to light mode',
        de: 'Zum Hellmodus wechseln',
        fr: 'Passer au mode clair',
        ja: 'ライトモードに切り替え',
        zh: '切换到浅色模式'
      },
      light: {
        es: 'Cambiar a modo oscuro',
        en: 'Switch to dark mode',
        de: 'Zum Dunkelmodus wechseln',
        fr: 'Passer au mode sombre',
        ja: 'ダークモードに切り替え',
        zh: '切换到深色模式'
      }
    };

    const currentLang = getCulture();
    const isDark = document.documentElement.classList.contains('dark');
    const key = isDark ? 'dark' : 'light';
    const label = labels[key][currentLang] || labels[key].es;

    document.querySelectorAll('.theme-toggle').forEach(function (btn) {
      btn.setAttribute('title', label);
      btn.setAttribute('aria-label', label);
    });
  }

  // 8. Inicialización de selectores y eventos
  const currentCulture = getCulture();

  if (typeof performance !== 'undefined' && performance.mark) {
    performance.mark('wm-i18n-core-init');
  }

  function initSelects() {
    const selects = document.querySelectorAll('.locale-select');
    selects.forEach(function (select) {
      select.value = currentCulture;

      if (select.dataset.localeBound) return;
      select.dataset.localeBound = "true";

      select.addEventListener('change', function () {
        const culture = this.value;
        setCultureCookie(culture);
        syncHtmlLang(culture);
        if (window.WahlMiraiLocale && typeof window.WahlMiraiLocale.applyLocalizedThemeLabels === 'function') {
          window.WahlMiraiLocale.applyLocalizedThemeLabels();
        }
        location.reload();
      });
    });

    if (currentCulture !== 'es') {
      if (typeof performance !== 'undefined' && performance.mark) {
        performance.mark('wm-i18n-translate-start');
      }
      translateDOM(currentCulture);
      if (typeof performance !== 'undefined' && performance.mark) {
        performance.mark('wm-i18n-translate-end');
        try {
          performance.measure('wm-i18n-dom-translation', 'wm-i18n-translate-start', 'wm-i18n-translate-end');
        } catch (e) {}
      }
      setupMutationObserver(currentCulture);
    }

    if (window.WahlMiraiLocale && typeof window.WahlMiraiLocale.applyLocalizedThemeLabels === 'function') {
      window.WahlMiraiLocale.applyLocalizedThemeLabels();
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initSelects);
  } else {
    initSelects();
  }

  // 9. API pública expuesta en window (idéntica a 0c)
  window.WahlMiraiLocale = window.WahlMiraiLocale || {};
  window.WahlMiraiLocale.getCulture = getCulture;
  window.WahlMiraiLocale.syncHtmlLang = syncHtmlLang;
  window.WahlMiraiLocale.applyLocalizedThemeLabels = applyLocalizedThemeLabels;
  window.WahlMiraiLocale.translate = function (text) {
    return window.translateText ? window.translateText(text) : text;
  };

  window.translateText = function (text) {
    if (!text || typeof text !== 'string') return text;
    const lang = getCulture();
    const data = getLanguageData(lang);
    if (!data) return text;
    return data.extra[text] || data.dict[text] || text;
  };

  // Envoltorios no intrusivos de alert y confirm activos para todos los idiomas
  const _nativeAlert = window.alert;
  window.alert = function (msg) {
    return _nativeAlert.call(window, typeof msg === 'string' ? window.translateText(msg) : msg);
  };
  const _nativeConfirm = window.confirm;
  window.confirm = function (msg) {
    return _nativeConfirm.call(window, typeof msg === 'string' ? window.translateText(msg) : msg);
  };

  // Compatibilidad con theme.js y ayuda-chatbot.js mediante Proxy dinámico
  window.__WahlMiraiLocaleMap = new Proxy({}, {
    get: function (_, prop) {
      if (typeof prop === 'string') {
        const data = getLanguageData(prop);
        return data ? data.dict : undefined;
      }
      return undefined;
    }
  });

  window.__WahlMiraiLocaleExtra = new Proxy({}, {
    get: function (_, prop) {
      if (typeof prop === 'string') {
        const data = getLanguageData(prop);
        return data ? data.extra : undefined;
      }
      return undefined;
    }
  });
})();
