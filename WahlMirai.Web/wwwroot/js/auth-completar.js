// ── Password visibility toggles ───────────────────────────────────────────
function setupToggle(btnId, iconId, inputId) {
    const btn   = document.getElementById(btnId);
    const icon  = document.getElementById(iconId);
    const input = document.getElementById(inputId);
    if (!btn || !icon || !input) return;
    btn.addEventListener('click', function () {
        const isPass = input.type === 'password';
        input.type   = isPass ? 'text' : 'password';
        icon.textContent = isPass ? 'visibility_off' : 'visibility';
    });
}

document.addEventListener('DOMContentLoaded', function() {
    setupToggle('toggle-pass-btn',    'toggle-pass-icon',    'compl-password-input');
    setupToggle('toggle-confirm-btn', 'toggle-confirm-icon', 'compl-confirm-input');

    // ── Live password strength hints ──────────────────────────────────────────
    const passInput = document.getElementById('compl-password-input');
    const hintLen   = document.getElementById('hint-len');
    const hintUpper = document.getElementById('hint-upper');
    const hintSym   = document.getElementById('hint-sym');

    function setHint(el, ok) {
        const icon = el.querySelector('.material-symbols-outlined');
        if (ok) {
            icon.textContent = 'check_circle';
            icon.className   = 'material-symbols-outlined text-sm text-green-600';
            el.classList.add('text-green-700');
            el.classList.remove('text-on-surface-variant');
        } else {
            icon.textContent = 'circle';
            icon.className   = 'material-symbols-outlined text-sm text-outline';
            el.classList.remove('text-green-700');
            el.classList.add('text-on-surface-variant');
        }
    }

    passInput?.addEventListener('input', function () {
        const v = this.value;
        setHint(hintLen,   v.length >= 8);
        setHint(hintUpper, /[A-Z]/.test(v));
        setHint(hintSym,   /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?`~]/.test(v));
    });

    // ── Prevent double-submit ─────────────────────────────────────────────────
    document.getElementById('compl-submit-btn')?.addEventListener('click', function () {
        const form = this.closest('form');
        if (form && form.checkValidity()) {
            this.disabled = true;
            this.innerHTML = '<span class="material-symbols-outlined" style="animation:spin 1s linear infinite">autorenew</span> Creando cuenta…';
            form.submit();
        }
    });
});
