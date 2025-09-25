namespace YamyProject.Services.Implementations
{
    public class MicroserviceClientt: IMicroserviceClientt
    {
        private readonly HttpClient _http;
        private readonly ILogger<MicroserviceClient> _logger;

        public MicroserviceClientt(HttpClient http, ILogger<MicroserviceClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task NotifySettlementCreatedAsync(int settlementId, CancellationToken ct = default)
        {
            var payload = new { SettlementId = settlementId, Timestamp = DateTime.UtcNow };
            var response = await _http.PostAsJsonAsync("/api/settlement/notify-created", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Microservice notify returned {Status}", response.StatusCode);
                // decide: swallow or throw. For resilience, log and continue.
            }
        }
    }
}
