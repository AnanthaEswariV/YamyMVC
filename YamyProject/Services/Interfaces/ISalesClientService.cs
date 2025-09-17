namespace YamyProject.Services.Interfaces
{
    public interface ISalesClientService
    {
        Task SendSaleTransactionAsync(int saleId, CancellationToken ct = default);
    }
}
