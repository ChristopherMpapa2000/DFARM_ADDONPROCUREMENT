using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AddonProcurement.Models.CustomEntity.Models;

namespace AddonProcurement.Models
{
    public class BudgetReportItem
    {
        public string BudgetCostCenter { get; set; }
        public string GLAccount { get; set; }
        public string InternalOrder { get; set; }
        public string DocumentNo { get; set; }
        public int MemoId { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public decimal Summary_Alloc { get; set; }
        public decimal Summary_Reserve { get; set; }
        public decimal Summary_Used { get; set; }
        public decimal Summary_Paid { get; set; }
        public decimal Summary_Cancelled { get; set; }
        public decimal Summary_Remaining { get; set; }
        public decimal Sum_Reserve { get; set; }
        public decimal Sum_Used { get; set; }
        public decimal Sum_Paid { get; set; }
        public List<BudgetReportItem> Children { get; set; } = new List<BudgetReportItem>();
    }

    public class BudgetData
    {
        public string BudgetCostCenter { get; set; }
        public string GLAccount { get; set; }
        public string InternalOrder { get; set; }
        public decimal Amount { get; set; }
        public IEnumerable<BudgetTransaction> Transactions { get; set; } = new List<BudgetTransaction>();
    }

    public class ReportReposne
    {
        public int TotalCount { get; set; }
        public List<BudgetReportItem> ReportData { get; set; }
    }

    public class IO
    {
        public int BudgetYear { get; set; }
        public string BudgetCostCenter { get; set; }
        public string GLAccount { get; set; }
        public string InternalOrder { get; set; }
        public string Key { get; set; }
        public string Type { get; set; }
        public string Desc { get; set; }
        public decimal Summary_Alloc { get; set; }
        public decimal Summary_Reserve { get; set; }
        public decimal Summary_Used { get; set; }
        public decimal Summary_Paid { get; set; }
        public decimal Summary_Cancelled { get; set; }
        public decimal Summary_Remaining { get; set; }
    }

    public class GL
    {
        public int BudgetYear { get; set; }
        public string BudgetCostCenter { get; set; }
        public string GLAccount { get; set; }
        public string Key { get; set; }
        public string Type { get; set; }
        public string Desc { get; set; }
        public decimal Summary_Alloc { get; set; }
        public decimal Summary_Reserve { get; set; }
        public decimal Summary_Used { get; set; }
        public decimal Summary_Paid { get; set; }
        public decimal Summary_Cancelled { get; set; }
        public decimal Summary_Remaining { get; set; }
        public List<IO> Children { get; set; } = new List<IO>();
    }

    public class Cost
    {
        public int BudgetYear { get; set; }
        public string BudgetCostCenter { get; set; }
        public string Key { get; set; }
        public string Type { get; set; }
        public string Desc { get; set; }
        public decimal Summary_Alloc { get; set; }
        public decimal Summary_Reserve { get; set; }
        public decimal Summary_Used { get; set; }
        public decimal Summary_Paid { get; set; }
        public decimal Summary_Cancelled { get; set; }
        public decimal Summary_Remaining { get; set; }
        public List<GL> Children { get; set; } = new List<GL>();
    }

}
