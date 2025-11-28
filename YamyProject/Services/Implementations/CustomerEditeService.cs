using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Reflection;
using System.Xml.Linq;

namespace YamyProject.Services.Implementations
{
    public class CustomerEditeService : IEditeCustomerService
    {
        private readonly YamyDbContext _context;
        public CustomerEditeService(YamyDbContext context) { _context = context; }
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
                   // CreatedBy=customer.CreatedBy,
                   // CreatedDate=customer.CreatedDate,
                    State=customer.State,
                    ProjectId=customer.ProjectId,
                    ProjectSite=customer.ProjectSite
                    }
            ;

                _context.TblCustomers.Add(customerRow);
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
        }
    }
}
    