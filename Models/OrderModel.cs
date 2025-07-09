using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonProcurement.Models
{
    public class OrderModel
    {
        public string Id { get; set; }
        public double Amount { get; set; }
        public double Price { get; set; }
        public bool ExtractMode { get; set; } = false;
    }
}
