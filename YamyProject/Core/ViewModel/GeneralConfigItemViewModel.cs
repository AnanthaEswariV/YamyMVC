namespace YamyProject.Core.ViewModel
    {
    public class GeneralConfigItemViewModel
        {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // This represents "status" 1/0 in your table
        public bool IsChecked { get; set; }
        }
    }
    
