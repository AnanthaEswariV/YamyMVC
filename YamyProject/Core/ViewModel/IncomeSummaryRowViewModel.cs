namespace YamyProject.Core.ViewModel
    {
    public class IncomeSummaryRowViewModel
        {
        public int SN { get; set; } = 1;          // Row number
        public int Id { get; set; }           // min_id (customer id)
        public string Name { get; set; } = "";
        public decimal Balance { get; set; }
        }
    }
    
