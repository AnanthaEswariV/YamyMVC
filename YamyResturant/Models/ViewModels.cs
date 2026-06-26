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

        public int? TableId { get; set; }
        public string OrderType { get; set; }

        public int? CustomerId { get; set; }

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
    public class CustomerRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int? CategoryId { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public DateTime? OpeningBalanceDate { get; set; }

        public string MainPhone { get; set; }
        public string WorkPhone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string CCEmail { get; set; }
        public string Website { get; set; }

        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        public string Region { get; set; }
        public string BuildingName { get; set; }
        public int? AccountId { get; set; }
        public string TRN { get; set; }
        public string FacilityName { get; set; }

        // ✅ FIX
        public bool Active { get; set; }

        // ✅ FIX: use int list
        public List<int> ProjectSites { get; set; } = new();
    }
}
