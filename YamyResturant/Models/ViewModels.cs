namespace YamyRestaurant.Models
{
    public class ViewModels
    {
    }
    public class CategoryRequest
    {
        public int Id { get; set; }

        public string? CategoryName { get; set; }
    }
    public class MenuItemRequest
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string SubCategoryName { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }

        public string MealTimesRaw { get; set; }

        public IFormFile ImageFile { get; set; }
    }

    public class TableRequest
    {
        public int Id { get; set; }

        public string? TableName { get; set; }

        public int Capacity { get; set; }

        public string? Location { get; set; }

        public string? Status { get; set; }

        public bool IsActive { get; set; }
    }

    public class OrderRequest
    {
        public int Id { get; set; }

        public int TableId { get; set; }

        public string? CustomerName { get; set; }

        public string? CustomerMobile { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal GrandTotal { get; set; }

        public string? PaymentStatus { get; set; }

        public string? OrderStatus { get; set; }

        public List<OrderItemRequest>? Items { get; set; }
    }
    public class OrderItemRequest
    {
        public int MenuItemId { get; set; }

        public string? ItemName { get; set; }

        public decimal Price { get; set; }

        public decimal Qty { get; set; }

        public decimal Amount { get; set; }
    }
}
