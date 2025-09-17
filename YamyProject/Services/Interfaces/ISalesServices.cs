using YamyProject.Core.ViewModel.YourApp.ViewModels;

namespace YamyProject.Services.Interfaces
{
    public interface ISalesServices
    {
        Task<int> CreateSaleAsync(SalesEditViewModel vm);
        Task<bool> UpdateSaleAsync(SalesEditViewModel vm);
        Task<SalesEditViewModel?> GetSaleForEditAsync(int id);
    }
}
