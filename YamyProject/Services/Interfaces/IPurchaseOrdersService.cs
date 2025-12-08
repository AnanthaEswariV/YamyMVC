namespace YamyProject.Services.Interfaces
    {
    public interface IPurchaseOrdersService
        {
        Task<IEnumerable<PurchaseRowViewModel>> GetPurchaseAsync(bool Subcontractors = false, string selectCustomer = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true);
        Task<IEnumerable<PurchaseRowViewModel>> GetDefaultReportAsync();
        Task<IEnumerable<PurchaseRowViewModel>> GetDetailedReportAsync();
        Task<string> GenerateVendorsInvoiceNoAsync();

        Task<PurchaseInvoiceViewModel> GetEditAsync(int id, string formType = "");
        Task CreateTaxInvoiceAsync(PurchaseInvoiceViewModel vm);
        Task UpdateTaxInvoiceAsync(PurchaseInvoiceViewModel vm);

        }
    }
