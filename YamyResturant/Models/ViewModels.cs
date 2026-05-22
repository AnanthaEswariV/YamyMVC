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
    // ===== REQUEST MODEL =====
    public class MenuItemRequest
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }

        public string SubCategoryName { get; set; }

        public List<string> MealTimes { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; }
    }
}
