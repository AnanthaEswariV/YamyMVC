namespace YamyProject.Services.Interfaces
    {
    public interface IPurchaseReturnService
        {
        Task<IEnumerable<PurchaseRowViewModel>> GetPurchaseREturnAsync(string selectCustomer = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true);
        Task<string> GenerateReturnInvoiceNoAsync();
        Task<PurchaseInvoiceViewModel> GetEditAsync(int id);


        Task CreateTaxInvoiceAsync(PurchaseInvoiceViewModel vm, int currentUserId);
        Task UpdateTaxInvoiceAsync(PurchaseInvoiceViewModel vm, int currentUserId);
        }
    }
