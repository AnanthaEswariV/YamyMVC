namespace YamyProject.Services.Interfaces
{
    public interface IStockSettlementService
    {
        Task<CreateUpdateSettlementViewmodel> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateUpdateSettlementViewmodel model, int userId);
        Task UpdateAsync(CreateUpdateSettlementViewmodel model, int userId);
        Task<List<TblWarehouse>> GetWarehousesAsync();
    }
}
