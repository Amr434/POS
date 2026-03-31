/**
 * نظام التنبيهات - Config فقط
 * الاستخدام: showAlert('رسالة', 'success|error|warning|info')
 */

// ✅ الـ Config - كل الإعدادات في مكان واحد
const ALERT_CONFIG = {
    success: {
        icon: 'bi-check-circle-fill',
        title: 'نجح',
        headerClass: 'success',
        buttonClass: 'success',
        buttonText: 'حسناً'
    },
    error: {
        icon: 'bi-exclamation-triangle-fill',
        title: 'خطأ',
        headerClass: 'error',
        buttonClass: 'error',
        buttonText: 'حسناً'
    },
    warning: {
        icon: 'bi-exclamation-circle-fill',
        title: 'تحذير',
        headerClass: 'warning',
        buttonClass: 'warning',
        buttonText: 'فهمت'
    },
    info: {
        icon: 'bi-info-circle-fill',
        title: 'معلومة',
        headerClass: 'info',
        buttonClass: 'info',
        buttonText: 'حسناً'
    }
};

/**
 * عرض تنبيه
 * @param {string} message - نص الرسالة
 * @param {string} type - نوع التنبيه (success|error|warning|info)
 */
function showAlert(message, type = 'info') {
    const modal = document.getElementById('alertModal');
    if (!modal) {
        console.error('Alert modal not found in DOM');
        return;
    }

    // الحصول على الـ config
    const config = ALERT_CONFIG[type] || ALERT_CONFIG.info;

    // العناصر
    const header = modal.querySelector('.alert-modal-header');
    const icon = modal.querySelector('.alert-modal-icon');
    const title = modal.querySelector('.alert-modal-title');
    const message_el = modal.querySelector('.alert-modal-message');
    const btn = modal.querySelector('.alert-modal-btn');

    // تطبيق الـ config
    header.className = `alert-modal-header ${config.headerClass}`;
    icon.className = `bi ${config.icon} alert-modal-icon`;
    title.textContent = config.title;
    message_el.textContent = message;
    btn.className = `alert-modal-btn ${config.buttonClass}`;
    btn.textContent = config.buttonText;

    // إظهار Modal
    modal.style.display = 'flex';
    setTimeout(() => modal.classList.add('show'), 10);

    // معالجات الإغلاق
    setupCloseHandlers(modal);
}

/**
 * إغلاق التنبيه
 */
function closeAlert() {
    const modal = document.getElementById('alertModal');
    if (modal) {
        modal.classList.remove('show');
        setTimeout(() => {
            modal.style.display = 'none';
        }, 300);
    }
}

/**
 * إعداد معالجات الإغلاق
 */
function setupCloseHandlers(modal) {
    const btn = modal.querySelector('.alert-modal-btn');

    // إزالة المستمعين القدامى
    const newBtn = btn.cloneNode(true);
    btn.parentNode.replaceChild(newBtn, btn);

    // إغلاق عند الضغط على الزر
    newBtn.addEventListener('click', closeAlert);

    // إغلاق عند الضغط خارج Modal
    const overlayClickHandler = (e) => {
        if (e.target === modal) {
            closeAlert();
            modal.removeEventListener('click', overlayClickHandler);
        }
    };
    modal.addEventListener('click', overlayClickHandler);

    // إغلاق بزر ESC
    const escapeHandler = (e) => {
        if (e.key === 'Escape') {
            closeAlert();
            document.removeEventListener('keydown', escapeHandler);
        }
    };
    document.addEventListener('keydown', escapeHandler);
}