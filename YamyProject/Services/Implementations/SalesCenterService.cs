using Microsoft.EntityFrameworkCore;

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
              .Include(s => s.TblSalesDetails)
              .Include(s => s.TblTransaction)
              .Include(s => s.Customer)
              .OrderBy(s => s.Date)
              .AsQueryable();

            // Apply Customer filter if provided
            if (!string.IsNullOrEmpty(Customer))
            {
                query = query.Where(s => s.CustomerId == int.Parse(Customer) || s.Customer.Name == Customer);
            }

            // Apply Starting date filter if provided
            if (Starting != default)
            {
                query = query.Where(s => s.Date >= Starting);
            }

            // Apply Ending date filter if provided
            if (Ending != default)
            {
                query = query.Where(s => s.Date <= Ending);
            }

            // Apply Payment Method filter if provided
            if (!string.IsNullOrEmpty(PayMethod))
            {
                query = query.Where(s => s.PaymentMethod == PayMethod);
            }

            // Project into anonymous type (with calculated fields)
            var sales = await query
               .Select(s => new
               {
                   Sale = s,

                   JvNo = "000" + s.TblTransaction.TransactionId,
                   CustomerName = s.Customer != null
                   ? s.Customer.Code + " - " + s.Customer.Name
                   : ""
               })
                .ToListAsync();
            var result = new List<SalesCenterViewModel>();

            int sn = 1;
            foreach (var s in sales)
            {
                result.Add(new SalesCenterViewModel
                {
                    SN = sn,
                    Date = s.Sale.Date,
                    Id = s.Sale.Id,
                    InvoiceId = s.Sale.InvoiceId,
                    CustomerName = s.CustomerName,
                    PaymentMethod = s.Sale.PaymentMethod,
                    Total = s.Sale.Total,
                    Vat = s.Sale.Vat,
                    Net = s.Sale.Net,
                    JvNo = s.JvNo
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
            {
                query = query.Where(s => s.CustomerId == int.Parse(Customer) || s.Customer.Name == Customer);
            }

            // Apply Starting date filter
            if (Starting != default)
            {
                query = query.Where(s => s.Date >= Starting);
            }

            // Apply Ending date filter
            if (Ending != default)
            {
                query = query.Where(s => s.Date <= Ending);
            }

            // Apply Payment Method filter
            if (!string.IsNullOrEmpty(PayMethod))
            {
                query = query.Where(s => s.PaymentMethod == PayMethod);
            }

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
                        ? s.Customer.Code + " - " + s.Customer.Name
                        : ""
                })
                .ToListAsync();

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
            var prefix =  "SI-0001"; // Prefix for Credit Note
            var lastCodeValue = _context.TblSales
               .Select(s => s.InvoiceId.Substring(3))
               .MaxAsync();

            return $"SI-{int.Parse(lastCodeValue.Result) + 1:D4}";
            

        }

    }
}