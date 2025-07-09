using System;

namespace AddonProcurement.Models.CustomEntity.Models
{
    public class ZSYNCBUDGET
    {
        public int Id { get; set; }

        public string InterfaceModule { get; set; }

        public string InterfaceDocNo { get; set; }

        public DateTime? InterfaceDate { get; set; }

        public string InterfaceByCode { get; set; }

        public string InterfaceByName { get; set; }

        public string InterfaceStatus { get; set; }

        public string InterfaceText { get; set; }

        public int? BudgetYear { get; set; }

        public string BudgetAdjustCategory { get; set; }

        public string BudgetCostCenter { get; set; }

        public string BudgetCostCenterName { get; set; }

        public string GLAccount { get; set; }

        public string GLAccountName { get; set; }

        public string InternalOrder { get; set; }

        public string InternalOrderName { get; set; }

        public decimal? Amount { get; set; }

        public bool? IsActive { get; set; }
    }
}
