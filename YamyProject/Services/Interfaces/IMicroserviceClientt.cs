namespace YamyProject.Services.Interfaces
{
    public interface IMicroserviceClientt
    {
        Task NotifySettlementCreatedAsync(int settlementId, CancellationToken ct = default);
    }
}
