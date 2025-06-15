using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class StockIn
{
    public int StockInId { get; set; }

    public int MedicineId { get; set; }

    public int Quantity { get; set; }

    public DateTime? ImportDate { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal ProfitMargin { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal? SellPrice { get; set; }

    public decimal? TotalCost { get; set; }

    public int SupplierId { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual Medicine Medicine { get; set; } = null!;
}

