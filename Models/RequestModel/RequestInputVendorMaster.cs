using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models.RequestModel
{
    public class RequestInputVendorMaster
    {
        public HeaderModel Header { get; set; }
        public List<WHTItemModel> WHTItems { get; set; }
    }
    public class HeaderModel
    {
        public string Activity { get; set; }
        public string PartnerNumber { get; set; }
        public string BPGroup { get; set; }
        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public string Name3 { get; set; }
        public string Name4 { get; set; }
        public string SearchTerm1 { get; set; }
        public string SearchTerm2 { get; set; }
        public string Street { get; set; }
        public string Street2 { get; set; }
        public string Street3 { get; set; }
        public string Street4 { get; set; }
        public string Dirtric { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string TaxCategory { get; set; }
        public string TaxID { get; set; }
        public string TradingPartner { get; set; }
        public string ID { get; set; }
        public string BankCountry { get; set; }
        public string BankKey { get; set; }
        public string BankAccountNumber { get; set; }
        public string InstructionKey { get; set; }
        public string CompanyCode { get; set; }
        public string ReconciliationAcct { get; set; }
        public string SortKey { get; set; }
        public string PaymentTerm { get; set; }
        public string PaymentMethod { get; set; }
        public string WHTType1 { get; set; }
        public string SubjectTax1 { get; set; }
        public string WHTCode1 { get; set; }
        public string TypeOfRecipient1 { get; set; }
        public string WHTType2 { get; set; }
        public string SubjectTax2 { get; set; }
        public string WHTCode2 { get; set; }
        public string TypeOfRecipient2 { get; set; }
        public string WHTType3 { get; set; }
        public string SubjectTax3 { get; set; }
        public string WHTCode3 { get; set; }
        public string TypeOfRecipient3 { get; set; }
        public string WHTType4 { get; set; }
        public string SubjectTax4 { get; set; }
        public string WHTCode4 { get; set; }
        public string TypeOfRecipient4 { get; set; }
        public string Branch { get; set; }
        public string BranchDes { get; set; }
        public string PostingBlock { get; set; }
        public string MarkForDelete { get; set; }
    }

    public class WHTItemModel
    {
        public string WHTType { get; set; }
        public string SubjectTax { get; set; }
        public string WHTCode { get; set; }
        public string TypeOfRecipient { get; set; }
        public string WHTTypeDetail { get; set; }
    }
}
