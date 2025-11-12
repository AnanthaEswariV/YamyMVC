namespace YamyProject.Core.ViewModel
    {
    public class JournalVoucherMasterViewModel
        {
        public bool AllDate { get; set; }
        public DateOnly From { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly To { get; set; }= DateOnly.FromDateTime(DateTime.Now);

        public List<JournalVoucherMasterCustomerViewModel> Customer { get; set; }
        public List<JournalVoucherMasterCustomerDetailsViewModel> Details { get; set; }
        }
    }
