using Microsoft.CodeAnalysis.Elfie.Model.Map;
namespace YamyProject.Services.Implementations
{
    public class SalesCenterService : ISalesCenterService
    {
        private string Customer = null;
        private DateOnly Starting = default;
        private DateOnly Ending = default;
        private string PayMethod = null;
        private readonly YamyDbContext _context;

        public SalesCenterService(YamyDbContext context)
        {
            _context = context;
        }
      
        public async Task<IEnumerable<SalesCenterViewModel>> GetSalesAsync(string selectCustomer = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true)
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

            var query = _context.TblSales
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
        // make it nullable so MAX works
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

                sn = sn + 1;
            }
            // result.ForEach(x => x.Customers = customerSelectList);
            return result;
        }
        public async Task<IEnumerable<SalesCenterViewModel>> GetDetailedReportAsync()
        { var query = _context.TblSales
                .Where(s => s.State == 0)
                .Include(s => s.TblSalesDetails)
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
                    Details = _context.TblSalesDetails
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
        Date = x.Sale.Date,
        Id = x.Sale.Id,
        INV_NO = x.Sale.InvoiceId,
        Customer_Name = x.CustomerName,
        Payment_Method = x.Sale.PaymentMethod,
        Total = x.Sale.Total,
        Vat = x.Sale.Vat,
        Net = x.Sale.Net,
        Item_Name = (d.Item != null ? (d.Item.Code + " - " + d.Item.Name) : ""),
        Qty = d.SalesDetails.Qty,
        Price = d.SalesDetails.Price,
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
                sn = sn + 1;
            }
            return result;
        }

        public async Task<string> GenerateInvoiceNoAsync()
        {
            var prefix =  "0000"; // Prefix for Credit Note
            var lastCodeValue =await _context.TblSales
               .Select(s => s.InvoiceId.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;

            return $"SI-{int.Parse(prefix) + 1:D4}";        
        }
    }
}