namespace YamyProject.Core.ViewModel
{
    public class IncomeByVendorSummaryRowViewModel
        {
        public int SN { get; set; }
        public int Id { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal Balance { get; set; }

        }
}