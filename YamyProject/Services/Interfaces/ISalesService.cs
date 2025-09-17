namespace YamyProject.Services.Interfaces
{
    public interface ISalesService
    {
        Task<int> CreateSaleAsync(SaleCreateViewModel dto, CancellationToken ct = default);
        Task UpdateSaleAsync(SaleEditViewModel ViewModel, CancellationToken ct = default);
        Task<SaleCreateViewModel?> GetSaleAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<SaleListItemViewModel>> QuerySalesAsync(DateTime? from, DateTime? to, int? customerId, string? paymentMethod, CancellationToken ct = default);
        Task DeleteSaleAsync(int id, CancellationToken ct = default);
    }
}
