using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AddonProcurement.GRServiceReference;

namespace AddonProcurement.Models.RequestModel
{
    public class RequestInputGRMaster
    {
        public BudgetHeader Header { get; set; }
        public List<Order> Orders { get; set; }
        public List<DT_TLI_AAI001_SNDRowCustom> APItems { get; set; }
        public List<DT_TLI_AAI001_SNDRowCustom> AAItems { get; set; }
    }
    public class DT_TLI_AAI001_SNDRowCustom : DT_TLI_AAI001_SNDRow
    {
        public string assetgroup { get; set; }
    }
}
