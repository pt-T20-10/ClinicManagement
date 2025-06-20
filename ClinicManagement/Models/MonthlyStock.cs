using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models;

public class MonthlyStock
{
    public int MonthlyStockId { get; set; }
    public int MedicineId { get; set; }
    public int Quantity { get; set; }
    public int CanUsed { get; set; }
    public string MonthYear { get; set; } = null!;
    public DateTime RecordedDate { get; set; } = DateTime.Now;

    public virtual Medicine Medicine { get; set; } = null!;
}
