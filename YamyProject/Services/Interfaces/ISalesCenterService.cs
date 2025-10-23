namespace YamyProject.Services.Interfaces
{
    public interface ISalesCenterService
    {
        Task<IEnumerable<SalesCenterViewModel>> GetSalesAsync(string selectCustomer=null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true);
        Task<IEnumerable<SalesCenterViewModel>> GetDefaultReportAsync();
        Task<IEnumerable<SalesCenterViewModel>> GetDetailedReportAsync();
        Task<string> GenerateInvoiceNoAsync();

    }
}
