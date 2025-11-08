namespace YamyProject.Core.ViewModel
    {
    public class AdvancePaymentVoucherViewModel
        {
        public string SelectedType { get; set; } = "Vendor";
        public DateTime From { get; set; } = DateTime.Today.AddDays(-30);
        public DateTime To { get; set; } = DateTime.Today;

        // Fill the drop-down with whatever types you support
        public List<string> Types { get; set; } = new() { "Vendor", "Customer", "Employee" };
        public IEnumerable<SelectListItem> TypesList { get; set; } = new[]
    {
            new SelectListItem("Vendor", "Vendor"),
            new SelectListItem("Customer", "Customer"),
            new SelectListItem("Employee", "Employee")
        };

        public List<AdvancePaymentVoucherRowViewModel> Rows { get; set; } = new();
        }

    }
    
