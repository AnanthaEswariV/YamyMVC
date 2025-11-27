namespace YamyProject.Services.Interfaces
    {
    public interface IVatCorporateService
        {
         Task<VatCorporateViewModel> GetVatCorporateAsync(DateOnly from, DateOnly to);
        }
    }
