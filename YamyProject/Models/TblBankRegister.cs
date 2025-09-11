using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblBankRegister
{
    public int Id { get; set; }

    public int? BankId { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }
}
