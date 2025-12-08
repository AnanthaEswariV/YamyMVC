namespace YamyProject.Services.Implementations
    {
    public interface IGlobalService
        {
        Task LogAudit(int UserId, string ActionType, string Module, int RecordId, string Details);
        Task<string> SelectDefaultLevelAccount( string accountName);
        }
    }
