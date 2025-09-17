namespace YamyProject.Core.ViewModel
{
    public class SalesListItemDto
    {
        public int Id { get; set; }
        public string InvoiceId { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; }
        public decimal Net { get; set; }
    }
}
