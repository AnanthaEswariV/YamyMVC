namespace YamyProject.Core.ViewModel
{
    public class SaleListItemViewModel
    {
        public int Id { get; init; }
        public DateTime Date { get; init; }
        public string InvoiceId { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty; // concat code - name from TblCustomer
        public string PaymentMethod { get; init; } = string.Empty;
        public decimal Total { get; init; }
        public decimal Vat { get; init; }
        public decimal Net { get; init; }
    }
}
