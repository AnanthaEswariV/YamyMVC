namespace YamyProject.Services.Interfaces
    {
    public interface IPurchaseReportServices
        {
        //purchase By Vendor
        Task<SalesByCustomerSummaryViewModel> GetPurchaseByVendorSummaryAsync(/*DateOnly? fromDate,DateOnly? toDate,string? invoiceType = null, string? sortBy = null*/);
        Task<CustomerSalesDetailListViewModel> GetVendorAndSubcontractorPurchaseDetailsAsync(int VendorId/*,DateTime startDate,DateTime endDate*/);
        //purchase By Subcontractor
        Task<SalesByCustomerSummaryViewModel> GetPurchaseBySubcontractorSummaryAsync(/*DateOnly? fromDate,DateOnly? toDate,string? invoiceType = null, string? sortBy = null*/);
        //   Task<CustomerSalesDetailListViewModel> GetSubcontractorPurchaseDetailsAsync(int SubcontractorId/*,DateTime startDate,DateTime endDate*/);
        //purchase By Items
        //Task<SalesByCustomerSummaryViewModel> GetPurchaseByItemsSummaryAsync(/*DateOnly? fromDate,DateOnly? toDate,string? invoiceType = null, string? sortBy = null*/);
        Task<List<ItemSalesReportRowViewModel>> GetPurchaseByItemsSummaryAsync(int? level,string? key,DateOnly startDate,DateOnly endDate);
        Task<SalesByItemDetailsViewModel> GetPurchaseByItemDetailsAsync(int Id, string? dateFilter, DateOnly? dateFrom, DateOnly? dateTo);
    //    Task<CustomerSalesDetailListViewModel> GetItemsPurchaseDetailsAsync(int ItemsId/*,DateTime startDate,DateTime endDate*/);

        }
    }
