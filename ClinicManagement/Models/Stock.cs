using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class Stock
{
    public int StockId { get; set; }
    public int MedicineId { get; set; }
    public int Quantity { get; set; }
    public int UsableQuantity { get; set; } // New field to store medications that aren't expired
    public DateTime? LastUpdated { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;
}
