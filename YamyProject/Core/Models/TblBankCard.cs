using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblBankCard
{
    public int Id { get; set; }

    public int? BankId { get; set; }

    public string? AccountName { get; set; }

    public string? AccountType { get; set; }

    public string? AccountNo { get; set; }

    public string? Swift { get; set; }

    public string? IbanNo { get; set; }

    public string? BranchName { get; set; }

    public string? Emirates { get; set; }

    public string? Currency { get; set; }

    public string? AccountManager { get; set; }

    public string? AccountSign { get; set; }

    public string? AccountMob { get; set; }

    public int? AccountId { get; set; }

    public int? State { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? CompanyAc { get; set; }
}
