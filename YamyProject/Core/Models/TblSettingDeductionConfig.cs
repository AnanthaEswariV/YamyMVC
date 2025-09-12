using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblSettingDeductionConfig
{
    public int Id { get; set; }

    public decimal? Latearrivaldeduction { get; set; }

    public TimeOnly? Delaytime { get; set; }
}
