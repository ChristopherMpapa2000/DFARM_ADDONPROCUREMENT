namespace AddonProcurement.Models.CustomEntity.Models
{
    public class BudgetTrackingSummary : BaseBudget
    {
        public string BudgetCostCenter { get; set; }
        public string BudgetCostCenterName { get; set; }
        public int? BudgetYear { get; set; }
    }
}
