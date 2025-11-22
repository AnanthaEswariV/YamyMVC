namespace YamyProject.Core.ViewModel
    {
    public class CustomerAgingRowViewModel
        {
        public int Sn { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public decimal? Current { get; set; }      // 0_days
        public decimal? Days1To30 { get; set; }    // 1_30
        public decimal? Days31To60 { get; set; }   // 31_60
        public decimal? Days61To90 { get; set; }   // 61_90
        public decimal? Days91Plus { get; set; }   // 91_plus

        public decimal? Total =>
            Current + Days1To30 + Days31To60 + Days61To90 + Days91Plus;
        }
    }
