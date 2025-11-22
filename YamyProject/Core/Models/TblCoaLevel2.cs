using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblCoaLevel2
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Code { get; set; }

    public int MainId { get; set; }
    [ForeignKey("MainId")]
    public virtual TblCoaLevel1? Account { get; set; }

    }
