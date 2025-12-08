namespace YamyProject.Services.Interfaces
{
    public interface IItemStockSettlementService
    {
        Task<IEnumerable<ItemStockSettlementListVm>> GetSettlementsAsync(
        DateTime? from = null,DateTime? to = null,String defaultMode = "Default");
        Task<IEnumerable<ItemStockSettlementListVm>> GetSettlementsAsync(String defaultMode = "Default");

        Task<ItemStockSettlementDetailsVm?> GetSettlementDetailsAsync(int id);

        public  Task<CreateUpdateSettlementVm> GetCreateUpdateSettlementVmAsync();

        Task<int> CreateSettlementAsync(CreateUpdateSettlementVm model);

        Task UpdateSettlementAsync(int id, CreateUpdateSettlementVm model);

        Task DeleteSettlementAsync(int id);
        Task<List<TblItem>> GetItemsByCodeAsync(string code, int warehouseId);
        Task<List<TblItem>> GetItemsByNameAsync(string name, int warehouseId);
        Task<List<ItemViewModel>> GetItemsAsync(string name, int warehouseId);

        Task<List<ItemStockSettlementListVm>> GetSettlementItemsAsync(int settleId);

        Task<string> GenerateNextCode();




    }
}
