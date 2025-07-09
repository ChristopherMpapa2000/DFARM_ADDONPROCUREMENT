using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models
{
    public class RefModel
    {
        public int MemoId { get; set; }

        public string DocumentNo { get; set; }

        public int? TemplateId { get; set; }

        public string TemplateName { get; set; }

        public string TemplateType { get; set; }

        public int? DepartmentId { get; set; }

        public string DocumentCode { get; set; }

        public int? CompanyId { get; set; }

        public string CompanyName { get; set; }

        public string ToPerson { get; set; }

        public string CcPerson { get; set; }

        public string TemplateSubject { get; set; }

        public int? CreatorId { get; set; }

        public string CNameTh { get; set; }

        public string CNameEn { get; set; }

        public int? CPositionId { get; set; }

        public int? RequesterId { get; set; }

        public string RNameTh { get; set; }

        public string RNameEn { get; set; }

        public int? RPositionId { get; set; }

        public string MemoSubject { get; set; }

        public string RPositionTh { get; set; }

        public string RPositionEn { get; set; }

        public string StatusName { get; set; }

        public decimal? Amount { get; set; }

        public SelectFieldModel SelectField { get; set; }
    }

    public class SelectFieldModel
    {
        public string label { get; set; }
        public string value { get; set; }
    }
}
