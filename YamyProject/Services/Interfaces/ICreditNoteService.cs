namespace YamyProject.Services.Interfaces
    {
    public interface ICreditNoteService
        {
        Task<MasterCreditNoteViewModel> QueryCreditNoteAsync(string selectCustomer = null, bool Custmer = true, DateOnly? from = default, DateOnly? torom = default, bool date = true, CancellationToken ct = default);
        Task<string> GenerateNextCreditNoteCode();
        }
    }
