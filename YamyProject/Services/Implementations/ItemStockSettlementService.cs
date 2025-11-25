namespace YamyProject.Services.Implementations
{
    public class ItemStockSettlementService: IItemStockSettlementService
    {// Services/ItemStockSettlementService.cs
        
            private readonly YamyDbContext _db;
            private readonly IMapper _mapper;
            private readonly ILogger<ItemStockSettlementService> _logger;

            public ItemStockSettlementService(YamyDbContext db, IMapper mapper, ILogger<ItemStockSettlementService> logger)
            {
                _db = db;
                _mapper = mapper;
                _logger = logger;
            }

            // Implements your SQL logic:
            // - defaultMode == true: group by settlement and return JV No = concat('000', MAX(transaction.transaction_id))
            // - defaultMode == false: return settlement rows with individual detail rows (items)
        public async Task<IEnumerable<ItemStockSettlementListVm>> GetSettlementsAsync(string defaultMode = "Default")
            {
                // base query
                 var q = _db.TblItemStockSettlements 
                           .Where(s => s.State == 0); // replicate WHERE tbl_item_stock_settlement.state = 0

            if (defaultMode == "Default")
                {
                    // Group and select: SN and JV NO (max transaction id)
                    var list = await q
                        .Select(s => new
                        {
                            s.Id,
                            s.Code,
                            Date = s.Date,
                            MaxTxn = s.Transactions
                            .AsEnumerable() // bring to memory
                            .OrderByDescending(t =>t.TransactionId) // numeric order
                            .Select(t => t.TransactionId).FirstOrDefault()
                        })
                        .AsNoTracking()
                        .ToListAsync();

                    return list.Select(x => new ItemStockSettlementListVm
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Date = x.Date.HasValue ? x.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        JvNo =  $"000{x.MaxTxn}"
                    });
                }
                else
                {
                    // Return settlements and summary fields; items count can be computed
                    var list = await q
                        .Select(s => new
                        {
                            s.Id,
                            s.Code,
                            Date = s.Date,
                            ItemsCount = s.Details.Count()
                        })
                        .AsNoTracking()
                        .ToListAsync();

                    return list.Select(x => new ItemStockSettlementListVm
                    {
                        Id = x.Id,
                        Code = x.Code,
                        Date = x.Date.HasValue ? x.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        ItemsCount = x.ItemsCount
                    });
                }
            }
        public async Task<IEnumerable<ItemStockSettlementListVm>> GetSettlementsAsync(DateTime? from = null, DateTime? to = null, string defaultMode = "Default")
            {
            // Base query: active settlements only
            var query = _db.TblItemStockSettlements
                           .AsNoTracking()
                           .Where(s => s.State == 0);

            // Apply optional date filtering
            if (from.HasValue)
            {
                var fromDateOnly = DateOnly.FromDateTime(from.Value.Date);
                query = query.Where(s => (s.Date ?? DateOnly.MinValue) >= fromDateOnly);
            }

            if (to.HasValue)
            {
                var toDateOnly = DateOnly.FromDateTime(to.Value.Date);
                query = query.Where(s => (s.Date ?? DateOnly.MinValue) <= toDateOnly);
            }
            if (defaultMode == "Default")
            {
                var settlements = await query
                 .Select(s => new ItemStockSettlementListVm
                 {
                     Id = s.Id,
                     Code = s.Code,
                     Date = s.Date.HasValue ? s.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                     JvNo = "000" + s.Transactions
                                  .OrderByDescending(t => t.TransactionId)
                                  .Select(t => t.TransactionId)
                                  .FirstOrDefault(),
                     ItemsCount = s.Details.Count
                 })
                 .OrderBy(s => s.Date)
                 .ToListAsync();

                return settlements;
            }          
                // Projection for Detailed mode
                var detailedSettlements = await query
                    .Select(s => new ItemStockSettlementListVm
                    {
                        Id = s.Id,
                        Code = s.Code,
                        Date = s.Date.HasValue ? s.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                        WarehouseId = s.WarehouseId,
                        TotalPlus = s.TotalPlus,
                        TotalMinus = s.TotalMinus,
                        JvNo = "000" + s.Transactions
                                     .OrderByDescending(t => t.TransactionId)
                                     .Select(t => t.TransactionId)
                                     .FirstOrDefault(),
                        ItemsCount = s.Details.Count
                    })
                    .OrderBy(s => s.Date)
                    .ToListAsync();
               return detailedSettlements;
           }
        public async Task<ItemStockSettlementDetailsVm?> GetSettlementDetailsAsync(int id)
            {
                var entity = await _db.TblItemStockSettlements
                    .Include(s => s.Details)
                        .ThenInclude(d => d.Item) // assume navigation property 'Item' to TblItem
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (entity == null) return null;

                var vm = new ItemStockSettlementDetailsVm
                {
                    Id = entity.Id,
                    Code = entity.Code,
                    Date = entity.Date.HasValue ? entity.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    TotalPlus = entity.TotalPlus,
                    TotalMinus = entity.TotalMinus,
                    WarehouseId = entity.WarehouseId,
                    Items = entity.Details.Select(d => new ItemStockSettlementItemVm
                    {
                        Id = d.Id,
                        ItemId = (int)d.ItemId,
                        ItemCode = d.Item?.Code,
                        ItemName = d.Item?.Name,
                        OnHand = d.OnHand,
                        Price = d.Price,
                        NewOnHand = d.NewOnHand,
                        MinusAmount = d.MinusAmount,
                        PlusAmount = d.PlusAmount
                    }).ToList()
                };
          
            return vm;
            }
        public async Task<CreateUpdateSettlementVm> GetCreateUpdateSettlementVmAsync()
        {
            var warehousesEntity = await _db.TblWarehouses
                .AsNoTracking()
                .ToListAsync();

            var warehousesVm = warehousesEntity
                .Select(w => new WarehouseViewModel
                {
                    Id = w.Id,
                    Name = w.Code + " - " + w.Name
                })
                .ToList();

            return new CreateUpdateSettlementVm
            {
                warehouse = warehousesEntity,   // raw entity list
                WarehousesVm = warehousesVm,    // formatted list
            };
        }
        public async Task<int> CreateSettlementAsync(CreateUpdateSettlementVm model, int currentUserId)
            {
                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    // main entity

                    var entity = new TblItemStockSettlement
                    {
                        Code = model.Code,
                        Date = model.Date,
                        WarehouseId = model.WarehouseId,
                        CreatedBy = currentUserId,
                        CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        State = 0
                    };

                    _db.TblItemStockSettlements.Add(entity);
                    await _db.SaveChangesAsync();

                    // details
                    foreach (var d in model.Items)
                    {
                        var detail = new TblItemStockSettlementDetail
                        {
                            SettleId = entity.Id,
                            ItemId = d.ItemId,
                            OnHand = d.OnHand,
                            Price = d.Price,
                            NewOnHand = d.NewOnHand,
                            MinusAmount = (int)d.MinusAmount,
                            PlusAmount = (int)d.PlusAmount
                        };
                        _db.TblItemStockSettlementDetails.Add(detail);
                    }

                    await _db.SaveChangesAsync();

                    await tx.CommitAsync();
                    return entity.Id;
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error creating settlement");
                    throw;
                }
            }
        public async Task UpdateSettlementAsync(int id, CreateUpdateSettlementVm model, int currentUserId)
            {
                using var tx = await _db.Database.BeginTransactionAsync();
                try
                {
                    var entity = await _db.TblItemStockSettlements
                        .Include(s => s.Details)
                        .FirstOrDefaultAsync(s => s.Id == id);

                    if (entity == null) throw new KeyNotFoundException("Settlement not found");

                    entity.Code = model.Code;
                    entity.Date = model.Date;
                    entity.ModifiedBy = currentUserId;
                    entity.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);

                    // sync details: naive approach - delete missing, update existing, add new
                    var incomingIds = model.Items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
                    var toDelete = entity.Details.Where(d => !incomingIds.Contains(d.Id)).ToList();
                    _db.TblItemStockSettlementDetails.RemoveRange(toDelete);

                    foreach (var item in model.Items)
                    {
                        if (item.Id.HasValue)
                        {
                            var det = entity.Details.First(d => d.Id == item.Id.Value);
                            det.ItemId = item.ItemId;
                            det.OnHand = item.OnHand;
                            det.Price = item.Price;
                            det.NewOnHand = item.NewOnHand;
                            det.MinusAmount = (int)item.MinusAmount;
                            det.PlusAmount = (int)item.PlusAmount;
                        }
                        else
                        {
                            _db.TblItemStockSettlementDetails.Add(new TblItemStockSettlementDetail
                            {
                                SettleId = entity.Id,
                                ItemId = item.ItemId,
                                OnHand = item.OnHand,
                                Price = item.Price,
                                NewOnHand = item.NewOnHand,
                                MinusAmount = (int)item.MinusAmount,
                                PlusAmount = (int)item.PlusAmount
                            });
                        }
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();
                    _logger.LogError(ex, "Error updating settlement {Id}", id);
                    throw;
                }
            }
        public async Task DeleteSettlementAsync(int id, int currentUserId)
            {
                var entity = await _db.TblItemStockSettlements.FindAsync(id);
                if (entity == null) throw new KeyNotFoundException("Settlement not found");

                // soft delete pattern suggested - set state to something else
                entity.State = 2; // e.g., 2 = deleted
                entity.ModifiedBy = currentUserId;
                entity.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                await _db.SaveChangesAsync();
            }
        public async Task<List<TblItem>> GetItemsByCodeAsync(string code, int warehouseId)
        {           
            return await _db.TblItems
                .Where(i => i.WarehouseId == warehouseId
                            && i.State == 0
                            && i.Active == 0
                            && i.Type.Contains("Inventory Part")
                            && i.Code.Contains(code))
                .OrderBy(i => i.Code)
                .Take(20)
                .ToListAsync();
        }
        public async Task<List<TblItem>> GetItemsByNameAsync(string name, int warehouseId)
        {
            return await _db.TblItems
                .Where(i => i.WarehouseId == warehouseId
                            && i.State == 0
                            && i.Active == 0
                            && i.Type.Contains("Inventory Part")
                            && i.Name.Contains(name))
               // .Include(t=>t.)
                .OrderBy(i => i.Name)
                .Take(20)
                .ToListAsync();
        }
        public async Task<List<ItemViewModel>> GetItemsAsync(string name, int warehouseId)
        {
            var query = from i in _db.TblItems
                       where( i.Name == name || i.Code==name) && i.WarehouseId == warehouseId
                        select new ItemViewModel
                        {
                            Id = i.Id,
                            Method = i.Method,
                            Type = i.Type,
                            Code = i.Code,
                            CostPrice = i.CostPrice,
                            Name = i.Name,
                            Qty = _db.TblItemTransactions
                                   .Where(t => t.ItemId == i.Id && t.WarehouseId == warehouseId)
                                   .Sum(t => (decimal?)(t.QtyIn - t.QtyOut)) ?? 0
                        };

            return await query.ToListAsync();
        }
        public async Task<List<ItemStockSettlementListVm>> GetSettlementItemsAsync(int settleId)
        {
            var data = await _db.TblItemStockSettlementDetails
               .Include(d => d.Item)          // include related item
               .Include(d => d.Settlement)    // include settlement if you need Code, Date, etc.
               .Where(d => d.SettleId == settleId && d.Settlement.WarehouseId==1)
               .ToListAsync();

            var result = data.Select(d => new ItemStockSettlementListVm
            {
                Id = d.Id,
                ItemName = d.Item != null ? $"{d.Item.Code} - {d.Item.Name}" : "",
                Quantity = d.Qty?.ToString() ?? "0",          // convert decimal? to string
                CostPrice = d.Price?.ToString("0.00") ?? "0",
                NewOnHand = d.NewOnHand?.ToString("0.00") ?? "0",
                TotalMinus=d.Minusamount,
                TotalPlus=d.Plusamount,

              }).ToList();
            return result;
        }
    }
}