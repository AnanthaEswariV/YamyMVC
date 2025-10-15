namespace YamyProject.Core.ViewModel
{
    public class AssemblyComponentViewModel
    {
        public int ItemId { get; set; }
        public decimal Qty { get; set; }
        public string Method { get; set; } = "";
        public decimal? UnitPrice { get; init; }
        public decimal? Discount { get; init; }
        public decimal? VatPercent { get; init; }
    }
}
