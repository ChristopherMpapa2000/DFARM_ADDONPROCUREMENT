using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models.CustomEntity.Models
{
    public class MSTAsset : BaseEntity
    {
        public int Id { get; set; }

        public string GLAccountCode { get; set; }

        public string AssetClass { get; set; }

        public string CategoryCode { get; set; } 

        public string AssetCode { get; set; }

        public string AssetName { get; set; }

    }
}
