document.addEventListener('DOMContentLoaded', function() {
    
    // ===== ALERTS AUTO-HIDE =====
    const successAlert = document.getElementById('successAlert');
    const errorAlert = document.getElementById('errorAlert');

    if (successAlert) {
        setTimeout(() => {
            successAlert.classList.add('fade-out');
            setTimeout(() => successAlert.style.display = 'none', 300);
        }, 2000);
    }

    if (errorAlert) {
        setTimeout(() => {
            errorAlert.classList.add('fade-out');
            setTimeout(() => errorAlert.style.display = 'none', 300);
        }, 2000);
    }

    // ===== SEARCH & FILTER =====
    let searchTimeout;
    const searchInput = document.getElementById('searchInput');
    const categoryFilter = document.getElementById('categoryFilter');

    if (searchInput) {
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                applyFilters();
            }, 800);
        });
    }

    if (categoryFilter) {
        categoryFilter.addEventListener('change', applyFilters);
    }

    function applyFilters() {
        const search = searchInput?.value || '';
        const category = categoryFilter?.value || '';
        const pageSize = document.getElementById('pageSizeSelect')?.value || 20;
        window.location.href = `/Products/Index?searchTerm=${search}&categoryId=${category}&pageSize=${pageSize}&pageNumber=1`;
    }

    // ===== MODAL OVERLAY CLOSE =====
    const deleteModal = document.getElementById('deleteModal');
    const previewModal = document.getElementById('previewModal');
    const editModal = document.getElementById('editModal');

    if (deleteModal) {
        deleteModal.addEventListener('click', (e) => {
            if (e.target === deleteModal) closeDeleteModal();
        });
    }

    if (previewModal) {
        previewModal.addEventListener('click', (e) => {
            if (e.target === previewModal) closePreviewModal();
        });
    }

    if (editModal) {
        editModal.addEventListener('click', (e) => {
            if (e.target === editModal) closeEditModal();
        });
    }

    // ===== EDIT FORM SUBMISSION =====
    const editForm = document.getElementById('editProductForm');
    
    if (editForm) {
        editForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            e.stopPropagation(); // Stop event bubbling
            
            console.log('✅ Form submission intercepted');

            const formData = new FormData();
            formData.append('id', document.getElementById('editProductId').value);
            formData.append('name', document.getElementById('editName').value);
            formData.append('categoryId', document.getElementById('editCategoryId').value);
            formData.append('salePrice', document.getElementById('editSalePrice').value);
            formData.append('barcode', document.getElementById('editBarcode').value || '');
            formData.append('status', document.getElementById('editStatus').value);
            formData.append('isMotorcycle', document.getElementById('editIsMotorcycle').checked);
            formData.append('engineNumber', document.getElementById('editEngineNumber').value || '');
            formData.append('chassisNumber', document.getElementById('editChassisNumber').value || '');

            const imageInput = document.getElementById('editImageFile');
            if (imageInput?.files?.[0]) {
                formData.append('image', imageInput.files[0]);
            }

            console.log('📤 Sending to /Products/UpdateQuick');

            try {
                const response = await fetch('/Products/UpdateQuick', {
                    method: 'POST',
                    body: formData
                });

                console.log('📥 Response status:', response.status);

                if (!response.ok) {
                    const errorText = await response.text();
                    console.error('❌ Server error:', errorText);
                    alert('حدث خطأ: ' + response.status);
                    return;
                }

                const result = await response.json();
                console.log('✅ Result:', result);

                alert(result.message || 'تم التحديث بنجاح');
                closeEditModal();
                location.reload();

            } catch (error) {
                console.error('❌ Fetch error:', error);
                alert('حدث خطأ في الاتصال بالخادم');
            }
        });
    } else {
        console.warn('⚠️ editProductForm not found!');
    }
});

// ========== GLOBAL FUNCTIONS ==========

function openDeleteModal(id, name) {
    document.getElementById('deleteProductId').value = id;
    document.getElementById('deleteProductName').textContent = name;
    document.getElementById('deleteModal').classList.add('show');
}

function closeDeleteModal() {
    document.getElementById('deleteModal').classList.remove('show');
}

async function confirmDelete() {
    const id = parseInt(document.getElementById('deleteProductId').value);
    
    try {
        const response = await fetch(`/Products/Delete/${id}`, { method: 'POST' });
        
        if (response.ok) {
            closeDeleteModal();
            location.reload();
        } else {
            alert('حدث خطأ أثناء الحذف');
        }
    } catch (error) {
        console.error('Delete error:', error);
        alert('حدث خطأ في الاتصال');
    }
}

async function openEditModal(id) {
    try {
        const response = await fetch(`/Products/GetForEdit/${id}`);
        const data = await response.json();

        if (!response.ok) {
            alert(data.error || 'حدث خطأ');
            return;
        }

        // Populate fields
        document.getElementById('editProductId').value = data.id;
        document.getElementById('editName').value = data.name;
        document.getElementById('editSalePrice').value = data.salePrice;
        document.getElementById('editBarcode').value = data.barcode || '';
        document.getElementById('editStatus').value = data.status;
        document.getElementById('editIsMotorcycle').checked = data.isMotorcycle;
        document.getElementById('editEngineNumber').value = data.engineNumber || '';
        document.getElementById('editChassisNumber').value = data.chassisNumber || '';

        // Handle image
        const currentImageEl = document.getElementById('editCurrentImage');
        const noImageEl = document.getElementById('editNoImage');

        if (data.imagePath) {
            currentImageEl.src = data.imagePath;
            currentImageEl.style.display = 'block';
            noImageEl.style.display = 'none';
        } else {
            currentImageEl.style.display = 'none';
            noImageEl.style.display = 'flex';
        }

        // Populate categories
        const categorySelect = document.getElementById('editCategoryId');
        categorySelect.innerHTML = '<option value="">اختر الفئة</option>';
        data.categories.forEach(cat => {
            const option = document.createElement('option');
            option.value = cat.value;
            option.textContent = cat.text;
            categorySelect.appendChild(option);
        });
        categorySelect.value = data.categoryId;

        toggleMotorcycleFieldsEdit();
        document.getElementById('editModal').classList.add('show');

    } catch (error) {
        console.error('Edit modal error:', error);
        alert('حدث خطأ أثناء تحميل البيانات');
    }
}

function closeEditModal() {
    const modal = document.getElementById('editModal');
    const form = document.getElementById('editProductForm');
    
    modal.classList.remove('show');
    if (form) form.reset();
    
    // Reset image preview
    const currentImageEl = document.getElementById('editCurrentImage');
    const noImageEl = document.getElementById('editNoImage');
    
    if (currentImageEl) {
        currentImageEl.src = '';
        currentImageEl.style.display = 'none';
    }
    if (noImageEl) {
        noImageEl.style.display = 'flex';
    }
    
    const fileInput = document.getElementById('editImageFile');
    if (fileInput) fileInput.value = '';
}

function toggleMotorcycleFieldsEdit() {
    const checkbox = document.getElementById('editIsMotorcycle');
    const fields = document.getElementById('editMotorcycleFields');
    if (checkbox && fields) {
        fields.style.display = checkbox.checked ? 'grid' : 'none';
    }
}

function previewEditImage(event) {
    const file = event.target.files[0];
    if (!file) return;
    
    const preview = document.getElementById('editCurrentImage');
    const placeholder = document.getElementById('editNoImage');
    
    const reader = new FileReader();
    reader.onload = (e) => {
        preview.src = e.target.result;
        preview.style.display = 'block';
        placeholder.style.display = 'none';
    };
    reader.readAsDataURL(file);
}

async function openPreviewModal(id) {
    try {
        const response = await fetch(`/Products/GetDetails/${id}`);
        const data = await response.json();

        if (!response.ok) {
            alert(data.error || 'حدث خطأ');
            return;
        }

        document.getElementById('previewName').textContent = data.name;
        document.getElementById('previewCategory').textContent = data.categoryName;
        document.getElementById('previewSalePrice').textContent = data.salePrice.toFixed(2) + ' ج.م';
        document.getElementById('previewBarcode').textContent = data.barcode || 'غير محدد';
        document.getElementById('previewStatus').textContent = getStatusText(data.status);

        // Handle image
        const imageContainer = document.getElementById('previewImageContainer');
        const previewImage = document.getElementById('previewImage');
        
        if (data.imagePath) {
            previewImage.src = data.imagePath;
            imageContainer.style.display = 'block';
        } else {
            imageContainer.style.display = 'none';
        }

        // Handle motorcycle fields
        const hasMotorcycleData = data.engineNumber || data.chassisNumber;
        document.getElementById('previewEngineContainer').style.display = hasMotorcycleData ? 'flex' : 'none';
        document.getElementById('previewChassisContainer').style.display = hasMotorcycleData ? 'flex' : 'none';
        
        if (hasMotorcycleData) {
            document.getElementById('previewEngine').textContent = data.engineNumber || 'غير محدد';
            document.getElementById('previewChassis').textContent = data.chassisNumber || 'غير محدد';
        }

        document.getElementById('previewModal').classList.add('show');

    } catch (error) {
        console.error('Preview error:', error);
        alert('حدث خطأ أثناء تحميل البيانات');
    }
}

function closePreviewModal() {
    document.getElementById('previewModal').classList.remove('show');
}

function getStatusText(status) {
    const map = {
        'New': 'جديد',
        'Reserved': 'محجوز',
        'Sold': 'مُباع'
    };
    return map[status] || status;
}

function changePageSize(pageSize) {
    const params = new URLSearchParams(window.location.search);
    const search = params.get('searchTerm') || '';
    const category = params.get('categoryId') || '';
    window.location.href = `/Products/Index?pageSize=${pageSize}&searchTerm=${search}&categoryId=${category}`;
}
