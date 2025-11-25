namespace YamyProject.Services.Interfaces
    {
    public interface ISalesReportServices
        {
        Task<SalesByCustomerSummaryViewModel> GetSalesByCustomerSummaryAsync(/*DateOnly? fromDate,DateOnly? toDate,string? invoiceType = null, string? sortBy = null*/);
        Task<CustomerSalesDetailListViewModel> GetCustomerSalesDetailsAsync(int customerId/*,DateTime startDate,DateTime endDate*/);
        Task<List<ItemSalesReportRowViewModel>> GetPurchaseByItemsSummaryAsync(int? level, string? key, DateOnly startDate, DateOnly endDate);

        Task<SalesByItemDetailsViewModel> GetSalesByItemDetailsAsync(int Id,string? dateFilter, DateOnly? dateFrom,DateOnly? dateTo);
        }
    }
