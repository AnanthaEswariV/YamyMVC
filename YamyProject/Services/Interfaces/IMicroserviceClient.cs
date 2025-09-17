namespace YamyProject.Services.Interfaces
{
    public interface IMicroserviceClient
    {
        Task NotifyAccountingAsync(int saleId);
    }
}
