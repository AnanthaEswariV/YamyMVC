namespace YamyProject.Services.Implementations
{
    // Note: ApplicationDbContext is your EF DbContext that exposes DbSet<TblSale>, DbSet<TblSalesDetail>, etc.
    public class SalesService : ISalesService
    {
        private readonly YamyDbContext _db;
        private readonly ISalesClientService _micro;
        private readonly ILogger<SalesService> _logger;

        public SalesService(YamyDbContext db, ISalesClientService micro, ILogger<SalesService> logger)
        {
            _db = db;
            _micro = micro;
            _logger = logger;
        }

        public async Task<int> CreateSaleAsync(SaleCreateViewModel dto, CancellationToken ct = default)
        {
            // Input validation (example)
            if (dto.Details == null || dto.Details.Count == 0) throw new ArgumentException("Sale must have at least one detail");

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var sale = new TblSale
                {
                    Date = DateOnly.FromDateTime(dto.Date),
                    CustomerId = dto.CustomerId,
                    InvoiceId = dto.InvoiceId,
                    WarehouseId = dto.WarehouseId,
                    PaymentMethod = dto.PaymentMethod,
                    Total = dto.Total,
                    Vat = dto.Vat,
                    Net = dto.Net,
                    Pay = dto.Pay,
                    Change = dto.Change,
                    Discount = dto.Discount,
                    CreatedBy = /* get from context/user */ 1,
                    CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    State = 0
                };

                _db.TblSales.Add(sale);
                await _db.SaveChangesAsync(ct); // will populate sale.Id

                var details = dto.Details.Select(d => new TblSalesDetail
                {
                    SalesId = sale.Id,
                    ItemId = d.ItemId,
                    Qty = d.Qty,
                    Price = d.Price,
                    Vatp = d.VatPercent,
                    Total = d.Total,
                    Discount = d.Discount ?? 0,
                    ProjectId = 0
                }).ToList();

                _db.TblSalesDetails.AddRange(details);
                await _db.SaveChangesAsync(ct);

                // call microservice to create accounting transaction (journal)
                await _micro.SendSaleTransactionAsync(sale.Id, ct);

                await tx.CommitAsync(ct);
                return sale.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateSaleAsync failed");
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task UpdateSaleAsync(SaleEditViewModel dto, CancellationToken ct = default)
        {
            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var sale = await _db.TblSales.FindAsync(new object[] { dto.Id }, ct);
                if (sale == null) throw new KeyNotFoundException($"Sale {dto.Id} not found");

                // Update simple fields
                sale.Date = DateOnly.FromDateTime(dto.Date);
                sale.CustomerId = dto.CustomerId;
                sale.InvoiceId = dto.InvoiceId;
                sale.WarehouseId = dto.WarehouseId;
                sale.PaymentMethod = dto.PaymentMethod;
                sale.Total = dto.Total;
                sale.Vat = dto.Vat;
                sale.Net = dto.Net;
                sale.Pay = dto.Pay;
                sale.Change = dto.Change;
                sale.Discount = dto.Discount;
                sale.ModifiedBy = 1;
                sale.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);

                // Remove details not present, update existing, add new
                var existingDetails = _db.TblSalesDetails.Where(d => d.SalesId == dto.Id).ToList();
                var incomingIds = dto.Details.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToHashSet();

                var toRemove = existingDetails.Where(ed => !incomingIds.Contains(ed.Id)).ToList();
                if (toRemove.Any()) _db.TblSalesDetails.RemoveRange(toRemove);

                foreach (var d in dto.Details)
                {
                    if (d.Id.HasValue)
                    {
                        var ed = existingDetails.SingleOrDefault(x => x.Id == d.Id.Value);
                        if (ed == null) continue;
                        ed.ItemId = d.ItemId;
                        ed.Qty = d.Qty;
                        ed.Price = d.Price;
                        ed.Vatp = d.VatPercent;
                        ed.Total = d.Total;
                        ed.Discount = d.Discount ?? 0;
                    }
                    else
                    {
                        _db.TblSalesDetails.Add(new TblSalesDetail
                        {
                            SalesId = dto.Id,
                            ItemId = d.ItemId,
                            Qty = d.Qty,
                            Price = d.Price,
                            Vatp = d.VatPercent,
                            Total = d.Total,
                            Discount = d.Discount ?? 0,
                            ProjectId = 0
                        });
                    }
                }

                await _db.SaveChangesAsync(ct);

                // Optionally update transaction in microservice
                await _micro.SendSaleTransactionAsync(dto.Id, ct);

                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateSaleAsync failed");
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<SaleCreateViewModel?> GetSaleAsync(int id, CancellationToken ct = default)
        {
            var sale = await _db.TblSales.FindAsync(new object[] { id }, ct);
            if (sale == null) return null;

            var details = await _db.TblSalesDetails.Where(d => d.SalesId == id).ToListAsync(ct);
            return new SaleCreateViewModel
            {
                Date = sale.Date.ToDateTime(TimeOnly.MinValue),
                CustomerId = sale.CustomerId,
                InvoiceId = sale.InvoiceId,
                WarehouseId = sale.WarehouseId,
                Discount = sale.Discount,
                PaymentMethod = sale.PaymentMethod,
                Total = sale.Total,
                Vat = sale.Vat,
                Net = sale.Net,
                Pay = sale.Pay,
                Change = sale.Change,
                Details = details.Select(d => new SaleDetailViewModel
                {
                    Id = d.Id,
                    ItemId = d.ItemId ?? 0,
                    Qty = d.Qty ?? 0,
                    Price = d.Price ?? 0,
                    VatPercent = d.Vatp,
                    Total = d.Total ?? 0,
                    Discount = d.Discount ?? 0
                }).ToList()
            };
        }

        public async Task<IEnumerable<SaleListItemViewModel>> QuerySalesAsync(DateTime? from, DateTime? to, int? customerId, string? paymentMethod, CancellationToken ct = default)
        {
            var q = _db.TblSales.AsQueryable().Where(s => s.State == 0);
            if (from.HasValue) q = q.Where(s => s.Date.ToDateTime(TimeOnly.MinValue) >= from.Value.Date);
            if (to.HasValue) q = q.Where(s => s.Date.ToDateTime(TimeOnly.MinValue) <= to.Value.Date);
            if (customerId.HasValue) q = q.Where(s => s.CustomerId == customerId.Value);
            if (!string.IsNullOrEmpty(paymentMethod)) q = q.Where(s => s.PaymentMethod == paymentMethod);

            var list = await q
                .Select(s => new SaleListItemViewModel
                {
                    Id = s.Id,
                    Date = s.Date.ToDateTime(TimeOnly.MinValue),
                    InvoiceId = s.InvoiceId,
                    CustomerName = /* join with customer to get code/name */ _db.TblCustomers
                                .Where(c => c.Id == s.CustomerId)
                                .Select(c => (c.Code.ToString() + " - " + (c.Name ?? "")))
                                .FirstOrDefault() ?? string.Empty,
                    PaymentMethod = s.PaymentMethod,
                    Total = s.Total,
                    Vat = s.Vat,
                    Net = s.Net
                })
                .ToListAsync(ct);

            return list;
        }

        public async Task DeleteSaleAsync(int id, CancellationToken ct = default)
        {
            var sale = await _db.TblSales.FindAsync(new object[] { id }, ct);
            if (sale == null) throw new KeyNotFoundException($"Sale {id} not found");
            sale.State = -1;
            await _db.SaveChangesAsync(ct);

            // Also mark transactions
            var txs = _db.TblTransactions.Where(t => t.TransactionId == id).ToList();
            foreach (var t in txs) t.State = -1;
            await _db.SaveChangesAsync(ct);

            // Optionally call microservice
            await _micro.SendSaleTransactionAsync(id, ct);
        }

    }
}