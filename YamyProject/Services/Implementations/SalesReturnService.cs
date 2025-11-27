namespace YamyProject.Services.Implementations
    {
    public class SalesReturnService(YamyDbContext context, IListServices listServices) : ISalesReturnService
        {

        private string Customer = null;
        private DateOnly Starting = default;
        private DateOnly Ending = default;
        private string PayMethod = null;
        private readonly YamyDbContext _context = context;
        private readonly IListServices _ListServices = listServices;
     

        public async Task<IEnumerable<SalesCenterViewModel>> GetSalesREturnAsync(string selectCustomer = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true)
            {
            if (Custmer == true)
                Customer = selectCustomer;
            if (Date == true)
                {
                if (From == default)
                    {
                    From = DateOnly.FromDateTime(DateTime.Today);
                    }
                if (To == default)
                    {
                    To = DateOnly.FromDateTime(DateTime.Today);
                    }

                Starting = From;
                Ending = To;
                }
            if (Pay == true)
                PayMethod = selectionMethodPay;

            if (selectionMethod == "Default")
                return await GetDefaultReportAsync();
            else
                return await GetDetailedReportAsync();
            }
        public async Task<IEnumerable<SalesCenterViewModel>> GetDefaultReportAsync()
            {
            //var transactions = await _context.TblTransactions.ToListAsync();

            var query = _context.TblSalesReturns
              .Where(s => s.State == 0)
              .Include(s => s.TblTransaction)
              .Include(s => s.Customer)
              .OrderBy(s => s.Date)
              .AsQueryable();

            // Apply Customer filter if provided
            if (!string.IsNullOrEmpty(Customer))
                query = query.Where(s => s.CustomerId == int.Parse(Customer) || s.Customer.Name == Customer);

            // Apply Starting date filter if provided
            if (Starting != default)
                query = query.Where(s => s.Date >= Starting);

            // Apply Ending date filter if provided
            if (Ending != default)
                query = query.Where(s => s.Date <= Ending);

            // Apply Payment Method filter if provided
            if (!string.IsNullOrEmpty(PayMethod))
                query = query.Where(s => s.PaymentMethod == PayMethod);

            const string Collation = "utf8mb4_0900_ai_ci"; // or your column’s exact collation

            var sales = await query
    .Select(s => new
        {
        s.Id,
        s.Date,
        InvNo = s.InvoiceId,
        TransactionId = (int?)(s.TblTransaction != null ? s.TblTransaction.TransactionId : (int?)null),
        CustomerName = s.Customer != null
            ? EF.Functions.Collate(s.Customer.Code + " - " + s.Customer.Name, Collation) : "",
        PayMethod = s.PaymentMethod,
        s.Total,
        s.Vat,
        s.Net
        })
    .GroupBy(x => new
        {
        x.Id,
        x.Date,
        x.InvNo,
        x.CustomerName,
        x.PayMethod,
        x.Total,
        x.Vat,
        x.Net
        })
    .Select(g => new
        {
        g.Key.Id,
        g.Key.Date,
        INV_NO = g.Key.InvNo,
        Jv_No = "000" + (g.Max(x => x.TransactionId) ?? 0),
        Customer_Name = g.Key.CustomerName,
        Payment_Method = g.Key.PayMethod,
        g.Key.Total,
        g.Key.Vat,
        g.Key.Net
        })
    .OrderBy(r => r.Date)
    .ToListAsync();
            var result = new List<SalesCenterViewModel>();

            int sn = 1;
            foreach (var s in sales)
                {
                result.Add(new SalesCenterViewModel
                    {
                    SN = sn,
                    Date = s.Date,
                    Id = s.Id,
                    InvoiceId = s.INV_NO,
                    CustomerName = s.Customer_Name,
                    PaymentMethod = s.Payment_Method,
                    Total = s.Total,
                    Vat = s.Vat,
                    Net = s.Net,
                    JvNo = s.Jv_No
                    });



                sn ++;
                }
            // result.ForEach(x => x.Customers = customerSelectList);
            return result;
            }
        public async Task<IEnumerable<SalesCenterViewModel>> GetDetailedReportAsync()
            {
            var query = _context.TblSalesReturns
                    .Where(s => s.State == 0)
                    .Include(s => s.SalesReturnDetai)
                    .Include(s => s.Customer)
                    .OrderBy(s => s.Date)
                    .AsQueryable();

            // Apply Customer filter if provided
            if (!string.IsNullOrEmpty(Customer))
                query = query.Where(s => s.CustomerId == int.Parse(Customer) || s.Customer.Name == Customer);

            // Apply Starting date filter
            if (Starting != default)
                query = query.Where(s => s.Date >= Starting);

            // Apply Ending date filter
            if (Ending != default)
                query = query.Where(s => s.Date <= Ending);

            // Apply Payment Method filter
            if (!string.IsNullOrEmpty(PayMethod))
                query = query.Where(s => s.PaymentMethod == PayMethod);

            const string Collation = "utf8mb4_0900_ai_ci"; // or your column’s exact collation

            // Project into anonymous type with customer and details
            var sales = await query
                .Select(s => new
                    {
                    Sale = s,
                    Details = _context.TblSalesReturnDetails
                                 .Where(sd => sd.SalesId == s.Id)
                                 .Select(sd => new
                                     {
                                     SalesDetails = sd,
                                     Item = _context.TblItems.FirstOrDefault(i => i.Id == sd.ItemId)
                                     }).ToList(),

                    CustomerName = s.Customer != null
                   ? EF.Functions.Collate(s.Customer.Code, Collation)
                     + " - "
                     + EF.Functions.Collate(s.Customer.Name, Collation)
                   : ""
                    })
                .ToListAsync();

            var flat = sales
    .OrderBy(x => x.Sale.Date) // for SN like ROW_NUMBER() OVER (ORDER BY date)
    .SelectMany(x => x.Details.Select(d => new
        {
        x.Sale.Date,
        x.Sale.Id,
        INV_NO = x.Sale.InvoiceId,
        Customer_Name = x.CustomerName,
        Payment_Method = x.Sale.PaymentMethod,
        x.Sale.Total,
        x.Sale.Vat,
        x.Sale.Net,
        Item_Name = (d.Item != null ? (d.Item.Code + " - " + d.Item.Name) : ""),
        d.SalesDetails.Qty,
        d.SalesDetails.Price,
        Item_Vat = d.SalesDetails.Vat,
        Item_Total = d.SalesDetails.Total
        }))
    // optional: group if you truly need it (mirrors SQL GROUP BY)
    .GroupBy(r => new
        {
        r.Date,
        r.Id,
        r.INV_NO,
        r.Customer_Name,
        r.Payment_Method,
        r.Total,
        r.Vat,
        r.Net,
        r.Item_Name,
        r.Qty,
        r.Price,
        r.Item_Vat,
        r.Item_Total
        })
    .Select(g => g.Key) // keys are the grouped rows
    .Select((r, idx) => new
        {
        SN = idx + 1,
        r.Date,
        r.Id,
        r.INV_NO,
        r.Customer_Name,
        r.Payment_Method,
        r.Total,
        r.Vat,
        r.Net,
        r.Item_Name,
        r.Qty,
        r.Price,
        r.Item_Vat,
        r.Item_Total
        })
    .ToList();

            var result = new List<SalesCenterViewModel>();
            int sn = 1;
            foreach (var s in sales)
                {
                foreach (var sd in s.Details)
                    {
                    result.Add(new SalesCenterViewModel
                        {
                        SN = sn++,
                        Date = s.Sale.Date,
                        Id = s.Sale.Id,
                        InvoiceId = s.Sale.InvoiceId,
                        CustomerName = s.CustomerName,
                        PaymentMethod = s.Sale.PaymentMethod,
                        Total = s.Sale.Total,
                        Vat = s.Sale.Vat,
                        Net = s.Sale.Net,
                        ItemName = sd.Item != null ? sd.Item.Code + " - " + sd.Item.Name : "",
                        Qty = sd.SalesDetails.Qty,
                        Price = sd.SalesDetails.Price,
                        ItemVat = sd.SalesDetails.Vat,
                        ItemTotal = sd.SalesDetails.Total
                        });
                    }
                sn ++;
                }
            return result;
            }
        public async Task<string> GenerateReturnInvoiceNoAsync()
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue =await _context.TblSales
               .Select(s => s.InvoiceId.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;

            return $"SRI-{int.Parse(prefix) + 1:D3}";
            }
        public async Task<SalesReturnViewModel> GetEditAsync(int id)
            {
            //var vm = new SalesReturnViewModel();
                var sale = await _context.TblSalesReturns
                                  .Include(s => s.SalesReturnDetai)
                                  .ThenInclude(d => d.Items)
                                  .Include(s => s.Customer)
                                  .FirstOrDefaultAsync(s => s.Id == id);
            
            var Warehouse = await _ListServices.GetWarehousesAsync();
            var WarehouseSelectList = Warehouse.Select(c => new TblWarehouse
                {
                Id = c.Id,
                Name = c.Name
                }).ToList();
             var Account = await _ListServices.GetAccountsAsync();
            var AccountList = Account.Select(c => new TblCoaLevel4
                {
                Id = c.Id,
                Name = c.Name
                }).ToList();
            var CostCenter = await _ListServices.GetCostCenterAsync();
            var CostCenterList = CostCenter.Select(c => new TblCostCenter
                {
                Id = c.Id,
                Code = c.Code
                }).ToList();
             var customers = await _ListServices.GetCustomersRetrunAsync();
            var customerSelectList = customers.Select(c => new TblCustomer
                {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name
                }).ToList();
            var vm = MapQuotationsToViewModel(sale);
           vm.Warehouses = WarehouseSelectList;
            vm.Accounts = AccountList;
            vm.CostCenter = CostCenterList;
            vm.Customers = customerSelectList;
          
            return vm;
            }
        private static SalesReturnViewModel MapQuotationsToViewModel(TblSalesReturn s)
            {
            // Coalesce details to an empty list to avoid NRE on OrderBy/Select
            var details = s.SalesReturnDetai ?? [];

            return new SalesReturnViewModel
                {
                CustomerId = s.CustomerId,
                CustomerCode = s.Customer?.Code,
                CustomerName = s.BillTo,
                Emirate = s.City,
                SalesMane = s.SalesMan,
                ShipDate = s.ShipDate ?? default,
                Ship = s.ShipTo,
                Val = s.ShipVia ?? string.Empty,
                AccountsId = s.AccountCashId,
                PaymentTerms = s.PaymentTerms,
                DueTo = s.PaymentDate,
                Date = s.Date,
                TotalBeforeVat = s.Total,
                TotalVat = s.Vat,
                TotaDiscount = 0,
                TotaAmount = s.Net,
                Items = [.. s.SalesReturnDetai
                    .OrderBy(d => d.Id)
                    .Select(d =>
                    {
                        // null-safe access for the navigation 'Items'
                        // If your schema has nullable decimals, coalesce to 0m as needed.
                        var qty = d.Qty;
                        var price = d.Price;
                        var disc = 0;
                        var vatAmt = d.Vatp;   // amount (keep as-is if that’s your schema)
                        var netPrice = (price * qty) - disc + vatAmt;

                        return new SalesReturnRowDataViewModel
                            {
                            Id = d.Id,
                            ItemId = d.ItemId ?? 0,                         // if non-nullable in DB, make model int (not int?) and remove ??
                            ItemCode = d.Items.Code ?? string.Empty,       // null-safe
                            ItemName = d.Items.Name ?? string.Empty,       // null-safe
                            Method = d.Items.Method ?? string.Empty,       // null-safe
                            Type = d.Items.Type ?? string.Empty,       // null-safe
                            QTY = d.Qty,
                            Price = d.Price,
                            Disc = 0,
                            NetPrice = netPrice,
                            VatPersint = d.Vat,                                  // percent (if that’s your schema)
                            VatAmonut = d.Vatp,                                 // amount
                            Amount = d.Total,
                            CostPrice = d.CostPrice,
                            WarehouseId = d.Items.WarehouseId,                  // if VM property is int? use: it?.WarehouseId
                            CostCenterId = d.CostCenterId                          // if nullable in DB, use: d.CostCenterId ?? 0
                            };
                    })]
                };
            }

        public async Task CreateSalesReturnInvoiceAsync(SalesReturnViewModel vm, int currentUserId)
            {
          
            // 1) Server-side totals
            decimal totalBeforeVat = 0m, totalVat = 0m, netAmount = 0m, totalDiscount = 0m;
            foreach (var r in vm.Items.Where(i => i != null))
                {
                if (r.ItemId != null && r.ItemId != 0)
                    {
                    var linePrice = r.Price * r.QTY;
                    var lineDisc = r.Disc;
                    var lineBase = linePrice - lineDisc;
                    var vatPct = (r.VatPersint ?? 0) / 100m;

                    var lineVat = (decimal)(lineBase * vatPct);
                    var lineTotal = lineBase + lineVat;

                    totalBeforeVat += (decimal)lineBase;
                    totalVat += lineVat;
                    totalDiscount += lineDisc;
                    netAmount += (decimal)lineTotal;
                    }
                }
            // 2) Generate invoice no (async)
            var invoiceNo = string.IsNullOrWhiteSpace(vm.Invoce)
                ? await GenerateReturnInvoiceNoAsync()
                : vm.Invoce.Trim();

            // 3) Strategy + Transaction
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    // 1) Header
                    var sale = new TblSalesReturn
                        {
                        Date = vm.Date,
                        CustomerId = vm.CustomerId ?? 0,
                        InvoiceId = invoiceNo,
                        WarehouseId = vm.WarehousesId ?? 0,
                        SalesRefId = vm.SalesRefId,
                        PoNum = vm.PONO ?? string.Empty,
                        BillTo = vm.CustomerName ?? string.Empty,
                        City = vm.Emirate ?? string.Empty,
                        SalesMan = vm.SalesMane ?? string.Empty,
                        ShipDate = vm.ShipDate,
                        ShipVia = vm.Val ?? string.Empty,
                        ShipTo = vm.Ship ?? string.Empty,
                        PaymentMethod = "Cash",
                        AccountCashId = vm.AccountsId ?? 0,
                        PaymentTerms = vm.PaymentTerms ?? string.Empty,
                        PaymentDate = vm.DueTo,
                        Total = totalBeforeVat,
                        Vat = totalVat,
                        Net = netAmount,
                        Pay =  netAmount,
                        Change =  0,
                        CreatedBy = currentUserId,
                        CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        State = 0,
                        Discount = totalDiscount,
                        Description = vm.Description ?? string.Empty
                        };
                    _context.TblSalesReturns.Add(sale);
                    await _context.SaveChangesAsync(); // need sale.Id
                    var saleId = sale.Id;
                    if (saleId == 0)
                        throw new InvalidOperationException("Failed to create sale record.");
                  
                    // 5) Details (batch add), then inventory moves per line
                    await InsertReturnInvItems(vm, saleId, invoiceNo);
                    // 6) Transfer flags + accounting entries
                //    await TransferSaleAsync(vm.InvoiceType ?? "", saleId, vm.PONO ?? "");
                    await Transaction(vm,
                        level4PaymentCreditMethodId: await _ListServices.DefaultAccountsSet("Customer"),
                       level4SalesInvoice:await _ListServices.DefaultAccountsSet("Sales"),
                        invid: saleId,
                        invoiceNo: vm.Invoce ?? invoiceNo,
                        level4VatId: await _ListServices.DefaultAccountsSet("Vat Output"));
                            

                    await tx.CommitAsync();
                    }
                catch
                    {
                    await tx.RollbackAsync();
                    throw;
                    }
            });
            }
        public async Task InsertReturnInvItems(SalesReturnViewModel vm, int saleId = 0, string invoiceNo = "")
            {
            var details = new List<TblSalesReturnDetail>(vm.Items.Count);
            saleId = saleId != 0 ? saleId : vm.Id;
            invoiceNo = !string.IsNullOrWhiteSpace(invoiceNo) ? invoiceNo : vm.Invoce;
            foreach (var r in vm.Items.Where(i => i != null))
                {
                if (r.ItemId != null && r.ItemId != 0)
                    {
                    var qty = r.QTY;
                    var price = r.Price;
                    var disc = r.Disc;
                    var vatPct = r.VatPersint ?? 0;
                    var lineBase = (price * qty) - disc;
                    var lineVat = (decimal)(lineBase * (vatPct / 100m));
                    var lineTot = lineBase + lineVat;

                    details.Add(new TblSalesReturnDetail
                        {
                        SalesId = saleId,
                        ItemId = r.ItemId,
                        Qty = qty,
                        CostPrice = r.CostPrice,
                        Price = price,
                        Discount = disc,
                        Vatp = lineVat,
                        Vat = (int)vatPct,
                        Total = lineTot,
                        CostCenterId = r.CostCenterId ?? 0
                        });
                    // Batch insert details then save once
                    _context.TblSalesReturnDetails.AddRange(details);

                    // Inventory logic (no overlap, fully awaited)
                    if (!string.IsNullOrWhiteSpace(r.Type))
                        {
                        if (r.Type.Contains("inventory part", StringComparison.OrdinalIgnoreCase))
                            {
                            await InsertItemTransaction(r, vm.Date, saleId,  invoiceNo);
                            var item = await _context.TblItems.FindAsync(r.ItemId);
                            item.OnHand += (int)r.QTY;
                            _context.TblItems.Update(item);

                            }
                        else if (r.Type.Contains("inventory assembly", StringComparison.OrdinalIgnoreCase))
                            {
                            var components = await _context.TblItemAssemblies
                                .AsNoTracking()
                                .Where(a => a.AssemblyId == r.Id)
                                .Select(a => new AssemblyComponentViewModel
                                    {
                                    ItemId = a.ItemId ?? 0,
                                    Qty = a.Qty ?? 0m,
                                    Method = a.Item!.Method

                                    })
                                .ToListAsync();

                            foreach (var comp in components)
                                {
                                var compQty = comp.Qty * qty;
                               await InsertItemTransaction(r,    vm.Date, saleId, invoiceNo);

                                var item = await _context.TblItems.FindAsync(comp.ItemId);
                                    item.OnHand += comp.Qty ;
                                    _context.TblItems.Update(item);

                                    }
                                }


                        // Cost Center per row (if present)
                        if (r.CostCenterId is not null)
                            {
                            await InsertCostCenterTransactionAsync(
                                vm.Date,
                                debit: 0m,
                                credit: (decimal)lineTot,
                                refId: saleId.ToString(),
                                type: "SALE Return",
                                description: "",
                                costCenterId: r.CostCenterId.Value
                            );
                            }
                        }

                    await _context.SaveChangesAsync();
                    }
                }
            }



        public async Task UpdateSalesReturnInvoiceAsync(SalesReturnViewModel vm, int currentUserId)
            {
            await _context.SaveChangesAsync();
            }
        public async Task InsertItemTransaction(SalesReturnRowDataViewModel vmRow, DateOnly date, int invId, string invoiceNo)
            {
            var row = new TblItemTransaction
                {
                Date = date,
                Type = vmRow.Type ?? "Sales Return Invoice",
                Reference = invId.ToString(),
                ItemId = vmRow.ItemId,
                CostPrice = vmRow.CostPrice,
                QtyIn = vmRow.QTY,
                SalesPrice = vmRow.Price,
                QtyOut = 0,
                Description = $"SalesReturn Invoice No. {invoiceNo}",
                WarehouseId = vmRow.WarehouseId
                };

            _context.TblItemTransactions.Add(row);
            await _context.SaveChangesAsync();

            }
        public async Task InsertCostCenterTransactionAsync(DateOnly date, decimal debit, decimal credit, string refId, string type, string description, int costCenterId)
            {
            var trx = new TblCostCenterTransaction
                {
                Type = type,
                Date = date,
                RefId = int.Parse(refId),
                Debit = debit,
                Credit = credit,
                Description = description,
                CostCenterId = costCenterId,
                ProjectId = 0
                };

            _context.TblCostCenterTransactions.Add(trx);
            await _context.SaveChangesAsync();
            }


        public async Task Transaction(SalesReturnViewModel model, int level4PaymentCreditMethodId,int level4SalesInvoice, int invid = 0, string invoiceNo = "", int level4VatId = 0)
            {
            invid = invid != 0 ? invid : model.Id;
            invoiceNo = !string.IsNullOrWhiteSpace(invoiceNo) ? invoiceNo : model.Invoce;
            level4VatId = level4VatId != 0 ? level4VatId : (int)model.AccountsId;
            if (model.TotaAmount <= 0) return;

            var accountId = (model.InvoiceId == "Credit")
                ? level4PaymentCreditMethodId
                : model.AccountsId;
            // AR / Cash
            await AddTransactionEntry(
                model.Date, accountId, 0, (int)model.TotaAmount, 
                invid, model.SalesRefId,
                model.Invoce == "Credit" ? "SalesReturn Invoice" : "SalesReturn Invoice ",
                "SALES RETURN", $"SalesReturn Invoice NO. {invoiceNo}",
                createdBy: 1, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                VoucherNo: model.NextCode
            );
            // Revenue
            await AddTransactionEntry(
                model.Date, level4SalesInvoice,  (int)model.TotalBeforeVat, 0,
                invid, 0,
                model.Invoce == "Credit" ? "SalesReturn Invoice" : "SalesReturn Invoice ",
                "SALES RETURN", $"SalesReturn For Invoice No. {invoiceNo}",
                createdBy: 1, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                VoucherNo: model.NextCode
            );
            // VAT
            if (model.TotalVat > 0)
                {
                await AddTransactionEntry(
                    model.Date, level4VatId,  (decimal)model.TotalVat, 0,
                    invid, 0,
                    model.Invoce == "Credit" ? "SalesReturn Invoice" : "SalesReturn Invoice ",
                    "SALES RETURN", $"Vat Input For Return Invoice No. {invoiceNo}",
                    createdBy: 1, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                    VoucherNo: model.NextCode
                );
                }
            }
        public async Task AddTransactionEntry(DateOnly date, int? accountId, decimal debit, decimal credit, int transactionId, int humId, string type, string voucherName,
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
                TType = voucherName,
                Type = type,
                Description = description,
                CreatedBy = createdBy,
                VoucherNo = VoucherNo,
                CreatedDate = createdDate,
                State = 0
                };

            _context.TblTransactions.Add(trx);
            await _context.SaveChangesAsync();
            }



        }
    }
