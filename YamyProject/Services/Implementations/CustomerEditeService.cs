using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Reflection;
using System.Xml.Linq;

namespace YamyProject.Services.Implementations
{
    public class CustomerEditeService(YamyDbContext context, IListServices listServices, IHttpContextAccessor httpContextAccessor, IGlobalService GlobalService) : IEditeCustomerService
    {
        private readonly YamyDbContext _context = context;
        private readonly IListServices _ListServices = listServices;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;

        public async Task<CustomerViewModel> GetCustomerFormDataAsync(int? id)
        {
            var vm = new CustomerViewModel
            {
               };

           
         if (id.HasValue)
          {
                    vm.Customer = await _context.TblCustomers
                     .FirstOrDefaultAsync(c => c.Id == id.Value) ?? new TblCustomer();

                    vm.Transactions = await _context.TblTransactions
                      .FirstOrDefaultAsync(t => t.HumId == id.Value && t.Type == "Customer Opening Balance") ?? new TblTransaction();
            }
            
            return vm;
        }

        public async Task<CustomerViewModel> GetCreateCustomerFormAsync()
        {
            // Initialize a new customer view model for creation
            var vm = new CustomerViewModel
            {
                Customer = new TblCustomer(),
                Transactions =new TblTransaction() // empty for new customer
            };
            return vm;
        }
        public  string GenerateNextCustomerCode()
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue =  _context.TblCustomers
               .Select(s => s.Code)
               .MaxAsync();
            prefix = lastCodeValue.ToString();
            return $"{int.Parse(prefix) + 1:D5}";
            }
        public async Task SaveCustomerAsync(TblCustomer customer)
        {
            

            if (customer.Id == 0)
                {
                var code =  GenerateNextCustomerCode();
                var customerRow = new TblCustomer {
                    Name = customer.Name,
                    Code = int.Parse(code),//customer.Code,
                    CatId=customer.CatId,
                 //   if(customer.CreditNotes != null) ,
                    Balance=customer.Balance,
                    Mobile=customer.Mobile,
                    Email=customer.Email,
                    Country=customer.Country,
                    City=customer.City,
                    Region=customer.Region,
                    BuildingName=customer.BuildingName,
                    AccountId=customer.AccountId,
                    Trn=customer.Trn,
                    FaciltyName=customer.FaciltyName,
                    Active=1,
                    CreatedBy= _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,
                     CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    State =customer.State,
                    ProjectId=customer.ProjectId,
                    ProjectSite=customer.ProjectSite
                    };

                _context.TblCustomers.Add(customerRow);
            ProcessOpeningBalanceTransactions(customerRow);

                _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "INSERT", "Customer Center", customerRow.Id, "New Customer Created - Code: " + customerRow.Code + ", Name: " + customerRow.Name);
                }
            else
                {
                var customerRow = await _context.TblCustomers.FindAsync(customer.Id);
                customerRow.Name = customer.Name;
                customerRow.Code = customer.Code;
                customerRow.CatId = customer.CatId;
                // customerRow.  if(customer.CreditNotes != null) ,
                customerRow.Balance = customer.Balance;
                customerRow.Mobile = customer.Mobile;
                customerRow.Email = customer.Email;
                customerRow.Country = customer.Country;
                customerRow.City = customer.City;
                customerRow.Region = customer.Region;
                customerRow.BuildingName = customer.BuildingName;
                customerRow.AccountId = customer.AccountId;
                customerRow.Trn = customer.Trn;
                customerRow.FaciltyName = customer.FaciltyName;
                customerRow.Active =customer.Active==1 ? 0 : -1;
                //customerRow. CreatedBy=customer.CreatedBy,
                //customerRow. CreatedDate=customer.CreatedDate,
                customerRow.State = customer.State;
                customerRow.ProjectId = customer.ProjectId;
                customerRow.ProjectSite = customer.ProjectSite;
               // _context.TblCustomers.Update(customer);
                }

            await _context.SaveChangesAsync();
            ProcessOpeningBalanceTransactions(customer);
        }
        public async Task ProcessOpeningBalanceTransactions(TblCustomer customer)
            {
            var Account = customer.AccountId;
            var openingBalanceEquity = _ListServices.DefaultAccountsSet("Opening Balance Equity").ToString();
            var result = _context.TblCoaLevel4s
                .FirstOrDefaultAsync(a => a.Name == "Opening Balance Equity");
            if (openingBalanceEquity == "0")
                {
                openingBalanceEquity = result.Id.ToString();
                }
            if (customer.Balance != null && customer.Balance != 0)
                {
                if (customer.Balance < 0)
                    {
                    AddTransactionEntry(customer.Date, int.Parse(openingBalanceEquity),(decimal)customer.Balance, 0,customer.Id,0, "Customer Opening Balance", "OPENING BALANCE", "Opening Balance Equity - Customer Code - " + customer.Code, _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow),"");
                    AddTransactionEntry(customer.Date,Account,0, (decimal)customer.Balance,customer.Id,customer.Id, "Customer Opening Balance", "OPENING BALANCE", "Opening Balance - Customer Code - " + customer.Code, _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow), "");
                    }
                else if (customer.Balance > 0)
                    {
                    AddTransactionEntry(customer.Date, int.Parse(openingBalanceEquity),0, (decimal)customer.Balance,customer.Id,0, "Customer Opening Balance", "OPENING BALANCE", "Opening Balance Equity - Customer Code - " + customer.Code, _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow),"");
                    AddTransactionEntry(customer.Date, Account, (decimal)customer.Balance,0, customer.Id, customer.Id, "Customer Opening Balance", "OPENING BALANCE", "Opening Balance - Customer Code - " + customer.Code, _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow), "");
                    }


                }
            }
        public async Task AddTransactionEntry(DateOnly? date, int? accountId, decimal debit, decimal credit, int transactionId, int humId, string type, string voucherName,
           string description, int createdBy, DateOnly createdDate, string VoucherNo)
            {
            var trx = new TblTransaction
                {
                Date = date,
                AccountId = accountId,
                Debit = debit,
                Credit = credit,
                TransactionId = transactionId,
                HumId = humId,
                TType = voucherName,
                Type = type,
                Description = description,
                CreatedBy = createdBy,
                VoucherNo = VoucherNo,
                CreatedDate = createdDate,
                State = 0
                };

            _context.TblTransactions.Add(trx);
            await _context.SaveChangesAsync();
            }
        }
}
    