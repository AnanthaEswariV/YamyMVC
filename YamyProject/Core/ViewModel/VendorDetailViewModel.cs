namespace YamyProject.Core.ViewModel
    {
    public class VendorDetailViewModel
        {
        public int Id { get; init; }
        public int Code { get; init; } 
        public string Name { get; init; } = "";
        public string? TRN { get; init; }
        public string? Email { get; init; }
        public string? WorkPhone { get; init; }
        public string? Category { get; init; }

        public string region { get; set; } = "";
        public string? Address { get; init; }
        public string? MainPhone { get; init; }
        public decimal balance { get; set; }
        }
    }
