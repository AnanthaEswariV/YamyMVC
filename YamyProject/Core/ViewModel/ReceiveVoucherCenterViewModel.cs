namespace YamyProject.Core.ViewModel
    {
    public class ReceiveVoucherCenterViewModel
        {
        public string SelectedType { get; set; } = "Vendor";
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public bool? All { get; set; }
        public IEnumerable<SelectListItem> TypesList { get; set; } = new[]
{
            new SelectListItem("Vendor", "Vendor"),
            new SelectListItem("Employee", "Employee")
        };
        public IEnumerable<ReceiveVouchersViewModel> ReceiptVouchers { get; set; }

        }
    }
