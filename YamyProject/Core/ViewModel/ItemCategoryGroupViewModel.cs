namespace YamyProject.Core.ViewModel
    {
    public class ItemCategoryGroupViewModel
        {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";   // e.g. "001 - Materials"
        public List<ItemGroupViewModel> Items { get; set; } = new();
        }
    }
