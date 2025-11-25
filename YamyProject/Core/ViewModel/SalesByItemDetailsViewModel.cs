namespace YamyProject.Core.ViewModel
    {
    public class SalesByItemDetailsViewModel
        {
        public int Id { get; set; }
        public string? DateFilter { get; set; } = "All"; // All, Today, ThisMonth, ThisYear, Custom
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }

        public List<ItemTypeGroupViewModel> Types { get; set; } = new();

        public bool HasRows => Types.Any();
        }
    }
