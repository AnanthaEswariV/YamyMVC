namespace YamyProject.Core.ViewModel
    {
    public class ItemGroupViewModel
        {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public List<ItemInvoiceRowViewModel> Rows { get; set; } = new();
        }
    }
