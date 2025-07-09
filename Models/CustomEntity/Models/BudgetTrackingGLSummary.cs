namespace AddonProcurement.Models.CustomEntity.Models
{
    public class BudgetTrackingGLSummary : BaseBudget
    {
        public string BudgetCostCenter { get; set; }
        public string BudgetCostCenterName { get; set; }
        public int BudgetYear { get; set; }
        public string GLAccount { get; set; }
        public string GLAccountName { get; set; }

    }

    public class BudgetTrackingGLSummaryDto : BaseBudget
    {
        public string BudgetCostCenter { get; set; }
        public string BudgetCostCenterName { get; set; }
        public int BudgetYear { get; set; }
        public string GLAccount { get; set; }
        public string GLAccountName { get; set; }

    }
    public class BudgetTrackingGLSummaryReport
    {
        public int BudgetYear { get; set; }
        public string BudgetCostCenter { get; set; }
        public string BudgetCostCenterName { get; set; }
        public string GLAccount { get; set; }
        public string GLAccountName { get; set; }
        public decimal Allocate { get; set; }
        public decimal Reserved { get; set; }
        public decimal Used { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
    }
}
