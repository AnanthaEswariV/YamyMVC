
namespace YamyProject.Core.Models;

public partial class TblCity
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? CountryId { get; set; }
    [ForeignKey("CountryId")]
    public virtual TblCountry? Country { get; set; }
}
