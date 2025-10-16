namespace YamyProject.Core.ViewModel
{
    public class SalesRowDataViewModel
    {
        public int? Id { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Method { get; set; }
        public string Type { get; set; }
        public decimal QTY { get; set; }
        public decimal Price { get; set; }
        public decimal Disc { get; set; }
        public decimal NetPrice { get; set; }
        public decimal VatPersint { get; set; }
        public decimal VatAmonut { get; set; }
        public decimal Amount { get; set; }
        public decimal CostPrice { get; set; }
        public int WarehouseId { get; set; }

        public string CostCenter { get; set; }

        public int? CostCenterId { get; set; }
        
    }
}
