namespace YamyProject.Services.Interfaces
    {
    public interface IVendorService
        {
        Task<string> GenerateNextCode();
        Task CreateVendorOrSubcontractorAcync(VendorSubContactViewModel Model);
        Task UpDateVendorOrSubcontractorAcync(VendorSubContactViewModel Model);
        }
    }
