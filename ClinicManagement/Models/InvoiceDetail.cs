using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class InvoiceDetail
{
    public int DetailsId { get; set; }

    public int InvoiceId { get; set; }

    public int MedicineId { get; set; }

    public int StockInId { get; set; }

    public int Quantity { get; set; }

    public decimal SalePrice { get; set; }

    public decimal? Discount { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Medicine Medicine { get; set; } = null!;

    public virtual StockIn StockIn { get; set; } = null!;
}
