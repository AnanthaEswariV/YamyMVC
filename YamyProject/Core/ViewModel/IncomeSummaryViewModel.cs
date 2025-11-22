namespace YamyProject.Core.ViewModel
    {
    public class IncomeSummaryViewModel
        {
        public DateOnly DateFrom  { get; set; }
        public DateOnly DateTo  { get; set; }
        public List<IncomeSummaryRowViewModel> Customer { get; set; }

        }
    }
