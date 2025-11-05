namespace YamyProject.Core.ViewModel
    {
    public class PurchaseItemViewModel
        {
        public string ItemCode { get; set; } = ""; 
        public string ItemName { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public decimal? Price { get; set; }
        public int SelectedROw { get; set; }

        public int? Id { get; set; }               // for edits (optional)
        public int? ItemId { get; set; }           // if you bind by id
        public decimal DiscPct { get; set; }
        public decimal Disc { get; set; }
        public decimal? NetPrice { get; set; }      // computed: CostPrice * (1 - Disc%)
        public decimal VatPct { get; set; } = 5;
        public decimal? VatAmonut { get; set; }        
        public decimal? Amount { get; set; }        // computed: Qty * NetPrice + VAT
        public int? CostCenterId { get; set; }
        public SelectList? VatRates { get; set; }
        public string Method { get; set; }
        public string Type { get; set; }
        public decimal? QTY { get; set; }
        public int? VatPersint { get; set; }
        public int WarehouseId { get; set; }
        public string CostCenter { get; set; }
        }
    }
