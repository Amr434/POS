(function () {
    const table = document.getElementById("purchase-items-table");
    if (!table) {
        return;
    }

    const tbody = table.querySelector("tbody");
    const addButton = document.getElementById("add-item-row");
    const grandTotalElement = document.getElementById("purchase-grand-total");

    const buildProductOptions = () => {
        const firstSelect = tbody.querySelector("select[name$='.ProductId']");
        if (!firstSelect) return "";
        return firstSelect.innerHTML;
    };

    const recalcRow = (row) => {
        const quantity = Number(row.querySelector(".quantity-input")?.value || 0);
        const unitPrice = Number(row.querySelector(".unit-price-input")?.value || 0);
        const lineTotal = quantity * unitPrice;
        const lineTotalInput = row.querySelector(".line-total-input");
        if (lineTotalInput) {
            lineTotalInput.value = lineTotal.toFixed(2);
        }
    };

    const recalcGrandTotal = () => {
        let total = 0;
        tbody.querySelectorAll("tr").forEach((row) => {
            const quantity = Number(row.querySelector(".quantity-input")?.value || 0);
            const unitPrice = Number(row.querySelector(".unit-price-input")?.value || 0);
            total += quantity * unitPrice;
        });
        if (grandTotalElement) {
            grandTotalElement.textContent = total.toFixed(2);
        }
    };

    const reindexRows = () => {
        tbody.querySelectorAll("tr").forEach((row, index) => {
            row.querySelectorAll("input, select, span[data-valmsg-for]").forEach((el) => {
                if (el.name) {
                    el.name = el.name.replace(/Items\[\d+\]/g, `Items[${index}]`);
                }
                const valMsg = el.getAttribute("data-valmsg-for");
                if (valMsg) {
                    el.setAttribute("data-valmsg-for", valMsg.replace(/Items\[\d+\]/g, `Items[${index}]`));
                }
            });
        });
    };

    const bindRowEvents = (row) => {
        row.querySelectorAll(".quantity-input, .unit-price-input").forEach((input) => {
            input.addEventListener("input", () => {
                recalcRow(row);
                recalcGrandTotal();
            });
        });

        const removeButton = row.querySelector(".remove-item-row");
        if (removeButton) {
            removeButton.addEventListener("click", () => {
                if (tbody.querySelectorAll("tr").length === 1) {
                    return;
                }
                row.remove();
                reindexRows();
                recalcGrandTotal();
            });
        }
    };

    const addRow = () => {
        const index = tbody.querySelectorAll("tr").length;
        const row = document.createElement("tr");
        const optionsHtml = buildProductOptions();
        row.innerHTML = `
            <td>
                <select name="Items[${index}].ProductId" class="form-select">
                    ${optionsHtml}
                </select>
                <span class="text-danger field-validation-valid" data-valmsg-for="Items[${index}].ProductId" data-valmsg-replace="true"></span>
            </td>
            <td>
                <input name="Items[${index}].Quantity" type="number" min="1" class="form-control quantity-input" value="1" />
                <span class="text-danger field-validation-valid" data-valmsg-for="Items[${index}].Quantity" data-valmsg-replace="true"></span>
            </td>
            <td>
                <input name="Items[${index}].UnitPrice" type="number" min="0" step="0.01" class="form-control unit-price-input" value="0" />
                <span class="text-danger field-validation-valid" data-valmsg-for="Items[${index}].UnitPrice" data-valmsg-replace="true"></span>
            </td>
            <td>
                <input type="text" class="form-control line-total-input" value="0.00" readonly />
            </td>
            <td>
                <button type="button" class="btn btn-link text-danger remove-item-row">
                    <i class="bi bi-trash"></i>
                </button>
            </td>
        `;
        tbody.appendChild(row);
        bindRowEvents(row);
        recalcGrandTotal();
    };

    tbody.querySelectorAll("tr").forEach((row) => {
        bindRowEvents(row);
        recalcRow(row);
    });
    recalcGrandTotal();

    if (addButton) {
        addButton.addEventListener("click", addRow);
    }
})();
