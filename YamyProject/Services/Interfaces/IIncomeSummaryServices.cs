namespace YamyProject.Services.Interfaces
    {
    public interface IIncomeSummaryServices
        {
        Task<IEnumerable<IncomeSummaryRowViewModel>> GetIncomeByCustomerSummary(/*DateOnly dateFrom, DateOnly dateTo, bool All = true*/);
        Task<IEnumerable<IncomeByVendorSummaryRowViewModel>> BuildIncomeByVendorSummary(/*DateOnly? fromDate, DateOnly? toDate, bool isAll = true*/);
        Task<IEnumerable<IncomeByCustomerRowViewModel>> GetIncomeByCustomerDetail(int customerId);
        Task<IEnumerable<IncomeByCustomerRowViewModel>> GetIncomeByVendorDetail(int customerId);
        Task<List<EquityBalanceRowViewModel>> GetEquityWithFinalBalanceAsync();
        Task<List<CashFlowRowViewModel>> GetCashFlowStatementAsync(/*DateOnly startDate, DateOnly endDate*/);
        Task<List<IncomeExpenseRowViewModel>> GetIncomeExpenseStatementAsync(/*DateOnly startDate, DateOnly endDate*/);
           Task<List<UserActivityRowViewModel>> GetUserActivityAsync( /*DateOnly startDate, DateOnly endDat*/);

        }
    }