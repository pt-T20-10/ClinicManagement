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

    public decimal CurrentUnitPrice
    {
        get
        {
            var availableStockIn = GetAvailableStockIns().FirstOrDefault();
            return availableStockIn?.UnitPrice ?? 0;
        }
    }

    public decimal CurrentSellPrice
    {
        get
        {
            var availableStockIn = GetAvailableStockIns().FirstOrDefault();
            return availableStockIn?.SellPrice ?? 0;
        }
    }


    /// Lấy danh sách các lô nhập còn hàng, sắp xếp theo ngày nhập (cũ nhất trước)

    private IEnumerable<StockInWithRemaining> GetAvailableStockIns()
    {
        if (StockIns == null || !StockIns.Any())
            return Enumerable.Empty<StockInWithRemaining>();

        // Tính tổng số lượng đã bán
        var totalSold = InvoiceDetails?.Sum(id => id.Quantity) ?? 0;
        var remainingToSubtract = totalSold;

        // Duyệt qua các lô nhập theo thứ tự thời gian (FIFO)
        var stockInsWithRemaining = new List<StockInWithRemaining>();

        foreach (var stockIn in StockIns.OrderBy(si => si.ImportDate))
        {
            var remainingInThisLot = stockIn.Quantity - Math.Min(remainingToSubtract, stockIn.Quantity);

            if (remainingInThisLot > 0)
            {
                stockInsWithRemaining.Add(new StockInWithRemaining
                {
                    StockIn = stockIn,
                    RemainingQuantity = remainingInThisLot
                });
            }

            remainingToSubtract = Math.Max(0, remainingToSubtract - stockIn.Quantity);

            if (remainingToSubtract <= 0) break;
        }

        return stockInsWithRemaining.Where(s => s.RemainingQuantity > 0);
    }


    /// Lấy thông tin chi tiết về các lô hàng còn tồn

    public IEnumerable<StockInWithRemaining> GetDetailedStock()
    {
        return GetAvailableStockIns();
    }


    /// Tổng số lượng còn lại tính theo FIFO

    public int CalculatedRemainingQuantity
    {
        get => GetAvailableStockIns().Sum(s => s.RemainingQuantity);
    }



/// Class hỗ trợ lưu thông tin lô hàng còn lại

public class StockInWithRemaining
{
    public StockIn StockIn { get; set; }
    public int RemainingQuantity { get; set; }

    public decimal UnitPrice => StockIn.UnitPrice;
    public decimal? SellPrice => StockIn.SellPrice;
    public DateTime? ImportDate => StockIn.ImportDate;
}

}
