namespace YamyProject.Core.ViewModel
    {
    public class VatCorporateViewModel
        {
        
        // Header
        public DateOnly PeriodFrom { get; set; }
        public DateOnly PeriodTo { get; set; }
        public DateTime DateDueReturnVat { get; set; }
        public string? CompanyName { get; set; }
        public string? TrnNo { get; set; }

        // === VAT on sales and all other (from your old loadData) ===
        public decimal AbuDhabiVat { get; set; }
        public decimal AbuDhabiAmount { get; set; }

        public decimal DubaiVat { get; set; }
        public decimal DubaiAmount { get; set; }

        public decimal SharjahVat { get; set; }
        public decimal SharjahAmount { get; set; }

        public decimal AjmanVat { get; set; }
        public decimal AjmanAmount { get; set; }

        public decimal UmmAlQuwainVat { get; set; }
        public decimal UmmAlQuwainAmount { get; set; }

        public decimal RasAlKhaimahVat { get; set; }
        public decimal RasAlKhaimahAmount { get; set; }

        public decimal FujairahVat { get; set; }
        public decimal FujairahAmount { get; set; }

        public decimal TouristRefundVat { get; set; }
        public decimal TouristRefundAmount { get; set; }

        public decimal ReverseAccountingSalesVat { get; set; }
        public decimal ReverseAccountingSalesAmount { get; set; }

        public decimal ZeroRatedAmount { get; set; }
        public decimal ExemptAmount { get; set; }

        public decimal GoodsImportedVat { get; set; }
        public decimal GoodsImportedAmount { get; set; }

        public decimal GoodsImportedSettlementVat { get; set; }
        public decimal GoodsImportedSettlementAmount { get; set; }

        // === VAT on expenses and all other inputs (you can fill from purchases) ===
        public decimal ExpensesBasicRateAmount { get; set; }
        public decimal ExpensesBasicRateVat { get; set; }

        public decimal ExpensesReverseAccountingAmount { get; set; }
        public decimal ExpensesReverseAccountingVat { get; set; }

        public decimal ExpensesTotalAmount { get; set; }
        public decimal ExpensesTotalVat { get; set; }

        public decimal NetVatDue { get; set; }
        public decimal TotalTaxRecoverable { get; set; }
        public decimal TaxPayableForPeriod { get; set; }
        }
    }

