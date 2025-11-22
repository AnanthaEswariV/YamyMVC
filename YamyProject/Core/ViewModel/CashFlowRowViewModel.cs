namespace YamyProject.Core.ViewModel
    {
    public class CashFlowRowViewModel
        {
        public string Name { get; set; } = string.Empty;
        public int? Id { get; set; }        // from l4.id (can be null for header rows)
        public decimal? Balance { get; set; }
        public string Mode { get; set; } = string.Empty;   // 'u' / 'n'
        public string Level { get; set; } = string.Empty;  // '1','2','4'
        public string Symbol { get; set; } = string.Empty; // ' ►     ' etc.
        public CashFlowRowType RowType { get; set; }

        }
    public enum CashFlowRowType
            {
            Normal,
            Section,   // e.g., OPERATING ACTIVITIES, INVESTING ACTIVITIES
            Total      // e.g., Net cash increase for period
            }
    }
