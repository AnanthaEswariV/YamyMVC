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
}
