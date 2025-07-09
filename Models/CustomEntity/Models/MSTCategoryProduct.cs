using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models.CustomEntity.Models
{
    public class MSTCategoryProduct
    {
        public int RowID { get; set; }
        public int ItemID { get; set; }
        public string CategoryGroupCode { get; set; }
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public string ItemPropertise { get; set; }
        public string ItemUnit { get; set; }
    }
}
