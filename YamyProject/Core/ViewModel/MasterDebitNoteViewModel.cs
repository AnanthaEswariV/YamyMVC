namespace YamyProject.Core.ViewModel
    {
    public class MasterDebitNoteViewModel
        {
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public bool? All { get; set; }
        public IEnumerable<TblVendor> Vendors { get; set; }
        public IEnumerable<MasterCreditViewModel> MasterDebit { get; set; }        //Table Coulmn

        public List<MasterCreditNoteListViewModel> Items { get; set; } = new();

        }
    }
