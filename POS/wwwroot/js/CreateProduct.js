
        // Toggle Motorcycle Fields
        function toggleMotorcycle() {
            const toggle = document.getElementById('motorcycleToggle');
            const fields = document.getElementById('motorcycleFields');
            const checkbox = document.getElementById('isMotorcycle');

            checkbox.checked = !checkbox.checked;

            if (checkbox.checked) {
                toggle.classList.add('active');
                fields.classList.add('show');
            } else {
                toggle.classList.remove('active');
                fields.classList.remove('show');
            }
        }

        // Calculate Profit Margin
        const purchaseInput = document.getElementById('purchasePrice');
        const saleInput = document.getElementById('salePrice');
        const displayPurchase = document.getElementById('displayPurchase');
        const displaySale = document.getElementById('displaySale');
        const displayProfit = document.getElementById('displayProfit');

        function calculateProfit() {
            const purchase = parseFloat(purchaseInput.value) || 0;
            const sale = parseFloat(saleInput.value) || 0;
            const profit = sale - purchase;
            const percentage = purchase > 0 ? ((profit / purchase) * 100).toFixed(1) : 0;

            displayPurchase.textContent = purchase.toFixed(2) + ' ج.م';
            displaySale.textContent = sale.toFixed(2) + ' ج.م';

            if (profit >= 0) {
                displayProfit.textContent = profit.toFixed(2) + ' ج.م (' + percentage + '%)';
                displayProfit.className = 'comparison-value profit';
            } else {
                displayProfit.textContent = profit.toFixed(2) + ' ج.م (' + percentage + '%)';
                displayProfit.className = 'comparison-value loss';
            }
        }

        purchaseInput.addEventListener('input', calculateProfit);
        saleInput.addEventListener('input', calculateProfit);

        // Stock Indicator
        const quantityInput = document.getElementById('quantity');
        const minStockInput = document.getElementById('minStock');
        const stockIndicator = document.getElementById('stockIndicator');

        function updateStockIndicator() {
            const quantity = parseInt(quantityInput.value) || 0;
            const minStock = parseInt(minStockInput.value) || 0;

            if (quantity === 0) {
                stockIndicator.className = 'stock-indicator danger mt-3';
                stockIndicator.innerHTML = '<i class="bi bi-x-circle-fill"></i><span>نفذ المخزون!</span>';
            } else if (quantity <= minStock) {
                stockIndicator.className = 'stock-indicator warning mt-3';
                stockIndicator.innerHTML = '<i class="bi bi-exclamation-circle-fill"></i><span>المخزون منخفض - يجب إعادة الطلب</span>';
            } else {
                stockIndicator.className = 'stock-indicator success mt-3';
                stockIndicator.innerHTML = '<i class="bi bi-check-circle-fill"></i><span>المخزون في الحد الآمن</span>';
            }
        }

        quantityInput.addEventListener('input', updateStockIndicator);
        minStockInput.addEventListener('input', updateStockIndicator);
