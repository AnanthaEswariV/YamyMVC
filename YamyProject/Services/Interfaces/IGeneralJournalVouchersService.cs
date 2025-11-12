namespace YamyProject.Services.Interfaces
    {
    public interface IGeneralJournalVouchersService
        {
        Task<JournalVoucherMasterViewModel> GetCustmerData(DateOnly From=default ,DateOnly To=default, bool All = true);
        Task<IEnumerable<JournalVoucherMasterCustomerDetailsViewModel>> GetJVDetails(int id);
        Task<string> GenerateNextReceiptCode();
        Task CreateJournalVoucher(JournalVoucherViewModel Model);
        Task UpdateJournalVoucher(JournalVoucherViewModel Model);
        Task<JournalVoucherViewModel> GEtJournalVoucher(int Id);

        }
    }
