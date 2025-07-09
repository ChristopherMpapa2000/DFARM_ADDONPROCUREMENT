using System;

namespace AddonProcurement.Models.CustomEntity.Models
{
    public class BudgetTransaction
    {
        public int Id { get; set; }
        public int BudgetYear { get; set; }
        public string BudgetCostCenter { get; set; }
        public string GLAccount { get; set; }
        public string InternalOrder { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string DocumentNo { get; set; }
        public string Description { get; set; }
    }

    public class ViewBudgetTransaction  
    {
        public int Id { get; set; }
        public int BudgetYear { get; set; }
        public int MemoId { get; set; }
        public int TemplateId { get; set; }
        public string DocumentCode { get; set; }
        public string BudgetCostCenter { get; set; }
        public string GLAccount { get; set; }
        public string InternalOrder { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string DocumentNo { get; set; }
        public string Description { get; set; }
    }

    public class ViewBudgetTransactionDto
    {
        public int Id { get; set; }
        public int BudgetYear { get; set; }
        public int MemoId { get; set; }
        public int TemplateId { get; set; }
        public string DocumentCode { get; set; }
        public string BudgetCostCenter { get; set; }
        public string GLAccount { get; set; }
        public string InternalOrder { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string DocumentNo { get; set; }
        public string Description { get; set; }
    }
}
