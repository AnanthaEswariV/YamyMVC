namespace YamyProject.Core.ViewModel
    {
    public class PurchasesCenterViewModel
        {
        public int? VendorId { get; set; }
        public String VendorType { get; set; } = "Vendor";  // Vendor or Subcontractor
        public bool VendorsType { get; set; } // Vendor or Subcontractor
        public DateOnly DateFrom { get; set; }  // tbl_sales.date
        public DateOnly DateTo { get; set; }  // tbl_sales.date


        public IEnumerable<SelectListItem> Vendors { get; set; }
        public IEnumerable<PurchaseRowViewModel> Purchases { get; set; } = [];
        public IEnumerable<PurchaseItemViewModel> Items { get; set; } = [];
        }
    }
