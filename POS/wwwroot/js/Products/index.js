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
let currentPage = 1;
let totalPages = 1;
let currentSearchTerm = '';
let currentCategoryId = null;

    // Update buttons
    document.getElementById('firstPageBtn').disabled = !data.hasPreviousPage;
    document.getElementById('prevPageBtn').disabled = !data.hasPreviousPage;
    document.getElementById('nextPageBtn').disabled = !data.hasNextPage;
    document.getElementById('lastPageBtn').disabled = !data.hasNextPage;

    // Update page numbers
    updatePageNumbers();


// Real-time search with debounce
let searchTimeout;
document.getElementById('searchInput')?.addEventListener('input', function() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
        goToPage(1);
    }, 800);
});// Real-time search with debounce


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
async function openEditModal(id) {
    try {
        const response = await fetch(`/Products/GetForEdit/${id}`);
        const data = await response.json();

        if (response.ok) {
            // Populate form fields
            document.getElementById('editProductId').value = data.id;
            document.getElementById('editName').value = data.name;
            document.getElementById('editPurchasePrice').value = data.purchasePrice;
            document.getElementById('editSalePrice').value = data.salePrice;
            document.getElementById('editQuantity').value = data.quantity;
            document.getElementById('editMinStock').value = data.minStock;
            document.getElementById('editBarcode').value = data.barcode || '';
            document.getElementById('editStatus').value = data.status;
            document.getElementById('editIsMotorcycle').checked = data.isMotorcycle;
            document.getElementById('editEngineNumber').value = data.engineNumber || '';
            document.getElementById('editChassisNumber').value = data.chassisNumber || '';

            // Handle existing image display
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

            // Populate categories dropdown
            const categorySelect = document.getElementById('editCategoryId');
            categorySelect.innerHTML = '<option value="">اختر الفئة</option>';
            data.categories.forEach(cat => {
                const option = document.createElement('option');
                option.value = cat.value;
                option.textContent = cat.text;
                categorySelect.appendChild(option);
            });
            categorySelect.value = data.categoryId;

            // Show/hide motorcycle fields
            toggleMotorcycleFieldsEdit();

            // Show the modal
            document.getElementById('editModal').classList.add('show');
        } else {
            alert(data.error || 'حدث خطأ أثناء تحميل البيانات');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('حدث خطأ أثناء تحميل البيانات');
    }
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

// Open preview modal
async function openPreviewModal(id) {
    try {
        const response = await fetch(`/Products/GetDetails/${id}`);
        const data = await response.json();

        if (response.ok) {
            document.getElementById('previewName').textContent = data.name;
            document.getElementById('previewCategory').textContent = data.categoryName;
            document.getElementById('previewPurchasePrice').textContent = data.purchasePrice.toFixed(2) + ' ج.م';
            document.getElementById('previewSalePrice').textContent = data.salePrice.toFixed(2) + ' ج.م';
            document.getElementById('previewQuantity').textContent = data.quantity;
            document.getElementById('previewMinStock').textContent = data.minStock;
            document.getElementById('previewBarcode').textContent = data.barcode || 'غير محدد';
            document.getElementById('previewStatus').textContent = getStatusText(data.status);

            // Handle image
            const imageContainer = document.getElementById('previewImageContainer');
            if (data.imagePath) {
                document.getElementById('previewImage').src = data.imagePath;
                imageContainer.style.display = 'block';
            } else {
                imageContainer.style.display = 'none';
            }

            // Handle motorcycle fields
            if (data.engineNumber || data.chassisNumber) {
                document.getElementById('previewEngineContainer').style.display = 'flex';
                document.getElementById('previewEngine').textContent = data.engineNumber || 'غير محدد';
                document.getElementById('previewChassisContainer').style.display = 'flex';
                document.getElementById('previewChassis').textContent = data.chassisNumber || 'غير محدد';
            } else {
                document.getElementById('previewEngineContainer').style.display = 'none';
                document.getElementById('previewChassisContainer').style.display = 'none';
            }

            document.getElementById('previewModal').classList.add('show');
        } else {
            alert(data.error || 'حدث خطأ أثناء تحميل البيانات');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('حدث خطأ أثناء تحميل البيانات');
    }
}

// Close preview modal
function closePreviewModal() {
    document.getElementById('previewModal').classList.remove('show');
}

// Update pagination controls
function updatePagination(data) {
    currentPage = data.pageIndex;
    totalPages = data.totalPages;
    try { 
        if (response.ok) {
            document.getElementById('editProductId').value = data.id;
            document.getElementById('editName').value = data.name;
            document.getElementById('editCategoryId').value = data.categoryId;
            document.getElementById('editPurchasePrice').value = data.purchasePrice;
            document.getElementById('editSalePrice').value = data.salePrice;
            document.getElementById('editQuantity').value = data.quantity;
            document.getElementById('editMinStock').value = data.minStock;
            document.getElementById('editBarcode').value = data.barcode || '';
            document.getElementById('editStatus').value = data.status;
            document.getElementById('editIsMotorcycle').checked = data.isMotorcycle;
            document.getElementById('editEngineNumber').value = data.engineNumber || '';
            document.getElementById('editChassisNumber').value = data.chassisNumber || '';

            // ✅ Handle existing image display
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

            // Populate categories dropdown
            const categorySelect = document.getElementById('editCategoryId');
            categorySelect.innerHTML = '<option value="">اختر الفئة</option>';
            data.categories.forEach(cat => {
                const option = document.createElement('option');
                option.value = cat.value;
                option.textContent = cat.text;
                categorySelect.appendChild(option);
            });
            categorySelect.value = data.categoryId;

            // Show/hide motorcycle fields
            toggleMotorcycleFieldsEdit();

            document.getElementById('editModal').classList.add('show');
        } else {
            alert(data.error || 'حدث خطأ أثناء تحميل البيانات');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('حدث خطأ أثناء تحميل البيانات');
    }
}

// Update closeEditModal to reset image preview
function closeEditModal() {
    const modal = document.getElementById('editModal');
    const form = document.getElementById('editProductForm');
    
    modal.classList.remove('show');
    
    if (form) {
        form.reset();
    }
    
    // Reset image preview
    const currentImageEl = document.getElementById('editCurrentImage');
    const noImageEl = document.getElementById('editNoImage');
    const fileInput = document.getElementById('editImageFile');
    
    if (currentImageEl) {
        currentImageEl.src = '';
        currentImageEl.style.display = 'none';
    }
    if (noImageEl) {
        noImageEl.style.display = 'flex';
    }
    if (fileInput) {
        fileInput.value = '';
    }
}

// Close edit modal

// Toggle motorcycle fields in edit form
function toggleMotorcycleFieldsEdit() {
    const checkbox = document.getElementById('editIsMotorcycle');
    const fields = document.getElementById('editMotorcycleFields');
    if (checkbox && fields) {
        fields.style.display = checkbox.checked ? 'grid' : 'none';
    }
}

function previewEditImage(event) {
    const file = event.target.files[0];
    const preview = document.getElementById('editCurrentImage');
    const placeholder = document.getElementById('editNoImage');
    
    if (file) {
        const reader = new FileReader();
        reader.onload = function(e) {
            preview.src = e.target.result;
            preview.style.display = 'block';
            placeholder.style.display = 'none';
        };
        reader.readAsDataURL(file);
    }
}

// Handle edit form submission
document.getElementById('editProductForm')?.addEventListener('submit', async function (e) {
    e.preventDefault();

    // ✅ Use FormData instead of JSON
    const formData = new FormData();
    
    formData.append('id', document.getElementById('editProductId').value);
    formData.append('name', document.getElementById('editName').value);
    formData.append('categoryId', document.getElementById('editCategoryId').value);
    formData.append('purchasePrice', document.getElementById('editPurchasePrice').value);
    formData.append('salePrice', document.getElementById('editSalePrice').value);
    formData.append('quantity', document.getElementById('editQuantity').value);
    formData.append('minStock', document.getElementById('editMinStock').value);
    formData.append('barcode', document.getElementById('editBarcode').value || '');
    formData.append('status', document.getElementById('editStatus').value);
    formData.append('isMotorcycle', document.getElementById('editIsMotorcycle').checked);
    formData.append('engineNumber', document.getElementById('editEngineNumber').value || '');
    formData.append('chassisNumber', document.getElementById('editChassisNumber').value || '');
    
    //  ✅ Append the file
    const imageInput = document.getElementById('editImageFile');
    if (imageInput && imageInput.files && imageInput.files[0]) {
        formData.append('image', imageInput.files[0]);
    }

    try {
        const response = await fetch('/Products/UpdateQuick', {
            method: 'POST',
            body: formData
        });

        const result = await response.json();

        if (response.ok) {
            alert(result.message);
            closeEditModal();
            location.reload();
        } else {
            alert(result.error || 'حدث خطأ أثناء الحفظ');
        }
    } catch (error) {
        console.error('Error:', error);
        alert('حدث خطأ أثناء حفظ المنتج');
    }
});

// Preview image when editing
function previewEditImage(event) {
    const file = event.target.files[0];
    const preview = document.getElementById('editCurrentImage');
    const placeholder = document.getElementById('editNoImage');
    
    if (file) {
        const reader = new FileReader();
        reader.onload = function(e) {
            preview.src = e.target.result;
            preview.style.display = 'block';
            placeholder.style.display = 'none';
        };
        reader.readAsDataURL(file);
    }
}


function closeEditModal() {
    document.getElementById('editModal').classList.remove('show');
    document.getElementById('editProductForm').reset();
}

function toggleMotorcycleFieldsEdit() {
    const checkbox = document.getElementById('editIsMotorcycle');
    const fields = document.getElementById('editMotorcycleFields');
    fields.style.display = checkbox.checked ? 'grid' : 'none';
}

// Helper function to get status text
function getStatusText(status) {
    const statusMap = {
        'New': 'جديد',
        'Reserved': 'محجوز',
        'Sold': 'مُباع'
    };
    return statusMap[status] || status;
}

// Close modals on overlay click
document.getElementById('previewModal')?.addEventListener('click', function (e) {
    if (e.target === this) {
        closePreviewModal();
    }
});

document.getElementById('editModal')?.addEventListener('click', function (e) {
    if (e.target === this) {
        closeEditModal();
    }
});


    function changePageSize(pageSize) {
            const currentSearch = '@ViewBag.CurrentSearch' || '';
    const currentCategory = '@ViewBag.CurrentCategory' || '';
    window.location.href = `/Products/Index?pageSize=${pageSize}&searchTerm=${currentSearch}&categoryId=${currentCategory}`;
        }

    // Update filters to include pagination
    document.getElementById('searchInput')?.addEventListener('input', function(e) {
            const search = e.target.value;
    const category = document.getElementById('categoryFilter').value;
    applyFiltersWithPagination(search, category);
        });

    document.getElementById('categoryFilter')?.addEventListener('change', function(e) {
            const search = document.getElementById('searchInput').value;
    const category = e.target.value;
    applyFiltersWithPagination(search, category);
        });

    function applyFiltersWithPagination(search, category) {
            const pageSize = document.getElementById('pageSizeSelect')?.value || 20;
    window.location.href = `/Products/Index?searchTerm=${search}&categoryId=${category}&pageSize=${pageSize}&pageNumber=1`;
        }
