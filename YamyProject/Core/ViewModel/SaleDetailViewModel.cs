namespace YamyProject.Core.ViewModel
{
    public class SaleDetailViewModel
    {
        public int? Id { get; init; }             // existing DB id (null for new)
        public int ItemId { get; init; }          // maps to TblSalesDetail.ItemId
        public decimal Qty { get; init; }
        public decimal Price { get; init; }
        public decimal? VatPercent { get; init; }
        public decimal Total { get; init; }
        public decimal? Discount { get; init; }
    }
}
