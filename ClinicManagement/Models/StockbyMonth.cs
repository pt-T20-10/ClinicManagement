using System;

namespace ClinicManagement.Models
{
    public class StockByMonth
    {
        public int MedicineId { get; set; }
        public virtual Medicine Medicine { get; set; }
        public int TotalQuantity { get; set; }        // Total physical stock
        public int UsableQuantity { get; set; }       // Stock not expired (with at least 8 days before expiry)
        public DateTime AsOfDate { get; set; }        // The end date of the period
        public decimal TotalValue { get; set; }       // Total value of the stock
        public DateOnly? EarliestExpiryDate { get; set; } // Earliest expiry date among remaining stock
        public string LatestSupplierName { get; set; }   // Name of the latest supplier

        // Month and year for filtering and display
        public int Month { get; set; }
        public int Year { get; set; }

        // Calculated properties for display
        public string PeriodDisplay => $"{Month}/{Year}";
        public string TotalValueDisplay => TotalValue.ToString("N0") + " VNĐ";
    }
}
