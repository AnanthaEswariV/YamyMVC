namespace YamyProject.Core.ViewModel
{
    // ViewModels/SalesViewModels.cs

    namespace YourApp.ViewModels
    {
        // Top-level VM used for Create and Edit pages
        public class SalesEditViewModel
        {
            public int? Id { get; set; } // null => create

            [Required]
            public DateTime Date { get; set; }

            [Required]
            public int CustomerId { get; set; }

            [Required]
            public string InvoiceId { get; set; }

            [Required]
            public int WarehouseId { get; set; }

            public string PoNum { get; set; }
            public string BillTo { get; set; }
            public string City { get; set; }
            public string SalesMan { get; set; }
            public DateTime? ShipDate { get; set; }
            public string ShipVia { get; set; }
            public string ShipTo { get; set; }

            [Required]
            public string PaymentMethod { get; set; } // "Cash" | "Credit"

            public int AccountCashId { get; set; }
            public string PaymentTerms { get; set; }
            public DateTime? PaymentDate { get; set; }

            [Range(0, double.MaxValue)]
            public decimal Total { get; set; }

            [Range(0, double.MaxValue)]
            public decimal Vat { get; set; }

            [Range(0, double.MaxValue)]
            public decimal Net { get; set; }

            public decimal Pay { get; set; }
            public decimal Change { get; set; }

            // Business/Accounting fields referenced in frmSales
            public int CreatedBy { get; set; }
            public DateTime CreatedDate { get; set; }

            // List of details
            public List<SalesDetailViewModel> Details { get; set; } = new();

            // UI helpers
            public string NextInvoiceCode { get; set; }
        }

        // Lightweight DTO returned to UI lists
       
    }

}
