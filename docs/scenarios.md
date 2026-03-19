# POS Inventory and Sales Scenarios (Starter Coverage)

This document describes the important business flows and edge cases for a POS that supports:
- Inventory batches (`InventoryBatch`) tracked per purchase, sold using FIFO
- Purchases (`Purchase`, `PurchaseItem`) from suppliers
- Sales (`Sale`, `SaleItem`) to customers
- Cash and installment payments (`PaymentType`, `Installment`, `Payment`)
- Low-stock notifications and basic expenses

Assumptions (align these with your implementation):
- Inventory on-hand for a product = `SUM(InventoryBatch.RemainingQuantity)` for that `ProductId`.
- FIFO cost uses `InventoryBatch.UnitPrice` for each batch consumed.
- When selling, batches are consumed ordered by `InventoryBatch.PurchaseDate` ascending, and then by `InventoryBatch.Id` ascending if dates tie.
- `SaleItem.UnitSalePrice` is stored at the time of sale and is the source for revenue/profit calculations.

---

## 1. Master Data Setup
### 1.1 Categories
- User creates/updates `Category` with a `Name`.
- A product must reference an existing `CategoryId`.
- Prevent orphan products: if category is deleted, product references must be handled (either block delete or cascade rules).

### 1.2 Suppliers
- User creates/updates `Supplier` with `Name`, `Phone`, `Address` (fields required by your validation).
- A purchase must reference a valid `SupplierId`.

### 1.3 Customers
- User creates/updates `Customer` with `Name`, `Phone`, `Address`, `NationalId` (if required).
- A sale must reference a valid `CustomerId`.
- Phone/NationalId formatting validation (as per your requirements).

### 1.4 Products
- User creates `Product` with:
  - `Name`, `Barcode`, `SalePrice` (default), `MinStock`, `Status`, and optional `EngineNumber`/`ChassisNumber`
  - `CategoryId`
- Barcode scanning scenarios:
  - Barcode exists -> product is loaded into sale/purchase UI.
  - Barcode not found -> show error and prevent sale line creation.
  - Barcode duplicated -> prevent creation or enforce unique constraint.
- Product status scenarios:
  - Selling allowed only for appropriate statuses (example: block if `Status == Reserved` if you treat reserved as not sellable).
  - Status changes after sale/purchase (define exact rules for your app).

---

## 2. Purchasing Stock (Receiving)
### 2.1 Create Purchase and PurchaseItems
- User creates `Purchase` for a `SupplierId` with `PurchaseDate` (default to now if not provided).
- User adds multiple `PurchaseItem` lines:
  - `ProductId` must exist
  - `Quantity` must be > 0
  - `UnitPrice` must be >= 0
- Prevent purchases with zero items.
- Purchase totals:
  - `Purchase.TotalAmount` = `SUM(PurchaseItem.Quantity * PurchaseItem.UnitPrice)`

### 2.2 InventoryBatch Creation per PurchaseItem
- For each `PurchaseItem`, system creates exactly one `InventoryBatch` that includes:
  - `ProductId`
  - `PurchaseItemId`
  - `Quantity` and `RemainingQuantity` (initially equal)
  - `UnitPrice` (copied from `PurchaseItem.UnitPrice`)
  - `PurchaseDate`
- Even if buying the same `ProductId` multiple times in one purchase:
  - Each line becomes its own `InventoryBatch` (do not merge in write-model unless you explicitly support aggregation).

### 2.3 Same Product, Different Prices
- Buying product X with different `UnitPrice` results in multiple batches with different `InventoryBatch.UnitPrice`.
- FIFO sales must later consume the oldest batch first, therefore producing correct profit per batch.

### 2.4 Same Product, Same Price
- Buying product X at the same `UnitPrice` still creates new batch records per `PurchaseItem`.
- Reporting can aggregate, but write-model should remain traceable to the purchase event.

### 2.5 Purchase Transaction Safety
- If a purchase request fails mid-way (DB error, invalid item):
  - No partial `InventoryBatch` should remain (use a DB transaction).

### 2.6 Purchase Cancellation / Void (if supported)
- If users can cancel a purchase after it was received:
  - Inventory must be reversed (remove or reduce the corresponding `InventoryBatch` quantities).
  - If any of those batches were already sold, define behavior (block cancel, or create compensating records).

---

## 3. Inventory Availability and FIFO Rules
### 3.1 Sell Eligibility Check (Pre-validation)
- Before writing a sale, system verifies for each `SaleItem`:
  - Total available = `SUM(RemainingQuantity)` across batches for that product
  - Must be >= requested `SaleItem.Quantity`
- If not enough stock:
  - Reject the sale (or define “partial sale” rules if you support them).

### 3.2 FIFO Batch Consumption
- When a sale requires quantity Q for product P:
  - Consume batches for product P in FIFO order (oldest `PurchaseDate` first).
  - For each batch:
    - `used = MIN(batch.RemainingQuantity, remainingToSell)`
    - batch.RemainingQuantity decreases by `used`
    - Profit is computed for that batch portion using `batch.UnitPrice` as cost
- If sale quantity spans multiple batches:
  - Sale should be successful and correctly allocate cost/profit across batches.

### 3.3 Prevent Invalid Inventory States
- `InventoryBatch.RemainingQuantity` must never become negative.
- Selling a `Quantity` of 0 or negative must be rejected.
- Purchasing a `Quantity` of 0 or negative must be rejected.

---

## 4. Making Sales (POS Checkout)
### 4.1 Create Sale (Cash)
- User selects `CustomerId`.
- User adds multiple `SaleItems`:
  - `Quantity` > 0
  - `UnitSalePrice`:
    - Default suggestion = `Product.SalePrice`
    - Allowed override per line (discounts/price changes)
  - `SaleItem.Total` = `Quantity * UnitSalePrice`
- Sale totals:
  - `Sale.TotalAmount` = `SUM(SaleItem.Total)`
  - `PaymentType = Cash`
  - `Sale.PaidAmount = Sale.TotalAmount`
  - `Sale.RemainingAmount = 0`

### 4.2 Create Sale (Installment)
- User selects `CustomerId`.
- Sale items follow the same validation rules as cash sales.
- `PaymentType = Installment`
- Create `Installment` with:
  - `TotalAmount` = `Sale.TotalAmount`
  - `DownPayment` between 0 and `TotalAmount`
  - `RemainingAmount = TotalAmount - DownPayment`
  - `Months > 0`
  - `MonthlyPayment` computed using your rounding rules
- Sale payment values:
  - `Sale.PaidAmount = DownPayment`
  - `Sale.RemainingAmount = RemainingAmount`
- Create installment payments only when/if they are actually collected (unless you implement “schedule pre-generation”).

### 4.3 Multi-Product Sale
- A single sale can include multiple products.
- Inventory batch consumption must happen independently for each product in the same transaction.

### 4.4 Discounts and Price Overrides
- If a user applies a discount to a line:
  - discount affects `SaleItem.UnitSalePrice` (since that’s the stored historical price)
- Profit:
  - Profit uses actual `SaleItem.UnitSalePrice` vs batch `InventoryBatch.UnitPrice` for the consumed quantity.

### 4.5 Partial Payments (Installment Payments Over Time)
- After the installment is created, payments are recorded via `Payment`.
- Each collected payment reduces remaining amounts:
  - `Payment.Amount` > 0
  - `Amount` cannot exceed the current `Installment.RemainingAmount` (define reject vs cap)
- Update behavior:
  - `Installment.RemainingAmount -= Payment.Amount`
  - `Sale.PaidAmount += Payment.Amount`
  - `Sale.RemainingAmount -= Payment.Amount`

### 4.6 Rounding and Currency Edge Cases
- Define how decimals are rounded (commonly 2 decimals).
- Ensure `RemainingAmount` and final payoff reach exactly 0:
  - If monthly payments don’t divide evenly, last payment adjustment rules must be defined.

### 4.7 Sale Cancellation / Void (if supported)
- If users cancel/void a sale:
  - Inventory consumed from FIFO batches must be restored.
  - If installment payments exist, define compensating actions (delete installment/payment records or mark them void).

---

## 5. Reporting and Profit Scenarios (Derived Results)
### 5.1 On-hand Stock by Product
- For a product P, “stock on hand” = sum of `InventoryBatch.RemainingQuantity`.

### 5.2 Profit by Sale and by Product
- For each sale, profit is the sum across all consumed batches:
  - Profit per batch portion = `(SaleItem.UnitSalePrice - InventoryBatch.UnitPrice) * usedQuantity`
- Profit must reflect the actual historical sale price and actual batch cost.

### 5.3 Profit correctness with multiple batches
- Scenario where product is purchased at 3 different prices:
  - Sale consumes old->new batches
  - Profit must match the batch-by-batch consumption, not a single average cost.

---

## 6. Low Stock Notifications
- A notification can be created when `Product` inventory drops below `Product.MinStock`.
- Trigger times (define behavior):
  - After a sale (inventory decreases)
  - After a purchase (inventory increases; maybe close/ignore low-stock notifications)
- Notification lifecycle:
  - `Notification.IsRead` can be marked true by the user.

---

## 7. Expenses (Operational Costs)
- User creates an `Expense` with:
  - `Title`, `Amount` >= 0, `ExpenseDate`, `Notes`
- Expenses do not directly change inventory batches.

---

## 8. Data Validation, Errors, and Concurrency
### 8.1 Missing References
- Invalid `CustomerId`/`SupplierId` -> reject request.
- Invalid `ProductId` in sale/purchase items -> reject request.

### 8.2 Zero/Negative Values
- Quantity <= 0 rejected for sale and purchase items.
- Amount/UnitPrice validations as per your rules.

### 8.3 Atomicity
- Sale write must be atomic:
  - `Sale` + `SaleItems` + FIFO inventory deductions must succeed together.
- Purchase write must be atomic:
  - `Purchase` + `PurchaseItems` + `InventoryBatch` creation must succeed together.

### 8.4 Concurrency / Preventing Oversell
- Two sales simultaneously for the same product when stock is low:
  - Only one sale should succeed if stock is insufficient.
  - The other must be rejected or retried after re-checking inventory.

---

## 9. Recommended Future Scenarios (If You Add Returns/Adjustments)
> These are common POS requirements, but they are not explicitly modeled in your current entities.

### 9.1 Returns / Refunds
- Returned quantity reduces customer debt/payment and restores inventory.
- You must decide how to reverse FIFO cost/profit:
  - Option A (recommended): store batch allocations per sale item so returns know which batches to restore.
  - Option B: define a consistent reverse policy (often “reverse FIFO”, but it must be deterministic).

### 9.2 Inventory Adjustments (Damage / Stock Count)
- Add a mechanism to correct inventory without a purchase/sale:
  - Damage reduces stock (and optionally tracks cost)
  - Stock count differences reconcile real stock vs system stock

---

## 10. Minimal Acceptance Criteria (Sanity Checks)
- Creating a purchase increases inventory by the purchased quantity (via batches).
- Creating a sale decreases inventory using FIFO order and never below zero.
- Profit calculations use `SaleItem.UnitSalePrice` and the exact consumed batch `UnitPrice`.
- Cash sales always end with `RemainingAmount = 0`.
- Installment sales:
  - `DownPayment` moves immediately to `PaidAmount`
  - `RemainingAmount` decreases only when `Payment` is recorded.