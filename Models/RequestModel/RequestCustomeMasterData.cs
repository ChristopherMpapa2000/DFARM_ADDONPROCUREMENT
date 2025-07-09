using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AddonProcurement.Models.CustomEntity.Models;
using WolfApprove.Model.CustomClass;

namespace AddonProcurement.Models.RequestModel
{
    public class RequestCustomeMasterData
    {
        public class RequestZSYNCCCSave: CustomClass
        {
            public List<ZSYNCCC> zSYNCCCs { get; set; }
        }

        public class RequestZSYNCGLSave: CustomClass
        {
            public List<ZSYNCGL>  zSYNCGLs { get; set; }
        }

        public class RequestZSYNCIOSave : CustomClass
        {
            public List<ZSYNCIO> zSYNCIOs { get; set; }
        }

        public class RequestMSTAsset : CustomClass
        {
            public List<MSTAsset> mstAssets { get; set; } 
        }
    }
}
