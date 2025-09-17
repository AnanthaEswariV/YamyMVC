namespace YamyProject.Core.ViewModel
{
    public class SalesInvoiceViewModel
{
    public string CustomerCode { get; set; }
    public int CustomerId { get; set; }
    public IEnumerable<SelectListItem> CustomerList { get; set; }
    public string BillTo { get; set; }
    public string ShipTo { get; set; }
    public string InvoiceType { get; set; }
    public string Emirates { get; set; }
    public int WarehouseId { get; set; }
    public IEnumerable<SelectListItem> WarehouseList { get; set; }
    public string PaymentTerms { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime ShipDate { get; set; }
    public DateTime DueDate { get; set; }
    public string InvoiceNumber { get; set; }
    public List<SalesItemViewModel> Items { get; set; } = new();
    public string Notes { get; set; }
    public decimal TotalBeforeVat { get; set; }
    public decimal TotalVat { get; set; }
    public decimal NetAmount { get; set; }
}
}