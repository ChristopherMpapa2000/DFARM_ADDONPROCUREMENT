using System.Collections.Generic;

namespace AddonProcurement.Models.RequestModel
{
    public class RequestCheckBudget
    {
        public BudgetHeader Header { get; set; }
        public List<Order> Orders {  get; set; }
    }

    public class BudgetHeader
    {
        public string DocumentNo { get; set; }
    }

    public class Order
    {
        public string BudgetYear { get; set; }
        public string Costcenter { get; set; }
        public string GLAccount { get; set; }
        public string IO { get; set; } = string.Empty;
        public string Amount { get; set; }
    }
}
