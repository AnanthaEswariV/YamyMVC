namespace YamyProject.Services.Interfaces
    {
    public interface IAdvancePaymentService
        {
        Task<AdvancePaymentVoucherViewModel> GetAdvancePayments();
        Task<string> GenerateNextAdvancePaymentCode();
        Task CreateAdvancePaymentAsync(AdvancePaymentViewModel model);
        Task<AdvancePaymentViewModel>GetEditAsync(int Id);
        Task UpdateAdvancePaymentAsync(AdvancePaymentViewModel Model);

        }
    }
