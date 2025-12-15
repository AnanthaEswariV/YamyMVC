using Microsoft.AspNetCore.Http.HttpResults;
using YamyProject.Core.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace YamyProject.Services.Implementations
    {
    public class VendorsCenterService(YamyDbContext context, IListServices ListServices, IHttpContextAccessor httpContextAccessor, IGlobalService GlobalService) : IVendorsCenterService
        {
        private string Vendors = null;
        private DateOnly Starting = default;
        private DateOnly Ending = default;
        private string PayMethod = null;
        private readonly YamyDbContext _context = context;
        private readonly IListServices _ListServices= ListServices;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;

        public async Task<IEnumerable<PurchaseRowViewModel>> GetPurchaseAsync(bool Subcontractors = false, string selectVendors = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true, string VendorType = "")
            {
            if (Custmer == true)
                Vendors = selectVendors;
            if (Date == true)
                {

                //if (From == default)
                //    {
                //    From = DateOnly.FromDateTime(DateTime.Today);
                //    }
                //if (To == default)
                //    {
                //    To = DateOnly.FromDateTime(DateTime.Today);
                //    }

                Starting = From;
                Ending = To;
                }
            if (Pay == true)
                PayMethod = selectionMethodPay;

            if (selectionMethod == "Default")
                return await GetDefaultReportAsync(VendorType);
            else
                return await GetDetailedReportAsync(VendorType);
            }
        public async Task<IEnumerable<PurchaseRowViewModel>> GetDefaultReportAsync( string VendorType = "")
            {
            var query = _context.TblPurchases
              .Where(s => s.State == 0 &&s.Vendors.Type== VendorType)
              .Include(s => s.Transaction)
              .Include(s => s.Vendors)
              .OrderBy(s => s.Date)
              .AsQueryable();
              if (!string.IsNullOrEmpty(Vendors))
                  query = query.Where(s => s.VendorId == int.Parse(Vendors) || s.Vendors.Name == Vendors);
              if (Starting != default)
                  query = query.Where(s => s.Date >= Starting);
              if (Ending != default)
                  query = query.Where(s => s.Date <= Ending);
              if (!string.IsNullOrEmpty(PayMethod))
                  query = query.Where(s => s.PaymentMethod == PayMethod);
              const string Collation = "utf8mb4_0900_ai_ci"; 

            var sales = await query
    .Select(s => new
        {
        s.Id,
        s.Date,
        InvNo = s.InvoiceId,      
        TransactionId = (int?)s.Transaction.TransactionId,
        VendorsName = s.Vendors != null
            ? EF.Functions.Collate(s.Vendors.Code + " - " + s.Vendors.Name, Collation) : "",
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
        x.VendorsName,
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
        Jv_No = $"{g.Max(x => x.TransactionId) ?? 0:D4}",
        Vendors_Name = g.Key.VendorsName,
        Payment_Method = g.Key.PayMethod,
        g.Key.Total,
        g.Key.Vat,
        g.Key.Net
        })
    .OrderBy(r => r.Date)
    .ToListAsync();
            var result = new List<PurchaseRowViewModel>();
            int sn = 1;
            foreach (var s in sales)
                {
                result.Add(new PurchaseRowViewModel
                    {
                    SN = sn,
                    Date = s.Date,
                    Id = s.Id,
                    InvoiceNo = s.INV_NO,
                    VendorName = s.Vendors_Name,
                    PaymentMethod = s.Payment_Method,
                    Total = s.Total,
                    Vat = s.Vat,
                    Net = s.Net,
                    JvNo = s.Jv_No
                    });
                sn ++;
                }
            return result;
            }
        public async Task<IEnumerable<PurchaseRowViewModel>> GetDetailedReportAsync( string VendorType = "")
            {
            var query = _context.TblPurchases
                    .Where(s => s.State == 0 && s.Vendors.Type == VendorType)
                    .Include(s => s.PurchaseDetails)
                    .Include(s => s.Vendors)
                    .OrderBy(s => s.Date)
                    .AsQueryable();
            if (!string.IsNullOrEmpty(Vendors))
                query = query.Where(s => s.VendorId == int.Parse(Vendors) || s.Vendors.Name == Vendors);
            if (Starting != default)
                query = query.Where(s => s.Date >= Starting);
            if (Ending != default)
                query = query.Where(s => s.Date <= Ending);
            if (!string.IsNullOrEmpty(PayMethod))
                query = query.Where(s => s.PaymentMethod == PayMethod);
            const string Collation = "utf8mb4_0900_ai_ci"; 
            var sales = await query
                .Select(s => new
                    {
                    Sale = s,
                    Details = _context.TblPurchaseDetails
                                 .Where(sd => sd.PurchaseId == s.Id)
                                 .Select(sd => new
                                     {
                                     VendorsDetails = sd,
                                     Item = _context.TblItems.FirstOrDefault(i => i.Id == sd.ItemId)
                                     }).ToList(),

                    VendorsName = s.Vendors != null
                   ? EF.Functions.Collate(s.Vendors.Code, Collation)
                     + " - "
                     + EF.Functions.Collate(s.Vendors.Name, Collation)
                   : ""
                    })
                .ToListAsync();
            var flat = sales
    .OrderBy(x => x.Sale.Date)
    .SelectMany(x => x.Details.Select(d => new
        {
        x.Sale.Date,
        x.Sale.Id,
        INV_NO = x.Sale.InvoiceId,
        Vendors_Name = x.VendorsName,
        Payment_Method = x.Sale.PaymentMethod,
        x.Sale.Total,
        x.Sale.Vat,
        x.Sale.Net,
        Item_Name = (d.Item != null ? (d.Item.Code + " - " + d.Item.Name) : ""),
        d.VendorsDetails.Qty,
        d.VendorsDetails.Price,
        Item_Vat = d.VendorsDetails.Vat,
        Item_Total = d.VendorsDetails.Total
        }))
    .GroupBy(r => new
        {
        r.Date,
        r.Id,
        r.INV_NO,
        r.Vendors_Name,
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
    .Select(g => g.Key) 
    .Select((r, idx) => new
        {
        SN = idx + 1,
        r.Date,
        r.Id,
        r.INV_NO,
        r.Vendors_Name,
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

            var result = new List<PurchaseRowViewModel>();
            int sn = 1;
            foreach (var s in sales)
                {
                foreach (var sd in s.Details)
                    {
                    result.Add(new PurchaseRowViewModel
                        {
                        SN = sn++,
                        Date = s.Sale.Date,
                        Id = s.Sale.Id,
                        InvoiceNo = s.Sale.InvoiceId,
                        VendorName = s.VendorsName,
                        PaymentMethod = s.Sale.PaymentMethod,
                        Total = s.Sale.Total,
                        Vat = s.Sale.Vat,
                        Net = s.Sale.Net,
                        ItemName = sd.Item != null ? sd.Item.Code + " - " + sd.Item.Name : "",
                        Qty = sd.VendorsDetails.Qty,
                        CostPrice = sd.VendorsDetails.Price,
                        ItemVat = sd.VendorsDetails.Vat,
                        ItemTotal = sd.VendorsDetails.Total
                        });
                    }
                sn++;
                }
            return result;
            }
        public async Task<int> GenerateVendorsInvoiceIdAsync()
            {
            const int increment = 1;

            var lastId = await _context.TblPurchases
                .Select(s => (int?)s.Id)  
                .MaxAsync() ?? 0;

            // Next invoice id
            return lastId + increment;
            }
        public async Task<string> GenerateVendorsInvoiceNoAsync()
            {
            var prefix = "0000"; 
            var lastCodeValue =await _context.TblPurchases
               .Select(s => s.InvoiceId.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;
            return $"PL-{int.Parse(prefix) + 1:D4}";
            }
        public async Task<PurchaseInvoiceViewModel> GetEditAsync(int id, string formType = "", string VendorType = "")
            {   
            var vm = new PurchaseInvoiceViewModel();

            if (formType == "PO")
                {
                var sale = await _context.TblPurchaseOrders
                                   .Include(s => s.PurchaseOrderDetail)
                                   .ThenInclude(d => d.Items)
                                   .Include(s => s.Vendors)
                                   .FirstOrDefaultAsync(s => s.Id == id);
                vm = MapOrdersToViewModel(sale);
                }
            else
                {
                var sale = await _context.TblPurchases
                                  .Include(s => s.PurchaseDetails)
                                  .ThenInclude(d => d.Items)
                                  .Include(s => s.Vendors)
                                  .FirstOrDefaultAsync(s => s.Id == id);
                vm = MapPurchaseToViewModel(sale);
                }

            var Warehouse = await _ListServices.GetWarehousesAsync();
            var WarehouseSelectList = Warehouse.Select(c => new TblWarehouse
                {
                Id = c.Id,
                Name = c.Name
                }).ToList();
            var vendors = string.Equals(VendorType, "Subcontractor", StringComparison.OrdinalIgnoreCase)
             ? await _ListServices.GetVendorSubcontractorsAsync(): await _ListServices.GetVendorsAsync();
            var VendorslectList = vendors?.Select(c => new TblVendor
               {
               Id = c.Id,
               Code = c.Code,
               Name = c.Name
               }).ToList()?? new List<TblVendor>();
            var Account = await _ListServices.GetAccountsAsync();
            var AccountList = Account.Select(c => new TblCoaLevel4
                {
                Id = c.Id,
                Name = c.Name
                }).ToList();
            var Vat = await _ListServices.GetVatAsync();
            var VatList = Vat.Select(c => new SelectListItem
                {
                Value = c.Value.ToString(),
                Text = c.Name
                }).OrderBy(c => c.Value).ToList();
            var CostCenter = await _ListServices.GetCostCenterAsync();
            var CostCenterList = CostCenter.Select(c => new TblCostCenter
                {
                Id = c.Id,
                Code = c.Code
                }).ToList();
            var FixedAsset = await _ListServices.GetFixedAssetAsync();
            var FixedAssetList = FixedAsset.Select(c => new TblFixedAssetsCategory
                {
                Id = c.Id,
                CategoryName = c.CategoryName
                }).ToList();
            vm.VendorType = string.Equals(VendorType, "Subcontractor", StringComparison.OrdinalIgnoreCase) ? "Subcontractor" : "Vendor";
            vm.FixedAssets = FixedAssetList;
            vm.Vendors = VendorslectList;
            vm.Warehouses = WarehouseSelectList;
            vm.Accounts = AccountList;
            vm.CostCenters = CostCenterList;
            vm.Vat = VatList;
            return vm;
            }
        private static PurchaseInvoiceViewModel MapPurchaseToViewModel(TblPurchase s)
            {
            var details = s.PurchaseDetails?? [];

            return new PurchaseInvoiceViewModel
                {
                Invoce=s.InvoiceId,
                VendorId = s.VendorId,
                VendorCode = s.Vendors?.Code.ToString(),
                VendorName = s.BillTo,
                Emirate = s.City,
                SalesMane = s.SalesMan,
                ShipDate = s.ShipDate ?? default,
                ShipTo = s.ShipTo,
                Via = s.ShipVia ?? string.Empty,
                InvoiceType = s.PaymentMethod,
                AccountId = s.AccountCashId,
                PaymentTerm = s.PaymentTerms,
                DueTo = s.PaymentDate,
                Date = s.Date,
                PONO = s.PoNum,
                TotalBeforeVat = s.Total,
                TotalVat = s.Vat,
                TotaDiscount = 0,
                TotaAmount = s.Net,

                Items = [.. s.PurchaseDetails
                    .OrderBy(d => d.Id)

                    .Select(d =>
                    {
                        var qty = d.Qty;
                        var price = d.CostPrice;
                        var disc = 0;
                        var vatAmt = d.Vatp;  
                        var netPrice = (price * qty) - disc + vatAmt;

                        return new PurchaseItemViewModel
                            {
                            Id = d.Id,
                            ItemId = d.ItemId ?? 0,                        
                            ItemCode = d.Items.Code ?? string.Empty,     
                            ItemName = d.Items.Name ?? string.Empty,     
                            Method = d.Items.Method ?? string.Empty,    
                            Type = d.Items.Type ?? string.Empty,       
                            QTY = d.Qty,
                            CostPrice = d.CostPrice,
                            Disc = 0,
                            NetPrice = netPrice,
                            VatPersint = d.Vat,                                
                            VatAmonut = d.Vatp,                                
                            Amount = d.Total,
                            Price = d.Price,
                            WarehouseId = d.Items.WarehouseId,                 
                            CostCenterId = d.CostCenterId                      
                            };
                    })]
                };
            }
        private static PurchaseInvoiceViewModel MapOrdersToViewModel(TblPurchaseOrder s)
            {
            var details = s.PurchaseOrderDetail ?? [];

            return new PurchaseInvoiceViewModel
                {
                VendorId = s.VendorId,
                VendorCode = s.Vendors?.Code.ToString(),
                VendorName = s.BillTo,
                Emirate = s.City,
                SalesMane = s.SalesMan,
                ShipDate = s.ShipDate ?? default,
                ShipTo = s.ShipTo,
                Via = s.ShipVia ?? string.Empty,
                InvoiceType = s.PaymentMethod,
                AccountId = s.AccountCashId,
                PaymentTerm = s.PaymentTerms,
                DueTo = s.PaymentDate,
                Date = s.Date,
                PONO = s.PoNum,
                TotalBeforeVat = s.Total,
                TotalVat = s.Vat,
                TotaDiscount = 0,
                TotaAmount = s.Net,

                Items = [.. s.PurchaseOrderDetail
                    .OrderBy(d => d.Id)

                    .Select(d =>
                    {

                        var qty = d.Qty;
                        var price = d.CostPrice;
                        var disc = 0;
                        var vatAmt = d.Vatp;  
                        var netPrice = (price * qty) - disc + vatAmt;

                        return new PurchaseItemViewModel
                            {
                            Id = d.Id,
                            ItemId = d.ItemId ?? 0,                         
                            ItemCode = d.Items.Code ?? string.Empty,       
                            ItemName = d.Items.Name ?? string.Empty,       
                            Method = d.Items.Method ?? string.Empty,       
                            Type = d.Items.Type ?? string.Empty, 
                            QTY = d.Qty,
                            CostPrice = d.CostPrice,
                            Disc = 0,
                            NetPrice = netPrice,
                            VatPersint = d.Vat,                                  
                            VatAmonut = d.Vatp,                                 
                            Amount = d.Total,
                            Price = d.Price,
                            WarehouseId = d.Items.WarehouseId,                  
                            CostCenterId = d.CostCenterId                       
                            };
                    })]
                };
            }
        public async Task CreateTaxInvoiceAsync(PurchaseInvoiceViewModel vm)
            {
            // 0) Guards
            //if (vm is null) throw new ArgumentNullException(nameof(vm));
            //if (vm.Items is null || vm.Items.Count == 0)
            //    throw new InvalidOperationException("Invoice must contain at least one item.");
            //// 1) Server-side totals
            decimal totalBeforeVat = 0m, totalVat = 0m, netAmount = 0m, totalDiscount = 0m;
            foreach (var r in vm.Items.Where(i => i != null))
                {
                if (r.ItemId != null && r.ItemId != 0)
                    {
                    var linePrice = r.CostPrice * r.QTY;
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
             var Code =await GenerateVendorsInvoiceNoAsync();
            var invoiceNo = Code.ToString();
            //if (vm.Invoce == null)
            //     invoiceNo = GenerateVendorsInvoiceNoAsync();
            // 3) Strategy + Transaction
            var strategy = _context.Database.CreateExecutionStrategy();
            var FixedAssetId= vm.FixedAssetId;
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    if (vm.PurchaseType != "Fixed Assets")
                        FixedAssetId = 0;
                        // 4) Header
                        var Purchase = new TblPurchase
                        {
                        Date = vm.Date,
                        VendorId = vm.VendorId ?? 0,
                        InvoiceId = invoiceNo,
                        City = vm.Emirate ?? string.Empty,
                        WarehouseId = vm.WarehouseId ?? 0,
                        PoNum = vm.PONO ?? string.Empty,
                        BillTo = vm.VendorName ?? string.Empty,
                        SalesMan = vm.SalesMane ?? string.Empty,
                        ShipDate = vm.ShipDate,
                        ShipVia = vm.Via ?? string.Empty,
                        ShipTo = vm.ShipTo ?? string.Empty,
                        PaymentMethod = vm.InvoiceType ?? string.Empty,
                        AccountCashId = vm.AccountId ,
                        PaymentTerms = vm.PaymentTerm ?? string.Empty,
                        PaymentDate = vm.DueTo,
                        Total = totalBeforeVat,
                        Vat = totalVat,
                        Net = netAmount,
                        Pay = string.Equals(vm.InvoiceType, "Cash", StringComparison.OrdinalIgnoreCase) ? netAmount : 0m,
                        Change = string.Equals(vm.InvoiceType, "Cash", StringComparison.OrdinalIgnoreCase) ? 0m : netAmount,
                        CreatedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,
                        CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        State = 0,
                        PurchaseType= vm.PurchaseType,
                        Description = vm.Description ?? string.Empty,
                        FixedAssetCategoryId = FixedAssetId
                        };
                    _context.TblPurchases.Add(Purchase);
                    await _context.SaveChangesAsync(); // need sale.Id
                    var PurchaseId = Purchase.Id;
                    if (PurchaseId == 0)
                        throw new InvalidOperationException("Failed to create sale record.");
                  
                    if (vm.From == "PO")
                        {
                        var PurchaseOrders = _context.TblPurchaseOrders.Find(vm.IdFromOtherTybe);
                        PurchaseOrders.TranferStatus = 1;
                        //  _context.TblSalesOrders.Update();
                        await _context.SaveChangesAsync();
                        }
                    // 5) Details (batch add), then inventory moves per line
                    await InsertInvItems(vm, PurchaseId, invoiceNo);
                    // 6) Transfer flags + accounting entries
                    await TransferSaleAsync(vm.InvoiceType ?? "", PurchaseId, vm.PONO ?? "");
                    await Transaction(vm,
                        level4PaymentCreditMethodId: await _ListServices.DefaultAccountsSet("Vendor"),
                       level4Inventory: await _ListServices.DefaultAccountsSet("Inventory"),
                        invid: PurchaseId,
                        invoiceNo:invoiceNo,
                        level4VatId: await _ListServices.DefaultAccountsSet("Vat Output"));
                    await UpdateOrAddFixedAssets(PurchaseId, vm.FixedAssetId);
                    await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Add Purchase Invoice", "PURCHASE", PurchaseId, "Added Purchase Invoice: " +  invoiceNo);
                    await tx.CommitAsync();
                    }
                catch
                    {
                    await tx.RollbackAsync();
                    throw;
                    }
            });
            }
      
        public async Task InsertInvItems(PurchaseInvoiceViewModel vm, int PurchaseId = 0, string invoiceNo = "")
            {
            var details = new List<TblPurchaseDetail>(vm.Items.Count);
            PurchaseId = PurchaseId != 0 ? PurchaseId : vm.Id;
            invoiceNo = !string.IsNullOrWhiteSpace(invoiceNo) ? invoiceNo : vm.Invoce;
            foreach (var r in vm.Items.Where(i => i != null))
                {
                if (r.ItemId != null && r.ItemId != 0)
                    {
                    var qty = r.QTY;
                    var price = r.CostPrice;
                    var disc = r.Disc;
                    var vatPct = r.VatPersint ?? 0;
                    var lineBase = (price * qty) - disc;
                    var lineVat = (decimal)(lineBase * (vatPct / 100m));
                    var lineTot = lineBase + lineVat;
                    details.Add(new TblPurchaseDetail
                        {
                        PurchaseId = PurchaseId,
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
                    // Inventory logic (no overlap, fully awaited)
                    if (!string.IsNullOrWhiteSpace(r.Type))
                        {
                        if (r.Type.Contains("inventory part", StringComparison.OrdinalIgnoreCase))
                            {
                            await InsertItemTransaction(r, vm.Date, PurchaseId.ToString(), invoiceNo, (int)vm.WarehouseId);
                            AddItemCardDetails(r, vm.Date, PurchaseId.ToString(), invoiceNo, (int)vm.WarehouseId);
                            UpdateOnHandItemAsync(r.ItemId??0);
                            }               
                        // Cost Center per row (if present)
                        if (r.CostCenterId is not null)
                            {
                            await InsertCostCenterTransactionAsync(
                                vm.Date,
                                debit: (decimal)lineTot,
                                credit:0,
                                refId: PurchaseId.ToString(),
                                type: "PURCHASE",
                                description: "",
                                costCenterId: r.CostCenterId.Value
                            );
                            }
                        }
                    // Batch insert details then save once
                    _context.TblPurchaseDetails.AddRange(details);
                    await _context.SaveChangesAsync();
                    }
                }
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
        public async Task InsertItemTransaction(PurchaseItemViewModel vmRow, DateOnly date, string invId,string invoiceNo,int warehouseId)
            {
            var row = new TblItemTransaction
                {
                Date = date,
                Type ="Purchase Invoice",
                Reference = invId,
                ItemId = vmRow.ItemId,
                CostPrice = vmRow.CostPrice,
                QtyIn = vmRow.QTY,
                QtyInc = vmRow.QTY,
                SalesPrice =0,
                QtyOut = 0,
                Description = $"Purchase Invoice No. {invoiceNo}",
                WarehouseId = warehouseId
                };
            _context.TblItemTransactions.Add(row);
            await _context.SaveChangesAsync();
             }
        
        public async Task TransferSaleAsync(string formType, int invId, string poNoOrId)
            {
            if (string.IsNullOrEmpty(formType)) return;
         if (formType == "SO")
                {
                await _context.TblPurchaseOrders
                    .Where(q => q.Id.Equals(poNoOrId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(q => q.TranferStatus, 1)
                        .SetProperty(q => q.VendorId, invId));
                }
            }
    
        public async Task Transaction(PurchaseInvoiceViewModel model, int level4PaymentCreditMethodId, int level4Inventory, int invid = 0, string invoiceNo = "", int level4VatId = 0)
            {
            invid = invid != 0 ? invid : model.Id;
            invoiceNo = !string.IsNullOrWhiteSpace(invoiceNo) ? invoiceNo : model.Invoce;
            level4VatId = level4VatId != 0 ? level4VatId : (int)model.AccountId;
            if (model.TotaAmount <= 0) return;

            var accountId = (model.InvoiceType == "Credit")
                ? level4PaymentCreditMethodId
                : model.AccountId;
            // AR / Cash
            await AddTransactionEntry(
                model.Date, accountId,  0,(int) model.TotaAmount,
                invid, model.VendorId ?? 0,
                model.InvoiceType == "Credit" ? "Purchase Invoice" : "Purchase Invoice Cash",
                "PURCHASE", $"Purchase Invoice NO. {invoiceNo}",
                createdBy: _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                VoucherNo: invoiceNo
            );
            // Revenue
            await AddTransactionEntry(
                model.Date, level4Inventory, (int)model.TotalBeforeVat, 0, 
                invid, 0,
                 "Purchase Invoice",
                "PURCHASE", $"Purchase  For Invoice No. {invoiceNo}",
                createdBy: _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                VoucherNo: invoiceNo
            );
            // VAT
            if (model.TotalVat > 0)
                {
                await AddTransactionEntry(
                    model.Date, level4VatId, (decimal)model.TotalVat, 0, 
                    invid, 0,
                     "Purchase Invoice",
                    "PURCHASE", $"Vat Input For Invoice No. {invoiceNo}",
                    createdBy: _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                    VoucherNo: invoiceNo
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
        public async Task UpdateOrAddFixedAssets(int invId, int FixedAssetId)
            {
            var OldVid=0;
            var PurchaseRow = await _context.TblPurchases
                 .AsNoTracking()
                .Where(s => s.Id == invId)
                 .Select(s => new
                     { 
                       s.Net,
                       VoucherNo = s.InvoiceId,
                       DATE = s.Date,
                       NAME = s.Vendors != null ? s.Vendors.Name : null
                }).FirstOrDefaultAsync();
            if (PurchaseRow != null)
                {
                var FixedAsset = await _context.TblFixedAssets
                      .AsNoTracking()
                    .Where(f=>
                                    f.PurchaseDate== PurchaseRow.DATE &&
                                    f.Supplier==PurchaseRow.NAME &&
                                    f.InvoiceNumber==PurchaseRow.VoucherNo)
                    .FirstOrDefaultAsync();
                if (FixedAsset != null)
                    {
                    OldVid = FixedAsset.Id;
                    }
                var FixedAssetCategorie = await _context.TblFixedAssetsCategories
                     .AsNoTracking()
                     .Where(A => A.Id == FixedAssetId).FirstOrDefaultAsync();
                if (FixedAssetCategorie != null)
                    {
                    int AssetsAccountId = FixedAssetCategorie?.AssetsAccountId ?? 0, DepreciationAccountId = FixedAssetCategorie?.DepreciationAccountId ?? 0, ExpenseAccountId = FixedAssetCategorie?.ExpenceAccountId ?? 0;

                    if (PurchaseRow.Net != null ||PurchaseRow.Net.ToString()!="")
                        {
                        if(OldVid>0)
                            {
                            var FixedAssets = _context.TblFixedAssets.Find(OldVid) ?? throw new KeyNotFoundException($"Sales Invoice with ID {OldVid} not found.");
                            FixedAssets.Date = PurchaseRow.DATE;
                            FixedAssets.CategoryId = FixedAssetId;
                            FixedAssets.Supplier = PurchaseRow.NAME;
                            FixedAssets.Status = "Draft";
                            FixedAssets.InvoiceNumber = PurchaseRow.VoucherNo;
                            FixedAssets.PurchaseDate = PurchaseRow.DATE;
                            FixedAssets.EndDate = PurchaseRow.DATE;
                            FixedAssets.DepreciationLife = 0;
                            FixedAssets.PurchasePrice = PurchaseRow.Net;
                            FixedAssets.DebitAccountId = AssetsAccountId;
                            FixedAssets.CreditAccountId = DepreciationAccountId;
                            FixedAssets.ExpenceAccountId = ExpenseAccountId;
                            FixedAssets.ModifiedBy =1;//User ID
                            FixedAssets.ModifiedDate = DateOnly.FromDateTime(DateTime.Today);

                            //Add loger
                            }
                        else 
                            {
                            var Code=await _context.TblFixedAssets
                                   .AsNoTracking()
                                   .Select(f => Convert.ToInt64(f.Code))  // Code is string
                                   .DefaultIfEmpty(0)
                                   .MaxAsync();
                            if (Code != null)
                                Code ++;
                            else
                                Code = 00001;

                            var Asset = new TblFixedAsset
                                {
                                Code = Code.ToString(),
                                Name = FixedAssetCategorie.CategoryName,
                                Brand = "",
                                Date = PurchaseRow.DATE,
                                CategoryId = FixedAssetId,
                                Supplier = PurchaseRow.NAME,
                                Status = "Draft",
                                InvoiceNumber = PurchaseRow.VoucherNo,
                                PurchaseDate = PurchaseRow.DATE,
                                EndDate = PurchaseRow.DATE,
                                DepreciationLife = 0,
                                PurchasePrice = PurchaseRow.Net,
                                DebitAccountId = AssetsAccountId,
                                CreditAccountId = DepreciationAccountId,
                                ExpenceAccountId = ExpenseAccountId,
                                ModifiedBy = 1,//HttpContext.Session.GetInt32("UserId"),//User ID
                                ModifiedDate = DateOnly.FromDateTime(DateTime.Today),
                                };
                            _context.TblFixedAssets.Add(Asset);
                            await _context.SaveChangesAsync();
                            //Add loger
                            }
                        }
                    }
                }
            }
        public async Task UpdateTaxInvoiceAsync(PurchaseInvoiceViewModel Model)
            {
            decimal totalBeforeVat = 0m, totalVat = 0m, netAmount = 0m, totalDiscount = 0m;
            foreach (var r in Model.Items.Where(i => i != null))
                {
                if (r.ItemId != null && r.ItemId != 0)
                    {
                    var linePrice = r.CostPrice * r.QTY;
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
            decimal paidAmount = 0;
            decimal? changeAmount = 0;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    if (Model.InvoiceType != "Cash")
                        {
                        var receiptvoucher = await _context.TblPaymentVoucherDetails
                                        .AsNoTracking()
                                        .Where(r => r.InvId == Model.Id)
                                        .SumAsync(r => (decimal?)r.Payment) ?? 0m;
                        if (receiptvoucher > 0)
                            {
                            paidAmount = receiptvoucher;
                            changeAmount = paidAmount - Model.TotaAmount;
                            }
                        }

                    var Sales = _context.TblPurchases.Find(Model.Id);//?? throw new KeyNotFoundException($"Purchase Invoice with ID {Model.Id} not found.");
                        
                    Sales.Date = Model.Date;
                    Sales.VendorId = Model.VendorId ?? 0;
                    Sales.InvoiceId = Model.Invoce; 
                    Sales.City = Model.Emirate ?? string.Empty; 
                    Sales.WarehouseId = Model.WarehouseId ?? 0;
                    Sales.PoNum = Model.PONO ?? string.Empty;
                    Sales.BillTo = Model.VendorName ?? string.Empty;
                    Sales.ShipDate = Model.ShipDate;
                    Sales.SalesMan = Model.SalesMane ?? string.Empty;
                    Sales.ShipVia = Model.Via ?? string.Empty;
                    Sales.ShipTo = Model.ShipTo ?? string.Empty;
                    Sales.PaymentMethod = Model.InvoiceType ?? string.Empty;
                    Sales.AccountCashId = Model.AccountId ;
                    Sales.PaymentTerms = Model.PaymentTerm ;
                    Sales.PaymentDate = Model.DueTo;
                    Sales.Total = totalBeforeVat;
                    Sales.Vat = totalVat;
                    Sales.Net = netAmount;
                    Sales.Description = Model.Description ?? string.Empty;
                    Sales.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                    Sales.ModifiedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                    Sales.Pay = paidAmount;
                    Sales.Change = (decimal)changeAmount;
                    Sales.PurchaseType = Model.PurchaseType;
                    if(Model.PurchaseType== "Fixed Assets")
                    Sales.FixedAssetCategoryId = Model.FixedAssetId;

                    await _context.SaveChangesAsync();

                    //Delete existing details and transactions and item transactions and item card details and cost center transactions and 
                  //  await ReturnItemsToInventory(Model.Id);
                    await DeleteItemsFromInventory(Model.Id);
                    //insert the new details and transactions and item transactions and item card details and cost center transactions
                    await InsertInvItems(Model);
                    await Transaction(Model,
                        level4PaymentCreditMethodId: await _ListServices.DefaultAccountsSet("Vendor"),
                       level4Inventory: await _ListServices.DefaultAccountsSet("Inventory"),
                        invoiceNo: Model.Invoce,
                        level4VatId: await _ListServices.DefaultAccountsSet("Vat Output"));
                    await UpdateOrAddFixedAssets(Model.Id, Model.FixedAssetId);

                    await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Update Purchase Invoice", "PURCHASE", Model.Id, "Update Purchase Invoice: " + Model.InvoiceNo);

                    await tx.CommitAsync();
                    }
                catch
                    {
                    await tx.RollbackAsync();
                    throw;
                    }
            });
         }
        public async Task DeleteItemsFromInventory(int PurchaseId)
            {
            var ItemTransactions = await _context.TblItemTransactions
                    .Where(d => d.Reference == PurchaseId.ToString() && d.Type == "Purchase Invoice").ToListAsync();
            _context.TblItemTransactions.RemoveRange(ItemTransactions);
            await _context.SaveChangesAsync();
            var ItemCardDetails = await _context.TblItemCardDetails
                    .Where(d => d.TransNo == PurchaseId && d.TransType == "Purchase Invoice").ToListAsync();
            _context.TblItemCardDetails.RemoveRange(ItemCardDetails);
            await _context.SaveChangesAsync();
            var SalesDetails = await _context.TblPurchaseDetails
                     .Where(d => d.PurchaseId == PurchaseId).ToListAsync();
            _context.TblPurchaseDetails.RemoveRange(SalesDetails);
            await _context.SaveChangesAsync();
            var CostCenterTransactions = await _context.TblCostCenterTransactions
                   .Where(d => d.RefId == PurchaseId && d.Type == "Purchase")
                   .ToListAsync();
            _context.TblCostCenterTransactions.RemoveRange(CostCenterTransactions);
            await _context.SaveChangesAsync();
            var Transactions = await _context.TblTransactions
                .Where(d => d.TransactionId == PurchaseId && d.Type == "PURCHASE")
                .ToListAsync();
            _context.TblTransactions.RemoveRange(Transactions);
            await _context.SaveChangesAsync();

            }

        //public async Task ReturnItemsToInventory(int PurchaseId)
        //    {
        //    var rows = await _context.TblPurchaseDetails
        //         .AsNoTracking()
        //         .Where(sd => sd.PurchaseId == PurchaseId)
        //         .Select(sd => new
        //             {
        //             Id = sd.ItemId,
        //             qty = sd.Qty ?? 0m,
        //             sd.Items.Type
        //             })
        //         .ToListAsync();
        //    foreach (var r in rows)
        //        {

        //        if (!string.IsNullOrWhiteSpace(r.Type))
        //            {
        //            if (r.Type.Contains("inventory part", StringComparison.OrdinalIgnoreCase))
        //                {
        //                await _context.TblItems
        //               .Where(i => i.Id == r.Id)
        //               .ExecuteUpdateAsync(setters => setters
        //                   .SetProperty(i => i.OnHand, i => i.OnHand + r.qty)
        //               );
        //                }

        //            }
        //        }
        //    }

        public async Task UpdateOnHandItemAsync(int itemId)
                {
                // 1) Calculate on_hand from tbl_item_transaction
                var onHand = await _context.TblItemTransactions
                    .Where(t => t.ItemId == itemId)
                    .SumAsync(t => (decimal?)(t.QtyIn - t.QtyOut)) ?? 0m; 

                // 2) Load item and update its OnHand field
                var item = await _context.TblItems
                    .FirstOrDefaultAsync(i => i.Id == itemId);

                if (item != null)
                    {
                    item.OnHand = onHand;
                    await _context.SaveChangesAsync();
                    }
                }
        public async Task AddItemCardDetails(PurchaseItemViewModel vmRow, DateOnly date, string invId, string invoiceNo, int warehouseId)
            {
            decimal debit = (decimal)(vmRow.QTY * vmRow.CostPrice);
            decimal credit = 0 ;
            var agg = await _context.TblItemCardDetails
       .Where(c => c.ItemId == vmRow.ItemId)
       .GroupBy(c => c.ItemId)
       .Select(g => new
           {
           QtyBalance = g.Sum(x => (decimal?)((x.QtyIn ) - (x.QtyOut ))) ?? 0m,
           Balance = g.Sum(x => (decimal?)((x.Debit ) - (x.Credit ))) ?? 0m
           }).FirstOrDefaultAsync();
            decimal previousQtyBalance = agg?.QtyBalance ?? 0m;
            decimal previousBalance = agg?.Balance ?? 0m;

            decimal qtyBalance =(decimal)(previousQtyBalance + (vmRow.QTY - 0));
            decimal balance = previousBalance + (debit - credit);
            var cardRow = new TblItemCardDetail
                {
                ItemId = (int)vmRow.ItemId,
                Date = date,
                WharehouseId = warehouseId,
                InvNo = invoiceNo,
                TransNo = int.Parse(invId),
                TransType = "Purchase Invoice",
                Description = $"Purchase Invoice No. {invoiceNo}",
                Price = (decimal)vmRow.CostPrice,
                QtyIn = (decimal)vmRow.QTY,
                QtyOut = 0,
                QtyBalance = qtyBalance,
                Debit = debit,
                Credit = credit,
                Balance = balance,
                FifoQty = 0,
                FifoCost = 0
                };
            _context.TblItemCardDetails.Add(cardRow);
            await _context.SaveChangesAsync();

            }

        }
    }