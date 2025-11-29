namespace YamyProject.Services.Interfaces
    {
    public interface ICurrentUserContextService
        {
        int? UserId { get; }
        string UserName { get; }
        string DatabaseName { get; }
        //int RolsId { get; set; }
        }
    }
