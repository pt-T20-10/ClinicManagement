using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClinicManagement.Models
{
    public class ActiveStockConfiguration
    {
        public int Id { get; set; }
        public int MedicineId { get; set; }
        public int ActiveStockInId { get; set; }
        public DateTime LastUpdated { get; set; }

        // Navigation properties
        public virtual Medicine Medicine { get; set; }
        public virtual StockIn ActiveStockIn { get; set; }
    }

}
