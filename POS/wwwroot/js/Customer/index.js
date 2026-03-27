    function openModal(mode) {
        document.getElementById('customerModal').classList.add('show');
    document.getElementById('customerForm').reset();
    document.getElementById('customerId').value = '';
    document.getElementById('modalTitle').textContent = 'إضافة عميل جديد';
    }

    function closeModal() {
        document.getElementById('customerModal').classList.remove('show');
    }

    function editCustomer(btn) {
        const row = btn.closest('tr');
    document.getElementById('customerId').value = row.dataset.id;
    document.getElementById('customerName').value = row.dataset.name;
    document.getElementById('customerPhone').value = row.dataset.phone || '';
    document.getElementById('customerEmail').value = row.dataset.email || '';
    document.getElementById('customerAddress').value = row.dataset.address || '';
    document.getElementById('modalTitle').textContent = 'تعديل بيانات العميل';
    document.getElementById('customerModal').classList.add('show');
    }

    async function deleteCustomer(btn) {
        const row = btn.closest('tr');
    const id = row.dataset.id;
    const name = row.dataset.name;

    if (!confirm(`هل أنت متأكد من حذف العميل "${name}"؟`)) return;

    try {
            const response = await fetch(`/Customers/Delete?id=${id}`, {method: 'POST' });
    const result = await response.json();

    if (response.ok) {
        showAlert(result.message, 'success');
                setTimeout(() => location.reload(), 1500);
            } else {
        showAlert(result.error, 'error');
            }
        } catch (error) {
        showAlert('حدث خطأ أثناء الحذف', 'error');
        }
    }

    document.getElementById('customerForm').addEventListener('submit', async function(e) {
        e.preventDefault();

    const id = document.getElementById('customerId').value;
    const data = {
        id: id ? parseInt(id) : 0,
    name: document.getElementById('customerName').value,
        NationalId: document.getElementById('NationalId').value,
    phone: document.getElementById('customerPhone').value,
    email: document.getElementById('customerEmail').value,
    address: document.getElementById('customerAddress').value
        };

    const url = id ? '/Customers/Update' : '/Customers/Create';

    try {
            const response = await fetch(url, {
        method: 'POST',
    headers: {'Content-Type': 'application/json' },
    body: JSON.stringify(data)
            });

    const result = await response.json();

    if (response.ok) {
        showAlert(result.message, 'success');
    closeModal();
                setTimeout(() => location.reload(), 1500);
            } else {
        showAlert(result.error, 'error');
            }
        } catch (error) {
        showAlert('حدث خطأ أثناء الحفظ', 'error');
        }
    });

    function showAlert(message, type = 'success') {
        const alertContainer = document.getElementById('alertContainer');
    const alert = document.createElement('div');
    alert.className = `alert alert-${type}`;
    alert.innerHTML = `
    <i class="bi bi-${type === 'success' ? 'check-circle-fill' : 'exclamation-triangle-fill'}"></i>
    <span>${message}</span>
    `;
    alertContainer.appendChild(alert);
        setTimeout(() => alert.remove(), 5000);
    }

    document.getElementById('customerModal').addEventListener('click', function(e) {
        if (e.target === this) closeModal();
    });
