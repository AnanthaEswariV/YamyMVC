namespace YamyProject.Core.ViewModel
{
    public class VatConfigurationViewModel
    {
        public TblVatConfigration Vat { get; set; } = new TblVatConfigration();
        public TblCorporateTaxConfigration CorporateTax { get; set; } = new TblCorporateTaxConfigration();

    }
}
