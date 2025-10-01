namespace YamyProject.Core.ViewModel
{
    public class SharedListViewModel<T>
    {
        public string ControllerName { get; set; } = string.Empty;   // dynamic controller
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public T? Selected { get; set; }   // optional: for detail section
    }

}
