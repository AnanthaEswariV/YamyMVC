namespace YamyProject.Services.Implementations
{
    public class MicroserviceClient : IMicroserviceClient
    {
        private readonly HttpClient _httpClient;

        public MicroserviceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> CallServiceAsync(string endpoint)
        {
            var response = await _httpClient.GetStringAsync(endpoint);
            return response;
        }

        Task IMicroserviceClient.NotifyAccountingAsync(int saleId)
        {
            throw new NotImplementedException();
        }
    }
}
