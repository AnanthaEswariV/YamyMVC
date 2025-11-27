namespace YamyProject.Services.Implementations
    {
    public class VatCorporateService (YamyDbContext context): IVatCorporateService
        {
        private readonly YamyDbContext _Context = context;
        public async Task<VatCorporateViewModel> GetVatCorporateAsync(DateOnly from, DateOnly to)
            {
            var model = new VatCorporateViewModel();
                //{
                //PeriodFrom = from,
                //PeriodTo = to,
                //DateDueReturnVat = DateTime.Today
                //};   

                // ===== loadCompany() ====
            var company = await _Context.TblCompanies.AsNoTracking().FirstOrDefaultAsync();
            if (company != null)
                {
                model.CompanyName = company.Name;
                model.TrnNo = company.TrnNo;
                }

            // ===== loadData() but with LINQ only =====

            // 1) Read all active tax rows
            var taxes = await _Context.TblTaxes
                .AsNoTracking()
                .Where(t => t.State == 0)
                .ToListAsync();

            // 2) Aggregate sales by tax id in ONE query
            var salesByTax = await
                (from s in _Context.TblSales
                 from sd in s.TblSalesDetails
                 where s.Vat > 0
                       //&& s.Date >= from
                       //&& s.Date <= to
                 group new { s, sd } by sd.Vat
                 into g
                 select new
                     {
                     VatId = g.Key,
                     AmountBefore = g.Sum(x => x.s.Total),
                     VatAmount = g.Sum(x => x.s.Vat),
                     TotalAmount = g.Sum(x => x.s.Net)
                     })
                .ToListAsync();

            // 3) Map each tax “name” to the right properties in the ViewModel
            foreach (var tax in taxes)
                {
                var agg = salesByTax.FirstOrDefault(x => x.VatId == tax.Id);
                if (agg == null) continue;

                decimal taxAmount = agg.VatAmount;
                decimal totalAmount = agg.TotalAmount;

                var name = tax.Name ?? string.Empty;

                if (name.Contains("Abu Dhabi", StringComparison.OrdinalIgnoreCase))
                    {
                    model.AbuDhabiVat = taxAmount;
                    model.AbuDhabiAmount = totalAmount;
                    }
                else if (name.Contains("Dubai", StringComparison.OrdinalIgnoreCase))
                    {
                    model.DubaiVat = taxAmount;
                    model.DubaiAmount = totalAmount;
                    }
                else if (name.Contains("Sharjah", StringComparison.OrdinalIgnoreCase))
                    {
                    model.SharjahVat = taxAmount;
                    model.SharjahAmount = totalAmount;
                    }
                else if (name.Contains("Ajman", StringComparison.OrdinalIgnoreCase))
                    {
                    model.AjmanVat = taxAmount;
                    model.AjmanAmount = totalAmount;
                    }
                else if (name.Contains("Umm Al Quwain", StringComparison.OrdinalIgnoreCase))
                    {
                    model.UmmAlQuwainVat = taxAmount;
                    model.UmmAlQuwainAmount = totalAmount;
                    }
                else if (name.Contains("Ras Al Khaimah", StringComparison.OrdinalIgnoreCase))
                    {
                    model.RasAlKhaimahVat = taxAmount;
                    model.RasAlKhaimahAmount = totalAmount;
                    }
                else if (name.Contains("Fujairah", StringComparison.OrdinalIgnoreCase))
                    {
                    model.FujairahVat = taxAmount;
                    model.FujairahAmount = totalAmount;
                    }
                else if (name.Contains("tax refund", StringComparison.OrdinalIgnoreCase))
                    {
                    model.TouristRefundVat = taxAmount;
                    model.TouristRefundAmount = totalAmount;
                    }
                else if (name.Contains("reverse accounting", StringComparison.OrdinalIgnoreCase))
                    {
                    model.ReverseAccountingSalesVat = taxAmount;
                    model.ReverseAccountingSalesAmount = totalAmount;
                    }
                else if (name.Contains("zero rated", StringComparison.OrdinalIgnoreCase))
                    {
                    model.ZeroRatedAmount = totalAmount;
                    }
                else if (name.Contains("Exempt", StringComparison.OrdinalIgnoreCase))
                    {
                    model.ExemptAmount = totalAmount;
                    }
                else if (name.Contains("Goods imported into the country",
                                       StringComparison.OrdinalIgnoreCase))
                    {
                    model.GoodsImportedVat = taxAmount;
                    model.GoodsImportedAmount = totalAmount;
                    }
                else if (name.Contains("Settlement of goods imported into the United Arab Emirates",
                                       StringComparison.OrdinalIgnoreCase))
                    {
                    model.GoodsImportedSettlementVat = taxAmount;
                    model.GoodsImportedSettlementAmount = totalAmount;
                    }
                }

            // ====== TODO: expenses side with EF too ======
            // Example placeholder (replace with your own purchase query logic):
            model.ExpensesBasicRateAmount = 0m;
            model.ExpensesBasicRateVat = 0m;
            model.ExpensesReverseAccountingAmount = 0m;
            model.ExpensesReverseAccountingVat = 0m;
            model.ExpensesTotalAmount =
                model.ExpensesBasicRateAmount + model.ExpensesReverseAccountingAmount;
            model.ExpensesTotalVat =
                model.ExpensesBasicRateVat + model.ExpensesReverseAccountingVat;

            model.NetVatDue = (model.AbuDhabiVat + model.DubaiVat + model.SharjahVat
                                        + model.AjmanVat + model.UmmAlQuwainVat
                                        + model.RasAlKhaimahVat + model.FujairahVat
                                        + model.TouristRefundVat + model.ReverseAccountingSalesVat)
                                       - model.ExpensesTotalVat;

            model.TotalTaxRecoverable = model.ExpensesTotalVat;
            model.TaxPayableForPeriod = model.NetVatDue - model.TotalTaxRecoverable;

            return model;
            }
        }
    }
