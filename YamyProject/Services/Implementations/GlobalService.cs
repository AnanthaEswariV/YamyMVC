using System.Net;
using System.Net.Sockets;

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
                    IpAddress = GetServerIpAddress(),
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
        public string GetServerIpAddress()
            {
            // First, try the IP bound to the server for this request
            var httpContext = _httpContextAccessor.HttpContext;
            var localIp = httpContext?.Connection.LocalIpAddress;

            if (localIp != null && localIp.AddressFamily == AddressFamily.InterNetwork)
                return localIp.ToString();

            // Fallback: resolve host machine IPs
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
                }

            return "127.0.0.1";
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

        public async Task<string> SelectDefaultLevelAccount( string accountName)
            {
            var Account = await _context.TblCoaLevel4s
            .AsNoTracking()
            .Where(x => x.Name == accountName)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
            return Account == 0 ? "0" : Account.ToString(); 
                }
    }
 }
    
