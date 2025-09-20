namespace YamyProject.Core.ViewModel
{
    public class ItemStockSettlementDetailsListViewModel
    {
        public string SelectionMethod { get; set; } = string.Empty;
        public List<ItemStockSettlementDetailViewModel> Details { get; set; } = new();
    }
}
