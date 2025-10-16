namespace YamyProject.Services.Interfaces
{
    public interface ISalesCreateService
    {
        Task<int> CreateTaxInvoiceAsync(TaxInvoiceViewModel vm, int currentUserId);
        Task<decimal> InsertItemTransaction(SalesRowDataViewModel request, int? itemId, decimal qty, string? method, DateOnly SelseDate, int invId,string invoiceNo);
        Task InsertItemTransaction(DateOnly date, string type, string Invoce, int? itemId, decimal costPrice, int qtyIn, decimal salesPrice, decimal qtyOut, 
                                  string description, int warehouseId);
        Task AddItemCardDetails(DateOnly date, string type, string Invoce, int itemId,
                                  decimal costPrice, decimal qtyIn, decimal salesPrice, decimal qtyOut,
                                  string description, int warehouseId);
        Task InsertCostCenterTransactionAsync(DateTime date, decimal debit, decimal credit, string refId, string type, string description, int costCenterId);
        // CancellationToken ct = default);
        Task TransferSaleAsync(string formType, int invId, string poNoOrId);
        Task addTransactionEntry(DateOnly date, int? accountId, decimal debit, decimal credit,
                            int transactionId, int humId, string type, string voucher_name, string description, int createdBy, DateOnly createdDate, string VoucherNo);
        Task Transaction(TaxInvoiceViewModel Model, int level4PaymentCreditMethodId, int Invid, string invoiceNo, int level4VatId);

       // Task<bool> CheckItemAvailability(int? itemId, decimal salesQty);
    }
}

