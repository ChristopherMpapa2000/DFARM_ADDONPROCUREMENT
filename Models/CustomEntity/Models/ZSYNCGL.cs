using System;
using System.ComponentModel.DataAnnotations;

namespace AddonProcurement.Models.CustomEntity.Models
{
    public class ZSYNCGL : BaseEntity
    {
        public int Id { get; set; }
        public string GLAccount { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string AccountKOKJ { get; set; }
        public string PostKey { get; set; }
        public string ANBWA { get; set; }
        public string NEWKO { get; set; }
        public string NASSETS { get; set; }
        public string ANLKL { get; set; }
        public string PostKeyAA { get; set; }
        public string NewkoAA { get; set; }
        public string ANBWAAA { get; set; }
        public string NEWUM { get; set; }
        public string assetgroup { get; set; }
        public string CategoryGroupCode { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
