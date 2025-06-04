using System;
using System.Collections.Generic;

namespace ClinicManagement.Models;

public partial class Medicine
{
    public int MedicineId { get; set; }

   

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

    public DateTime? LatestImportDate
    {
        get
        {
            // Clear the cache to ensure we get fresh data
            _availableStockInsCache = null;

            // If there are no stock-ins, return null
            if (StockIns == null || !StockIns.Any())
                return null;

            // Get the most recent import date directly from StockIns
            return StockIns.OrderByDescending(si => si.ImportDate).FirstOrDefault()?.ImportDate;
        }
    }


    public decimal CurrentUnitPrice
    {
        get
        {
            // Get the most recent stock-in for price information
            var latestStockIn = StockIns?.OrderByDescending(si => si.ImportDate).FirstOrDefault();
            return latestStockIn?.UnitPrice ?? 0;
        }
    }

    public decimal CurrentSellPrice
    {
        get
        {
            // Get the most recent stock-in for price information
            var latestStockIn = StockIns?.OrderByDescending(si => si.ImportDate).FirstOrDefault();
            return latestStockIn?.SellPrice ?? 0;
        }
    }



    /// Lấy danh sách các lô nhập kho, bao gồm cả các lô có cùng ngày nhập nhưng khác giá
    /// </summary>
    private List<StockInWithRemaining> _availableStockInsCache;
    private bool _isCalculatingStockIns = false;
    private IEnumerable<StockInWithRemaining> GetAvailableStockIns()
    {
        // Return cache if available to prevent recursive calls
        if (_availableStockInsCache != null)
            return _availableStockInsCache;

        // Guard against recursion
        if (_isCalculatingStockIns)
            return Enumerable.Empty<StockInWithRemaining>();

        _isCalculatingStockIns = true;

        try
        {
            if (StockIns == null || !StockIns.Any())
                return Enumerable.Empty<StockInWithRemaining>();

            // Tính tổng số lượng đã bán
            var totalSold = InvoiceDetails?.Sum(id => id.Quantity) ?? 0;
            var remainingToSubtract = totalSold;

            // Duyệt qua các lô nhập theo thứ tự thời gian (FIFO)
            var stockInsWithRemaining = new List<StockInWithRemaining>();

            // Sắp xếp theo ngày và thời gian nhập chính xác
            foreach (var stockIn in StockIns.OrderBy(si => si.ImportDate))
            {
                var remainingInThisLot = stockIn.Quantity - Math.Min(remainingToSubtract, stockIn.Quantity);

                // Thêm vào danh sách mọi lô nhập để hiển thị lịch sử đầy đủ
                stockInsWithRemaining.Add(new StockInWithRemaining
                {
                    StockIn = stockIn,
                    RemainingQuantity = Math.Max(0, remainingInThisLot)
                });

                remainingToSubtract = Math.Max(0, remainingToSubtract - stockIn.Quantity);
            }

            // Cache the result
            _availableStockInsCache = stockInsWithRemaining;
            return stockInsWithRemaining;
        }
        finally
        {
            _isCalculatingStockIns = false;
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết về các lô hàng
    /// </summary>
 // Modify the public method to clear the cache before recalculating
    public IEnumerable<StockInWithRemaining> GetDetailedStock()
    {
        // Clear the cache to ensure we get fresh data
        _availableStockInsCache = null;
        return GetAvailableStockIns();
    }

    /// <summary>
    /// Tổng số lượng còn lại
    /// </summary>
    public int CalculatedRemainingQuantity
    {
        get => GetAvailableStockIns().Sum(s => s.RemainingQuantity);
    }

    /// <summary>
    /// Tổng số lượng tồn kho chính xác bằng cách tính trực tiếp từ dữ liệu
    /// </summary>
    public int TotalStockQuantity
    {
        get
        {
            // Tính bằng cách lấy tổng số lượng nhập trừ đi tổng số lượng đã bán
            var totalStockIn = StockIns?.Sum(si => si.Quantity) ?? 0;
            var totalSold = InvoiceDetails?.Sum(id => id.Quantity) ?? 0;
            return totalStockIn - totalSold;
        }
    }

    /// <summary>
    /// Class hỗ trợ lưu thông tin lô hàng còn lại
    /// </summary>
    public class StockInWithRemaining
    {
        public StockIn StockIn { get; set; }
        public int RemainingQuantity { get; set; }

        public decimal UnitPrice => StockIn.UnitPrice;
        public decimal? SellPrice => StockIn.SellPrice;
        public DateTime? ImportDate => StockIn.ImportDate;
    }

}
