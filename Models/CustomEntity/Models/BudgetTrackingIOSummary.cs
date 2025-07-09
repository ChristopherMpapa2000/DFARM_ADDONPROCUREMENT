namespace AddonProcurement.Models.CustomEntity.Models
{
    public class BudgetTrackingIOSummary : BaseBudget
    {
        public string BudgetCostCenter { get; set; }
        public string BudgetCostCenterName { get; set; }

        public int BudgetYear { get; set; }
        public string GLAccount { get; set; }
        public string GLAccountName { get; set; }

        public string InternalOrder { get; set; }
        public string InternalOrderName { get; set; }

    }
    public class BudgetTrackingIOSummaryDto : BaseBudget
    {
        public string BudgetCostCenter { get; set; }
        public string BudgetCostCenterName { get; set; }

        public int BudgetYear { get; set; }
        public string GLAccount { get; set; }
        public string GLAccountName { get; set; }

        public string InternalOrder { get; set; }
        public string InternalOrderName { get; set; }

    }
}
