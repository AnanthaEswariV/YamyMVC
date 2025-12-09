namespace YamyProject.Services.Interfaces
    {
    public interface IReceiptVoucherService
        {
        Task<string> GenerateNextReceiptCode();
        Task<string> GenerateNextReceiptId();
        Task<ReceiptVoucherCenterViewModel> QuerySalesAsync(DateOnly from=default, DateOnly to = default, bool date = true, CancellationToken ct = default);
      //  Task updateRvAsync(ReceiptVouchersViewModel Model);
        Task<ReceiptVoucherViewModel> GetReceiptVoucherByIdAsync(int id);
        Task CreatePvAsync(ReceiptVoucherViewModel Model);
        Task UpdatePvAsync(ReceiptVoucherViewModel model);
        }
    }
