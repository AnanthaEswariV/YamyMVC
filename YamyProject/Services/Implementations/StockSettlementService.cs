namespace YamyProject.Services.Implementations
{
    public class StockSettlementService: IStockSettlementService
    {
        private readonly YamyDbContext _context;

        public StockSettlementService(YamyDbContext context)
        {
            _context = context;
        }

        public async Task<CreateUpdateSettlementViewmodel> GetByIdAsync(int id)
        {
            var settlement = await _context.TblItemStockSettlements
                .Include(s => s.Details)
                .ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null) return new CreateUpdateSettlementViewmodel();

            return new CreateUpdateSettlementViewmodel
            {
                Id = settlement.Id,
                Code = settlement.Code ?? string.Empty,
                Date = settlement.Date ?? DateOnly.FromDateTime(DateTime.Now),
                WarehouseId = settlement.WarehouseId,
                Items = settlement.Details.Select(d => new StockSettlementItemVm
                {
                    Id = d.Id,
                    ItemId = d.ItemId ?? 0,
                    ItemCode = d.Item?.Code ?? string.Empty,
                    ItemName = d.Item?.Name ?? string.Empty,
                    OnHand = d.OnHand ?? 0,
                    Price = d.Price ?? 0,
                    NewOnHand = d.NewOnHand ?? 0
                }).ToList()
            };
        }

        public async Task<int> CreateAsync(CreateUpdateSettlementViewmodel model, int userId)
        {
            var settlement = new TblItemStockSettlement
            {
                Code = model.Code,
                Date = model.Date,
                WarehouseId = model.WarehouseId,
                CreatedBy = userId,
                CreatedDate = DateOnly.FromDateTime(DateTime.Now),
                State = 1,
                TotalMinus = model.TotalMinus,
                TotalPlus = model.TotalPlus,
                Details = model.Items.Select(i => new TblItemStockSettlementDetail
                {
                    ItemId = i.ItemId,
                    OnHand = i.OnHand,
                    Price = i.Price,
                    NewOnHand = i.NewOnHand,
                    MinusAmount = (int)i.MinusAmount,
                    PlusAmount = (int)i.PlusAmount
                }).ToList()
            };

            _context.TblItemStockSettlements.Add(settlement);
            await _context.SaveChangesAsync();
            return settlement.Id;
        }

        public async Task UpdateAsync(CreateUpdateSettlementViewmodel model, int userId)
        {
            var settlement = await _context.TblItemStockSettlements
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.Id == model.Id);

            if (settlement == null) throw new Exception("Settlement not found");

            settlement.Date = model.Date;
            settlement.WarehouseId = model.WarehouseId;
            settlement.ModifiedBy = userId;
            settlement.ModifiedDate = DateOnly.FromDateTime(DateTime.Now);
            settlement.TotalMinus = model.TotalMinus;
            settlement.TotalPlus = model.TotalPlus;

            // Remove existing details and add new
            _context.TblItemStockSettlementDetails.RemoveRange(settlement.Details);

            settlement.Details = model.Items.Select(i => new TblItemStockSettlementDetail
            {
                ItemId = i.ItemId,
                OnHand = i.OnHand,
                Price = i.Price,
                NewOnHand = i.NewOnHand,
                MinusAmount = (int)i.MinusAmount,
                PlusAmount = (int)i.PlusAmount
            }).ToList();

            await _context.SaveChangesAsync();
        }

        public async Task<List<TblWarehouse>> GetWarehousesAsync()
        {
            return await _context.TblWarehouses.ToListAsync();
        }
    }
}
