namespace YamyProject.Services.Interfaces
    {
    public interface IVendorsCenterService
        {
        Task<IEnumerable<PurchaseRowViewModel>> GetPurchaseAsync(bool Subcontractors = false, string selectCustomer = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true, string VendorType = "");
         Task<string> GenerateVendorsInvoiceNoAsync();

        Task<PurchaseInvoiceViewModel> GetEditAsync(int id, string formType = "", string VendorType = "");
        Task CreateTaxInvoiceAsync(PurchaseInvoiceViewModel vm, int currentUserId);
        Task UpdateTaxInvoiceAsync(PurchaseInvoiceViewModel vm, int currentUserId);


        }
    }
