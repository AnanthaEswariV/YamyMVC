namespace YamyProject.Core.ViewModel
    {
    public class MasterCreditNoteViewModel
        {
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public bool? All { get; set; }
        public IEnumerable<TblCustomer> Customers { get; set; }
        public IEnumerable<MasterCreditViewModel> MasterCredit { get; set; }        //Table Coulmn

        public List<MasterCreditNoteListViewModel> Items { get; set; } = new();

        }
    }
