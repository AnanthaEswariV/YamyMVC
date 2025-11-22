namespace YamyProject.Services.Interfaces
    {
    public interface IVendorSummaryService
        {

        Task<List<CustomerSummaryRowViewModel>> GetVendorBalancesAsync(/*DateOnly? dateFrom, DateOnly? dateTo*/);
        Task<List<CustomerSummaryBalanceDetailRowViewModel>> GetVendorStatementAsync(int humId/*, DateOnly? dateFrom, DateOnly? dateTo*/);
        Task<CustomerAgingSummaryViewModel> GetVendorAgingAsync(DateOnly? dateFrom = null, DateOnly? dateTo = null);
        Task<CustomerSalesListViewModel> GetVendorSalesAsync(int customerId, DateOnly? dateFrom, DateOnly? dateTo);
        Task<CustomerBalanceSummaryViewModel> GetVendorBalancesSummryAsync();
        Task<CustomerBalanceDetailViewModel> GetVendorDetailsStatementAsync(int humId, DateOnly? dateFrom = null, DateOnly? dateTo = null);

        }
    }