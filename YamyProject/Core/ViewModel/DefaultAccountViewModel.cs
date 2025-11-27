using System.Security.Policy;

namespace YamyProject.Core.ViewModel
    {
    public class DefaultAccountViewModel
        {
        public int? PettyCashAccountId { get; set; }
        // Left (PDC)
        public int? PdcReceivableId { get; set; }
        public int? PdcReceivableReturnId { get; set; }
        public int? PdcReceivableHoldId { get; set; }
        public int? PdcPayableId { get; set; }
        public int? PdcPayableReturnId { get; set; }
        public int? PdcPayableHoldId { get; set; }
        // Right (Cash/OB/Prepaid/FA)
        public int? DefaultCashAccountId { get; set; }
        public int? OpeningBalanceAccountId { get; set; }
        public int? OpeningBalanceEquityAccountId { get; set; }
        public int? PrepaidExpenseDebitAccountId { get; set; }
        public int? PrepaidExpenseCreditAccountId { get; set; }
        public int? FixedAssetDebitAccountId { get; set; }
        public int? FixedAssetCreditAccountId { get; set; }
        ///VendorPurchaseSection 
        public int? VendorId { get; set; }
        public int? VatInputId { get; set; }
        public int? PurchasePaymentCashMethodId { get; set; }
        public int? PurchaseInvoiceId { get; set; }
        public int? PurchaseReturnInvoiceId { get; set; }
        /////InventorySection
        public int? InventoryId { get; set; }
        public int? ItemCogsId { get; set; }
        public int? InventoryDamageId { get; set; }
        public int? StockSettlementId { get; set; }
        ///CustomerSalesSection
        public int? CustomerId { get; set; }
        public int? InvoicePaymentCashMethodId { get; set; }
        public int? VatOutputId { get; set; }
        public int? SalesInvoiceId { get; set; }
        public int? SalesReturnInvoiceId { get; set; }
        ///EmployeeSection
        public int? AccruedSalariesId { get; set; }
        public int? SalariesId { get; set; }
        public int? AccrualLeaveSalaryId { get; set; }
        public int? EmployeeReceivableId { get; set; }
        public int? GratuityId { get; set; }
        public int? EosDebitId { get; set; }
        public int? EosCreditId { get; set; }
        public int? LeaveSalaryDebitId { get; set; }
        public int? LeaveSalaryCreditId { get; set; }
        ///BankOtherSection
        public int? DefaultBankAccountId { get; set; }
        }
    }
