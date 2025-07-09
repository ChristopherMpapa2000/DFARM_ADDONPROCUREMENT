using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolfApprove.Model.CustomClass;

namespace AddonProcurement.Models.RequestModel
{
    public class RequestMadvanceForm : CustomClass
    {
        [JsonProperty("template_ID")]
        public int TemplateId { get; set; }
        public List<int> MemoIds { get; set; } = new List<int>();

    }
}
