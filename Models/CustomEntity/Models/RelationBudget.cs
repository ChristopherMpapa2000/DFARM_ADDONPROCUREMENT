using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models.CustomEntity.Models
{
    public class RelationBudget
    {
        public string Id { get; set; }
        public int BudgetId { get; set; }
        public int MemoId { get; set; }
        public int RefByMemoId { get; set; }
    }
}
