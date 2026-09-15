// wwwroot/js/theme.js
// Handles dark/light theme toggling and persistence via cookie and localStorage

(function () {
  const THEME_COOKIE = 'theme';
  const THEME_DARK = 'dark';
  const THEME_LIGHT = 'light';

  function setCookie(name, value, days) {
    const d = new Date();
    d.setTime(d.getTime() + (days * 24 * 60 * 60 * 1000));
    const expires = "expires=" + d.toUTCString();
    document.cookie = name + "=" + (value || "") + ";" + expires + ";path=/;SameSite=Lax";
    try {
      localStorage.setItem(name, value);
    } catch (e) {}
  }

  function getSavedTheme() {
    try {
      const local = localStorage.getItem(THEME_COOKIE);
      if (local === THEME_DARK || local === THEME_LIGHT) return local;
    } catch (e) {}

    const match = document.cookie.match(new RegExp('(^| )' + THEME_COOKIE + '=([^;]+)'));
    if (match && (match[2] === THEME_DARK || match[2] === THEME_LIGHT)) {
      return match[2];
    }

    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches 
      ? THEME_DARK 
      : THEME_LIGHT;
  }

  function updateIcons(isDark) {
    const toggleButtons = document.querySelectorAll('.theme-toggle');
    toggleButtons.forEach(btn => {
      const iconSpan = btn.querySelector('.material-symbols-outlined');
      if (iconSpan) {
        iconSpan.textContent = isDark ? 'light_mode' : 'dark_mode';
      }
      btn.setAttribute('title', isDark ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro');
      btn.setAttribute('aria-label', isDark ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro');
    });
  }

  function applyTheme(theme) {
    const isDark = (theme === THEME_DARK);
    const html = document.documentElement;

    if (isDark) {
      html.classList.add('dark');
      html.classList.remove('light');
    } else {
      html.classList.remove('dark');
      html.classList.add('light');
    }

    updateIcons(isDark);
  }

  // Inicializar inmediatamente
  const initialTheme = getSavedTheme();
  applyTheme(initialTheme);

  // Escuchar cuando el DOM esté listo para refrescar iconos
  function bindToggleEvents() {
    const currentTheme = document.documentElement.classList.contains('dark') ? THEME_DARK : THEME_LIGHT;
    updateIcons(currentTheme === THEME_DARK);
  }

  // Delegación de eventos global para que funcione en cualquier botón .theme-toggle presente o dinámico
  document.addEventListener('click', function (e) {
    const btn = e.target.closest('.theme-toggle');
    if (!btn) return;
    e.preventDefault();
    const isCurrentlyDark = document.documentElement.classList.contains('dark');
    const nextTheme = isCurrentlyDark ? THEME_LIGHT : THEME_DARK;
    setCookie(THEME_COOKIE, nextTheme, 365);
    applyTheme(nextTheme);
  });

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bindToggleEvents);
  } else {
    bindToggleEvents();
  }

  // Escuchar cambios de preferencia del sistema si el usuario no ha forzado un tema
  if (window.matchMedia) {
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
      const hasExplicit = document.cookie.includes(THEME_COOKIE + '=');
      if (!hasExplicit) {
        applyTheme(e.matches ? THEME_DARK : THEME_LIGHT);
      }
    });
  }
})();

