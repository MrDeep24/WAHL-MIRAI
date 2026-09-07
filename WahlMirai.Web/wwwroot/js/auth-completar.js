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
    const confirmInput = document.getElementById('compl-confirm-input');
    const emailInput = document.getElementById('compl-email-input');
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

    function validateEmailField() {
        if (!emailInput) return;
        const value = emailInput.value.trim();
        if (!value) {
            emailInput.setCustomValidity('El correo de contacto es obligatorio.');
            return;
        }
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
            emailInput.setCustomValidity('Ingrese un correo electrónico válido.');
            return;
        }
        emailInput.setCustomValidity('');
    }

    function validatePasswordFields() {
        if (!passInput || !confirmInput) return;
        const password = passInput.value;
        const confirm = confirmInput.value;

        if (password.length === 0) {
            passInput.setCustomValidity('');
        } else if (password.length < 8) {
            passInput.setCustomValidity('La contraseña debe tener al menos 8 caracteres.');
        } else if (!/[A-Z]/.test(password)) {
            passInput.setCustomValidity('La contraseña debe contener al menos una letra mayúscula.');
        } else if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?`~]/.test(password)) {
            passInput.setCustomValidity('La contraseña debe contener al menos un símbolo especial.');
        } else {
            passInput.setCustomValidity('');
        }

        if (confirm.length > 0 && confirm !== password) {
            confirmInput.setCustomValidity('Las contraseñas no coinciden.');
        } else if (confirm.length === 0) {
            confirmInput.setCustomValidity('');
        } else {
            confirmInput.setCustomValidity('');
        }
    }

    emailInput?.addEventListener('input', validateEmailField);
    passInput?.addEventListener('input', function () {
        const v = this.value;
        setHint(hintLen,   v.length >= 8);
        setHint(hintUpper, /[A-Z]/.test(v));
        setHint(hintSym,   /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?`~]/.test(v));
        validatePasswordFields();
    });
    confirmInput?.addEventListener('input', validatePasswordFields);

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
