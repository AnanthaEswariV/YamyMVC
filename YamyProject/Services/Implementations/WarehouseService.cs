namespace YamyProject.Services.Implementations
{
    public class WarehouseService: IWarehouseService
    {
        private readonly YamyDbContext _context;

        public WarehouseService(YamyDbContext context)
        {
            _context = context;
        }

        public async Task<List<SelectListItem>> GetWarehousesAsync()
        {
            var warehouses = await _context.TblWarehouses
                .AsNoTracking()
                .Select(w => new SelectListItem
                {
                    Value = w.Id.ToString(),
                    Text = w.Code + " - " + w.Name
                })
                .ToListAsync();

            return warehouses;
        }
        public async Task<TblCoaConfig?> GetByCategoryAsync(string category)
        {
            return await _context.TblCoaConfigs
                .FirstOrDefaultAsync(c => c.Category == category);
        }

        public async Task<int> GetInventoryAccountIdAsync()
        {
            var config = await GetByCategoryAsync("Inventory");
            if (config == null || config.AccountId == null)
                throw new InvalidOperationException("Inventory Account must be set first.");

            return config.AccountId.Value;
        }

        public async Task<int> GetStockSettlementAccountIdAsync()
        {
            var config = await GetByCategoryAsync("Stock Settlement");
            if (config == null || config.AccountId == null)
                throw new InvalidOperationException("Stock Settlement Account must be set first.");

            return config.AccountId.Value;
        }

        //----
        public async Task<string> GenerateNextCodeAsync()
        {
            var maxNumber = await _context.TblItemStockSettlements.Select(s=>s.Code).MaxAsync();
            return $"SIS-{maxNumber:D4}"; // Produces SIS-0001, SIS-0002, etc.
        }
        public async Task<TblItemStockSettlement> CreateAsync(TblItemStockSettlement settlement)
        {
            // Generate a new code
            settlement.Code = await GenerateNextCodeAsync();
            settlement.CreatedDate = DateOnly.FromDateTime(DateTime.Now);

            _context.TblItemStockSettlements.Add(settlement);
            await _context.SaveChangesAsync();
            return settlement;
        }
        public async Task<decimal> CreateAndGetIdAsync(TblItemStockSettlement settlement)
        {
            settlement.Code = await GenerateNextCodeAsync();
            settlement.CreatedDate = DateOnly.FromDateTime(DateTime.Now);
            settlement.State = 0; // default state

            _context.TblItemStockSettlements.Add(settlement);
            await _context.SaveChangesAsync();

            return settlement.Id; 
        }
        public async Task<int> CreateTransactionAsync(TblTransaction transaction)
        {
            transaction.CreatedDate = DateOnly.FromDateTime(DateTime.Now);
            transaction.State = 0; // default state

            _context.TblTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction.Id; // EF Core automatically populates the identity
        }

        public async Task<IEnumerable<StockSettlementDetailViewModel>> GetSettlementDetailsAsync(int settleId)
        {
            return await _context.TblItemStockSettlementDetails
                .Include(d => d.Item)
                .Where(d => d.SettleId == settleId)
                .Select(d => new StockSettlementDetailViewModel
                {
                    Id = d.Id,
                    SettleId = d.SettleId,
                    ItemCode = d.Item.Code,
                    ItemType = d.Item.Type,
                    Method = d.Item.Method,
                    OnHand = d.OnHand,
                    Price = d.Price,
                    NewOnHand = d.NewOnHand,
                    Qty = d.Qty,
                    MinusAmount = d.MinusAmount,
                    PlusAmount = d.PlusAmount
                })
                .ToListAsync();
        }

      
    }
}