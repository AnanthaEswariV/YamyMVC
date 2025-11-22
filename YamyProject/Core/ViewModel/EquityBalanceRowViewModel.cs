namespace YamyProject.Core.ViewModel
    {
    public class EquityBalanceRowViewModel
        {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Balance { get; set; }
        public List<EquityBalanceRowViewModel> Children { get; set; } = new();
        }
    }
