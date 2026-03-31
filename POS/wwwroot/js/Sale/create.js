let cart = [];
let paymentType = 1; // 1 = Cash, 2 = Installment
let searchedProducts = []; // List of products that have been searched and added
let searchTimeout;
let currentInterestAmount = 0; // ✅ متغير لحفظ قيمة الفائدة الحالية

// Product Search with AJAX
const searchInput = document.getElementById('productSearch');
const searchResults = document.getElementById('searchResults');

searchInput.addEventListener('input', function () {
    const search = this.value.trim();

    // Clear previous timeout
    clearTimeout(searchTimeout);

    if (search.length === 0) {
        searchResults.classList.remove('show');
        return;
    }

    // Show loading state
    searchResults.innerHTML = '<div style="padding: 20px; text-align: center;"><i class="bi bi-hourglass-split"></i> جاري البحث...</div>';
    searchResults.classList.add('show');

    // Debounce search - wait 500ms after user stops typing
    searchTimeout = setTimeout(async () => {
        await searchProducts(search);
    }, 500);
});

// AJAX Search Function
async function searchProducts(search) {
    try {
        const response = await fetch(`/Sales/GetAvailableProducts?search=${encodeURIComponent(search)}`);

        if (!response.ok) {
            throw new Error('فشل في جلب المنتجات');
        }

        const products = await response.json();

        if (products.length === 0) {
            searchResults.innerHTML = '<div style="padding: 20px; text-align: center; color: #6b7280;">لا توجد منتجات متاحة</div>';
            searchResults.classList.add('show');
            return;
        }

        // Render products
        searchResults.innerHTML = products.map(p => {
            // Check if already in cart or searched list
            const inCart = cart.some(item => item.id === p.id);
            const inSearched = searchedProducts.some(item => item.id === p.id);
            const badge = inCart ? '<span class="in-cart-badge">في العربة</span>' :
                inSearched ? '<span class="searched-badge">تم البحث</span>' : '';

            return `
                <div class="product-item ${inCart ? 'disabled' : ''}" onclick='${!inCart ? `addToCart(${JSON.stringify(p).replace(/'/g, "&apos;")})` : ''}'>
                    <img src="${p.imagePath || '/images/no-image.png'}" class="product-image" alt="${p.name}">
                    <div class="product-info">
                        <div class="product-name">${p.name}</div>
                        <div class="product-category">${p.category}</div>
                        ${p.barcode ? `<div class="product-barcode">${p.barcode}</div>` : ''}

                    </div>
                    <div class="product-price-container">
                        <div class="product-price">${p.price.toFixed(2)} ج.م</div>
                        ${badge}
                    </div>
                </div>
            `;
        }).join('');


        searchResults.classList.add('show');

    } catch (error) {
        console.error('Search error:', error);
        searchResults.innerHTML = '<div style="padding: 20px; text-align: center; color: #dc2626;">حدث خطأ أثناء البحث</div>';
    }
}

// Close search results when clicking outside
document.addEventListener('click', function (e) {
    if (!e.target.closest('.product-search')) {
        searchResults.classList.remove('show');
    }
});

// Add to Cart
function addToCart(product) {
    const existing = cart.find(item => item.id === product.id);

    if (existing) {
        existing.quantity++;
        // showAlert(`تم زيادة كمية ${product.name}`, 'success'); // ❌ احذف هذا السطر
    } else {
        // Add to cart
        cart.push({
            id: product.id,
            name: product.name,
            price: product.price,
            quantity: 1,
            imagePath: product.imagePath,
            barcode: product.barcode,
            category: product.category
        });

        // Add to searched products list if not already there
        if (!searchedProducts.some(p => p.id === product.id)) {
            searchedProducts.push({
                id: product.id,
                name: product.name,
                price: product.price,
                imagePath: product.imagePath,
                barcode: product.barcode,
                category: product.category
            });
        }

       // showAlert(`تم إضافة ${product.name} إلى العربة`, 'success');
    }

    renderCart();
    renderSearchedProducts();

    // ✅ تحديث المبلغ المدفوع تلقائياً في حالة النقدي
    if (paymentType === 1) {
        const subtotal = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);
        const paidAmountInput = document.getElementById('paidAmount');
        if (paidAmountInput && !paidAmountInput.disabled) {
            paidAmountInput.value = subtotal.toFixed(2);
        }
    }
    calculateTotals();
    searchInput.value = '';
    searchResults.classList.remove('show');
}

// Remove from Cart
function removeFromCart(index) {
    const item = cart[index];
    const productId = item.id;

    cart.splice(index, 1);

    // Remove from searched products list
    const searchedIndex = searchedProducts.findIndex(p => p.id === productId);
    if (searchedIndex !== -1) {
        searchedProducts.splice(searchedIndex, 1);
    }

    renderCart();
    renderSearchedProducts();
    // ✅ تحديث المبلغ المدفوع تلقائياً في حالة النقدي
    if (paymentType === 1) {
        const subtotal = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);
        const paidAmountInput = document.getElementById('paidAmount');
        if (paidAmountInput && !paidAmountInput.disabled) {
            paidAmountInput.value = subtotal.toFixed(2);
        }
    }

    calculateTotals();
}

// Update Quantity
function updateQuantity(index, quantity) {
    const qty = parseInt(quantity);
    if (qty > 0) {
        // 1️⃣ تحديث الكمية
        cart[index].quantity = qty;

        // 2️⃣ تحديث المبلغ المدفوع (للنقدي فقط)
        if (paymentType === 1) {
            const subtotal = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);
            const paidAmountInput = document.getElementById('paidAmount');
            if (paidAmountInput && !paidAmountInput.disabled) {
                paidAmountInput.value = subtotal.toFixed(2);
            }
        }

        // 3️⃣ إعادة حساب كل شيء (subtotal, total, remaining)
        calculateTotals();
    } else {
        removeFromCart(index);
    }
}
// Render Searched Products List
function renderSearchedProducts() {
    const container = document.getElementById('searchedProductsList');

    if (!container) return; // If container doesn't exist, skip

    if (searchedProducts.length === 0) {
        container.innerHTML = '<p style="color: #9ca3af; text-align: center; padding: 20px;">لا توجد منتجات محفوظة</p>';
        return;
    }

    container.innerHTML = `
        <div class="searched-products-grid">
            ${searchedProducts.map(p => {
        const inCart = cart.some(item => item.id === p.id);
        return `
                    <div class="searched-product-card ${inCart ? 'in-cart' : ''}" onclick='${!inCart ? `addToCartQuick(${JSON.stringify(p).replace(/'/g, "&apos;")})` : ''}'>
                        <img src="${p.imagePath || '/images/no-image.png'}" alt="${p.name}">
                        <div class="searched-product-info">
                            <div class="searched-product-name">${p.name}</div>
                            <div class="searched-product-price">${p.price.toFixed(2)} ج.م</div>
                            ${inCart ? '<span class="in-cart-indicator">✓ في العربة</span>' : '<span class="add-indicator">+ إضافة</span>'}
                        </div>
                    </div>
                `;
    }).join('')}
        </div>
    `;
}

// Quick add from searched products list
function addToCartQuick(product) {
    addToCart(product);
}

// Render Cart
function renderCart() {
    const container = document.getElementById('cartContainer');

    if (cart.length === 0) {
        container.innerHTML = `
            <div class="cart-empty">
                <i class="bi bi-cart-x" style="font-size: 48px; color: #d1d5db;"></i>
                <p>لا توجد منتجات في العربة</p>
                <small>استخدم البحث أعلاه لإضافة منتجات</small>
            </div>
        `;
        document.getElementById('completeSaleBtn').disabled = true;
        return;
    }

    let html = `
        <table class="cart-table">
            <thead>
                <tr>
                    <th>المنتج</th>
                    <th>السعر</th>
                    <th>الكمية</th>
                    <th>الإجمالي</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
    `;

    cart.forEach((item, index) => {
        const itemTotal = item.price * item.quantity;
        html += `
            <tr>
                <td>
                    <div style="display: flex; align-items: center; gap: 10px;">
                        <img src="${item.imagePath || '/images/no-image.png'}"
                             style="width: 40px; height: 40px; border-radius: 6px; object-fit: cover;">
                        <strong>${item.name}</strong>
                    </div>
                </td>
                <td>${item.price.toFixed(2)} ج.م</td>
                <td>
                    <input type="number"
                           class="qty-input"
                           value="${item.quantity}"
                           min="1"
                           onchange="updateQuantity(${index}, this.value)">
                </td>
                <td style="font-weight: 700; color: #1e3a8a;">${itemTotal.toFixed(2)} ج.م</td>
                <td>
                    <button class="btn-remove" onclick="removeFromCart(${index})" title="حذف">
                        <i class="bi bi-trash"></i>
                    </button>
                </td>
            </tr>
        `;
    });

    html += `
            </tbody>
        </table>
    `;

    container.innerHTML = html;
    document.getElementById('completeSaleBtn').disabled = false;
}

// Calculate Totals
function calculateTotals() {
    const subtotal = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    
    // ✅ إضافة الفائدة للمبلغ الكلي في حالة التقسيط
    const total = paymentType === 2 ? subtotal + currentInterestAmount : subtotal;
    
    const paidAmount = parseFloat(document.getElementById('paidAmount').value) || 0;
    const remaining = total - paidAmount;

    document.getElementById('subtotal').textContent = subtotal.toFixed(2) + ' ج.م';
    document.getElementById('total').textContent = total.toFixed(2) + ' ج.م';
    document.getElementById('remaining').textContent = remaining.toFixed(2) + ' ج.م';

    // Update remaining amount badge color
    const remainingElement = document.getElementById('remaining').parentElement;
    if (remainingElement) {
        if (remaining > 0) {
            remainingElement.style.color = '#dc2626';
        } else {
            remainingElement.style.color = '#059669';
        }
    }
}

// Payment Amount Change
document.getElementById('paidAmount')?.addEventListener('input', calculateTotals);

// Payment Method Selection
document.querySelectorAll('.payment-method').forEach(method => {
    method.addEventListener('click', function () {
        document.querySelectorAll('.payment-method').forEach(m => m.classList.remove('active'));
        this.classList.add('active');
        paymentType = parseInt(this.dataset.type);

        const subtotal = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);
        const paidAmountInput = document.getElementById('paidAmount');
        const installmentSection = document.getElementById('installmentSection');

        if (paymentType === 1) {
            // ✅ CASH: Reset interest and auto-fill full amount
            currentInterestAmount = 0; // إعادة تعيين الفائدة
            if (paidAmountInput) {
                paidAmountInput.value = subtotal.toFixed(2);
                paidAmountInput.disabled = false; // ✅ إعادة تفعيل الحقل
            }
            if (installmentSection) {
                installmentSection.style.display = 'none';
            }
        } else {
            // ✅ INSTALLMENT: Clear paid amount (will use down payment)
            if (paidAmountInput) {
                paidAmountInput.value = '0';
                paidAmountInput.disabled = true;
            }
            if (installmentSection) {
                installmentSection.style.display = 'block';
                const minDownPayment = (subtotal * 0.1).toFixed(2);
                const minDownPaymentEl = document.getElementById('minDownPayment');
                if (minDownPaymentEl) {
                    minDownPaymentEl.textContent = `( الحد الأدنى: ${minDownPayment} ج.م )`;
                }
                calculateInstallment();
            }
        }

        calculateTotals(); // ✅ تحديث الإجماليات بعد تغيير طريقة الدفع
    });
});

// Update installment calculation to sync paid amount
document.getElementById('downPayment')?.addEventListener('input', function() {
    // ✅ Sync down payment with paid amount for installments
    if (paymentType === 2) {
        const paidAmountInput = document.getElementById('paidAmount');
        if (paidAmountInput) {
            paidAmountInput.value = this.value;
        }
        calculateTotals(); // ✅ تحديث المتبقي عند تغيير الدفعة المقدمة
    }
    calculateInstallment();
});

// Installment Calculation
const monthsSelect = document.getElementById('numberOfMonths');
const downPaymentInput = document.getElementById('downPayment');

if (monthsSelect) monthsSelect.addEventListener('change', function() {
    calculateInstallment();
    // ✅ تحديث المتبقي فوراً بعد تغيير عدد الأشهر
    if (paymentType === 2) {
        calculateTotals();
    }
});
if (downPaymentInput) downPaymentInput.addEventListener('input', calculateInstallment);

function calculateInstallment() {
    const numberOfMonths = parseInt(document.getElementById('numberOfMonths')?.value || 0);
    const downPayment = parseFloat(document.getElementById('downPayment')?.value || 0);

    const subtotal = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);

    const minDownPayment = subtotal * 0.1;
    // ❌ إزالة التنبيه من هنا - سيتم التحقق فقط عند إتمام البيع
    // if (downPayment < minDownPayment && downPayment > 0) {
    //     showAlert(`الدفعة المقدمة يجب أن تكون ${minDownPayment.toFixed(2)} ج.م على الأقل`, 'error');
    // }

    const interestRate = getInterestRate(numberOfMonths);
    const interestAmount = (subtotal * interestRate).toFixed(2);

    // ✅ حفظ قيمة الفائدة في المتغير العام
    currentInterestAmount = parseFloat(interestAmount);

    const totalWithInterest = (subtotal + currentInterestAmount).toFixed(2);
    const remainingForInstallment = (parseFloat(totalWithInterest) - downPayment).toFixed(2);
    const monthlyPayment = numberOfMonths > 0 ? (remainingForInstallment / numberOfMonths).toFixed(2) : '0.00';

    const interestRateEl = document.getElementById('interestRateDisplay');
    const interestAmountEl = document.getElementById('interestAmountDisplay');
    const totalWithInterestEl = document.getElementById('totalWithInterestDisplay');
    const monthlyPaymentEl = document.getElementById('monthlyPaymentDisplay');

    if (interestRateEl) interestRateEl.textContent = `${(interestRate * 100).toFixed(2)}%`;
    if (interestAmountEl) interestAmountEl.textContent = `${interestAmount} ج.م`;
    if (totalWithInterestEl) totalWithInterestEl.textContent = `${totalWithInterest} ج.م`;
    if (monthlyPaymentEl) monthlyPaymentEl.textContent = `${monthlyPayment} ج.م`;

    // ✅ تحديث المبلغ الكلي والمتبقي بعد حساب الفائدة
    calculateTotals();
}
function getInterestRate(months) {
    switch (months) {
        case 3: return 0.015;
        case 6: return 0.02;
        case 12: return 0.025;
        case 24: return 0.03;
        default: return 0;
    }
}

// Complete Sale - تحديث totalAmount ليشمل الفائدة
document.getElementById('completeSaleBtn')?.addEventListener('click', async function () {
    if (cart.length === 0) {
        showAlert('الرجاء إضافة منتجات للعربة', 'error');
        return;
    }

    const customerId = document.getElementById('customerId').value;
    if (!customerId) {
        showAlert('الرجاء اختيار عميل', 'error');
        return;
    }

    const saleDate = document.getElementById('saleDate').value;
    const subtotal = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);
    
    // ✅ المبلغ الكلي يشمل الفائدة في حالة التقسيط
    const totalAmount = paymentType === 2 ? subtotal + currentInterestAmount : subtotal;
    
    let paidAmount = 0;
    let numberOfMonths = null;
    let downPayment = null;

    if (paymentType === 1) {
        // Cash payment
        paidAmount = parseFloat(document.getElementById('paidAmount').value) || 0;
    } else {
        // Installment payment
        downPayment = parseFloat(document.getElementById('downPayment').value);
        paidAmount = downPayment;
        numberOfMonths = parseInt(document.getElementById('numberOfMonths').value);

        const minDownPayment = subtotal * 0.1; // ✅ الحد الأدنى على السعر الأصلي
        if (downPayment < minDownPayment) {
            showAlert(`الدفعة المقدمة يجب أن تكون ${minDownPayment.toFixed(2)} ج.م على الأقل`, 'error');
            return;
        }
    }
    const interestRate = getInterestRate(numberOfMonths);

    const remainingAmount = totalAmount - paidAmount;
    var installment = {
        numberOfMonths: numberOfMonths,
        InterestRate: interestRate,
        DownPayment: downPayment,   


    };
    const data = {
        customerId: parseInt(customerId),
        saleDate: saleDate,
        totalAmount: totalAmount, // ✅ الآن يشمل الفائدة
        paidAmount: paidAmount,
        remainingAmount: remainingAmount,
        paymentType: paymentType,
        numberOfMonths: numberOfMonths,
        downPayment: downPayment,
        Installment: paymentType=="2"? installment:null,
        items: cart.map(item => ({
            productId: item.id,
            quantity: item.quantity,
            unitPrice: item.price,
            total: item.price * item.quantity
        }))
    };

    const btn = this;
    btn.disabled = true;
    btn.innerHTML = '<i class="bi bi-hourglass-split"></i> جاري الحفظ...';

    try {
        const response = await fetch('/Sales/Create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify(data)
        });

        if (response.ok) {
            showAlert('تم إنشاء الفاتورة بنجاح ✓', 'success');
            setTimeout(() => {
                window.location.href = '/Sales/Index';
            }, 1500);
        } else {
            const error = await response.text();
            showAlert('حدث خطأ: ' + error, 'error');
            btn.disabled = false;
            btn.innerHTML = '<i class="bi bi-check-circle"></i> إتمام البيع';
        }
    } catch (error) {
        console.error('Error:', error);
        showAlert('حدث خطأ أثناء الحفظ', 'error');
        btn.disabled = false;
        btn.innerHTML = '<i class="bi bi-check-circle"></i> إتمام البيع';
    }
});


// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    const saleDateInput = document.getElementById('saleDate');
    if (saleDateInput && !saleDateInput.value) {
        saleDateInput.value = new Date().toISOString().split('T')[0];
    }

    searchInput?.focus();
    renderSearchedProducts();
    
    // ✅ إخفاء قسم التقسيط عند تحميل الصفحة (Cash هو الافتراضي)
    const installmentSection = document.getElementById('installmentSection');
    if (installmentSection) {
        installmentSection.style.display = 'none';
    }
    
    // ✅ تفعيل حقل المبلغ المدفوع
    const paidAmountInput = document.getElementById('paidAmount');
    if (paidAmountInput) {
        paidAmountInput.disabled = false;
    }
});