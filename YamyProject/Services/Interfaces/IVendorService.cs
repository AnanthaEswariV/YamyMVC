namespace YamyProject.Services.Interfaces
    {
    public interface IVendorService
        {
        string GenerateNextCode();
        Task CreateVendorOrSubcontractorAcync(VendorSubContactViewModel Model);
        Task UpDateVendorOrSubcontractorAcync(VendorSubContactViewModel Model);
        }
    }
