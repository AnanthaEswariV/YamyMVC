namespace YamyProject.Services.Interfaces
    {
    public interface ICustomerSummaryService
        {
        Task<List<CustomerSummaryRowViewModel>> GetCustomerBalancesAsync(/*DateOnly? dateFrom, DateOnly? dateTo*/);
        Task<List<CustomerSummaryBalanceDetailRowViewModel>> GetCustomerStatementAsync(int humId/*, DateOnly? dateFrom, DateOnly? dateTo*/);
        Task<CustomerAgingSummaryViewModel> GetCustomerAgingAsync(DateOnly? dateFrom = null,DateOnly? dateTo = null);
        Task<CustomerSalesListViewModel> GetCustomerSalesAsync(int customerId,DateOnly? dateFrom,DateOnly? dateTo);
        Task<CustomerBalanceSummaryViewModel> GetCustomerBalancesSummryAsync();
        Task<CustomerBalanceDetailViewModel> GetCustomerDetailsStatementAsync(int humId, DateOnly? dateFrom = null, DateOnly? dateTo = null);
        }
    }
