/**
 * InterFullMarkt - Platform Ortak İstemci Yardımcıları
 * Scout takip listesi, sepet senkronizasyonu ve genel arayüz araçları.
 */

window.InterFullMarkt = {
    // Scout Takip Listesi Rozetini Güncelle
    updateWatchlistBadge: function () {
        fetch('/Scout/GetWatchlistCount')
            .then(res => res.json())
            .then(data => {
                const badge = document.getElementById('watchlistBadge');
                if (badge) {
                    badge.textContent = data.count || 0;
                    badge.style.display = data.count > 0 ? 'inline-block' : 'none';
                }
            })
            .catch(() => {});
    },

    // Sepet Sayısını Güncelle
    updateCartBadge: function (count) {
        const badge = document.getElementById('cartBadge');
        if (badge) {
            badge.textContent = count || 0;
            badge.style.display = count > 0 ? 'inline-block' : 'none';
        }
    },

    // Bilgilendirme Toasty / Bildirimi Göster
    showNotification: function (message, isSuccess = true) {
        const toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) return;

        const toastEl = document.createElement('div');
        toastEl.className = `toast align-items-center text-white ${isSuccess ? 'bg-success' : 'bg-danger'} border-0 mb-2`;
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');
        toastEl.innerHTML = `
            <div class="d-flex">
                <div class="toast-body fw-semibold">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Kapat"></button>
            </div>
        `;
        toastContainer.appendChild(toastEl);
        const toast = new bootstrap.Toast(toastEl, { delay: 3500 });
        toast.show();
        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }
};

// Sayfa yüklendiğinde rozetleri senkronize et
document.addEventListener('DOMContentLoaded', function () {
    if (typeof window.InterFullMarkt !== 'undefined') {
        window.InterFullMarkt.updateWatchlistBadge();
    }
});
