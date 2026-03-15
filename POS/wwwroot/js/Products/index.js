    document.addEventListener('DOMContentLoaded', function() {
        const successAlert = document.getElementById('successAlert');
    const errorAlert = document.getElementById('errorAlert');

    if (successAlert) {
        setTimeout(function () {
            successAlert.classList.add('fade-out');
            setTimeout(function () {
                successAlert.style.display = 'none';
            }, 300);
        }, 2000);
        }

    if (errorAlert) {
        setTimeout(function () {
            errorAlert.classList.add('fade-out');
            setTimeout(function () {
                errorAlert.style.display = 'none';
            }, 300);
        }, 2000);
        }
    });
    // Filter functionality
    function applyFilters() {
        const searchTerm = document.getElementById('searchInput').value.toLowerCase();
    const categoryId = document.getElementById('categoryFilter').value;
    const rows = document.querySelectorAll('#productsTableBody tr');

        rows.forEach(row => {
            const name = row.dataset.name?.toLowerCase() || '';
    const barcode = row.dataset.barcode?.toLowerCase() || '';
    const category = row.dataset.category || '';

    const matchSearch = !searchTerm || name.includes(searchTerm) || barcode.includes(searchTerm);
    const matchCategory = !categoryId || category === categoryId;

    row.style.display = matchSearch && matchCategory  ? '' : 'none';
        });
    }

    // Real-time search
    document.getElementById('searchInput')?.addEventListener('input', applyFilters);
    document.getElementById('categoryFilter')?.addEventListener('change', applyFilters);
    document.getElementById('stockFilter')?.addEventListener('change', applyFilters);

    // Open delete modal
    function openDeleteModal(id, name) {
        document.getElementById('deleteProductId').value = id;
    document.getElementById('deleteProductName').textContent = name;
    document.getElementById('deleteModal').classList.add('show');
    }

    // Close delete modal
    function closeDeleteModal() {
        document.getElementById('deleteModal').classList.remove('show');
    }

    // Confirm delete
    async function confirmDelete() {
        const id = parseInt(document.getElementById('deleteProductId').value);

    try {
            const response = await fetch(`/Products/Delete/${id}`, {
        method: 'POST'
            });

    if (response.ok) {
        closeDeleteModal();
    location.reload();
            } else {
        alert('حدث خطأ أثناء حذف المنتج');
            }
        } catch (error) {
        alert('حدث خطأ في الاتصال');
        }
    }

    // Close modal on overlay click
    document.getElementById('deleteModal')?.addEventListener('click', function (e) {
        if (e.target === this) {
        closeDeleteModal();
        }
    });
