namespace YamyProject.Core.ViewModel
{
    public class ItemStockSettlementListViewModel
    {
        public string SelectionMethod { get; set; } = "Default";
        public List<ItemStockSettlementIndexViewModel> Settlements { get; set; } = new();

    }
}
