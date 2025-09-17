
namespace YamyProject.Services.Implementations
{
    public class SalesServices : ISalesServices
    {
        private readonly YamyDbContext _db;
        private readonly IMicroserviceClient _microserviceClient;

        public SalesServices(YamyDbContext db, IMicroserviceClient microserviceClient)
        {
            _db = db;
            _microserviceClient = microserviceClient;
        }

        public async Task<SalesEditViewModel?> GetSaleForEditAsync(int id)
        {
            var sale = await _db.TblSales
                .Include(s => s.TblSalesDetails) // depends on your EF navigation properties
                .FirstOrDefaultAsync(s => s.Id == id && s.State == 0);

            if (sale == null) return null;

            var vm = new SalesEditViewModel
            {
                Id = sale.Id,
                Date = sale.Date.ToDateTime(TimeOnly.MinValue),
                CustomerId = sale.CustomerId,
                InvoiceId = sale.InvoiceId,
                WarehouseId = sale.WarehouseId,
                PoNum = sale.PoNum,
                BillTo = sale.BillTo,
                City = sale.City,
                SalesMan = sale.SalesMan,
                ShipDate = sale.ShipDate?.ToDateTime(TimeOnly.MinValue),
                ShipVia = sale.ShipVia,
                ShipTo = sale.ShipTo,
                PaymentMethod = sale.PaymentMethod,
                AccountCashId = sale.AccountCashId,
                PaymentTerms = sale.PaymentTerms,
                PaymentDate = sale.PaymentDate.ToDateTime(TimeOnly.MinValue),
                Total = sale.Total,
                Vat = sale.Vat,
                Net = sale.Net,
                Pay = sale.Pay,
                Change = sale.Change,
                Details = sale.TblSalesDetails.Select(d => new SalesDetailViewModel
                {
                    Id = d.Id,
                    ItemId = d.ItemId ?? 0,
                    Qty = d.Qty ?? 0,
                    CostPrice = d.CostPrice,
                    Price = d.Price ?? 0,
                    Vatp = d.Vatp,
                    Vat = d.Vat,
                    Total = d.Total,
                    Discount = d.Discount,
                    CostCenterId = d.CostCenterId
                }).ToList()
            };

            return vm;
        }

        public async Task<int> CreateSaleAsync(SalesEditViewModel vm)
        {
            // Basic validation
            if (vm.Details == null || !vm.Details.Any())
                throw new InvalidOperationException("Can't save empty invoice");

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // map VM to entity
                var entity = new TblSale
                {
                    Date = DateOnly.FromDateTime(vm.Date),
                    CustomerId = vm.CustomerId,
                    InvoiceId = vm.InvoiceId,
                    WarehouseId = vm.WarehouseId,
                    PoNum = vm.PoNum ?? "",
                    BillTo = vm.BillTo ?? "",
                    City = vm.City ?? "",
                    SalesMan = vm.SalesMan ?? "",
                    ShipDate = vm.ShipDate.HasValue ? DateOnly.FromDateTime(vm.ShipDate.Value) : null,
                    ShipVia = vm.ShipVia ?? "",
                    ShipTo = vm.ShipTo ?? "",
                    PaymentMethod = vm.PaymentMethod,
                    AccountCashId = vm.AccountCashId,
                    PaymentTerms = vm.PaymentTerms ?? "",
                    PaymentDate = vm.PaymentDate.HasValue ? DateOnly.FromDateTime(vm.PaymentDate.Value) : DateOnly.FromDateTime(DateTime.Now),
                    Total = vm.Total,
                    Vat = vm.Vat,
                    Net = vm.Net,
                    Pay = vm.Pay,
                    Change = vm.Change,
                    CreatedBy = vm.CreatedBy,
                    CreatedDate = DateOnly.FromDateTime(vm.CreatedDate),
                    State = 0,
                    Discount = 0,
                    CostCenterId = 0,
                    ProjectId = 0
                };

                _db.TblSales.Add(entity);
                await _db.SaveChangesAsync();

                // Insert details and handle inventory / item transactions
                decimal totalInventoryCost = 0m;
                foreach (var d in vm.Details)
                {
                    var detail = new TblSalesDetail
                    {
                        SalesId = entity.Id,
                        ItemId = d.ItemId,
                        Qty = d.Qty,
                        CostPrice = d.CostPrice,
                        Price = d.Price,
                        Discount = d.Discount,
                        Vatp = d.Vatp,
                        Vat = d.Vat,
                        Total = d.Total,
                        CostCenterId = d.CostCenterId
                    };
                    _db.TblSalesDetails.Add(detail);
                    await _db.SaveChangesAsync();

                    // Inventory handling for non-service items
                    // Read item type & method from tbl_items (I use raw query assuming Items DbSet exists)
                    var item = await _db.TblItems.FindAsync(d.ItemId);
                    if (item != null && item.Type != "12 - Service")
                    {
                        // reduce on_hand
                        item.OnHand -= d.Qty;
                        _db.TblItems.Update(item);

                        // call the FIFO/LIFO/AVG cost calculation helper (extracted from frmSales.InsertItemTransaction)
                        var cost = await ProcessItemTransactionAsync(entity.Id, d.ItemId, d.Qty, item.Method, vm.Date, d.Price);
                        totalInventoryCost += cost;
                    }
                }

                // Accounting transactions
                // Recreate the Transaction() logic: add transaction entries via CommonInsert (or local methods)
                if (vm.Net > 0)
                {
                    // Example: create transaction entries - these would call accounting submodule/repository
                    await AddTransactionEntriesAsync(entity.Id, vm);
                    if (totalInventoryCost > 0)
                    {
                        // Create COGS and inventory entries
                        await AddCogsInventoryEntriesAsync(entity.Id, totalInventoryCost, vm);
                    }
                }

                await tx.CommitAsync();

                // Notify microservice (accounting, posting)
                await _microserviceClient.NotifyAccountingAsync(entity.Id);

                return entity.Id;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateSaleAsync(SalesEditViewModel vm)
        {
            if (!vm.Id.HasValue) throw new ArgumentException("Id required for update");
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var sale = await _db.TblSales.Include(s => s.TblSalesDetails).FirstOrDefaultAsync(s => s.Id == vm.Id.Value);
                if (sale == null) return false;

                // simple mapping of header fields
                sale.Date = DateOnly.FromDateTime(vm.Date);
                sale.CustomerId = vm.CustomerId;
                sale.InvoiceId = vm.InvoiceId;
                sale.WarehouseId = vm.WarehouseId;
                sale.PoNum = vm.PoNum ?? "";
                sale.BillTo = vm.BillTo ?? "";
                sale.City = vm.City ?? "";
                sale.SalesMan = vm.SalesMan ?? "";
                sale.ShipDate = vm.ShipDate.HasValue ? DateOnly.FromDateTime(vm.ShipDate.Value) : null;
                sale.ShipVia = vm.ShipVia;
                sale.ShipTo = vm.ShipTo;
                sale.PaymentMethod = vm.PaymentMethod;
                sale.AccountCashId = vm.AccountCashId;
                sale.PaymentTerms = vm.PaymentTerms ?? "";
                sale.PaymentDate = vm.PaymentDate.HasValue ? DateOnly.FromDateTime(vm.PaymentDate.Value) : sale.PaymentDate;
                sale.Total = vm.Total;
                sale.Vat = vm.Vat;
                sale.Net = vm.Net;
                sale.Pay = vm.Pay;
                sale.Change = vm.Change;
                sale.ModifiedBy = vm.CreatedBy;
                sale.ModifiedDate = DateOnly.FromDateTime(DateTime.Now);

                // Remove old details and reverse inventory transactions similar to ReturnItemsToInventory
                // Here we shall: for each old detail -> increase on_hand back, reverse item transactions (domain logic)
                foreach (var old in sale.TblSalesDetails.ToList())
                {
                    var item = await _db.TblItems.FindAsync(old.ItemId);
                    if (item != null && item.Type != "12 - Service")
                    {
                        item.OnHand += old.Qty ?? 0;
                        _db.TblItems.Update(item);

                        // revert item transactions (complex) - for brevity, call a helper:
                        await RevertItemTransactionsAsync(sale.Id, old.ItemId.Value, old.Qty ?? 0);
                    }
                }
                // delete old details
                _db.TblSalesDetails.RemoveRange(sale.TblSalesDetails);
                await _db.SaveChangesAsync();

                // insert new details (similar to Create)
                decimal totalInventoryCost = 0;
                foreach (var d in vm.Details)
                {
                    var detail = new TblSalesDetail
                    {
                        SalesId = sale.Id,
                        ItemId = d.ItemId,
                        Qty = d.Qty,
                        CostPrice = d.CostPrice,
                        Price = d.Price,
                        Discount = d.Discount,
                        Vatp = d.Vatp,
                        Vat = d.Vat,
                        Total = d.Total,
                        CostCenterId = d.CostCenterId
                    };
                    _db.TblSalesDetails.Add(detail);
                    await _db.SaveChangesAsync();

                    var item = await _db.TblItems.FindAsync(d.ItemId);
                    if (item != null && item.Type != "12 - Service")
                    {
                        item.OnHand -= d.Qty;
                        _db.TblItems.Update(item);

                        var cost = await ProcessItemTransactionAsync(sale.Id, d.ItemId, d.Qty, item.Method, vm.Date, d.Price);
                        totalInventoryCost += cost;
                    }
                }

                // Recreate accounting entries (delete previous entries and add new ones)
                await RecreateAccountingEntriesAsync(sale.Id, vm, totalInventoryCost);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // notify microservice
                await _microserviceClient.NotifyAccountingAsync(sale.Id);

                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        #region Helper private methods (sketches)
        private async Task<decimal> ProcessItemTransactionAsync(int saleId, int itemId, decimal qty, string method, DateTime saleDate, decimal salePrice)
        {
            // This helper implements the FIFO/LIFO/AVG logic from frmSales.InsertItemTransaction
            // For brevity: return computed cost based on available item transactions, and create item transaction entries in tbl_item_transaction using a repository
            // Pseudocode steps:
            // 1. if method == fifo/lifo: read item transactions ordered asc/desc and consume qty, compute cost
            // 2. else compute average cost from item transactions
            // 3. insert new item_transaction record(s) for this sale (CommonInsert.InsertItemTransaction equivalent)
            // 4. return total cost (qty * cost_price)
            //
            // Implement according to your exact tbl_item_transaction schema.

            decimal totalCost = 0m;
            // ... implement DB logic here using _db.Database.ExecuteSqlRawAsync or EF entities for item_transaction
            return totalCost;
        }

        private async Task RevertItemTransactionsAsync(int saleId, int itemId, decimal qty)
        {
            // Revert earlier commitments to qty_inc etc. Implement per your business rules (mirrors ReturnItemsToInventory and ProcessInventoryReturn)
        }

        private async Task AddTransactionEntriesAsync(int saleId, SalesEditViewModel vm)
        {
            // map to CommonInsert.addTransactionEntry in your WinForms code.
            // This likely inserts rows into tbl_transactions. Implement repository call or direct SQL as per your project.
        }

        private async Task AddCogsInventoryEntriesAsync(int saleId, decimal totalInventoryCost, SalesEditViewModel vm)
        {
            // Add COGS and inventory accounting entries similar to frmSales.Transaction().
        }

        private async Task RecreateAccountingEntriesAsync(int saleId, SalesEditViewModel vm, decimal totalInventoryCost)
        {
            // Delete existing transaction entries for saleId and re-add based on new totals.
        }
        #endregion
    }
}
