namespace YamyProject.Core.Models;

public partial class YamyDbContext : DbContext
{
    public YamyDbContext()
    {
    }
    public YamyDbContext(DbContextOptions<YamyDbContext> options) : base(options)
    {
    }

    public virtual DbSet<TblAdvancePaymentVoucher> TblAdvancePaymentVouchers { get; set; }

    public virtual DbSet<TblAdvancePaymentVoucherDetail> TblAdvancePaymentVoucherDetails { get; set; }

    public virtual DbSet<TblAttendanceSalary> TblAttendanceSalaries { get; set; }

    public virtual DbSet<TblAttendancesheet> TblAttendancesheets { get; set; }

    public virtual DbSet<TblAuditLog> TblAuditLogs { get; set; }

    public virtual DbSet<TblBank> TblBanks { get; set; }

    public virtual DbSet<TblBankCard> TblBankCards { get; set; }

    public virtual DbSet<TblBankRegister> TblBankRegisters { get; set; }

    public virtual DbSet<TblCheckDetail> TblCheckDetails { get; set; }

    public virtual DbSet<TblCheque> TblCheques { get; set; }

    public virtual DbSet<TblCity> TblCities { get; set; }

    public virtual DbSet<TblCoa> TblCoas { get; set; }

    public virtual DbSet<TblCoaConfig> TblCoaConfigs { get; set; }

    public virtual DbSet<TblCoaLevel1> TblCoaLevel1s { get; set; }

    public virtual DbSet<TblCoaLevel2> TblCoaLevel2s { get; set; }

    public virtual DbSet<TblCoaLevel3> TblCoaLevel3s { get; set; }

    public virtual DbSet<TblCoaLevel4> TblCoaLevel4s { get; set; }

    public virtual DbSet<TblColor> TblColors { get; set; }

    public virtual DbSet<TblCompany> TblCompanies { get; set; }

    public virtual DbSet<TblContractor> TblContractors { get; set; }

    public virtual DbSet<TblCorporateTaxConfigration> TblCorporateTaxConfigrations { get; set; }

    public virtual DbSet<TblCostCenter> TblCostCenters { get; set; }

    public virtual DbSet<TblCostCenterTransaction> TblCostCenterTransactions { get; set; }

    public virtual DbSet<TblCountry> TblCountries { get; set; }

    public virtual DbSet<TblCreditNote> TblCreditNotes { get; set; }

    public virtual DbSet<TblCreditNoteDetail> TblCreditNoteDetails { get; set; }

    public virtual DbSet<TblCrmcustomer> TblCrmcustomers { get; set; }

    public virtual DbSet<TblCustomer> TblCustomers { get; set; }

    public virtual DbSet<TblCustomerCategory> TblCustomerCategories { get; set; }

    public virtual DbSet<TblDamage> TblDamages { get; set; }

    public virtual DbSet<TblDamageDetail> TblDamageDetails { get; set; }

    public virtual DbSet<TblDebitNote> TblDebitNotes { get; set; }

    public virtual DbSet<TblDebitNoteDetail> TblDebitNoteDetails { get; set; }

    public virtual DbSet<TblDeletedRecord> TblDeletedRecords { get; set; }

    public virtual DbSet<TblDepartment> TblDepartments { get; set; }

    public virtual DbSet<TblEmployee> TblEmployees { get; set; }

    public virtual DbSet<TblEndOfService> TblEndOfServices { get; set; }

    public virtual DbSet<TblFinalSettlement> TblFinalSettlements { get; set; }

    public virtual DbSet<TblFixedAsset> TblFixedAssets { get; set; }

    public virtual DbSet<TblFixedAssetsCategory> TblFixedAssetsCategories { get; set; }

    public virtual DbSet<TblGeneralSetting> TblGeneralSettings { get; set; }

    public virtual DbSet<TblItem> TblItems { get; set; }

    public virtual DbSet<TblItemAssembly> TblItemAssemblies { get; set; }

    public virtual DbSet<TblItemAssemblyBo> TblItemAssemblyBos { get; set; }

    public virtual DbSet<TblItemCardDetail> TblItemCardDetails { get; set; }

    public virtual DbSet<TblItemCategory> TblItemCategories { get; set; }

    public virtual DbSet<TblItemStockSettlement> TblItemStockSettlements { get; set; }

    public virtual DbSet<TblItemStockSettlementDetail> TblItemStockSettlementDetails { get; set; }

    public virtual DbSet<TblItemTransaction> TblItemTransactions { get; set; }

    public virtual DbSet<TblItemWarehouseTransaction> TblItemWarehouseTransactions { get; set; }

    public virtual DbSet<TblItemsBoq> TblItemsBoqs { get; set; }

    public virtual DbSet<TblItemsBoqDetail> TblItemsBoqDetails { get; set; }

    public virtual DbSet<TblItemsUnit> TblItemsUnits { get; set; }

    public virtual DbSet<TblItemsWarehouse> TblItemsWarehouses { get; set; }

    public virtual DbSet<TblJournalVoucher> TblJournalVouchers { get; set; }

    public virtual DbSet<TblJournalVoucherDetail> TblJournalVoucherDetails { get; set; }

    public virtual DbSet<TblLeaveSalary> TblLeaveSalaries { get; set; }

    public virtual DbSet<TblLedger> TblLedgers { get; set; }

    public virtual DbSet<TblLoan> TblLoans { get; set; }

    public virtual DbSet<TblMainMenu> TblMainMenus { get; set; }

    public virtual DbSet<TblManufacturerBatch> TblManufacturerBatches { get; set; }

    public virtual DbSet<TblManufacturerBatchdetail> TblManufacturerBatchdetails { get; set; }

    public virtual DbSet<TblManufacturerTask> TblManufacturerTasks { get; set; }

    public virtual DbSet<TblManufacturerTaskDetail> TblManufacturerTaskDetails { get; set; }

    public virtual DbSet<TblPaymentVoucher> TblPaymentVouchers { get; set; }

    public virtual DbSet<TblPaymentVoucherDetail> TblPaymentVoucherDetails { get; set; }

    public virtual DbSet<TblPettyCash> TblPettyCashes { get; set; }

    public virtual DbSet<TblPettyCashCard> TblPettyCashCards { get; set; }

    public virtual DbSet<TblPettyCashCategory> TblPettyCashCategories { get; set; }

    public virtual DbSet<TblPettyCashDetail> TblPettyCashDetails { get; set; }

    public virtual DbSet<TblPettyCashRequest> TblPettyCashRequests { get; set; }

    public virtual DbSet<TblPettyCashSubmition> TblPettyCashSubmitions { get; set; }

    public virtual DbSet<TblPettyCashSubmitionDetail> TblPettyCashSubmitionDetails { get; set; }

    public virtual DbSet<TblPosition> TblPositions { get; set; }

    public virtual DbSet<TblPrepaidExpense> TblPrepaidExpenses { get; set; }

    public virtual DbSet<TblPrepaidExpenseCategory> TblPrepaidExpenseCategories { get; set; }

    public virtual DbSet<TblPrintConfig> TblPrintConfigs { get; set; }

    public virtual DbSet<TblPrintconfg> TblPrintconfgs { get; set; }

    public virtual DbSet<TblProject> TblProjects { get; set; }

    public virtual DbSet<TblProjectActivity> TblProjectActivities { get; set; }

    public virtual DbSet<TblProjectActivityAssignment> TblProjectActivityAssignments { get; set; }

    public virtual DbSet<TblProjectEstimate> TblProjectEstimates { get; set; }

    public virtual DbSet<TblProjectManagement> TblProjectManagements { get; set; }

    public virtual DbSet<TblProjectMaterialRequest> TblProjectMaterialRequests { get; set; }

    public virtual DbSet<TblProjectPlan> TblProjectPlans { get; set; }

    public virtual DbSet<TblProjectPlanning> TblProjectPlannings { get; set; }

    public virtual DbSet<TblProjectResource> TblProjectResources { get; set; }

    public virtual DbSet<TblProjectRole> TblProjectRoles { get; set; }

    public virtual DbSet<TblProjectSite> TblProjectSites { get; set; }

    public virtual DbSet<TblProjectTender> TblProjectTenders { get; set; }

    public virtual DbSet<TblProjectTenderDetail> TblProjectTenderDetails { get; set; }

    public virtual DbSet<TblProjectWorkDone> TblProjectWorkDones { get; set; }

    public virtual DbSet<TblProjectWorkDoneDetail> TblProjectWorkDoneDetails { get; set; }

    public virtual DbSet<TblPurchase> TblPurchases { get; set; }

    public virtual DbSet<TblPurchaseDetail> TblPurchaseDetails { get; set; }

    public virtual DbSet<TblPurchaseOrder> TblPurchaseOrders { get; set; }

    public virtual DbSet<TblPurchaseOrderDetail> TblPurchaseOrderDetails { get; set; }

    public virtual DbSet<TblPurchaseReturn> TblPurchaseReturns { get; set; }

    public virtual DbSet<TblPurchaseReturnDetail> TblPurchaseReturnDetails { get; set; }

    public virtual DbSet<TblReceiptVoucher> TblReceiptVouchers { get; set; }

    public virtual DbSet<TblReceiptVoucherDetail> TblReceiptVoucherDetails { get; set; }

    public virtual DbSet<TblRmsdetail> TblRmsdetails { get; set; }

    public virtual DbSet<TblRmsmain> TblRmsmains { get; set; }

    public virtual DbSet<TblRmstable> TblRmstables { get; set; }

    public virtual DbSet<TblSalary> TblSalaries { get; set; }

    public virtual DbSet<TblSalaryAdjustment> TblSalaryAdjustments { get; set; }

    public virtual DbSet<TblSale> TblSales { get; set; }

    public virtual DbSet<TblSalesDetail> TblSalesDetails { get; set; }

    public virtual DbSet<TblSalesOrder> TblSalesOrders { get; set; }

    public virtual DbSet<TblSalesOrderDetail> TblSalesOrderDetails { get; set; }

    public virtual DbSet<TblSalesProforma> TblSalesProformas { get; set; }

    public virtual DbSet<TblSalesProformaDetail> TblSalesProformaDetails { get; set; }

    public virtual DbSet<TblSalesQuotation> TblSalesQuotations { get; set; }

    public virtual DbSet<TblSalesQuotationDetail> TblSalesQuotationDetails { get; set; }

    public virtual DbSet<TblSalesReturn> TblSalesReturns { get; set; }

    public virtual DbSet<TblSalesReturnDetail> TblSalesReturnDetails { get; set; }

    public virtual DbSet<TblSecRole> TblSecRoles { get; set; }

    public virtual DbSet<TblSecRoleForm> TblSecRoleForms { get; set; }

    public virtual DbSet<TblSecUser> TblSecUsers { get; set; }

    public virtual DbSet<TblSetMainMenu> TblSetMainMenus { get; set; }

    public virtual DbSet<TblSetMenuForm> TblSetMenuForms { get; set; }

    public virtual DbSet<TblSettingAttendance> TblSettingAttendances { get; set; }

    public virtual DbSet<TblSettingDeductionConfig> TblSettingDeductionConfigs { get; set; }

    public virtual DbSet<TblSettingDefaultAccount> TblSettingDefaultAccounts { get; set; }

    public virtual DbSet<TblSubCostCenter> TblSubCostCenters { get; set; }

    public virtual DbSet<TblSubMenu> TblSubMenus { get; set; }

    public virtual DbSet<TblTax> TblTaxes { get; set; }

    public virtual DbSet<TblTenderName> TblTenderNames { get; set; }

    public virtual DbSet<TblTool> TblTools { get; set; }

    public virtual DbSet<TblTransaction> TblTransactions { get; set; }

    public virtual DbSet<TblUnit> TblUnits { get; set; }

    public virtual DbSet<TblUserPermission> TblUserPermissions { get; set; }

    public virtual DbSet<TblVatConfigration> TblVatConfigrations { get; set; }

    public virtual DbSet<TblVendor> TblVendors { get; set; }

    public virtual DbSet<TblVendorCategory> TblVendorCategories { get; set; }

    public virtual DbSet<TblWarehouse> TblWarehouses { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    => optionsBuilder.UseMySql("name=YamyDb1", ServerVersion.Parse("8.0.30-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<TblAdvancePaymentVoucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_advance_payment_voucher")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 4)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CreditAccountId).HasColumnName("credit_account_id");
            entity.Property(e => e.CreditCostCenterId).HasColumnName("credit_cost_center_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DebitAccountId).HasColumnName("debit_account_id");
            entity.Property(e => e.DebitCostCenterId).HasColumnName("debit_cost_center_id");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .HasColumnName("method");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.PvCode)
                .HasMaxLength(50)
                .HasColumnName("pv_code");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
        });

        modelBuilder.Entity<TblAdvancePaymentVoucherDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_advance_payment_voucher_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 6)
                .HasColumnName("amount");
            entity.Property(e => e.BankAccountName)
                .HasMaxLength(500)
                .HasColumnName("bank_account_name");
            entity.Property(e => e.BankName)
                .HasMaxLength(500)
                .HasColumnName("bank_name");
            entity.Property(e => e.BookNo).HasColumnName("book_no");
            entity.Property(e => e.CheckDate).HasColumnName("check_date");
            entity.Property(e => e.CheckName)
                .HasMaxLength(250)
                .HasColumnName("check_name");
            entity.Property(e => e.CheckNo).HasColumnName("check_no");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasColumnName("name");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.TransDate).HasColumnName("trans_date");
            entity.Property(e => e.TransName)
                .HasMaxLength(250)
                .HasColumnName("trans_name");
            entity.Property(e => e.TransRef)
                .HasMaxLength(250)
                .HasColumnName("trans_ref");
        });

        modelBuilder.Entity<TblAttendanceSalary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_attendance_salary")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AbsenceDays).HasColumnName("absence_days");
            entity.Property(e => e.Change)
                .HasPrecision(20, 6)
                .HasColumnName("change");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DelayMinutes)
                .HasPrecision(20, 6)
                .HasColumnName("delay_minutes");
            entity.Property(e => e.EmpCode)
                .HasMaxLength(50)
                .HasColumnName("emp_code");
            entity.Property(e => e.NetSalary)
                .HasPrecision(20, 6)
                .HasColumnName("net_salary");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 6)
                .HasColumnName("pay");
            entity.Property(e => e.SsNo)
                .HasDefaultValueSql("'0'")
                .HasColumnName("ss_no");
            entity.Property(e => e.TotalAbsence)
                .HasPrecision(20, 6)
                .HasColumnName("total_absence");
            entity.Property(e => e.TotalDelay)
                .HasPrecision(20, 6)
                .HasColumnName("total_delay");
            entity.Property(e => e.TotalLoan)
                .HasPrecision(20, 6)
                .HasColumnName("total_loan");
        });

        modelBuilder.Entity<TblAttendancesheet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_attendancesheet")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AttendanceSalaryId).HasColumnName("attendance_salary_id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.DayOfWeek).HasMaxLength(10);
            entity.Property(e => e.RefCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("Ref_Code");
            entity.Property(e => e.Reference).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.TimeIn).HasColumnType("time");
            entity.Property(e => e.TimeOut).HasColumnType("time");
        });

        modelBuilder.Entity<TblAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_audit_log")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActionTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("action_time");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .HasColumnName("action_type");
            entity.Property(e => e.Details)
                .HasColumnType("text")
                .HasColumnName("details");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.MachineName)
                .HasMaxLength(100)
                .HasColumnName("machine_name");
            entity.Property(e => e.ModuleName)
                .HasMaxLength(100)
                .HasColumnName("module_name");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<TblBank>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_bank")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AbbName)
                .HasMaxLength(50)
                .HasColumnName("abb_name");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CountryId).HasColumnName("country_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.EntId)
                .HasMaxLength(50)
                .HasColumnName("ent_id");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.RouteNum)
                .HasMaxLength(50)
                .HasColumnName("route_num");
            entity.Property(e => e.State).HasColumnName("state");
        });

        modelBuilder.Entity<TblBankCard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_bank_card")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.AccountManager)
                .HasMaxLength(50)
                .HasColumnName("account_manager");
            entity.Property(e => e.AccountMob)
                .HasMaxLength(50)
                .HasColumnName("account_mob");
            entity.Property(e => e.AccountName)
                .HasMaxLength(70)
                .HasColumnName("account_name");
            entity.Property(e => e.AccountNo)
                .HasMaxLength(50)
                .HasColumnName("account_no");
            entity.Property(e => e.AccountSign)
                .HasMaxLength(50)
                .HasColumnName("account_sign");
            entity.Property(e => e.AccountType)
                .HasMaxLength(50)
                .HasColumnName("account_type");
            entity.Property(e => e.BankId).HasColumnName("bank_id");
            entity.Property(e => e.BranchName)
                .HasMaxLength(50)
                .HasColumnName("branch_name");
            entity.Property(e => e.CompanyAc)
                .HasDefaultValueSql("'0'")
                .HasColumnName("company_ac");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Currency)
                .HasMaxLength(50)
                .HasColumnName("currency");
            entity.Property(e => e.Emirates)
                .HasMaxLength(50)
                .HasColumnName("emirates");
            entity.Property(e => e.IbanNo)
                .HasMaxLength(50)
                .HasColumnName("iban_no");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Swift)
                .HasMaxLength(50)
                .HasColumnName("swift");
        });

        modelBuilder.Entity<TblBankRegister>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_bank_register")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BankId).HasColumnName("bank_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
        });

        modelBuilder.Entity<TblCheckDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_check_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 6)
                .HasColumnName("amount");
            entity.Property(e => e.CancelDate).HasColumnName("cancel_date");
            entity.Property(e => e.CheckDate).HasColumnName("check_date");
            entity.Property(e => e.CheckId).HasColumnName("check_id");
            entity.Property(e => e.CheckName)
                .HasMaxLength(50)
                .HasColumnName("check_name");
            entity.Property(e => e.CheckNo).HasColumnName("check_no");
            entity.Property(e => e.CheckType)
                .HasMaxLength(50)
                .HasColumnName("check_type");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.HoldDate).HasColumnName("hold_date");
            entity.Property(e => e.PassDate).HasColumnName("pass_date");
            entity.Property(e => e.PvcNo).HasColumnName("pvc_no");
            entity.Property(e => e.ReturnDate).HasColumnName("return_date");
            entity.Property(e => e.State)
                .HasMaxLength(50)
                .HasColumnName("state");
        });

        modelBuilder.Entity<TblCheque>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_cheque")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BankCardId).HasColumnName("bank_card_id");
            entity.Property(e => e.ChqBookNo).HasColumnName("chq_book_no");
            entity.Property(e => e.ChqBookQty).HasColumnName("chq_book_qty");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.LeavesEndIn)
                .HasMaxLength(100)
                .HasColumnName("leaves_end_in");
            entity.Property(e => e.LeavesStartFrom)
                .HasMaxLength(100)
                .HasColumnName("leaves_start_from");
        });

        modelBuilder.Entity<TblCity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_city")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CountryId).HasColumnName("country_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblCoa>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_coa")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.AccountCode, "account_code").IsUnique();

            entity.HasIndex(e => e.ParentId, "parent_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountCategory)
                .HasMaxLength(50)
                .HasColumnName("account_category");
            entity.Property(e => e.AccountCode)
                .HasMaxLength(20)
                .HasColumnName("account_code");
            entity.Property(e => e.AccountName)
                .HasMaxLength(100)
                .HasColumnName("account_name");
            entity.Property(e => e.AccountType)
                .HasColumnType("enum('Asset','Liability','Equity','Revenue','Expense')")
                .HasColumnName("account_type");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.IsGroup)
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_group");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("tbl_coa_ibfk_1");
        });

        modelBuilder.Entity<TblCoaConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_coa_config")
                .UseCollation("utf8mb4_general_ci");

            entity.HasIndex(e => e.Category, "category").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Category)
                .HasMaxLength(150)
                .HasColumnName("category");
        });

        modelBuilder.Entity<TblCoaLevel1>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_coa_level_1")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryCode)
                .HasMaxLength(50)
                .HasColumnName("category_code");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblCoaLevel2>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_coa_level_2")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.MainId).HasColumnName("main_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblCoaLevel3>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_coa_level_3")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.MainId).HasColumnName("main_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblCoaLevel4>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_coa_level_4")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Credit)
                .HasPrecision(20, 3)
                .HasDefaultValueSql("'0.000'")
                .HasColumnName("credit");
            entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .HasColumnName("date");
            entity.Property(e => e.Debit)
                .HasPrecision(20, 3)
                .HasDefaultValueSql("'0.000'")
                .HasColumnName("debit");
            entity.Property(e => e.MainId).HasColumnName("main_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblColor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_color")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HeaderColor)
                .HasMaxLength(50)
                .HasDefaultValueSql("'White'")
                .HasColumnName("headerColor");
            entity.Property(e => e.TextColor)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Black'");
        });

        modelBuilder.Entity<TblCompany>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_company")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .HasDefaultValueSql("''")
                .HasColumnName("address");
            // entity.Property(e => e.CountryId).HasColumnName("country_id");
            //  entity.Ignore(e => e.CountryId);

            entity.Property(e => e.Descriptions)
                .HasMaxLength(200)
                .HasDefaultValueSql("''")
                .HasColumnName("descriptions");
            entity.Property(e => e.Gmail)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("gmail");
            entity.Property(e => e.LogoComp).HasColumnName("logoComp");
            entity.Property(e => e.MobileNumber)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("mobile_number");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.Phone1)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("phone1");
            entity.Property(e => e.Phone2)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("phone2");
            entity.Property(e => e.TrnNo)
                .HasMaxLength(100)
                .HasDefaultValueSql("''")
                .HasColumnName("trn_no");
            entity.Property(e => e.Website)
                .HasMaxLength(100)
                .HasDefaultValueSql("''")
                .HasColumnName("website");
        });

        modelBuilder.Entity<TblContractor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_contractor")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CountryId).HasColumnName("country_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblCorporateTaxConfigration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_corporate_tax_configration")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CorporateTaxNo)
                .HasMaxLength(100)
                .HasDefaultValueSql("'0'")
                .HasColumnName("corporateTax_no");
            entity.Property(e => e.CorporatetaxDueDate).HasColumnName("corporatetax_due_date");
            entity.Property(e => e.CorporatetaxEndDate).HasColumnName("corporatetax_end_date");
            entity.Property(e => e.CorporatetaxStartDate).HasColumnName("corporatetax_start_date");
            entity.Property(e => e.TrnIssueDate).HasColumnName("trn_issue_date");
        });

        modelBuilder.Entity<TblCostCenter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_cost_center")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
        });

        modelBuilder.Entity<TblCostCenterTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_cost_center_transaction")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("cost_center_id");
            entity.Property(e => e.Credit)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("credit");
            entity.Property(e => e.Date)
                .HasColumnType("DateOnly")
                .HasColumnName("date");
            entity.Property(e => e.Debit)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("debit");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasDefaultValueSql("''")
                .HasColumnName("description");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RefId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("ref_id");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("type");

        });
        //modelBuilder.Entity<TblCostCenterTransaction>()
        //    .Ignore("PaymentVoucherId");

        modelBuilder.Entity<TblCostCenterTransaction>(e =>
{
    e.HasOne(x => x.PaymentVoucher)
     .WithMany()
     .HasForeignKey(x => x.RefId); // <-- real DB column (NOT PaymentVoucherId)
});
        //modelBuilder.Entity<TblPaymentVoucherDetail>(e =>
        //{
        //    e.HasOne(d => d.PaymentVoucher)
        //   //  .WithMany(p => p.PaymentVoucherDetails)
        //     .HasForeignKey(d => d.PaymentId)     // ✅ your real FK column
        //     .HasPrincipalKey(p => p.Id);
        //});

        modelBuilder.Entity<TblCountry>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_country")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblCreditNote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_credit_note")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CreditAccount)
                .HasDefaultValueSql("'0'")
                .HasColumnName("credit_account");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DebitAccount).HasColumnName("debit_account");
            entity.Property(e => e.Description)
                .HasMaxLength(150)
                .HasDefaultValueSql("''")
                .HasColumnName("description");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("total");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("type");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("vat");
        });

        modelBuilder.Entity<TblCreditNoteDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_credit_note_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Balance)
                .HasPrecision(20, 2)
                .HasColumnName("balance");
            entity.Property(e => e.InvNo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("inv_no");
            entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.InvoiceType)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_type");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RefId).HasColumnName("ref_id");
            entity.Property(e => e.Remaining)
                .HasPrecision(20, 2)
                .HasColumnName("remaining");
            entity.Property(e => e.Total)
                .HasPrecision(20, 2)
                .HasColumnName("total");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 2)
                .HasColumnName("vat");
        });

        modelBuilder.Entity<TblCrmcustomer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_crmcustomer")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Amount).HasPrecision(15, 2);
            entity.Property(e => e.Assigendto).HasMaxLength(100);
            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.CustName).HasMaxLength(50);
            entity.Property(e => e.Custcode).HasColumnName("custcode");
            entity.Property(e => e.Date).HasColumnType("datetime");
            entity.Property(e => e.Discription).HasMaxLength(1000);
            entity.Property(e => e.LeadName).HasMaxLength(50);
            entity.Property(e => e.Openlvl)
                .HasMaxLength(50)
                .HasColumnName("openlvl");
            entity.Property(e => e.Stage).HasMaxLength(50);
        });

        modelBuilder.Entity<TblCustomer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_customer")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Balance).HasPrecision(20, 4);
            entity.Property(e => e.BuildingName)
                .HasMaxLength(50)
                .HasColumnName("building_name");
            entity.Property(e => e.CatId).HasColumnName("Cat_id");
            entity.Property(e => e.Ccemail)
                .HasMaxLength(50)
                .HasColumnName("ccemail");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasColumnName("city");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Country)
                .HasMaxLength(50)
                .HasColumnName("country");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.FaciltyName)
                .HasMaxLength(50)
                .HasColumnName("facilty_name");
            entity.Property(e => e.MainPhone)
                .HasMaxLength(20)
                .HasColumnName("main_phone");
            entity.Property(e => e.Mobile)
                .HasMaxLength(50)
                .HasColumnName("mobile");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectSite)
                .HasMaxLength(150)
                .HasDefaultValueSql("''")
                .HasColumnName("project_site");
            entity.Property(e => e.Region)
                .HasMaxLength(50)
                .HasColumnName("region");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Trn)
                .HasMaxLength(50)
                .HasColumnName("trn");
            entity.Property(e => e.Website)
                .HasMaxLength(50)
                .HasColumnName("website");
            entity.Property(e => e.WorkPhone)
                .HasMaxLength(20)
                .HasColumnName("work_phone");
        });

        modelBuilder.Entity<TblCustomerCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_customer_category")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblDamage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_damage")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.DamageReason)
                .HasMaxLength(255)
                .HasDefaultValueSql("'0'")
                .HasColumnName("damage_reason");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.ReferenceNo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("reference_no");
            entity.Property(e => e.ReportedBy).HasColumnName("reported_by");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblDamageDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_damage_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 4)
                .HasColumnName("cost_price");
            entity.Property(e => e.DamageId).HasColumnName("damage_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
        });

        modelBuilder.Entity<TblDebitNote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_debit_note")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CreditAccount)
                .HasDefaultValueSql("'0'")
                .HasColumnName("credit_account");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DebitAccount).HasColumnName("debit_account");
            entity.Property(e => e.Description)
                .HasMaxLength(150)
                .HasDefaultValueSql("''")
                .HasColumnName("description");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("total");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("type");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("vat");
        });

        modelBuilder.Entity<TblDebitNoteDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_debit_note_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Balance)
                .HasPrecision(20, 2)
                .HasColumnName("balance");
            entity.Property(e => e.InvNo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("inv_no");
            entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.InvoiceType)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_type");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RefId).HasColumnName("ref_id");
            entity.Property(e => e.Remaining)
                .HasPrecision(20, 2)
                .HasColumnName("remaining");
            entity.Property(e => e.Total)
                .HasPrecision(20, 2)
                .HasColumnName("total");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 2)
                .HasColumnName("vat");
        });

        modelBuilder.Entity<TblDeletedRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_deleted_records");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeletedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("deleted_at");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.RecordData)
                .HasColumnType("text")
                .HasColumnName("record_data");
            entity.Property(e => e.TableName)
                .HasMaxLength(100)
                .HasColumnName("table_name");
        });

        modelBuilder.Entity<TblDepartment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_departments")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblEmployee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_employee")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.AccruedSalariesId).HasColumnName("Accrued_Salaries_id");
            entity.Property(e => e.AcroalLeaveSalaryId).HasColumnName("Acroal_Leave_Salary_id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Address)
                .HasMaxLength(50)
                .HasColumnName("address");
            entity.Property(e => e.BankAccountNumber)
                .HasMaxLength(50)
                .HasColumnName("Bank_account_Number");
            entity.Property(e => e.BankId).HasColumnName("bank_id");
            entity.Property(e => e.BasicSalary).HasPrecision(10, 2);
            entity.Property(e => e.BirthDay).HasColumnName("birth_day");
            entity.Property(e => e.CityId).HasColumnName("city_id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CountryOfIssue).HasMaxLength(100);
            entity.Property(e => e.DepartmentId).HasColumnName("Department_id");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.EmergencyAddress).HasMaxLength(50);
            entity.Property(e => e.EmergencyName).HasMaxLength(100);
            entity.Property(e => e.EmergencyPhone).HasMaxLength(20);
            entity.Property(e => e.EmiratesIdexpiryDate).HasColumnName("EmiratesIDExpiryDate");
            entity.Property(e => e.EmiratesIdfileNumber)
                .HasMaxLength(50)
                .HasColumnName("EmiratesIDFileNumber");
            entity.Property(e => e.EmiratesIdissueDate).HasColumnName("EmiratesIDIssueDate");
            entity.Property(e => e.EmiratesIdissuingAuthority)
                .HasMaxLength(100)
                .HasColumnName("EmiratesIDIssuingAuthority");
            entity.Property(e => e.EmployeeRecivableId).HasColumnName("Employee_Recivable_id");
            entity.Property(e => e.GratuitId).HasColumnName("Gratuit_id");
            entity.Property(e => e.HousingAllowance).HasPrecision(10, 2);
            entity.Property(e => e.IbanNumber)
                .HasMaxLength(50)
                .HasColumnName("Iban_Number");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Other).HasPrecision(10, 2);
            entity.Property(e => e.PassportNumber).HasMaxLength(50);
            entity.Property(e => e.PettyCashId).HasColumnName("Petty_Cash_id");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.PositionId).HasColumnName("Position_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Relation).HasMaxLength(50);
            entity.Property(e => e.ResidencyFileNumber).HasMaxLength(50);
            entity.Property(e => e.ResidencyIssuingAuthority).HasMaxLength(100);
            entity.Property(e => e.SRole)
                .HasMaxLength(50)
                .HasColumnName("sRole");
            entity.Property(e => e.SocialInsuranceNumber)
                .HasMaxLength(50)
                .HasColumnName("Social_Insurance_Number");
            entity.Property(e => e.SocialStatus)
                .HasMaxLength(50)
                .HasColumnName("Social_Status");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.TransportationAllowance).HasPrecision(10, 2);
            entity.Property(e => e.WorkContractNumber).HasMaxLength(50);
            entity.Property(e => e.WorkContractType).HasMaxLength(50);
            entity.Property(e => e.Workinghours).HasColumnName("workinghours");
        });

        modelBuilder.Entity<TblEndOfService>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_end_of_service")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Credit)
                .HasPrecision(20, 6)
                .HasColumnName("credit");
            entity.Property(e => e.Date)
                .HasMaxLength(50)
                .HasColumnName("date");
            entity.Property(e => e.Debit)
                .HasPrecision(20, 6)
                .HasColumnName("debit");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .HasColumnName("description");
            entity.Property(e => e.LeaveDays)
                .HasPrecision(20, 6)
                .HasColumnName("leave_days");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Reference).HasMaxLength(50);
        });

        modelBuilder.Entity<TblFinalSettlement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_final_settlement")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.NetAccruals).HasPrecision(20, 2);
            entity.Property(e => e.OtherAdditions).HasPrecision(20, 2);
            entity.Property(e => e.OtherDeductions).HasPrecision(20, 2);
            entity.Property(e => e.Payments).HasPrecision(20, 2);
            entity.Property(e => e.TotalAdditions).HasPrecision(20, 2);
            entity.Property(e => e.TotalDeductions).HasPrecision(20, 2);
            entity.Property(e => e.TotalSalary).HasPrecision(20, 2);
        });

        modelBuilder.Entity<TblFixedAsset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_fixed_assets")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Brand)
                .HasMaxLength(500)
                .HasColumnName("brand");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CreditAccountId).HasColumnName("credit_account_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DebitAccountId).HasColumnName("debit_account_id");
            entity.Property(e => e.DepreciationLife).HasColumnName("depreciation_life");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.ExpenceAccountId).HasColumnName("expence_account_id");
            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(500)
                .HasColumnName("invoice_number");
            entity.Property(e => e.Manufacture)
                .HasDefaultValueSql("'0'")
                .HasColumnName("manufacture");
            entity.Property(e => e.ManufactureStatus)
                .HasMaxLength(50)
                .HasColumnName("manufactureStatus");
            entity.Property(e => e.Model)
                .HasMaxLength(500)
                .HasColumnName("model");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasColumnName("name");
            entity.Property(e => e.PurchaseDate).HasColumnName("purchase_date");
            entity.Property(e => e.PurchasePrice)
                .HasPrecision(20, 6)
                .HasColumnName("purchase_price");
            entity.Property(e => e.State)
                .HasDefaultValueSql("'0'")
                .HasColumnName("state");
            entity.Property(e => e.Status)
                .HasMaxLength(250)
                .HasColumnName("status");
            entity.Property(e => e.Supplier)
                .HasMaxLength(500)
                .HasColumnName("supplier");
        });

        modelBuilder.Entity<TblFixedAssetsCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_fixed_assets_category")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssetsAccountId).HasColumnName("assets_account_id");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(50)
                .HasColumnName("category_name");
            entity.Property(e => e.DepreciationAccountId).HasColumnName("depreciation_account_id");
            entity.Property(e => e.ExpenceAccountId).HasColumnName("expence_account_id");
        });

        modelBuilder.Entity<TblGeneralSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_general_settings")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(150)
                .HasDefaultValueSql("''")
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'0'")
                .HasColumnName("status");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<TblItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_items")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.AssetAccountId).HasColumnName("asset_account_id");
            entity.Property(e => e.Barcode)
                .HasMaxLength(70)
                .HasDefaultValueSql("'0'")
                .HasColumnName("barcode");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("code");
            entity.Property(e => e.CogsAccountId).HasColumnName("cogs_account_id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 4)
                .HasColumnName("cost_price");
            entity.Property(e => e.CreatedBy).HasColumnName("created_By");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DeletedBy).HasColumnName("deleted_by");
            entity.Property(e => e.Img)
                .HasColumnType("blob")
                .HasColumnName("img");
            entity.Property(e => e.IncomeAccountId).HasColumnName("income_account_id");
            entity.Property(e => e.ItemType)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("item_type");
            entity.Property(e => e.MaxAmount)
                .HasPrecision(20, 4)
                .HasColumnName("max_amount");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("method");
            entity.Property(e => e.MinAmount)
                .HasPrecision(20, 4)
                .HasColumnName("min_amount");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasDefaultValueSql("'0'")
                .HasColumnName("name");
            entity.Property(e => e.OnHand)
                .HasPrecision(20, 4)
                .HasColumnName("on_hand");
            entity.Property(e => e.PosItem).HasColumnName("posItem");
            entity.Property(e => e.SalesPrice)
                .HasPrecision(20, 4)
                .HasColumnName("sales_price");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.TaxCodeId).HasColumnName("tax_code_id");
            entity.Property(e => e.TotalValue)
                .HasPrecision(20, 6)
                .HasColumnName("total_value");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("type");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblItemAssembly>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_item_assembly")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssemblyId).HasColumnName("assembly_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
        });

        modelBuilder.Entity<TblItemAssemblyBo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_item_assembly_bos")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssemblyId).HasColumnName("assembly_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Qty)
                .HasPrecision(10, 2)
                .HasColumnName("qty");
        });

        modelBuilder.Entity<TblItemCardDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_item_card_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Balance)
                .HasPrecision(20, 4)
                .HasColumnName("balance");
            entity.Property(e => e.Credit)
                .HasPrecision(20, 4)
                .HasColumnName("credit");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Debit)
                .HasPrecision(20, 4)
                .HasColumnName("debit");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasDefaultValueSql("''")
                .HasColumnName("description");
            entity.Property(e => e.FifoCost)
                .HasPrecision(20, 4)
                .HasColumnName("fifo_cost");
            entity.Property(e => e.FifoQty)
                .HasPrecision(20, 4)
                .HasColumnName("fifo_qty");
            entity.Property(e => e.InvNo)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("inv_no");
            entity.Property(e => e.ItemId).HasColumnName("itemId");
            entity.Property(e => e.Price)
                .HasPrecision(20, 4)
                .HasColumnName("price");
            entity.Property(e => e.QtyBalance)
                .HasPrecision(20, 4)
                .HasColumnName("qty_balance");
            entity.Property(e => e.QtyIn)
                .HasPrecision(20, 4)
                .HasColumnName("qty_in");
            entity.Property(e => e.QtyOut)
                .HasPrecision(20, 4)
                .HasColumnName("qty_out");
            entity.Property(e => e.TransNo).HasColumnName("trans_no");
            entity.Property(e => e.TransType)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("trans_type");
            entity.Property(e => e.WharehouseId).HasColumnName("wharehouse_id");
        });

        modelBuilder.Entity<TblItemCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_item_category")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblItemStockSettlement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_item_stock_settlement")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code").UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.State)
                .HasDefaultValueSql("'0'")
                .HasColumnName("state");
            entity.Property(e => e.TotalMinus)
                .HasPrecision(20, 6)
                .HasColumnName("total_minus");
            entity.Property(e => e.TotalPlus)
                .HasPrecision(20, 6)
                .HasColumnName("total_plus");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblItemStockSettlementDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_item_stock_settlement_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Minusamount)
                .HasPrecision(20, 4)
                .HasColumnName("minusamount");
            entity.Property(e => e.NewOnHand)
                .HasPrecision(20, 4)
                .HasColumnName("new_on_hand");
            entity.Property(e => e.OnHand)
                .HasPrecision(20, 4)
                .HasColumnName("on_hand");
            entity.Property(e => e.Plusamount)
                .HasPrecision(20, 4)
                .HasColumnName("plusamount");
            entity.Property(e => e.Price)
                .HasPrecision(20, 4)
                .HasColumnName("price");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.SettleId).HasColumnName("settle_id");
        });

        modelBuilder.Entity<TblItemTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_item_transaction")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 8)
                .HasColumnName("cost_price");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasColumnName("description");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.QtyIn)
                .HasPrecision(20, 4)
                .HasColumnName("qty_in");
            entity.Property(e => e.QtyInc)
                .HasPrecision(20, 4)
                .HasColumnName("qty_inc");
            entity.Property(e => e.QtyOut)
                .HasPrecision(20, 4)
                .HasColumnName("qty_out");
            entity.Property(e => e.Reference)
                .HasMaxLength(50)
                .HasColumnName("reference");
            entity.Property(e => e.SalesPrice)
                .HasPrecision(20, 4)
                .HasColumnName("sales_price");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblItemWarehouseTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_item_warehouse_transaction")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .HasColumnName("description");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 6)
                .HasColumnName("qty");
            entity.Property(e => e.WarehouseFrom).HasColumnName("warehouse_from");
            entity.Property(e => e.WarehouseTo).HasColumnName("warehouse_to");
        });

        modelBuilder.Entity<TblItemsBoq>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_items_boq")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Length)
                .HasPrecision(10, 2)
                .HasColumnName("length");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasColumnName("note");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Qty)
                .HasPrecision(10, 2)
                .HasColumnName("qty");
            entity.Property(e => e.RefId).HasColumnName("ref_id");
            entity.Property(e => e.Sr)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("sr");
            entity.Property(e => e.Thickness)
                .HasMaxLength(20)
                .HasColumnName("thickness");
            entity.Property(e => e.Type)
                .HasMaxLength(255)
                .HasColumnName("type");
            entity.Property(e => e.UnitName)
                .HasMaxLength(50)
                .HasColumnName("unit_name");
            entity.Property(e => e.Width)
                .HasPrecision(10, 2)
                .HasColumnName("width");
        });

        modelBuilder.Entity<TblItemsBoqDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_items_boq_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active)
                .HasDefaultValueSql("'1'")
                .HasColumnName("active");
            entity.Property(e => e.AssetAccountId).HasColumnName("asset_account_id");
            entity.Property(e => e.Barcode)
                .HasMaxLength(100)
                .HasColumnName("barcode");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.CogsAccountId).HasColumnName("cogs_account_id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(10, 2)
                .HasColumnName("cost_price");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Img)
                .HasMaxLength(255)
                .HasColumnName("img");
            entity.Property(e => e.IncomeAccountId).HasColumnName("income_account_id");
            entity.Property(e => e.MaxAmount)
                .HasPrecision(10, 2)
                .HasColumnName("max_amount");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .HasColumnName("method");
            entity.Property(e => e.MinAmount)
                .HasPrecision(10, 2)
                .HasColumnName("min_amount");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.OnHand)
                .HasPrecision(10, 2)
                .HasColumnName("on_hand");
            entity.Property(e => e.RefId).HasColumnName("ref_id");
            entity.Property(e => e.SalesPrice)
                .HasPrecision(10, 2)
                .HasColumnName("sales_price");
            entity.Property(e => e.State)
                .HasDefaultValueSql("'0'")
                .HasColumnName("state");
            entity.Property(e => e.TotalValue)
                .HasPrecision(15, 2)
                .HasColumnName("total_value");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblItemsUnit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_items_unit")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Factor).HasColumnName("factor");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
        });

        modelBuilder.Entity<TblItemsWarehouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_items_warehouse")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 6)
                .HasColumnName("qty");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblJournalVoucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_journal_voucher")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Credit)
                .HasPrecision(20, 4)
                .HasColumnName("credit");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Debit)
                .HasPrecision(20, 4)
                .HasColumnName("debit");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.State).HasColumnName("state");
        });

        modelBuilder.Entity<TblJournalVoucherDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_journal_voucher_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Credit)
                .HasPrecision(20, 4)
                .HasColumnName("credit");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Debit)
                .HasPrecision(20, 4)
                .HasColumnName("debit");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasDefaultValueSql("'0'")
                .HasColumnName("description");
            entity.Property(e => e.InvId).HasColumnName("inv_id");
            entity.Property(e => e.Partner)
                .HasMaxLength(250)
                .HasDefaultValueSql("''")
                .HasColumnName("partner");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
        });

        modelBuilder.Entity<TblLeaveSalary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_leave_salary")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Credit)
                .HasPrecision(20, 6)
                .HasDefaultValueSql("'0.000000'")
                .HasColumnName("credit");
            entity.Property(e => e.Date)
                .HasMaxLength(50)
                .HasColumnName("date");
            entity.Property(e => e.Debit)
                .HasPrecision(20, 6)
                .HasDefaultValueSql("'0.000000'")
                .HasColumnName("debit");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.LeaveDays)
                .HasPrecision(20, 6)
                .HasColumnName("leave_days");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasColumnName("name");
            entity.Property(e => e.Reference).HasMaxLength(50);
        });

        modelBuilder.Entity<TblLedger>(entity =>
        {
            entity.HasKey(e => e.LedgerId).HasName("PRIMARY");

            entity
                .ToTable("tbl_ledger")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.LedgerId).HasColumnName("ledger_id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Balance)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("balance");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("city");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Country)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("country");
            entity.Property(e => e.CreatedBy)
                .HasDefaultValueSql("'0'")
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("email");
            entity.Property(e => e.EntityType)
                .HasColumnType("enum('vendor','bank','level4','customer','employee')")
                .HasColumnName("entity_type");
            entity.Property(e => e.Mobile)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("mobile");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("phone");
            entity.Property(e => e.Region)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("region");
            entity.Property(e => e.State)
                .HasDefaultValueSql("'0'")
                .HasColumnName("state");
            entity.Property(e => e.Website)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("website");
        });

        modelBuilder.Entity<TblLoan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_loan")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount).HasPrecision(20, 6);
            entity.Property(e => e.Change)
                .HasPrecision(20, 6)
                .HasDefaultValueSql("'0.000000'")
                .HasColumnName("change");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreditAccountId).HasColumnName("credit_account_id");
            entity.Property(e => e.DebitAccountId).HasColumnName("debit_account_id");
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(255)
                .HasColumnName("EmployeeID");
            entity.Property(e => e.EmployeeName).HasMaxLength(255);
            entity.Property(e => e.LoanDates).HasColumnName("loanDates");
            entity.Property(e => e.Months).HasMaxLength(200);
            entity.Property(e => e.Pay)
                .HasPrecision(20, 6)
                .HasDefaultValueSql("'0.000000'")
                .HasColumnName("pay");
            entity.Property(e => e.RequestAmount).HasPrecision(20, 6);
        });

        modelBuilder.Entity<TblMainMenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_main_menus");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblManufacturerBatch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_manufacturer_batch");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(62, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Batchname)
                .HasMaxLength(100)
                .HasColumnName("batchname");
            entity.Property(e => e.Costamount).HasPrecision(64, 2);
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.FixedStatus)
                .HasMaxLength(50)
                .HasColumnName("fixedStatus");
            entity.Property(e => e.FixedassetsId).HasColumnName("fixedassetsID");
            entity.Property(e => e.Hours)
                .HasPrecision(10)
                .HasColumnName("hours");
            entity.Property(e => e.ProductId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("product_id");
            entity.Property(e => e.ProductQty)
                .HasPrecision(10)
                .HasDefaultValueSql("'0'")
                .HasColumnName("product_qty");
            entity.Property(e => e.Userinsert)
                .HasMaxLength(50)
                .HasColumnName("userinsert");
            entity.Property(e => e.WarehouseId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblManufacturerBatchdetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_manufacturer_batchdetails");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatchId)
                .HasMaxLength(50)
                .HasColumnName("batchID");
            entity.Property(e => e.Cost)
                .HasPrecision(63, 2)
                .HasColumnName("cost");
            entity.Property(e => e.Itemid).HasColumnName("itemid");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 2)
                .HasColumnName("qty");
            entity.Property(e => e.ReceiveQty)
                .HasPrecision(20, 2)
                .HasDefaultValueSql("'0.00'");
            entity.Property(e => e.RequestQty)
                .HasPrecision(20, 2)
                .HasDefaultValueSql("'0.00'");
            entity.Property(e => e.Total).HasPrecision(63, 2);
        });

        modelBuilder.Entity<TblManufacturerTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_manufacturer_task");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatchId).HasColumnName("BatchID");
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.MachineId).HasColumnName("MachineID");
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("userID");
        });

        modelBuilder.Entity<TblManufacturerTaskDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_manufacturer_task_details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DepartmentId).HasColumnName("DepartmentID");
            entity.Property(e => e.EmployeeId).HasColumnName("EmployeeID");
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.Remarks).HasColumnType("text");
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TaskId).HasColumnName("TaskID");
        });

        modelBuilder.Entity<TblPaymentVoucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_payment_voucher")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 4)
                .HasColumnName("amount");
            entity.Property(e => e.BankAccountId).HasColumnName("bank_account_id");
            entity.Property(e => e.BankId).HasColumnName("bank_id");
            entity.Property(e => e.BookNo).HasColumnName("book_no");
            entity.Property(e => e.CheckDate).HasColumnName("check_date");
            entity.Property(e => e.CheckName)
                .HasMaxLength(100)
                .HasColumnName("check_name");
            entity.Property(e => e.CheckNo)
                .HasMaxLength(50)
                .HasColumnName("check_no");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CreditAccountId).HasColumnName("credit_account_id");
            entity.Property(e => e.CreditCostCenterId).HasColumnName("credit_cost_center_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DebitAccountId).HasColumnName("debit_account_id");
            entity.Property(e => e.DebitCostCenterId).HasColumnName("debit_cost_center_id");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .HasColumnName("method");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.TransDate).HasColumnName("trans_date");
            entity.Property(e => e.TransName)
                .HasMaxLength(50)
                .HasColumnName("trans_name");
            entity.Property(e => e.TransRef)
                .HasMaxLength(50)
                .HasColumnName("trans_ref");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
        });

        modelBuilder.Entity<TblPaymentVoucherDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_payment_voucher_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.HumId).HasColumnName("hum_id");
            entity.Property(e => e.InvCode)
                .HasMaxLength(50)
                .HasColumnName("inv_code");
            entity.Property(e => e.InvId).HasColumnName("inv_id");
            entity.Property(e => e.Payment)
                .HasPrecision(20, 6)
                .HasColumnName("payment");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Total)
                .HasPrecision(20, 6)
                .HasColumnName("total");
            entity.Property(e => e.VoucherType)
                .HasMaxLength(200)
                .HasDefaultValueSql("''")
                .HasColumnName("voucher_type");
        });

        modelBuilder.Entity<TblPettyCash>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_petty_cash");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CashAccountId).HasColumnName("cash_account_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.Notes)
                .HasColumnType("text")
                .HasColumnName("notes");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Total)
                .HasPrecision(20, 6)
                .HasColumnName("total");
            entity.Property(e => e.VoucherDate).HasColumnName("voucher_date");
        });

        modelBuilder.Entity<TblPettyCashCard>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_petty_cash_card")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Email)
                .HasMaxLength(250)
                .HasColumnName("email");
            entity.Property(e => e.Mobile)
                .HasMaxLength(50)
                .HasColumnName("mobile");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("name");
            entity.Property(e => e.WhatsappNo)
                .HasMaxLength(50)
                .HasColumnName("whatsapp_no");
        });

        modelBuilder.Entity<TblPettyCashCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_petty_cash_category")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblPettyCashDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_petty_cash_details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.EntryDate).HasColumnName("entry_date");
            entity.Property(e => e.HumId).HasColumnName("hum_id");
            entity.Property(e => e.Note)
                .HasColumnType("text")
                .HasColumnName("note");
            entity.Property(e => e.PettyCashId).HasColumnName("petty_cash_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RefId)
                .HasMaxLength(100)
                .HasColumnName("ref_id");
        });

        modelBuilder.Entity<TblPettyCashRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_petty_cash_request")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 6)
                .HasColumnName("amount");
            entity.Property(e => e.ApprovedDate).HasColumnName("approved_date");
            entity.Property(e => e.Change)
                .HasPrecision(20, 6)
                .HasColumnName("change");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CreditAccountId).HasColumnName("credit_account_id");
            entity.Property(e => e.DebitAccountId).HasColumnName("debit_account_id");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .HasColumnName("description");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 6)
                .HasColumnName("pay");
            entity.Property(e => e.PettyCashName)
                .HasMaxLength(50)
                .HasColumnName("Petty_cash_name");
            entity.Property(e => e.RequestDate).HasColumnName("request_date");
            entity.Property(e => e.RequestRef)
                .HasMaxLength(100)
                .HasColumnName("request_ref");
            entity.Property(e => e.State)
                .HasMaxLength(50)
                .HasColumnName("state");
        });

        modelBuilder.Entity<TblPettyCashSubmition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_petty_cash_submition")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 6)
                .HasColumnName("amount");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasColumnName("name");
            entity.Property(e => e.NetAmount)
                .HasPrecision(20, 6)
                .HasColumnName("net_amount");
            entity.Property(e => e.TotalBeforeVat)
                .HasPrecision(20, 6)
                .HasColumnName("total_before_vat");
            entity.Property(e => e.TotalVat)
                .HasPrecision(20, 6)
                .HasColumnName("total_vat");
        });

        modelBuilder.Entity<TblPettyCashSubmitionDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_petty_cash_submition_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 6)
                .HasColumnName("amount");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Note)
                .HasMaxLength(250)
                .HasColumnName("note");
            entity.Property(e => e.PettyId).HasColumnName("petty_id");
            entity.Property(e => e.State)
                .HasMaxLength(50)
                .HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 6)
                .HasColumnName("total");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 6)
                .HasColumnName("vat");
        });

        modelBuilder.Entity<TblPosition>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_position")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblPrepaidExpense>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_prepaid_expense")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 4)
                .HasColumnName("amount");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CreditAccountId).HasColumnName("credit_account_id");
            entity.Property(e => e.DebitAccountId).HasColumnName("debit_account_id");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Fee)
                .HasPrecision(20, 4)
                .HasColumnName("fee");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.State)
                .HasDefaultValueSql("'0'")
                .HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
        });

        modelBuilder.Entity<TblPrepaidExpenseCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_prepaid_expense_category")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblPrintConfig>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("tbl_print_config")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.CompanyName)
                .HasDefaultValueSql("'0'")
                .HasColumnName("company_name");
            entity.Property(e => e.Landscape)
                .HasDefaultValueSql("'0'")
                .HasColumnName("landscape");
            entity.Property(e => e.Orientation)
                .HasDefaultValueSql("'1'")
                .HasColumnName("orientation");
            entity.Property(e => e.Portrait)
                .HasDefaultValueSql("'0'")
                .HasColumnName("portrait");
            entity.Property(e => e.TableBorder)
                .HasDefaultValueSql("'0'")
                .HasColumnName("table_border");
        });

        modelBuilder.Entity<TblPrintconfg>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_printconfg");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PrintName).HasMaxLength(150);
        });

        modelBuilder.Entity<TblProject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_projects")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(250)
                .HasDefaultValueSql("''")
                .HasColumnName("category")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.CityId).HasColumnName("city_id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.CountryId).HasColumnName("country_id");
            entity.Property(e => e.Description)
                .HasMaxLength(350)
                .HasDefaultValueSql("''")
                .HasColumnName("description")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasDefaultValueSql("''")
                .HasColumnName("name")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Planned'")
                .HasColumnName("status");
        });

        modelBuilder.Entity<TblProjectActivity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_project_activity");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(500)
                .HasDefaultValueSql("''")
                .HasColumnName("code")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Name)
                .HasMaxLength(500)
                .HasDefaultValueSql("''")
                .HasColumnName("name")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.PlanningId).HasColumnName("planning_id");
            entity.Property(e => e.Progress)
                .HasPrecision(20, 3)
                .HasColumnName("progress");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Status).HasColumnName("status");
        });

        modelBuilder.Entity<TblProjectActivityAssignment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_project_activity_assignment");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActivityId).HasColumnName("activity_id");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
        });

        modelBuilder.Entity<TblProjectEstimate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_project_estimate")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasDefaultValueSql("''")
                .HasColumnName("description");
            entity.Property(e => e.EquipmentCost)
                .HasPrecision(20, 4)
                .HasColumnName("equipment_cost");
            entity.Property(e => e.FundAccountId).HasColumnName("fund_account_id");
            entity.Property(e => e.LaborCost)
                .HasPrecision(20, 4)
                .HasColumnName("labor_cost");
            entity.Property(e => e.MaterialCost)
                .HasPrecision(20, 4)
                .HasColumnName("material_cost");
            entity.Property(e => e.OverheadCost)
                .HasPrecision(20, 4)
                .HasColumnName("overhead_cost");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.TotalEstimate)
                .HasPrecision(20, 4)
                .HasColumnName("total_estimate");
        });

        modelBuilder.Entity<TblProjectManagement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_project_management")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualCost)
                .HasPrecision(20, 4)
                .HasColumnName("actual_cost");
            entity.Property(e => e.Budget)
                .HasPrecision(20, 4)
                .HasColumnName("budget");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectPlanningId).HasColumnName("project_planning_id");
            entity.Property(e => e.RemainingBudget)
                .HasPrecision(20, 4)
                .HasColumnName("remaining_budget");
            entity.Property(e => e.State).HasColumnName("state");
        });

        modelBuilder.Entity<TblProjectMaterialRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_project_material_requests")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IssuedQty).HasPrecision(10, 2);
            entity.Property(e => e.ItemId).HasColumnName("itemId");
            entity.Property(e => e.PlanningId).HasColumnName("planning_id");
            entity.Property(e => e.ReceivedQty).HasPrecision(10, 2);
            entity.Property(e => e.RequestedQty).HasPrecision(10, 2);
            entity.Property(e => e.TenderId).HasColumnName("tender_id");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("unit")
                .UseCollation("utf8mb4_general_ci");
        });

        modelBuilder.Entity<TblProjectPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_project_plan")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignedTeam)
                .HasMaxLength(250)
                .HasDefaultValueSql("''")
                .HasColumnName("assigned_team")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.CreatedBy)
                .HasDefaultValueSql("'0'")
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasDefaultValueSql("''")
                .HasColumnName("description")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.EstimatedBudget)
                .HasPrecision(20, 4)
                .HasColumnName("estimated_budget");
            entity.Property(e => e.FundAccountId).HasColumnName("fund_account_id");
            entity.Property(e => e.FundPeriod)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("fund_period")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Location)
                .HasMaxLength(250)
                .HasDefaultValueSql("'0'")
                .HasColumnName("location")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.ModifiedBy)
                .HasDefaultValueSql("'0'")
                .HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.PlotNumber)
                .HasMaxLength(50)
                .HasColumnName("plot_number")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Progress)
                .HasDefaultValueSql("'0'")
                .HasColumnName("progress");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectType)
                .HasMaxLength(80)
                .HasDefaultValueSql("''")
                .HasColumnName("project_type")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Site).HasColumnName("site");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.State)
                .HasDefaultValueSql("'0'")
                .HasColumnName("state");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("status")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.TenderId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("tender_id");
            entity.Property(e => e.TenderNameId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("tender_name_id");
        });

        modelBuilder.Entity<TblProjectPlanning>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_project_planning")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignedTeam)
                .HasMaxLength(250)
                .HasDefaultValueSql("''")
                .HasColumnName("assigned_team")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.CreatedBy)
                .HasDefaultValueSql("'0'")
                .HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasDefaultValueSql("''")
                .HasColumnName("description")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.EstimatedBudget)
                .HasPrecision(20, 4)
                .HasColumnName("estimated_budget");
            entity.Property(e => e.FundAccountId).HasColumnName("fund_account_id");
            entity.Property(e => e.FundPeriod)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("fund_period")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Location)
                .HasMaxLength(250)
                .HasDefaultValueSql("'0'")
                .HasColumnName("location")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.ModifiedBy)
                .HasDefaultValueSql("'0'")
                .HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.PlotNumber)
                .HasMaxLength(50)
                .HasColumnName("plot_number")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Progress)
                .HasDefaultValueSql("'0'")
                .HasColumnName("progress");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectType)
                .HasMaxLength(80)
                .HasDefaultValueSql("''")
                .HasColumnName("project_type")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Site).HasColumnName("site");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.State)
                .HasDefaultValueSql("'0'")
                .HasColumnName("state");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("status")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.TenderId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("tender_id");
            entity.Property(e => e.TenderNameId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("tender_name_id");
        });

        modelBuilder.Entity<TblProjectResource>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_project_resource");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("code")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
            entity.Property(e => e.MaxUnitTime)
                .HasPrecision(20, 4)
                .HasColumnName("max_unit_time");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasDefaultValueSql("''")
                .HasColumnName("name")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasDefaultValueSql("''")
                .HasColumnName("phone")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.PriceUnit)
                .HasPrecision(20, 4)
                .HasColumnName("price_unit");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Type)
                .HasMaxLength(80)
                .HasDefaultValueSql("''")
                .HasColumnName("type")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.UnitTime)
                .HasPrecision(20, 4)
                .HasColumnName("unit_time");
        });

        modelBuilder.Entity<TblProjectRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_project_role");

            entity.HasIndex(e => e.Code, "code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name")
                .UseCollation("utf8mb4_general_ci");
        });

        modelBuilder.Entity<TblProjectSite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_project_sites")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(250)
                .HasDefaultValueSql("''")
                .HasColumnName("address")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("code")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.LocationId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("location_id");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasDefaultValueSql("''")
                .HasColumnName("name")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.PlotNumber)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("plot_number")
                .UseCollation("utf8mb4_general_ci");
        });

        modelBuilder.Entity<TblProjectTender>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_project_tender")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 4)
                .HasColumnName("amount");
            entity.Property(e => e.BidAmount)
                .HasPrecision(20, 4)
                .HasColumnName("bid_amount");
            entity.Property(e => e.ContractorId).HasColumnName("contractor_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description)
                .HasMaxLength(150)
                .HasDefaultValueSql("''")
                .HasColumnName("description")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.EstimateStatus)
                .HasDefaultValueSql("'0'")
                .HasColumnName("estimate_status");
            entity.Property(e => e.Fees)
                .HasPrecision(20, 4)
                .HasColumnName("fees");
            entity.Property(e => e.ModifiedBy)
                .HasDefaultValueSql("'0'")
                .HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("status")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.SubmissionDate).HasColumnName("submission_date");
            entity.Property(e => e.TenderName)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("tender_name")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.TenderNameId).HasColumnName("tender_name_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblProjectTenderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_project_tender_details")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 4)
                .HasColumnName("amount");
            entity.Property(e => e.Assigned).HasColumnName("assigned");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.ItemId)
                .HasMaxLength(350)
                .HasDefaultValueSql("''")
                .HasColumnName("item_id")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Length)
                .HasPrecision(20, 4)
                .HasColumnName("length");
            entity.Property(e => e.MarginAmount)
                .HasPrecision(20, 4)
                .HasColumnName("margin_amount");
            entity.Property(e => e.MarginPercentage)
                .HasPrecision(20, 4)
                .HasColumnName("margin_percentage");
            entity.Property(e => e.Note)
                .HasMaxLength(500)
                .HasDefaultValueSql("''")
                .HasColumnName("note")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Progress)
                .HasPrecision(20, 4)
                .HasColumnName("progress");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.Rate)
                .HasPrecision(20, 4)
                .HasColumnName("rate");
            entity.Property(e => e.Sr)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("sr")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.TenderId).HasColumnName("tender_id");
            entity.Property(e => e.Thickness)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("thickness")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.UnitId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("unit_id")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Width)
                .HasPrecision(20, 4)
                .HasColumnName("width");
        });

        modelBuilder.Entity<TblProjectWorkDone>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_project_work_done");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.PlanningId).HasColumnName("planning_id");
            entity.Property(e => e.State)
                .HasDefaultValueSql("'0'")
                .HasColumnName("state");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblProjectWorkDoneDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_project_work_done_details");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.MainId).HasColumnName("main_id");
            entity.Property(e => e.QtyTotal)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("qty_total");
            entity.Property(e => e.QtyUsed)
                .HasPrecision(10, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("qty_used");
            entity.Property(e => e.RefId).HasColumnName("ref_id");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");
        });

        modelBuilder.Entity<TblPurchase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_purchase")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountCashId).HasColumnName("account_cash_id");
            entity.Property(e => e.BillTo)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("bill_to");
            entity.Property(e => e.Change)
                .HasPrecision(20, 4)
                .HasColumnName("change");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("city");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.FixedAssetCategoryId).HasColumnName("fixed_asset_category_id");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.Net)
                .HasPrecision(20, 4)
                .HasColumnName("net");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 4)
                .HasColumnName("pay");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentTerms)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_terms");
            entity.Property(e => e.PoNum)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("po_num");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.PurchaseType)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("purchase_type");
            entity.Property(e => e.SalesMan)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("sales_man");
            entity.Property(e => e.ShipDate).HasColumnName("ship_date");
            entity.Property(e => e.ShipTo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("ship_to");
            entity.Property(e => e.ShipVia)
                .HasMaxLength(50)
                .HasColumnName("ship_via");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 4)
                .HasColumnName("vat");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblPurchaseDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_purchase_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 4)
                .HasColumnName("cost_price");
            entity.Property(e => e.Discount)
                .HasPrecision(20, 4)
                .HasColumnName("discount");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Price)
                .HasPrecision(20, 4)
                .HasColumnName("price");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.PurchaseId).HasColumnName("purchase_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat).HasColumnName("vat");
            entity.Property(e => e.Vatp)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("vatp");
        });

        modelBuilder.Entity<TblPurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_purchase_order")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountCashId).HasColumnName("account_cash_id");
            entity.Property(e => e.BillTo)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("bill_to");
            entity.Property(e => e.Change)
                .HasPrecision(20, 4)
                .HasColumnName("change");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("city");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.Net)
                .HasPrecision(20, 4)
                .HasColumnName("net");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 4)
                .HasColumnName("pay");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentTerms)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_terms");
            entity.Property(e => e.PoNum)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("po_num");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.PurchaseId).HasColumnName("purchase_id");
            entity.Property(e => e.SalesMan)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("sales_man");
            entity.Property(e => e.ShipDate).HasColumnName("ship_date");
            entity.Property(e => e.ShipTo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("ship_to");
            entity.Property(e => e.ShipVia)
                .HasMaxLength(50)
                .HasColumnName("ship_via");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.TranferStatus).HasColumnName("tranfer_status");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 4)
                .HasColumnName("vat");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblPurchaseOrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_purchase_order_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("cost_price");
            entity.Property(e => e.Discount)
                .HasPrecision(20, 4)
                .HasColumnName("discount");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Price)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("price");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.PurchaseId).HasColumnName("purchase_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat).HasColumnName("vat");
            entity.Property(e => e.Vatp)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("vatp");
        });

        modelBuilder.Entity<TblPurchaseReturn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_purchase_return")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountCashId).HasColumnName("account_cash_id");
            entity.Property(e => e.BillTo)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("bill_to");
            entity.Property(e => e.Change)
                .HasPrecision(20, 4)
                .HasColumnName("change");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("city");
            entity.Property(e => e.PurchaseRefId).HasColumnName("Purchase_Ref_Id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.Net)
                .HasPrecision(20, 4)
                .HasColumnName("net");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 4)
                .HasColumnName("pay");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentTerms)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_terms");
            entity.Property(e => e.PoNum)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("po_num");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SalesMan)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("sales_man");
            entity.Property(e => e.ShipDate).HasColumnName("ship_date");
            entity.Property(e => e.ShipTo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("ship_to");
            entity.Property(e => e.ShipVia)
                .HasMaxLength(50)
                .HasColumnName("ship_via");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 4)
                .HasColumnName("vat");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblPurchaseReturnDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_purchase_return_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 4)
                .HasColumnName("cost_price");
            entity.Property(e => e.Discount)
                .HasPrecision(20, 4)
                .HasColumnName("discount");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Price)
                .HasPrecision(20, 4)
                .HasColumnName("price");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.PurchaseId).HasColumnName("purchase_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat).HasColumnName("vat");
            entity.Property(e => e.Vatp)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("vatp");
        });

        modelBuilder.Entity<TblReceiptVoucher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_receipt_voucher")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasPrecision(20, 6)
                .HasColumnName("amount");
            entity.Property(e => e.BankAccount).HasColumnName("bank_account");
            entity.Property(e => e.BankAccountId).HasColumnName("bank_account_id");
            entity.Property(e => e.BankCode)
                .HasMaxLength(250)
                .HasColumnName("bank_code");
            entity.Property(e => e.BankId).HasColumnName("bank_id");
            entity.Property(e => e.BookNo).HasColumnName("book_no");
            entity.Property(e => e.CheckDate).HasColumnName("check_date");
            entity.Property(e => e.CheckName)
                .HasMaxLength(500)
                .HasColumnName("check_name");
            entity.Property(e => e.CheckNo)
                .HasMaxLength(500)
                .HasColumnName("check_no");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CreditAccountId).HasColumnName("credit_account_id");
            entity.Property(e => e.CreditCostCenterId).HasColumnName("credit_cost_center_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.DebitAccountId).HasColumnName("debit_account_id");
            entity.Property(e => e.DebitCostCenterId).HasColumnName("debit_cost_center_id");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.HumId).HasColumnName("hum_id");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .HasColumnName("method");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.TransDate).HasColumnName("trans_date");
            entity.Property(e => e.TransName)
                .HasMaxLength(50)
                .HasColumnName("trans_name");
            entity.Property(e => e.TransRef)
                .HasMaxLength(50)
                .HasColumnName("trans_ref");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");
        });

        modelBuilder.Entity<TblReceiptVoucherDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_receipt_voucher_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.HumId).HasColumnName("hum_id");
            entity.Property(e => e.InvCode)
                .HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("inv_code");
            entity.Property(e => e.InvId).HasColumnName("inv_id");
            entity.Property(e => e.Payment)
                .HasPrecision(20, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("payment");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Total)
                .HasPrecision(20, 2)
                .HasDefaultValueSql("'0.00'")
                .HasColumnName("total");
        });

        modelBuilder.Entity<TblRmsdetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PRIMARY");

            entity
                .ToTable("tbl_rmsdetails")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.DetailId).HasColumnName("DetailID");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.MainId).HasColumnName("MainID");
            entity.Property(e => e.ProId).HasColumnName("proID");
            entity.Property(e => e.Qty).HasColumnName("qty");
        });

        modelBuilder.Entity<TblRmsmain>(entity =>
        {
            entity.HasKey(e => e.MainId).HasName("PRIMARY");

            entity
                .ToTable("tbl_rmsmain")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.ADate).HasColumnName("aDate");
            entity.Property(e => e.Changetot).HasColumnName("changetot");
            entity.Property(e => e.CusTname)
                .HasMaxLength(50)
                .HasColumnName("CusTName");
            entity.Property(e => e.CustPhone).HasMaxLength(50);
            entity.Property(e => e.DriverId).HasColumnName("DriverID");
            entity.Property(e => e.OrderType)
                .HasMaxLength(50)
                .HasColumnName("orderType");
            entity.Property(e => e.PaidSt).HasMaxLength(50);
            entity.Property(e => e.Received).HasColumnName("received");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.TableName)
                .HasMaxLength(50)
                .HasColumnName("tableName");
            entity.Property(e => e.Time)
                .HasMaxLength(50)
                .HasColumnName("time");
            entity.Property(e => e.UserSale).HasMaxLength(50);
            entity.Property(e => e.WaiterName)
                .HasMaxLength(50)
                .HasColumnName("waiterName");
        });

        modelBuilder.Entity<TblRmstable>(entity =>
        {
            entity.HasKey(e => e.Tid).HasName("PRIMARY");

            entity
                .ToTable("tbl_rmstables")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Tid).HasColumnName("tid");
            entity.Property(e => e.Tname)
                .HasMaxLength(50)
                .HasColumnName("tname");
        });

        modelBuilder.Entity<TblSalary>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_salary")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Change)
                .HasPrecision(20, 6)
                .HasColumnName("change");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.EmployeeName)
                .HasMaxLength(500)
                .HasColumnName("employee_name");
            entity.Property(e => e.Month)
                .HasMaxLength(50)
                .HasColumnName("month");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 6)
                .HasColumnName("pay");
            entity.Property(e => e.Salary)
                .HasPrecision(20, 6)
                .HasColumnName("salary");
            entity.Property(e => e.Year).HasColumnName("year");
        });

        modelBuilder.Entity<TblSalaryAdjustment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_salary_adjustments")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AdjustmentType)
                .HasMaxLength(50)
                .HasColumnName("adjustment_type");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 6)
                .HasColumnName("amount");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .HasColumnName("code");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Description)
                .HasMaxLength(250)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(250)
                .HasColumnName("name");
            entity.Property(e => e.RefId).HasColumnName("ref_id");
        });

        modelBuilder.Entity<TblSale>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sales")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountCashId).HasColumnName("account_cash_id");
            entity.Property(e => e.BillTo)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("bill_to");
            entity.Property(e => e.Change)
                .HasPrecision(20, 4)
                .HasColumnName("change");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("city");
            entity.Property(e => e.CostCenterId)
                .HasPrecision(20, 6)
                .HasColumnName("cost_center_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Discount)
                .HasPrecision(20, 6)
                .HasColumnName("discount");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.Net)
                .HasPrecision(20, 4)
                .HasColumnName("net");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 4)
                .HasColumnName("pay");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentTerms)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_terms");
            entity.Property(e => e.PoNum)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("po_num");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SalesMan)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("sales_man");
            entity.Property(e => e.ShipDate).HasColumnName("ship_date");
            entity.Property(e => e.ShipTo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("ship_to");
            entity.Property(e => e.ShipVia)
                .HasMaxLength(50)
                .HasColumnName("ship_via");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 4)
                .HasColumnName("vat");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblSalesDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sales_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 4)
                .HasColumnName("cost_price");
            entity.Property(e => e.Discount)
                .HasPrecision(20, 4)
                .HasColumnName("discount");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Price)
                .HasPrecision(20, 4)
                .HasColumnName("price");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.SalesId).HasColumnName("sales_id");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat).HasColumnName("vat");
            entity.Property(e => e.Vatp)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("vatp");
        });

        modelBuilder.Entity<TblSalesOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sales_order")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountCashId).HasColumnName("account_cash_id");
            entity.Property(e => e.BillTo)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("bill_to");
            entity.Property(e => e.Change)
                .HasPrecision(20, 4)
                .HasColumnName("change");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("city");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.InvId).HasColumnName("inv_id");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.Net)
                .HasPrecision(20, 4)
                .HasColumnName("net");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 4)
                .HasColumnName("pay");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentTerms)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_terms");
            entity.Property(e => e.PoNum)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("po_num");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SalesId).HasColumnName("sales_id");
            entity.Property(e => e.SalesMan)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("sales_man");
            entity.Property(e => e.ShipDate).HasColumnName("ship_date");
            entity.Property(e => e.ShipTo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("ship_to");
            entity.Property(e => e.ShipVia)
                .HasMaxLength(50)
                .HasColumnName("ship_via");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.TranferStatus).HasColumnName("tranfer_status");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 4)
                .HasColumnName("vat");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblSalesOrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sales_order_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 4)
                .HasColumnName("cost_price");
            entity.Property(e => e.Discount)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("discount");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Price)
                .HasPrecision(20, 4)
                .HasColumnName("price");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.SalesId).HasColumnName("sales_id");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat).HasColumnName("vat");
            entity.Property(e => e.Vatp)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("vatp");
        });

        modelBuilder.Entity<TblSalesProforma>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sales_proforma")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountCashId).HasColumnName("account_cash_id");
            entity.Property(e => e.BillTo)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("bill_to");
            entity.Property(e => e.Change)
                .HasPrecision(20, 4)
                .HasColumnName("change");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("city");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.Net)
                .HasPrecision(20, 4)
                .HasColumnName("net");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 4)
                .HasColumnName("pay");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentTerms)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_terms");
            entity.Property(e => e.PoNum)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("po_num");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SalesId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("sales_id");
            entity.Property(e => e.SalesMan)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("sales_man");
            entity.Property(e => e.ShipDate).HasColumnName("ship_date");
            entity.Property(e => e.ShipTo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("ship_to");
            entity.Property(e => e.ShipVia)
                .HasMaxLength(50)
                .HasColumnName("ship_via");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.TranferStatus)
                .HasDefaultValueSql("'0'")
                .HasColumnName("tranfer_status");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 4)
                .HasColumnName("vat");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblSalesProformaDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sales_proforma_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 4)
                .HasColumnName("cost_price");
            entity.Property(e => e.Discount)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("discount");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Price)
                .HasPrecision(20, 4)
                .HasColumnName("price");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.SalesId).HasColumnName("sales_id");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat).HasColumnName("vat");
            entity.Property(e => e.Vatp)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("vatp");
        });

        modelBuilder.Entity<TblSalesQuotation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sales_quotation")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountCashId).HasColumnName("account_cash_id");
            entity.Property(e => e.BillTo)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("bill_to");
            entity.Property(e => e.Change)
                .HasPrecision(20, 4)
                .HasColumnName("change");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("city");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.Net)
                .HasPrecision(20, 4)
                .HasColumnName("net");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 4)
                .HasColumnName("pay");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentTerms)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_terms");
            entity.Property(e => e.PoNum)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("po_num");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SalesId)
                .HasDefaultValueSql("'0'")
                .HasColumnName("sales_id");
            entity.Property(e => e.SalesMan)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("sales_man");
            entity.Property(e => e.ShipDate).HasColumnName("ship_date");
            entity.Property(e => e.ShipTo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("ship_to");
            entity.Property(e => e.ShipVia)
                .HasMaxLength(50)
                .HasColumnName("ship_via");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.TranferStatus)
                .HasDefaultValueSql("'0'")
                .HasColumnName("tranfer_status");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 4)
                .HasColumnName("vat");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblSalesQuotationDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sales_quotation_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 4)
                .HasColumnName("cost_price");
            entity.Property(e => e.Discount)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("discount");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Price)
                .HasPrecision(20, 4)
                .HasColumnName("price");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.SalesId).HasColumnName("sales_id");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat).HasColumnName("vat");
            entity.Property(e => e.Vatp)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("vatp");
        });

        modelBuilder.Entity<TblSalesReturn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sales_return")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountCashId).HasColumnName("account_cash_id");
            entity.Property(e => e.BillTo)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("bill_to");
            entity.Property(e => e.Change)
                .HasPrecision(20, 4)
                .HasColumnName("change");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("city");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.InvoiceId)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("invoice_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.Net)
                .HasPrecision(20, 4)
                .HasColumnName("net");
            entity.Property(e => e.SalesRefId)
                .HasColumnName("sales_ref_id");
            entity.Property(e => e.Pay)
                .HasPrecision(20, 4)
                .HasColumnName("pay");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentTerms)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("payment_terms");
            entity.Property(e => e.PoNum)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("po_num");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SalesMan)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("sales_man");
            entity.Property(e => e.ShipDate).HasColumnName("ship_date");
            entity.Property(e => e.ShipTo)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("ship_to");
            entity.Property(e => e.ShipVia)
                .HasMaxLength(50)
                .HasColumnName("ship_via");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat)
                .HasPrecision(20, 4)
                .HasColumnName("vat");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
        });

        modelBuilder.Entity<TblSalesReturnDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sales_return_details")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CostCenterId).HasColumnName("cost_center_id");
            entity.Property(e => e.CostPrice)
                .HasPrecision(20, 4)
                .HasColumnName("cost_price");
            entity.Property(e => e.Discount)
                .HasPrecision(20, 4)
                .HasColumnName("discount");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Price)
                .HasPrecision(20, 4)
                .HasColumnName("price");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Qty)
                .HasPrecision(20, 4)
                .HasColumnName("qty");
            entity.Property(e => e.SalesId).HasColumnName("sales_id");
            entity.Property(e => e.Total)
                .HasPrecision(20, 4)
                .HasColumnName("total");
            entity.Property(e => e.Vat).HasColumnName("vat");
            entity.Property(e => e.Vatp)
                .HasPrecision(20, 4)
                .HasDefaultValueSql("'0.0000'")
                .HasColumnName("vatp");
        });

        modelBuilder.Entity<TblSecRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sec_roles")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblSecRoleForm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sec_role_form")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FormId).HasColumnName("form_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
        });

        modelBuilder.Entity<TblSecUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sec_users")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.EmpId)
                .HasMaxLength(256)
                .HasColumnName("emp_id");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .HasColumnName("last_name");
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.PasswordLastUpdate).HasColumnName("password_last_update");
            entity.Property(e => e.PasswordUpdatedBy).HasColumnName("password_updated_by");
            entity.Property(e => e.RoleId).HasColumnName("Role_Id");
            entity.Property(e => e.Salt)
                .HasMaxLength(256)
                .HasColumnName("salt");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .HasColumnName("user_name");
        });

        modelBuilder.Entity<TblSetMainMenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_set_main_menu")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblSetMenuForm>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_set_menu_forms")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FormName)
                .HasMaxLength(50)
                .HasColumnName("form_name");
            entity.Property(e => e.FormText)
                .HasMaxLength(50)
                .HasColumnName("form_text");
            entity.Property(e => e.MenuId).HasColumnName("menu_id");
            entity.Property(e => e.Params)
                .HasMaxLength(200)
                .HasColumnName("params");
            entity.Property(e => e.Seq).HasColumnName("seq");
        });

        modelBuilder.Entity<TblSettingAttendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_setting_attendance")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Day)
                .HasMaxLength(150)
                .HasColumnName("day");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Timein)
                .HasColumnType("time")
                .HasColumnName("timein");
            entity.Property(e => e.Timeout)
                .HasColumnType("time")
                .HasColumnName("timeout");
        });

        modelBuilder.Entity<TblSettingDeductionConfig>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_setting_deduction_config")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Delaytime)
                .HasColumnType("time")
                .HasColumnName("delaytime");
            entity.Property(e => e.Latearrivaldeduction)
                .HasPrecision(20, 2)
                .HasColumnName("latearrivaldeduction");
        });

        modelBuilder.Entity<TblSettingDefaultAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_setting_default_account")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Level4Id).HasColumnName("level4_id");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasDefaultValueSql("'0'")
                .HasColumnName("type");
        });

        modelBuilder.Entity<TblSubCostCenter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_sub_cost_center")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.MainId).HasColumnName("main_id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
        });

        modelBuilder.Entity<TblSubMenu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_sub_menus");

            entity.HasIndex(e => e.MId, "m_id");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.MId).HasColumnName("m_id");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");

            entity.HasOne(d => d.MIdNavigation).WithMany(p => p.TblSubMenus)
                .HasForeignKey(d => d.MId)
                .HasConstraintName("tbl_sub_menus_ibfk_1");
        });

        modelBuilder.Entity<TblTax>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_tax")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.State)
                .HasDefaultValueSql("'0'")
                .HasColumnName("state");
            entity.Property(e => e.Value).HasColumnName("value");
        });

        modelBuilder.Entity<TblTenderName>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_tender_names")
                .UseCollation("utf8mb4_unicode_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasDefaultValueSql("'0'")
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblTool>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_tools")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsSelected)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_selected");
            entity.Property(e => e.ToolName)
                .HasMaxLength(50)
                .HasColumnName("tool_name");
        });

        modelBuilder.Entity<TblTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_transaction")
                .UseCollation("utf8mb4_general_ci");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Credit).HasPrecision(20, 4)
                .HasColumnName("credit");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Debit).HasPrecision(20, 4)
                .HasColumnName("debit");
            entity.Property(e => e.Description)
                .HasMaxLength(200).HasColumnName("description");
            entity.Property(e => e.HumId).HasDefaultValueSql("'0'")
                .HasColumnName("hum_id");
            entity.Property(e => e.ModifiedBy).HasColumnName("modified_by");
            entity.Property(e => e.ModifiedDate).HasColumnName("modified_date");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.TType).HasMaxLength(200)
                .HasColumnName("t_type");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.Type).HasMaxLength(200)
                .HasColumnName("type");
            entity.Property(e => e.VoucherNo).HasMaxLength(50)
                .HasDefaultValueSql("''")
                .HasColumnName("voucher_no");
        });

        modelBuilder.Entity<TblUnit>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_unit")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasDefaultValueSql("'0'")
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblUserPermission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tbl_user_permissions");

            entity.HasIndex(e => new { e.UserId, e.SubMenuId }, "uniq_user_submenu").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CanDelete).HasColumnName("can_delete");
            entity.Property(e => e.CanEdit).HasColumnName("can_edit");
            entity.Property(e => e.CanView).HasColumnName("can_view");
            entity.Property(e => e.SubMenuId).HasColumnName("sub_menu_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<TblVatConfigration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_vat_configration")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.QuarterFourDueDate).HasColumnName("quarter_four_due_date");
            entity.Property(e => e.QuarterFourEndDate).HasColumnName("quarter_four_end_date");
            entity.Property(e => e.QuarterFourStartDate).HasColumnName("quarter_four_start_date");
            entity.Property(e => e.QuarterOneDueDate).HasColumnName("quarter_one_due_date");
            entity.Property(e => e.QuarterOneEndDate).HasColumnName("quarter_one_end_date");
            entity.Property(e => e.QuarterOneStartDate).HasColumnName("quarter_one_start_date");
            entity.Property(e => e.QuarterThreeDueDate).HasColumnName("quarter_three_due_date");
            entity.Property(e => e.QuarterThreeEndDate).HasColumnName("quarter_three_end_date");
            entity.Property(e => e.QuarterThreeStartDate).HasColumnName("quarter_three_start_date");
            entity.Property(e => e.QuarterTwoDueDate).HasColumnName("quarter_two_due_date");
            entity.Property(e => e.QuarterTwoEndDate).HasColumnName("quarter_two_end_date");
            entity.Property(e => e.QuarterTwoStartDate).HasColumnName("quarter_two_start_date");
            entity.Property(e => e.RegistrationNo)
                .HasMaxLength(100)
                .HasColumnName("registration_no");
            entity.Property(e => e.TrnissueDate).HasColumnName("TRNIssue_date");
        });

        modelBuilder.Entity<TblVendor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_vendor")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Balance).HasPrecision(20, 4);
            entity.Property(e => e.BuildingName)
                .HasMaxLength(50)
                .HasColumnName("building_name");
            entity.Property(e => e.CatId).HasColumnName("Cat_id");
            entity.Property(e => e.Ccemail)
                .HasMaxLength(50)
                .HasColumnName("ccemail");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasColumnName("city");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.Country)
                .HasMaxLength(50)
                .HasColumnName("country");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .HasColumnName("email");
            entity.Property(e => e.FaciltyName)
                .HasMaxLength(50)
                .HasColumnName("facilty_name");
            entity.Property(e => e.MainPhone)
                .HasMaxLength(20)
                .HasColumnName("main_phone");
            entity.Property(e => e.Mobile)
                .HasMaxLength(50)
                .HasColumnName("mobile");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectSite)
                .HasMaxLength(150)
                .HasDefaultValueSql("''")
                .HasColumnName("project_site");
            entity.Property(e => e.Region)
                .HasMaxLength(50)
                .HasColumnName("region");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.Trn)
                .HasMaxLength(50)
                .HasColumnName("trn");
            entity.Property(e => e.Type)
                .HasMaxLength(150)
                .HasDefaultValueSql("'Vendor'")
                .HasColumnName("type");
            entity.Property(e => e.Website)
                .HasMaxLength(50)
                .HasColumnName("website");
            entity.Property(e => e.WorkPhone)
                .HasMaxLength(20)
                .HasColumnName("work_phone");
        });

        modelBuilder.Entity<TblVendorCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_vendor_category")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasDefaultValueSql("'0'")
                .HasColumnName("name");
        });

        modelBuilder.Entity<TblWarehouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity
                .ToTable("tbl_warehouse")
                .UseCollation("utf8mb4_general_ci");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.BuildingName)
                .HasMaxLength(500)
                .HasColumnName("building_name");
            entity.Property(e => e.City)
                .HasMaxLength(50)
                .HasColumnName("city");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .HasColumnName("code");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedDate).HasColumnName("created_date");
            entity.Property(e => e.EmpId).HasColumnName("emp_id");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.State).HasColumnName("state");
        });

        modelBuilder.Entity<TblSale>()
         .HasOne(s => s.TblTransaction)
         .WithOne(t => t.Sale)
         .HasForeignKey<TblTransaction>(t => t.TransactionId);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
