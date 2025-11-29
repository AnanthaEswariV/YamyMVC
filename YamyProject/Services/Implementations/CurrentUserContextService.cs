namespace YamyProject.Services.Implementations
    {
    public class CurrentUserContextService(IHttpContextAccessor httpContextAccessor) : ICurrentUserContextService 
        {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        public int? UserId => Session?.GetInt32("UserId");

        public string UserName => Session?.GetString("UserName") ?? string.Empty;

        public string DatabaseName => Session?.GetString("DatabaseName") ?? string.Empty;

        }
    }
