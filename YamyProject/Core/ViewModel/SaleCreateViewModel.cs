namespace YamyProject.Core.ViewModel
{
    public class SaleCreateViewModel
    {
        public DateTime Date { get; init; }
        public int CustomerId { get; init; }
        public string InvoiceId { get; init; } = string.Empty;
        public int WarehouseId { get; init; }
        public string PaymentMethod { get; init; } = string.Empty;
        public decimal Total { get; init; }
        public decimal Vat { get; init; }
        public decimal Net { get; init; }
        public decimal Pay { get; init; }
        public decimal Change { get; init; }
        public decimal Discount { get; init; }
        public List<SaleDetailViewModel> Details { get; init; } = new();
    }
}
