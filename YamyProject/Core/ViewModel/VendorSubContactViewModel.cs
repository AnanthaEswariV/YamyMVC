namespace YamyProject.Core.ViewModel
    {
    public class VendorSubContactViewModel
        {
        public int Id { get; set; }
        public string Type { get; set; } 
        public string Name { get; set; }
        public string Code { get; set; }
        public IEnumerable<TblVendorCategory> Categoriess { get; set; } = new List<TblVendorCategory>();
        public int CategoryId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal OpeningAmount { get; set; }
        public DateOnly Date {  get; set; }
        public string MainPhone { get; set; }
        public string WorkPhone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string EmailCC { get; set; }
        public string Website { get; set; }
        public IEnumerable<TblCountry> Country { get; set; } = new List<TblCountry>();
        public int CountryId { get; set; }
        public IEnumerable<TblCity> City { get; set; } = new List<TblCity>();
        public int CityId { get; set; }
        public string Region { get; set; }
        public string BulidingNumber { get; set; }
        public IEnumerable<TblCoaLevel4> Account { get; set; } = new List<TblCoaLevel4>();
        public int AccountId { get; set; }
        public string TRN { get; set; }
        public string FaciltyName { get; set; }
        public bool IsActive {get; set ;}
        public IEnumerable<TblProject> Project { get; set; } = new List<TblProject>();
        public int ProjectId { get; set; }
        }
    }