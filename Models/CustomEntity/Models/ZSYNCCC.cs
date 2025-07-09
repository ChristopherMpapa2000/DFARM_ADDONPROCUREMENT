using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models.CustomEntity.Models
{
    public class ZSYNCCC : BaseEntity
    {
        public int Id { get; set; }
        public string CostCenterCode { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
