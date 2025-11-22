namespace YamyProject.Services.Interfaces
    {
    public interface ISalesReportServices
        {
        Task<SalesByCustomerSummaryViewModel> GetSalesByCustomerSummaryAsync(/*DateOnly? fromDate,DateOnly? toDate,string? invoiceType = null, string? sortBy = null*/);
        Task<CustomerSalesDetailListViewModel> GetCustomerSalesDetailsAsync(int customerId/*,DateTime startDate,DateTime endDate*/);
        }
    }
