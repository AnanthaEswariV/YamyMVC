namespace YamyProject.Core.ViewModel
    {
    public class PurchaseInvoiceViewModel
        {
        public int Id { get; set; }
        public bool VendorsType { get; set; } // Vendor or Subcontractor
        public string VendorType { get; set; } = "Vendor"; // Vendor or Subcontractor
        public int PurchasesRefId { get; set; }
        public int IdFromOtherTybe { get; set; }
        public String From { get; set; }
        public String VendorCode { get; set; }
        public String VendorName { get; set; }
        public String Invoce { get; set; }
        public String NextCode { get; set; }
        public int? VendorId { get; set; }
        public int ShipVendorId { get; set; }
        public int FixedAssetId { get; set; }
        public String ShipTo { get; set; }
        public DateOnly ShipDate { get; set; }
        public String Via { get; set; }
        public String? InvoiceType { get; set; } // "Cash" | "Credit"
        public int? WarehouseId { get; set; }
        public String Emirate { get; set; }     // Abu Dhabi, Dubai, ...
        public String SalesMane { get; set; }    
        public int SalesManId { get; set; }
        public int PaymentTermId { get; set; }
        public String PaymentType { get; set; }
        public String PaymentTermDays { get; set; }
        public DateOnly DueDate { get; set; }
        public String PONo { get; set; }
        public int AccountId { get; set; }
        public int PurchaseTypeId { get; set; }
        public String? InvoiceNo { get; set; }
        public String Description { get; set; }
        public DateOnly Date { get; set; }
        public DateOnly DueTo { get; set; }
       

        // -------- Grid Items --------
        public List<PurchaseItemViewModel> Items { get; set; } = new();

        // -------- Totals (optional; computed on server if you like) --------
        public decimal? TotalBeforeVat { get; set; }
        public decimal? TotalVat { get; set; }
        public decimal? NetAmount { get; set; }
        public decimal? TotaAmount { get; set; }
        public decimal? TotaDiscount { get; set; }
        // -------- Dropdowns / lookups --------
        public IEnumerable<TblVendor>Vendors { get; set; }
        public IEnumerable<TblFixedAssetsCategory> FixedAssets { get; set; }
        public IEnumerable<TblWarehouse> Warehouses { get; set; }
        public SelectList? Emirates { get; set; }        // text-only list is fine
        public String? SalesMen { get; set; }
        public String PaymentTerm { get; set; }
        public SelectList? PaymentTerms { get; set; }
        public IEnumerable<SelectListItem> Vat { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<TblCoaLevel4> Accounts { get; set; }
        public SelectList? PurchaseTypes { get; set; }
        public IEnumerable<TblCostCenter> CostCenters { get; set; }
        // Helper to start with 1 blank row
        public string PONO { get; set; } = null!; 
        public static PurchaseInvoiceViewModel CreateEmpty()
            => new()
                {
                Date = DateOnly.FromDateTime(DateTime.Today),
                ShipDate = DateOnly.FromDateTime(DateTime.Today),
                Items = new List<PurchaseItemViewModel> { new PurchaseItemViewModel() }
                };
        }
    }
