using System;

namespace ClinicManagement.Models
{
    public class StockByMonth
    {
        public int MedicineId { get; set; }
        public virtual Medicine Medicine { get; set; }
        public int TotalQuantity { get; set; }     
        public int UsableQuantity { get; set; }       
        public DateTime AsOfDate { get; set; }       
        public decimal TotalValue { get; set; }    
        public DateOnly? EarliestExpiryDate { get; set; } 
        public string LatestSupplierName { get; set; }  

      
        public int Month { get; set; }
        public int Year { get; set; }

     
        public string PeriodDisplay => $"{Month}/{Year}";
        public string TotalValueDisplay => TotalValue.ToString("N0") + " VNĐ";
    }
}
