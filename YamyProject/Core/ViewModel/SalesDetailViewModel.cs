
namespace YamyProject.Core.ViewModel
{
     public class SalesDetailViewModel
    {
        public int? Id { get; set; } // if existing detail row
        [Required]
        public int ItemId { get; set; }
        [Required]
        [Range(0.00001, double.MaxValue)]
        public decimal Qty { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal Price { get; set; }
        public decimal? Vatp { get; set; }
        public int? Vat { get; set; }
        public decimal? Total { get; set; }
        public decimal? Discount { get; set; }
        public int? CostCenterId { get; set; }
        public string ItemMethod { get; set; } // fifo/lifo/avg
        public string ItemType { get; set; } // "11 - Inventory Part" etc.
    }


}
