using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models.ResponeModel
{
    public class InsertGRCustomResponeModel
    {
        public InsertGRCustomResponeModel()
        {
            @return = new List<ErrorDetail>();
        }
        public string Type { get; set; }
        public string Types { get; set; }
        public string Message { get; set; }
        public List<ErrorDetail> @return { get; set; }
        public string Number { get; set; }
    }
    public class ErrorDetail
    {
        public string Reference1 { get; set; }
        public string InventNumber { get; set; }
        public string AssetTypeName { get; set; }
        public string CompanyCode { get; set; }
        public string DocNumber { get; set; }
        public string FiscalYear { get; set; }
        public string AssetNo { get; set; }
        public string SubNumber { get; set; }
        public string Type { get; set; }
        public string Id { get; set; }
        public string Number { get; set; }
        public string Message { get; set; }
        public string LogNo { get; set; }
        public string LogMsgNo { get; set; }
        public string MessageV1 { get; set; }
        public string MessageV2 { get; set; }
        public string MessageV3 { get; set; }
        public string MessageV4 { get; set; }
        public string Parameter { get; set; }
        public string Row { get; set; }
        public string Field { get; set; }
        public string System { get; set; }
    }
}
