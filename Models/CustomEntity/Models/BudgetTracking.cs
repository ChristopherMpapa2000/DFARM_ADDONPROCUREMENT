using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models.CustomEntity.Models
{
    public class BudgetTracking
    {
        public int Id { get; set; }
        public int BudgetYear { get; set; }
        public string BudgetCostCenter { get; set; }
        public string BudgetCostCenterName { get; set; }
        public string GLAccount { get; set; }
        public string GLAccountName { get; set; }
        public string InternalOrder { get; set; }
        public string InternalOrderName { get; set; }
        public decimal Reserved { get; set; }
        public decimal Used { get; set; }
        public decimal Paid { get; set; }
        public decimal Allocate { get; set; }
        public decimal Remaining { get; set; }
        public decimal Cancelled { get; set; }
    }

    public class BudgetTrackingDto
    {
        public int Id { get; set; }
        public int BudgetYear { get; set; }
        public string BudgetCostCenter { get; set; }
        public string BudgetCostCenterName { get; set; }
        public string GLAccount { get; set; }
        public string GLAccountName { get; set; }
        public string InternalOrder { get; set; }
        public string InternalOrderName { get; set; }
        public decimal Reserved { get; set; }
        public decimal Used { get; set; }
        public decimal Paid { get; set; }
        public decimal Allocate { get; set; }
        public decimal Remaining { get; set; }
        public decimal Cancelled { get; set; }
    }

    public class BaseBudget
    {
        public string Id { get; set; }
        public decimal Reserved { get; set; }
        public decimal Used { get; set; }
        public decimal Paid { get; set; }
        public decimal Allocate { get; set; }
        public decimal Remaining { get; set; }
        public decimal Cancelled { get; set; }
    }
}
