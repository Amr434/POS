    async function viewSale(id) {
            const modal = new bootstrap.Modal(document.getElementById('detailsModal'));
    modal.show();

    // ✅ إظهار Loading وإخفاء المحتوى
    showState('loading');

    try {
                const response = await fetch(`/Sales/GetDetails/${id}`);
    const data = await response.json();

    if (!response.ok) {
                    throw new Error(data.error || 'حدث خطأ');
                }

    // ✅ ملء البيانات في HTML الثابت
    populateInvoiceData(data);

    // ✅ إظهار المحتوى
    showState('content');

            } catch (error) {
        // ✅ إظهار الخطأ
        showState('error', error.message);
            }
        }

    // ✅ دالة ملء البيانات
    function populateInvoiceData(data) {
        // Invoice Header
        document.getElementById('invoiceId').textContent = `#${data.id}`;
    document.getElementById('invoiceDate').textContent = new Date(data.saleDate).toLocaleDateString('ar-EG');
    document.getElementById('customerName').textContent = data.customerName;
    document.getElementById('customerPhone').textContent = data.customerPhone;

    // Payment Method Badge
    const badge = document.getElementById('paymentMethodBadge');
    const isCash = data.paymentType == "1";
    badge.className = `badge ${isCash ? 'badge-cash' : 'badge-installment'}`;
    badge.textContent = isCash ? 'كاش' : 'تقسيط';

    // Products List
    const productsList = document.getElementById('productsList');
            productsList.innerHTML = data.items.map(item => `
    <tr>
        <td style="padding: 10px; border-bottom: 1px solid #f3f4f6;">${item.productName}</td>
        <td style="padding: 10px; text-align: center; border-bottom: 1px solid #f3f4f6;">${item.quantity}</td>
        <td style="padding: 10px; border-bottom: 1px solid #f3f4f6;">${item.unitPrice.toFixed(2)} ج.م</td>
        <td style="padding: 10px; font-weight: 700; border-bottom: 1px solid #f3f4f6;">${item.total.toFixed(2)} ج.م</td>
    </tr>
    `).join('');

    // Summary
    document.getElementById('totalAmount').textContent = `${data.totalAmount.toFixed(2)} ج.م`;
    document.getElementById('paidAmount').textContent = `${data.paidAmount.toFixed(2)} ج.م`;
    document.getElementById('remainingAmount').textContent = `${data.remainingAmount.toFixed(2)} ج.م`;
        }

    // ✅ دالة إدارة الحالات
    function showState(state, message = '') {
            const loadingState = document.getElementById('loadingState');
    const errorState = document.getElementById('errorState');
    const contentState = document.getElementById('contentState');

    // Hide all
    loadingState.style.display = 'none';
    errorState.style.display = 'none';
    contentState.style.display = 'none';

    // Show selected state
    if (state === 'loading') {
        loadingState.style.display = 'block';
            } else if (state === 'error') {
        errorState.style.display = 'block';
    document.getElementById('errorMessage').textContent = message;
            } else if (state === 'content') {
        contentState.style.display = 'block';
            }
        }

    async function deleteSale(id, customerName) {
            if (!confirm(`هل أنت متأكد من حذف فاتورة "${customerName}"؟\nسيتم إرجاع المنتجات إلى حالة "جديد".`)) {
                return;
            }

    try {
                const response = await fetch(`/Sales/Delete/${id}`, {method: 'POST' });
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