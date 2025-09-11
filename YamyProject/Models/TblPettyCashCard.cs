using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblPettyCashCard
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public int? AccountId { get; set; }

    public string? Mobile { get; set; }

    public string? WhatsappNo { get; set; }

    public string? Email { get; set; }
}
