namespace YamyProject.Services.Interfaces
    {
    public interface ISalesReturnService
        {
        Task<IEnumerable<SalesCenterViewModel>> GetSalesREturnAsync(string selectCustomer = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true);
        Task<string> GenerateReturnInvoiceNoAsync();
        Task<SalesReturnViewModel> GetEditAsync(int id);

        Task CreateSalesReturnInvoiceAsync(SalesReturnViewModel vm, int currentUserId);
        // Task<int> GetInvoiceCountAsync(int currentUserId);
        // Task<int> GetInvoiceCountAsync(TaxInvoiceViewModel vm);
        // Task<int> GetInvoiceCountAsync(string currentUserId);
        // Task InsertItemTransaction(DateOnly date, string type, string Invoce, int? itemId, decimal costPrice, int qtyIn, decimal salesPrice, decimal qtyOut,
        //                        string description, int warehouseId);
        // Task AddItemCardDetails(DateOnly date, string type, string Invoce, int itemId,
        //                           decimal costPrice, decimal qtyIn, decimal salesPrice, decimal qtyOut,
        //                           string description, int warehouseId);
        // Task InsertCostCenterTransactionAsync(DateTime date, decimal debit, decimal credit, string refId, string type, string description, int costCenterId);
        // // CancellationToken ct = default);
        // Task TransferSaleAsync(string formType, int invId, string poNoOrId);
        // Task addTransactionEntry(DateOnly date, int? accountId, decimal debit, decimal credit,
        //                     int transactionId, int humId, string type, string voucher_name, string description, int createdBy, DateOnly createdDate, string VoucherNo);
        // Task Transaction(TaxInvoiceViewModel Model, int level4PaymentCreditMethodId, int Invid, string invoiceNo, int level4VatId);

        // // Task<bool> CheckItemAvailability(int? itemId, decimal salesQty);

        // //  Task UpdateAsync(int id, TaxInvoiceViewModel vm, int currentUserId);
        // Task<bool> CheckItemValidity(TaxInvoiceViewModel vm, int itemId);
         Task UpdateSalesReturnInvoiceAsync(SalesReturnViewModel vm, int currentUserId);
        }
    }
