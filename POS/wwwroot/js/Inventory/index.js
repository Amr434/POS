// ═══════════════════════════════════════════
// Inventory Page JavaScript
// ═══════════════════════════════════════════

let currentPage = 1;
let searchTimeout = null;

// ── Auto-dismiss alerts ──
document.addEventListener('DOMContentLoaded', function () {
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.5s';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 500);
        }, 4000);
    });
});

// ── Search & Filter ──
document.getElementById('searchInput')?.addEventListener('input', function () {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
        currentPage = 1;
        loadPage(1);
    }, 400);
});

document.getElementById('categoryFilter')?.addEventListener('change', function () {
    currentPage = 1;
    loadPage(1);
});

document.getElementById('stockFilter')?.addEventListener('change', function () {
    currentPage = 1;
    loadPage(1);
});

// ── Pagination ──
function goToPage(page) {
    if (page < 1) return;
    currentPage = page;
    loadPage(page);
}

function loadPage(pageNumber) {
    const searchTerm = document.getElementById('searchInput')?.value || '';
    const categoryId = document.getElementById('categoryFilter')?.value || '';
    const stockFilter = document.getElementById('stockFilter')?.value || '';

    const url = `/Inventory/GetPage?pageNumber=${pageNumber}&searchTerm=${encodeURIComponent(searchTerm)}&categoryId=${categoryId}&stockFilter=${stockFilter}`;

    fetch(url)
        .then(r => r.json())
        .then(data => {
            renderTable(data.items);
            renderPagination(data);
            document.getElementById('totalCount').textContent = data.totalCount;
        })
        .catch(err => console.error('Error loading page:', err));
}

function renderTable(items) {
    const tbody = document.getElementById('inventoryTableBody');
    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7">
                    <div class="empty-state">
                        <i class="bi bi-archive"></i>
                        <h3>لا توجد نتائج</h3>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(item => {
        const imageHtml = item.imagePath
            ? `<img src="${item.imagePath}" alt="${item.productName}" style="width:100%;height:100%;object-fit:cover;border-radius:8px;">`
            : `<i class="bi bi-box-seam"></i>`;

        const stockClass = item.stockStatus === 'out' ? 'text-danger' : item.stockStatus === 'low' ? 'text-warning' : 'text-success';

        return `
            <tr>
                <td><div class="product-image">${imageHtml}</div></td>
                <td>
                    <div class="product-cell">
                        <div class="product-info">
                            <span class="product-name">${item.productName}</span>
                            ${item.barcode ? `<span class="product-sku">SKU: ${item.barcode}</span>` : ''}
                        </div>
                    </div>
                </td>
                <td><span class="category-badge">${item.categoryName}</span></td>
                <td><span class="stock-value ${stockClass}">${item.totalStock}</span></td>
                <td>${item.minStock}</td>
                <td><span class="stock-badge ${item.stockStatus}">${item.stockStatusText}</span></td>
                <td>
                    <div class="actions-cell">
                        <a href="javascript:void(0)" onclick="openDetailsModal(${item.productId})" class="btn-action btn-view" title="عرض الدفعات">
                            <i class="bi bi-eye"></i>
                        </a>
                        <a href="javascript:void(0)" onclick="openAdjustModal(${item.productId})" class="btn-action btn-edit" title="تعديل المخزون">
                            <i class="bi bi-sliders"></i>
                        </a>
                    </div>
                </td>
            </tr>`;
    }).join('');
}

function renderPagination(data) {
    const container = document.querySelector('.pagination-container');
    if (!container) return;

    if (data.totalPages <= 1) {
        container.style.display = 'none';
        return;
    }
    container.style.display = 'flex';

    // Update info
    const from = (data.pageIndex - 1) * 10 + 1;
    const to = Math.min(data.pageIndex * 10, data.totalCount);
    const itemsFrom = document.getElementById('itemsFrom');
    const itemsTo = document.getElementById('itemsTo');
    const itemsTotal = document.getElementById('itemsTotal');
    if (itemsFrom) itemsFrom.textContent = from;
    if (itemsTo) itemsTo.textContent = to;
    if (itemsTotal) itemsTotal.textContent = data.totalCount;

    // Update buttons
    const prevBtn = document.getElementById('prevPageBtn');
    const nextBtn = document.getElementById('nextPageBtn');
    if (prevBtn) {
        prevBtn.disabled = !data.hasPreviousPage;
        prevBtn.onclick = () => goToPage(data.pageIndex - 1);
    }
    if (nextBtn) {
        nextBtn.disabled = !data.hasNextPage;
        nextBtn.onclick = () => goToPage(data.pageIndex + 1);
    }

    // Update page numbers
    const numbersContainer = document.getElementById('paginationNumbers');
    if (numbersContainer) {
        let html = '';
        const startPage = Math.max(1, data.pageIndex - 2);
        const endPage = Math.min(data.totalPages, data.pageIndex + 2);
        for (let i = startPage; i <= endPage; i++) {
            html += `<button class="pagination-number ${i === data.pageIndex ? 'active' : ''}" onclick="goToPage(${i})">${i}</button>`;
        }
        numbersContainer.innerHTML = html;
    }
}

// ── Details Modal ──
function openDetailsModal(productId) {
    fetch(`/Inventory/GetDetails/${productId}`)
        .then(r => r.json())
        .then(data => {
            // Product info
            document.getElementById('detailsProductName').textContent = data.productName;
            document.getElementById('detailsCategoryName').textContent = data.categoryName;
            document.getElementById('detailsBarcode').textContent = data.barcode || '—';
            document.getElementById('detailsTotalStock').textContent = data.totalStock;
            document.getElementById('detailsMinStock').textContent = data.minStock;

            // Image
            const img = document.getElementById('detailsImage');
            const noImg = document.getElementById('detailsNoImage');
            if (data.imagePath) {
                img.src = data.imagePath;
                img.style.display = 'block';
                noImg.style.display = 'none';
            } else {
                img.style.display = 'none';
                noImg.style.display = 'flex';
            }

            // Batches
            const tbody = document.getElementById('batchesTableBody');
            const emptyBatches = document.getElementById('emptyBatches');

            if (data.batches && data.batches.length > 0) {
                tbody.innerHTML = data.batches.map((b, idx) => `
                    <tr>
                        <td>${idx + 1}</td>
                        <td>${b.purchaseDate}</td>
                        <td>${b.quantity}</td>
                        <td><strong>${b.remainingQuantity}</strong></td>
                        <td>${Number(b.unitPrice).toLocaleString('ar-EG')} ج.م</td>
                    </tr>
                `).join('');
                emptyBatches.style.display = 'none';
                tbody.parentElement.style.display = 'table';
            } else {
                tbody.innerHTML = '';
                emptyBatches.style.display = 'block';
                tbody.parentElement.style.display = 'none';
            }

            document.getElementById('detailsModal').classList.add('show');
        })
        .catch(err => {
            console.error(err);
            showToast('حدث خطأ أثناء تحميل البيانات', 'error');
        });
}

function closeDetailsModal() {
    document.getElementById('detailsModal').classList.remove('show');
}

// ── Adjust Stock Modal ──
let currentAdjustProductId = null;

function openAdjustModal(productId) {
    currentAdjustProductId = productId;

    fetch(`/Inventory/GetDetails/${productId}`)
        .then(r => r.json())
        .then(data => {
            document.getElementById('adjustProductName').textContent = data.productName;

            // Populate batch select
            const batchSelect = document.getElementById('adjustBatchId');
            batchSelect.innerHTML = '<option value="">اختر دفعة...</option>';

            if (data.batches && data.batches.length > 0) {
                data.batches.forEach(b => {
                    batchSelect.innerHTML += `<option value="${b.id}">دفعة ${b.purchaseDate} — متبقي: ${b.remainingQuantity} — سعر: ${Number(b.unitPrice).toLocaleString('ar-EG')} ج.م</option>`;
                });
            }

            document.getElementById('adjustQuantity').value = 1;
            document.getElementById('adjustType').value = 'add';
            document.getElementById('adjustReason').value = '';

            document.getElementById('adjustModal').classList.add('show');
        })
        .catch(err => {
            console.error(err);
            showToast('حدث خطأ أثناء تحميل البيانات', 'error');
        });
}

function closeAdjustModal() {
    document.getElementById('adjustModal').classList.remove('show');
    currentAdjustProductId = null;
}

function submitAdjustment() {
    const batchId = document.getElementById('adjustBatchId')?.value;
    const quantity = parseInt(document.getElementById('adjustQuantity')?.value);
    const adjustmentType = document.getElementById('adjustType')?.value;
    const reason = document.getElementById('adjustReason')?.value;

    if (!batchId) {
        showToast('يرجى اختيار دفعة', 'error');
        return;
    }

    if (!quantity || quantity < 1) {
        showToast('يرجى إدخال كمية صحيحة', 'error');
        return;
    }

    fetch('/Inventory/AdjustStock', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            batchId: parseInt(batchId),
            quantity: quantity,
            adjustmentType: adjustmentType,
            reason: reason
        })
    })
        .then(r => r.json())
        .then(data => {
            if (data.error) {
                showToast(data.error, 'error');
            } else {
                showToast(data.message, 'success');
                closeAdjustModal();
                // Refresh current page
                loadPage(currentPage);
            }
        })
        .catch(err => {
            console.error(err);
            showToast('حدث خطأ أثناء تعديل المخزون', 'error');
        });
}

// ── Toast Notification ──
function showToast(message, type) {
    const toast = document.getElementById('toastNotification');
    const icon = document.getElementById('toastIcon');
    const msg = document.getElementById('toastMessage');

    toast.className = `toast-notification toast-${type}`;
    icon.className = `bi ${type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill'}`;
    msg.textContent = message;
    toast.style.display = 'flex';

    setTimeout(() => {
        toast.style.display = 'none';
    }, 3500);
}

// ── Close modals on overlay click ──
document.querySelectorAll('.modal-overlay').forEach(overlay => {
    overlay.addEventListener('click', function (e) {
        if (e.target === this) {
            this.classList.remove('show');
        }
    });
});
