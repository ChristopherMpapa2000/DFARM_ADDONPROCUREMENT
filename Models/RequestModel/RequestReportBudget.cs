using WolfApprove.Model.CustomClass;

namespace AddonProcurement.Models.RequestModel
{
    public class RequestReportBudget : CustomClass
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public FilterReportBudget Filter { get; set; }
    }

    public class FilterReportBudget
    {
        public int BudgetYear { get; set; } = 0;
        public string Description { get; set; }
        public string CostCenter { get; set; }
        public string GLAccount { get; set; }
        public string InternalOrder { get; set; }
    }
}
