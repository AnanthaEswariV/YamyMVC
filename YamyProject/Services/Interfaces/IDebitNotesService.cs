namespace YamyProject.Services.Interfaces
    {
    public interface IDebitNotesService
        {
        Task<MasterDebitNoteViewModel> QueryDebitNoteAsync( DateOnly from = default, DateOnly to = default, bool date = true, CancellationToken ct = default);
        Task<string> GenerateNextDebitNoteCode();
        Task CreateDebitNoteAsync(DebitNoteViewModel model);
        }
    }
