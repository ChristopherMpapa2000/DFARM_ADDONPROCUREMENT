using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models.CustomEntity.Models
{
    public class ViewCostcenterOrganizer
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string NameTh { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentNameTh { get; set; } = string.Empty;
        public string CenterCCC { get; set; } = string.Empty;
        public string CenterCCCDesc { get; set; } = string.Empty;
        public string BranchCCC { get; set; } = string.Empty;
        public string BranchCCCDesc { get; set; } = string.Empty;
    }
}
