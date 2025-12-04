namespace YamyProject.Services.Interfaces
    {
    public interface IPaymentVoucherService
        {
        Task<string> GenerateNextPaymentCode();
        Task<string> GenerateNextPaymentId();
        Task<ReceiveVoucherCenterViewModel> QueryPurchaseAsync(DateOnly from=default, DateOnly to = default, bool date = true, CancellationToken ct = default);
        Task CreatePvAsync(ReceiveVoucherViewModel Model);

        Task updatePvAsync(ReceiveVoucherViewModel Model);
        Task<ReceiveVoucherViewModel> GetEditAsync(int id);

        }
    }
