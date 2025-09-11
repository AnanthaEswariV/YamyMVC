using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblCrmcustomer
{
    public int Id { get; set; }

    public string? LeadName { get; set; }

    public int? Custcode { get; set; }

    public string? CustName { get; set; }

    public string? Openlvl { get; set; }

    public string? Stage { get; set; }

    public DateTime? Date { get; set; }

    public decimal? Amount { get; set; }

    public string? Discription { get; set; }

    public string? Assigendto { get; set; }

    public DateTime? CreateAt { get; set; }
}
