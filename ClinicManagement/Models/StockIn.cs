using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models;

public partial class StockIn
{
    public int StockInId { get; set; }

    public int MedicineId { get; set; }

    public int? StaffId { get; set; }

    public int Quantity { get; set; }

    public int RemainQuantity { get; set; }

    public DateTime? ImportDate { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal ProfitMargin { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal? SellPrice { get; set; }

    public decimal? TotalCost { get; set; }

    public int SupplierId { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual Staff? Staff { get; set; }

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual Medicine Medicine { get; set; } = null!;


  
        // Existing properties remain unchanged

        /// <summary>
        /// Returns the remaining quantity (ConLai) from this stock entry
        /// Uses RemainQuantity field which is already being maintained
        /// </summary>
        [NotMapped]
        public int ConLai => RemainQuantity;

        /// <summary>
        /// Determines if this stock entry is expired
        /// </summary>
        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue &&
                               ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.Today);

        /// <summary>
        /// Determines if this stock entry is near expiry (not expired but within minimum days)
        /// </summary>
        [NotMapped]
        public bool IsNearExpiry
        {
            get
            {
                if (!ExpiryDate.HasValue) return false;

                var today = DateOnly.FromDateTime(DateTime.Today);
                var minimumExpiryDate = today.AddDays(Medicine.MinimumDaysBeforeExpiry);

                return ExpiryDate.Value > today &&
                       ExpiryDate.Value <= minimumExpiryDate;
            }
        }

        /// <summary>
        /// Determines if this stock is usable (not expired and not near expiry)
        /// </summary>
        [NotMapped]
        public bool IsUsable
        {
            get
            {
                if (!ExpiryDate.HasValue) return true;

                var today = DateOnly.FromDateTime(DateTime.Today);
                var minimumExpiryDate = today.AddDays(Medicine.MinimumDaysBeforeExpiry);

                return ExpiryDate.Value > minimumExpiryDate;
            }
        }
    
}

