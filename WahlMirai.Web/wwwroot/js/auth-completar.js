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
    const hintLen   = document.getElementById('hint-len');
    const hintUpper = document.getElementById('hint-upper');
    const hintSym   = document.getElementById('hint-sym');

    function setHint(el, ok) {
        if (!el) return;
        const icon = el.querySelector('.material-symbols-outlined');
        if (ok) {
            if (icon) {
                icon.textContent = 'check_circle';
                icon.className   = 'material-symbols-outlined text-sm text-green-600';
            }
            el.classList.add('text-green-700');
            el.classList.remove('text-on-surface-variant');
        } else {
            if (icon) {
                icon.textContent = 'circle';
                icon.className   = 'material-symbols-outlined text-sm text-outline';
            }
            el.classList.remove('text-green-700');
            el.classList.add('text-on-surface-variant');
        }
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

    passInput?.addEventListener('input', function () {
        const v = this.value;
        setHint(hintLen,   v.length >= 8);
        setHint(hintUpper, /[A-Z]/.test(v));
        setHint(hintSym,   /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?`~]/.test(v));
        validatePasswordFields();
    });
    confirmInput?.addEventListener('input', validatePasswordFields);

    // ── Prevent double-submit and validate consent checkbox ──────────────────
    const termsCheck = document.getElementById('accept-terms-check');
    const termsErrorSpan = document.querySelector('[data-valmsg-for="AcceptTermsAndPrivacy"]');

    if (termsCheck && termsErrorSpan) {
        termsCheck.addEventListener('change', function () {
            if (this.checked) {
                termsErrorSpan.textContent = '';
                termsErrorSpan.classList.remove('field-validation-error');
                termsErrorSpan.classList.add('field-validation-valid');
            }
        });
    }

    document.querySelectorAll('#compl-submit-btn, #reset-submit-btn').forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            const form = this.closest('form');
            if (!form) return;

            if (this.id === 'compl-submit-btn' && termsCheck && !termsCheck.checked) {
                e.preventDefault();
                termsCheck.focus();
                const msg = (typeof window.translateText === 'function')
                    ? window.translateText('Debes aceptar los Términos de Uso y la Política de Privacidad.')
                    : 'Debes aceptar los Términos de Uso y la Política de Privacidad.';
                if (termsErrorSpan) {
                    termsErrorSpan.textContent = msg;
                    termsErrorSpan.classList.remove('field-validation-valid');
                    termsErrorSpan.classList.add('field-validation-error');
                }
                return;
            }

            if (form.checkValidity()) {
                this.disabled = true;
                const busyLabel = this.getAttribute('data-busy-label') || 'Creando cuenta…';
                this.innerHTML = '<span class="material-symbols-outlined" style="animation:spin 1s linear infinite">autorenew</span> ' + busyLabel;
                form.submit();
            }
        });
    });
});
