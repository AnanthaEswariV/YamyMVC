namespace YamyProject.Core.ViewModel
    {
    public class ItemTypeGroupViewModel
        {
        public string TypeName { get; set; } = "";       // e.g. "11 - Inventory Part"
        public List<ItemCategoryGroupViewModel> Categories { get; set; } = new();
        }
    }
