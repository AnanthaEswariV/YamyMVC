    namespace YamyProject.Core.ViewModel
    {
    public class SalesListRowViewModel
        {
        public int Id { get; set; }
        public string Inv { get; set; } = "";
        public DateOnly Date { get; set; }
        public decimal Amount { get; set; }
        }
    }
