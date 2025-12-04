namespace YamyProject.Services.Implementations
{
    public class SalesClientService : ISalesClientService
    {
        private readonly HttpClient _http;
        private readonly ILogger<SalesClientService> _logger;

        public SalesClientService(HttpClient http, ILogger<SalesClientService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task SendSaleTransactionAsync(int saleId, CancellationToken ct = default)
        {
            var payload = new { SaleId = saleId };
            var res = await System.Net.Http.Json.HttpClientJsonExtensions
                .PostAsJsonAsync(_http, "/transactions/sales", payload, ct);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Microservice returned {Status} when sending sale transaction {SaleId}", res.StatusCode, saleId);
              
            }
        }
    }
}
