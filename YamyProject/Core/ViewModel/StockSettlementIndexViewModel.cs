namespace YamyProject.Core.ViewModel
{
    public class StockSettlementIndexViewModel
    {
        // Config values from tbl_coa_config
        public int InventoryAccountId { get; set; }
        public int StockSettlementAccountId { get; set; }

        // UI hints (web analogues to DataGridView column styles)
        public bool NewQtyReadOnly { get; set; } = false;
        public string? NewQtyCssClass { get; set; }

        // Items to display (empty for now; populate as needed)
        public List<StockSettlementItemViewModel> Items { get; set; } = new();

        // Accumulated validation/configuration errors to show in the view
        public List<string> Errors { get; set; } = new();

        public bool IsConfigurationValid => Errors.Count == 0;
    }
}
