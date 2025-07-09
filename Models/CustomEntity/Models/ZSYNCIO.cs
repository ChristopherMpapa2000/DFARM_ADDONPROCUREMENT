using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models.CustomEntity.Models
{
    public class ZSYNCIO : BaseEntity
    {
        public int Id { get; set; }

        public string InternalOrder { get; set; }

        public string Name { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

    }
}
