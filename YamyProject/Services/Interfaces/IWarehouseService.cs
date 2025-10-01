namespace YamyProject.Services.Interfaces
{
    public interface IWarehouseService
    {
        Task<List<SelectListItem>> GetWarehousesAsync();
        Task<TblCoaConfig?> GetByCategoryAsync(string category);
        Task<int> GetInventoryAccountIdAsync();
        Task<int> GetStockSettlementAccountIdAsync();
        //Task<TblItemStockSettlement> CreateAsync(TblItemStockSettlement settlement);
        //---

        Task<string> GenerateNextCodeAsync();
        Task<TblItemStockSettlement> CreateAsync(TblItemStockSettlement settlement);
        //--
        Task<decimal> CreateAndGetIdAsync(TblItemStockSettlement settlement);
        Task<int> CreateTransactionAsync(TblTransaction transaction);
        //--

        Task<IEnumerable<StockSettlementDetailViewModel>> GetSettlementDetailsAsync(int settleId);


    }
}
