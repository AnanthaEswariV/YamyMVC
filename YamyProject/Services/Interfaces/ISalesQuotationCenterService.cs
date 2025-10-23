namespace YamyProject.Services.Interfaces
{
    public interface ISalesQuotationCenterService
    {
        Task<IEnumerable<SalesCenterViewModel>> GetSalesAsync(string selectCustomer = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true);
        Task<IEnumerable<SalesCenterViewModel>> GetDefaultReportAsync();
        Task<IEnumerable<SalesCenterViewModel>> GetDetailedReportAsync();
        Task<string> GenerateInvoiceNoAsync();
        Task<TaxInvoiceViewModel> GetSalesProformaDataAsync(int ID);
        Task CreateQuotationCenterAsync(TaxInvoiceViewModel vm, int currentUserId);
        Task<TaxInvoiceViewModel> GetEditAsync(int id, string formType = "");
         Task UpdateTaxInvoiceAsync(TaxInvoiceViewModel vm, int currentUserId);
    }
}