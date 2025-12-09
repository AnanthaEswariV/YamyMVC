namespace YamyProject.Services.Implementations
{
    public class ItemStockSettlementService(YamyDbContext db, IMapper mapper, ILogger<ItemStockSettlementService> logger, IHttpContextAccessor httpContextAccessor
        , IListServices listServices, IGlobalService GlobalService) : IItemStockSettlementService
        {
        private readonly YamyDbContext _context = db;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<ItemStockSettlementService> _logger = logger;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;
        private readonly IListServices _ListServices = listServices;

        public async Task<IEnumerable<ItemStockSettlementListVm>> GetSettlementsAsync(string defaultMode = "Default")
            {
            // base query
            var q = _context.TblItemStockSettlements
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
                        .OrderByDescending(t => t.TransactionId) // numeric order
                        .Select(t => t.TransactionId).FirstOrDefault()
                        })
                    .AsNoTracking()
                    .ToListAsync();

                return list.Select(x => new ItemStockSettlementListVm
                    {
                    Id = x.Id,
                    Code = x.Code,
                    Date = x.Date.HasValue ? x.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    JvNo = $"000{x.MaxTxn}"
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
            var query = _context.TblItemStockSettlements
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
            var entity = await _context.TblItemStockSettlements
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
        public async Task<string> GenerateNextCode()
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue = await _context.TblItemStockSettlements
               .Select(s => s.Code.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;
            return $"SIS-{int.Parse(prefix) + 1:D4}";
            }

        public async Task<CreateUpdateSettlementVm> GetCreateUpdateSettlementVmAsync()
            {
            var warehousesEntity = await _context.TblWarehouses
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
        public async Task<int> CreateSettlementAsync(CreateUpdateSettlementVm model)
            {
            var code = await GenerateNextCode();
            using var tx = await _context.Database.BeginTransactionAsync();
            try
                {
                // main entity
                if (string.IsNullOrEmpty(model.Code) || model.Code != code)
                    {
                    model.Code = code;
                    }
                var entity = new TblItemStockSettlement
                    {
                    Code = model.Code,
                    Date = model.Date,
                    WarehouseId = model.WarehouseId,
                    CreatedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,
                    CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    State = 0
                    };

                _context.TblItemStockSettlements.Add(entity);
                await _context.SaveChangesAsync();

                // details
                foreach (var Item in model.Items)
                    {
                    var detail = new TblItemStockSettlementDetail
                        {
                        SettleId = entity.Id,
                        ItemId = Item.ItemId,
                        OnHand = Item.OnHand,
                        Price = Item.Price,
                        NewOnHand = Item.NewOnHand,
                        MinusAmount = (int)Item.MinusAmount,
                        PlusAmount = (int)Item.PlusAmount
                        };
                    _context.TblItemStockSettlementDetails.Add(detail);
                    await InsertItemTransactionAsync(Item, (DateOnly)model.Date, (int)model.Id, model.Code);

                    await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Insert Stock Settlement Item", "Stock Settlement", entity.Id,
                         "Inserted Item: " + Item.ItemId + " in Stock Settlement: " + model.Code);
                    }
                await addTransaction(model, entity.Id, model.Code);
                await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Insert Stock Settlement", "Stock Settlement",
                  (int)model.Id, "Inserted Stock Settlement: " + model.Code);

                await _context.SaveChangesAsync();

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
        public async Task UpdateSettlementAsync(int id, CreateUpdateSettlementVm model)//, int currentUserId)
            {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
                {
                var entity = await _context.TblItemStockSettlements
                    .Include(s => s.Details)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (entity == null) throw new KeyNotFoundException("Settlement not found");

                entity.Code = model.Code;
                entity.Date = model.Date;
                entity.ModifiedBy = -_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                entity.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);

                var incomingIds = model.Items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
                var toDelete = entity.Details.Where(d => !incomingIds.Contains(d.Id)).ToList();
                _context.TblItemStockSettlementDetails.RemoveRange(toDelete);
               await ReturnItemsToInventoryAsync(id);
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
                        _context.TblItemStockSettlementDetails.Add(new TblItemStockSettlementDetail
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
                    await InsertItemTransactionAsync(item, (DateOnly)model.Date, (int)model.Id, model.Code);
                    await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Insert Stock Settlement Item", "Stock Settlement", entity.Id,
                       "Inserted Item: " + item.ItemId + " in Stock Settlement: " + model.Code);
                    }
                await _context.TblTransactions.Where(t => t.TransactionId == id &&
                  EF.Functions.Like(t.Description, "Stock Inventory Settlement%")).ExecuteDeleteAsync();
                await addTransaction(model, (int)model.Id, model.Code);

                await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Update Stock Settlement", "Stock Settlement",
                    (int)model.Id, "Update Stock Settlement: " + model.Code);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                }
            catch (Exception ex)
                {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error updating settlement {Id}", id);
                throw;
                }
            }
        public async Task DeleteSettlementAsync(int id)
            {
            var entity = await _context.TblItemStockSettlements.FindAsync(id);
            if (entity == null) throw new KeyNotFoundException("Settlement not found");

            // soft delete pattern suggested - set state to something else
            entity.State = 2; // e.g., 2 = deleted
            entity.ModifiedBy = -_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
            entity.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
            await _context.SaveChangesAsync();
            }
        public async Task<List<TblItem>> GetItemsByCodeAsync(string code, int warehouseId)
            {
            return await _context.TblItems
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
            return await _context.TblItems
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
            var query = from i in _context.TblItems
                        where (i.Name == name || i.Code == name) && i.WarehouseId == warehouseId
                        select new ItemViewModel
                            {
                            Id = i.Id,
                            Method = i.Method,
                            Type = i.Type,
                            Code = i.Code,
                            CostPrice = i.CostPrice,
                            Name = i.Name,
                            Qty = _context.TblItemTransactions
                                   .Where(t => t.ItemId == i.Id && t.WarehouseId == warehouseId)
                                   .Sum(t => (decimal?)(t.QtyIn - t.QtyOut)) ?? 0
                            };

            return await query.ToListAsync();
            }
        public async Task<List<ItemStockSettlementListVm>> GetSettlementItemsAsync(int settleId)
            {
            var data = await _context.TblItemStockSettlementDetails
               .Include(d => d.Item)          // include related item
               .Include(d => d.Settlement)    // include settlement if you need Code, Date, etc.
               .Where(d => d.SettleId == settleId && d.Settlement.WarehouseId == 1)
               .ToListAsync();

            var result = data.Select(d => new ItemStockSettlementListVm
                {
                Id = d.Id,
                ItemName = d.Item != null ? $"{d.Item.Code} - {d.Item.Name}" : "",
                Quantity = d.Qty?.ToString() ?? "0",          // convert decimal? to string
                CostPrice = d.Price?.ToString("0.00") ?? "0",
                NewOnHand = d.NewOnHand?.ToString("0.00") ?? "0",
                TotalMinus = d.Minusamount,
                TotalPlus = d.Plusamount,

                }).ToList();
            return result;
            }
        public async Task addTransaction(CreateUpdateSettlementVm Model, int invId, string Code)
            {
            var stockSettleAccount = await _ListServices.DefaultAccountsSet("Stock Settlement");
            var inventoryAccount = await _ListServices.DefaultAccountsSet("Inventory");
            // var level4VatId = await _ListServices.DefaultAccountsSet("Vat Output");
            if (Model.Items.Sum(x => x.MinusAmount) > 0)
                { await addTransactionEntry((DateOnly)Model.Date, stockSettleAccount, Model.Items.Sum(x => x.MinusAmount), 0, invId, invId, "Stock Inventory Settlement", "", "Stock Inventory Settlement NO. " + Code,
              _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow), Code);
                await addTransactionEntry((DateOnly)Model.Date, inventoryAccount, 0, Model.Items.Sum(x => x.MinusAmount), invId, invId, "Inventory Settlement", "", "Vat Output  Note NO. " + Code,
              _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow), Code);
                }
            else if (Model.Items.Sum(x => x.PlusAmount) > 0)
                {
                await addTransactionEntry((DateOnly)Model.Date, inventoryAccount, Model.Items.Sum(x => x.PlusAmount), 0, invId, 0, "Inventory Settlement", "", "Inventory Settlement NO.  " + Code,
              _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow), Code);
                await addTransactionEntry((DateOnly)Model.Date, stockSettleAccount, 0, Model.Items.Sum(x => x.PlusAmount), invId, 0, "Stock Inventory Settlement", "", "Stock Inventory Settlement NO. " + Code,
              _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow), Code); }
            }


        public async Task addTransactionEntry(DateOnly date, int? accountId, decimal debit, decimal credit, int transactionId, int humId, string type, string voucher_name,
    string description, int createdBy, DateOnly createdDate, string VoucherNo)
            {
            var trx = new TblTransaction
                {
                Date = date,
                AccountId = accountId,
                Debit = debit,
                Credit = credit,
                TransactionId = transactionId,
                HumId = humId,
                TType = voucher_name,
                Type = type,
                Description = description,
                CreatedBy = createdBy,
                VoucherNo = voucher_name,
                CreatedDate = createdDate,
                State = 0
                };

            _context.TblTransactions.Add(trx);
            await _context.SaveChangesAsync();
            }

    //    public async Task InsertItemTransaction(CreateUpdateSettlementItemVm item, DateOnly date, string invId, string invCode,decimal qty, CancellationToken ct)
    //{
    //    decimal remainingQty = qty;

    //    var layers = await _context.TblItemTransactions
    //        .Where(t => t.ItemId == item.ItemId && t.QtyInc > 0)
    //        .OrderBy(t => t.Id) // FIFO
    //        .ToListAsync(ct);

    //    foreach (var layer in layers)
    //    {
    //        if (remainingQty <= 0)
    //            break;

    //        decimal availableQty = (decimal)layer.QtyInc;
    //        if (availableQty <= 0)
    //            continue;

    //        decimal qtyToUse = Math.Min(remainingQty, availableQty);
    //    remainingQty -= qtyToUse;

    //        // Insert outgoing transaction
    //        var trx = new TblItemTransaction
    //            {
    //            Date = date,
    //            Type = "Stock Settlement",
    //            Reference = invId.ToString(),
    //            ItemId = item.ItemId,
    //            CostPrice = item.Price, 
    //            SalesPrice=0,
    //            QtyIn = 0,
    //            QtyOut = qtyToUse,
    //            QtyInc = 0,
    //            Description = "Stock Settlement No. " + invCode,
    //            WarehouseId = 0
    //            };
    //    _context.TblItemTransactions.Add(trx);

    //        // Update qty_inc of that layer
    //        layer.QtyInc -= qtyToUse;
    //    }

    //    await _context.SaveChangesAsync(ct);
    //    }





        public async Task InsertItemTransactionAsync(CreateUpdateSettlementItemVm item, DateOnly date, int invId, string invCode, CancellationToken ct = default)
            {
            if (item.ItemId <= 0)
                throw new ArgumentException("Invalid Item ID.");

            if (item.NewOnHand <= 0)
                throw new ArgumentException("Invalid Quantity.");
            decimal qty;
            if (item.NewOnHand < item.OnHand)
                {
                 qty = item.OnHand - item.NewOnHand;
                if (string.Equals(item.Method, "fifo", StringComparison.OrdinalIgnoreCase))
                    {    
                        await ProcessFifoAsync(item, date, invId, invCode, qty, ct);
                    }
                else if (string.Equals(item.Method, "lifo", StringComparison.OrdinalIgnoreCase))
                    {
                        await ProcessLifoAsync(item, date, invId, invCode, qty, ct);
                    }
                else
                    {
                        await ProcessAverageAsync(item, date, invId, invCode, qty, ct);
                    }
                }
            else
                {
                qty = item.NewOnHand - item.OnHand; // how much we are increasing

                // This matches your last else block
                var trx = new TblItemTransaction
                    {
                    Date = date,
                    Type = "Stock Settlement",          // "Stock Settlement"
                    Reference = invId.ToString(),            // adjust to your column
                    ItemId = item.ItemId,
                    CostPrice = item.Price,
                    QtyIn = qty,                         // stock in
                    QtyOut = 0,
                    QtyInc = 0,
                   // QtyDec = qty,                         // or 0 depending on your schema
                    Description = "Stock Settlement No. " + invCode,
                    WarehouseId = 0
                    };

                _context.TblItemTransactions.Add(trx);
                await _context.SaveChangesAsync(ct);
                await AddItemCardDetailsAsync(date, "Stock Settlement", invId.ToString(), (int)item.Id, item.Price,
qty, 0, 0, qty, "Stock Settlement No. " + invCode, 0);
                }
            }


        private async Task ProcessFifoAsync(CreateUpdateSettlementItemVm item,DateOnly date,int invId,string invCode,decimal qty,CancellationToken ct)
            {
            decimal remainingQty = qty;

            var layers = await _context.TblItemTransactions
                .Where(t => t.ItemId == item.ItemId && t.QtyInc > 0)
                .OrderBy(t => t.Id) // FIFO
                .ToListAsync(ct);

            foreach (var layer in layers)
                {
                if (remainingQty <= 0)
                    break;

                decimal availableQty = (decimal)layer.QtyInc;
                if (availableQty <= 0)
                    continue;

                decimal qtyToUse = Math.Min(remainingQty, availableQty);
                remainingQty -= qtyToUse;

                // Insert outgoing transaction
                var trx = new TblItemTransaction
                    {
                    Date = date,
                    Type = "Stock Settlement",
                    Reference = invId.ToString(),
                    ItemId = item.ItemId,
                    CostPrice = item.Price, 
                    QtyIn = 0,
                    QtyOut = qtyToUse,
                    QtyInc = 0,
                   // QtyDec = qtyToUse,
                    Description = "Stock Settlement No. " + invCode,
                    WarehouseId= 0
                    };
                _context.TblItemTransactions.Add(trx);

                // Update qty_inc of that layer
                layer.QtyInc -= qtyToUse;
                }

            await _context.SaveChangesAsync(ct);
            UpdateOnHandItemAsync((int)item.Id);
            await AddItemCardDetailsAsync(date,"Stock Settlement",invId.ToString(),(int)item.Id,item.Price,
    0,0, qty, 0,"Stock Settlement No. " + invCode,0);

            }

        private async Task ProcessLifoAsync(CreateUpdateSettlementItemVm item,DateOnly date,int invId,string invCode,decimal qty,CancellationToken ct)
            {
            decimal remainingQty = qty;

            var layers = await _context.TblItemTransactions
                .Where(t => t.ItemId == item.ItemId && t.QtyInc > 0)
                .OrderByDescending(t => t.Id) // LIFO
                .ToListAsync(ct);

            foreach (var layer in layers)
                {
                if (remainingQty <= 0)
                    break;

                decimal availableQty =(decimal) layer.QtyInc;
                if (availableQty <= 0)
                    continue;

                decimal qtyToUse = Math.Min(remainingQty, availableQty);
                remainingQty -= qtyToUse;

                var trx = new TblItemTransaction
                    {
                    Date = date,
                    Type = "Stock Settlement",
                    Reference = invId.ToString(),
                    ItemId = item.ItemId,
                    CostPrice = item.Price, // or layer.CostPrice
                    QtyIn = 0,
                    QtyOut = qtyToUse,
                    QtyInc = 0,
                    //QtyDec = qtyToUse,
                    Description = "Stock Settlement No. " + invCode,
                    WarehouseId = 0
                    };
                _context.TblItemTransactions.Add(trx);

                layer.QtyInc -= qtyToUse;
                }

            await _context.SaveChangesAsync(ct);
            UpdateOnHandItemAsync((int)item.Id);
            await AddItemCardDetailsAsync(date, "Stock Settlement", invId.ToString(), (int)item.Id, item.Price,
0, 0, qty, 0, "Stock Settlement No. " + invCode, 0);
            }

        private async Task ProcessAverageAsync(CreateUpdateSettlementItemVm item,DateOnly date,int invId,string invCode,decimal qty,CancellationToken ct)
            {
            // Average cost: SUM(qty_in * cost_price) / SUM(qty_in)
            var avgInfo = await _context.TblItemTransactions
                .Where(t => t.ItemId == item.ItemId && t.QtyIn > 0)
                .GroupBy(t => t.ItemId)
                .Select(g => new
                    {
                    SumQty = g.Sum(x => x.QtyIn),
                    SumCost = g.Sum(x => x.QtyIn * x.CostPrice)
                    })
                .FirstOrDefaultAsync(ct);

            decimal avgCost = (decimal)item.Price;
            if (avgInfo != null && avgInfo.SumQty > 0)
                {
                avgCost = (decimal)(avgInfo.SumCost / avgInfo.SumQty);
                }

            var trx = new TblItemTransaction
                {
                Date = date,
                Type = "Stock Settlement",
                Reference = invId.ToString(),
                ItemId = item.ItemId,
                CostPrice = avgCost,
                QtyIn = 0,
                QtyOut = qty,
                QtyInc = 0,
                //QtyDec = qty,
                Description = "Stock Settlement No. " + invCode,
                WarehouseId= 0
                };

            _context.TblItemTransactions.Add(trx);
            await _context.SaveChangesAsync(ct);
            UpdateOnHandItemAsync((int)item.Id);
            await AddItemCardDetailsAsync(date, "Stock Settlement", invId.ToString(), (int)item.Id, item.Price,
0, item.Price, qty, 0, "Stock Settlement No. " + invCode, 0);
            }

        public async Task UpdateOnHandItemAsync(int itemId, CancellationToken ct = default)
            {
            // 1) Calculate total on-hand = SUM(qty_in - qty_out)
            var totalOnHand = await _context.TblItemTransactions
                .Where(t => t.ItemId == itemId)
                .SumAsync(
                    t => (decimal?)(t.QtyIn - t.QtyOut), // nullable so SumAsync works on empty set
                    ct
                ) ?? 0m;

            // 2) Load the item and update its OnHand field
            var item = await _context.TblItems
                .FirstOrDefaultAsync(i => i.Id == itemId, ct);

            if (item == null)
                return; // or throw if you prefer

            item.OnHand = totalOnHand;

            await _context.SaveChangesAsync(ct);
            }

        public async Task AddItemCardDetailsAsync(DateOnly date,string type,string reference,int itemId,decimal costPrice,decimal qtyIn,decimal salesPrice,decimal qtyOut,
            decimal qtyInc, string description,int warehouseId,CancellationToken ct = default)
            {
            var invoiceNo = "INV-" + reference;
            var transNo = reference;
            var transType = type;

            decimal debit = 0m;
            decimal credit = 0m;

            // Calculate debit / credit amounts based on qty * costPrice
            if (qtyIn > 0)
                debit = qtyIn * costPrice;

            if (qtyOut > 0)
                credit = qtyOut * costPrice;

            // ===== Previous QtyBalance: SUM(qty_in - qty_out) for this item =====
            var previousQtyBalance = await _context.TblItemCardDetails
                .Where(c => c.ItemId == itemId)
                .SumAsync(
                    c => (decimal?)(c.QtyIn - c.QtyOut),
                    ct
                ) ?? 0m;

            var qtyBalance = previousQtyBalance + (qtyIn - qtyOut);

            // ===== Previous Balance: SUM(debit - credit) for this item =====
            var previousBalance = await _context.TblItemCardDetails
                .Where(c => c.ItemId == itemId)
                .SumAsync(
                    c => (decimal?)(c.Debit - c.Credit),
                    ct
                ) ?? 0m;

            var balance = previousBalance + (debit - credit);

            // fifoQty / fifoCost are 0 in your original code
            decimal fifoQty = 0m;
            decimal fifoCost = 0m;

            // INSERT INTO tbl_item_card_details ...
            var cardDetail = new TblItemCardDetail
                {
                // Adjust property names to match your entity exactly:
                ItemId = itemId,
                Date = date,                // DateOnly column
                WharehouseId = warehouseId,         // if your column is wharehouse_id, property might be WharehouseId
                InvNo = invoiceNo,
                TransNo = int.Parse(transNo),
                TransType = transType,
                Description = description,
                Price = costPrice,
                QtyIn = qtyIn,
                QtyOut = qtyOut,
                QtyBalance = qtyBalance,
                Debit = debit,
                Credit = credit,
                Balance = balance,
                FifoQty = fifoQty,
                FifoCost = fifoCost
                };

            _context.TblItemCardDetails.Add(cardDetail);
            await _context.SaveChangesAsync(ct);
            }

        public async Task ReturnItemsToInventoryAsync(int settleId, CancellationToken ct = default)
            {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(ct);

                var details = await _context.TblItemStockSettlementDetails
                    .Include(d => d.Item) 
                    .Where(d => d.SettleId == settleId)
                    .ToListAsync(ct);

                foreach (var detail in details)
                    {
                    var qty = detail.Qty;         
                    var item = detail.Item;      

                    if (qty > 0)
                        {
                        item.OnHand -=(decimal) qty;

                        await _context.TblItemTransactions
                            .Where(t => t.Reference == settleId.ToString()
                                        && t.Type == "Stock Inventory Settlement")
                            .ExecuteDeleteAsync(ct);
                        }
                    else
                        {
                        var absQty = -qty;

                        item.OnHand += (decimal)absQty;

                        await _context.TblItemTransactions
                            .Where(t => t.Reference == settleId.ToString()
                                        && t.Type == "Stock Inventory Settlement")
                            .ExecuteDeleteAsync(ct);

                        var method = item.Method?.ToLower();

                        if (method != "agv")
                            {
                            var layers = await _context.TblItemTransactions
                                .Where(t => t.ItemId == detail.ItemId
                                            && t.QtyIn != t.QtyInc
                                            && t.QtyOut == 0)
                                .OrderBy(t => t.Id)
                                .ToListAsync(ct);

                            var attempQty = absQty; 

                            foreach (var layer in layers)
                                {
                                if (attempQty <= 0)
                                    break;

                                var qtyInc = layer.QtyInc;
                                var qtyIn = layer.QtyIn;

                                if (qtyInc + attempQty <= qtyIn)
                                    {
                                    layer.QtyInc = qtyInc + attempQty;
                                    attempQty = 0;
                                    break;
                                    }
                                else
                                    {
                                    attempQty = attempQty - qtyIn - qtyInc;
                                    layer.QtyInc = qtyIn;
                                    }
                                }
                            }
                        }
                    _context.TblItemStockSettlementDetails.Remove(detail);
                    }
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            });
            }


        }
    }