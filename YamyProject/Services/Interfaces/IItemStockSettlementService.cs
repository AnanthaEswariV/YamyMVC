namespace YamyProject.Services.Interfaces
{
    public interface IItemStockSettlementService
    {
        Task<IEnumerable<ItemStockSettlementListVm>> GetSettlementsAsync(
        DateTime? from = null,DateTime? to = null,String defaultMode = "Default");
        Task<IEnumerable<ItemStockSettlementListVm>> GetSettlementsAsync(String defaultMode = "Default");

        Task<ItemStockSettlementDetailsVm?> GetSettlementDetailsAsync(int id);

        Task<int> CreateSettlementAsync(CreateUpdateSettlementVm model, int currentUserId);

        Task UpdateSettlementAsync(int id, CreateUpdateSettlementVm model, int currentUserId);

        Task DeleteSettlementAsync(int id, int currentUserId);
    }
}
