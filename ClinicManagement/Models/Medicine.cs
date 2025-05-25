using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class Medicine
{
    public int MedicineId { get; set; }

    public string? Mshnnb { get; set; }

    public string Name { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? UnitId { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public bool? IsDeleted { get; set; }

    public int? SupplierId { get; set; }

    public string? BarCode { get; set; }

    public string? QrCode { get; set; }

    public virtual MedicineCategory? Category { get; set; }

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<StockIn> StockIns { get; set; } = new List<StockIn>();

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    public virtual Supplier? Supplier { get; set; }

    public virtual Unit? Unit { get; set; }
}
