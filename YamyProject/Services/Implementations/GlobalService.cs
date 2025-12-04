namespace YamyProject.Services.Implementations
    {
    public class GlobalService(YamyDbContext context, ILogger<GlobalService> logger, IHttpContextAccessor httpContextAccessor) : IGlobalService
        {
        private readonly YamyDbContext _context = context;
        private readonly ILogger _logger =logger;
        private readonly IHttpContextAccessor _httpContextAccessor= httpContextAccessor;

        public async Task LogAudit(int UserId, string ActionType, string Module, int RecordId, string Details)
            {
            try
                {
                var log = new TblAuditLog
                    {
                    UserId = UserId,
                    ActionType = ActionType,
                    ModuleName = Module,
                    RecordId = RecordId,
                    Details = Details,
                    IpAddress = GetClientIpAddress(),
                    MachineName = Environment.MachineName,
                 //   CreatedAt = DateTime.UtcNow
                    };

                _context.TblAuditLogs.Add(log);
                await _context.SaveChangesAsync();
                }
            catch (Exception ex)
                {
                _logger.LogError(ex,
                    "Error while writing audit log. UserId={UserId}, Action={Action}, Module={Module}, RecordId={RecordId}",
                    UserId, ActionType, Module, RecordId);
                }
            }
        public string  GetClientIpAddress() 
            { 
             var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "127.0.0.1";

            // If behind a proxy / load balancer
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                // Can contain multiple IPs: "client, proxy1, proxy2"
                var firstIp = forwardedFor.Split(',').FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstIp))
                    return firstIp.Trim();
            }

            var ipAddress = httpContext.Connection.RemoteIpAddress;
            return ipAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
        }
        //public async Task LogErrorAsync(string procedureName, object message, int userId)
        //    {
        //    try
        //        {
        //        var errorLog = new TblErrorLog
        //            {
        //            ProcedureName = procedureName,
        //            ErrorMessage = message?.ToString() ?? string.Empty,
        //            UserId = userId,
        //            CreatedAt = DateTime.UtcNow
        //            };

        //        _context.TblErrorLogs.Add(errorLog);
        //        await _context.SaveChangesAsync();
        //        }
        //    catch (Exception ex)
        //        {
        //        // Here we only log to ILogger to avoid infinite recursion of logging errors into DB
        //        _logger.LogError(
        //            ex,
        //            "Failed to write error log. Procedure={Procedure}, UserId={UserId}",
        //            procedureName, userId
        //        );
        //        }
        //    }
        }
    }
