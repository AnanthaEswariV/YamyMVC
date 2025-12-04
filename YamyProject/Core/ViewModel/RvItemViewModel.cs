namespace YamyProject.Core.ViewModel
    {
    public class RvItemViewModel
        {
        public int? SN { get; set; }
        public int? humId { get; set; }
        public int? invId { get; set; }
        public DateOnly? Date { get; set; }
        public string? InvoiceNo { get; set; }
        public decimal Amount { get; set; }
        public bool Pay { get; set; }
        public decimal Payment { get; set; }
        public string? Description { get; set; }
        public string? VoucherType { get; set; }
        public string? CostCenterId { get; set; }
        public string? ProjectId { get; set; }
        }
    }