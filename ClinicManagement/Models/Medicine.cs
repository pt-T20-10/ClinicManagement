using ClinicManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models;

public partial class Medicine : BaseViewModel
{
    private int _tempQuantity = 1;

    public int MedicineId { get; set; }

    public string Name { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? UnitId { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public bool? IsDeleted { get; set; }

    public string? BarCode { get; set; }

    public string? QrCode { get; set; }

    public Supplier LatestSupplier
    {
        get
        {
            var latestStockIn = StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();
            return latestStockIn?.Supplier;
        }
    }

    // Thêm thuộc tính TempQuantity để sử dụng khi thêm vào giỏ hàng
    [NotMapped]
    public int TempQuantity
    {
        get => _tempQuantity;
        set
        {
            if (_tempQuantity != value)
            {
                _tempQuantity = value;
                OnPropertyChanged(nameof(TempQuantity));
            }
        }
    }

    // Thêm thuộc tính Code để hiển thị trong danh sách
    public string Code => $"M{MedicineId}";

    public virtual MedicineCategory? Category { get; set; }

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ICollection<StockIn> StockIns { get; set; } = new List<StockIn>();

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

 

    public virtual Unit? Unit { get; set; }

    public DateTime? LatestImportDate
    {
        get
        {
            // Clear the cache để make sure clean data 
            _availableStockInsCache = null;

            // If there are no stock-ins, return null
            if (StockIns == null || !StockIns.Any())
                return null;

            // Get the most recent import date directly from StockIns
            return StockIns.OrderByDescending(si => si.ImportDate).FirstOrDefault()?.ImportDate;
        }
    }
  
    //Get Current Unit Price based on the most recent stock-in that has remaining stock and valid expiry date
    public decimal CurrentUnitPrice
    {
        get
        {
            // Lấy danh sách các lô nhập kho có thông tin số lượng còn lại và còn hạn sử dụng
            var stockWithRemaining = GetAvailableStockIns()
                .Where(s => s.RemainingQuantity > 0)
                .ToList();

            // Sắp xếp theo ngày nhập mới nhất
            var firstAvailableStock = stockWithRemaining
                .OrderByDescending(s => s.ImportDate)
                .FirstOrDefault();

            // Nếu có lô còn hàng, trả về giá nhập của lô đó
            if (firstAvailableStock != null)
                return firstAvailableStock.UnitPrice;

            // Nếu không có lô nào còn hàng và còn hạn, lấy giá của lô mới nhất (theo logic cũ)
            var latestStockIn = StockIns?.OrderByDescending(si => si.ImportDate).FirstOrDefault();
            return latestStockIn?.UnitPrice ?? 0;
        }
    }

    public decimal CurrentSellPrice
    {
        get
        {
            // Lấy danh sách các lô nhập kho có thông tin số lượng còn lại và còn hạn sử dụng
            var stockWithRemaining = GetAvailableStockIns()
                .Where(s => s.RemainingQuantity > 0)
                .ToList();

            // Sắp xếp theo ngày nhập mới nhất
            var firstAvailableStock = stockWithRemaining
                .OrderByDescending(s => s.ImportDate)
                .FirstOrDefault();

            // Nếu có lô còn hàng và còn hạn, trả về giá bán của lô đó
            if (firstAvailableStock != null)
                return firstAvailableStock.SellPrice ?? 0;

            // Nếu không có lô nào còn hàng và còn hạn, lấy giá của lô mới nhất (theo logic cũ)
            var latestStockIn = StockIns?.OrderByDescending(si => si.ImportDate).FirstOrDefault();
            return latestStockIn?.SellPrice ?? 0;
        }
    }

    /// <summary>
    /// Lấy danh sách các lô nhập kho, bao gồm cả các lô có cùng ngày nhập nhưng khác giá
    /// </summary>
    public List<StockInWithRemaining> _availableStockInsCache;

    private bool _isCalculatingStockIns = false;

    /// <summary>
    /// Lấy danh sách các lô nhập kho, bao gồm cả các lô có cùng ngày nhập nhưng khác giá
    /// Chỉ lấy các lô còn hạn sử dụng tối thiểu 8 ngày
    /// </summary>
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

            // Get today's date for expiry calculation
            var today = DateOnly.FromDateTime(DateTime.Today);
            var minimumExpiryDate = today.AddDays(8); // Must be at least 8 days from expiry

            // Tính tổng số lượng đã bán
            var totalSold = InvoiceDetails?.Sum(id => id.Quantity) ?? 0;
            var remainingToSubtract = totalSold;

            // Duyệt qua các lô nhập theo thứ tự thời gian (FIFO), nhưng chỉ lấy các lô còn hạn sử dụng
            var stockInsWithRemaining = new List<StockInWithRemaining>();

            // 🔧 FIX: Sử dụng StockIn.ExpiryDate thay vì Medicine.ExpiryDate
            foreach (var stockIn in StockIns
                .Where(si => !si.ExpiryDate.HasValue || si.ExpiryDate.Value >= minimumExpiryDate) // ✅ Fixed: si.ExpiryDate
                .OrderBy(si => si.ImportDate))
            {
                var remainingInThisLot = stockIn.Quantity - Math.Min(remainingToSubtract, stockIn.Quantity);

                // Thêm vào danh sách các lô còn hạn sử dụng
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
        get
        {
            try
            {
                var total = GetAvailableStockIns().Sum(s => s.RemainingQuantity);
                return Math.Max(0, total); // Ensure non-negative
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Tổng số lượng tồn kho chính xác bằng cách tính trực tiếp từ dữ liệu
    /// </summary>
    /// <summary>
    /// Tổng số lượng tồn kho chính xác bằng cách tính trực tiếp từ dữ liệu
    /// Chỉ tính các lô còn hạn sử dụng tối thiểu 8 ngày
    /// </summary>
    /// <summary>
    /// Tổng số lượng tồn kho sử dụng được (còn hạn sử dụng tối thiểu 8 ngày)
    /// </summary>
    public int TotalStockQuantity
    {
        get
        {
            try
            {
                // Get today's date for expiry calculation
                var today = DateOnly.FromDateTime(DateTime.Today);
                var minimumExpiryDate = today.AddDays(8);

                // ✅ Không cần filter lại vì GetAvailableStockIns() đã filter rồi
                var validStock = GetAvailableStockIns()
                    .Sum(s => s.RemainingQuantity);

                // Ensure we don't return negative values
                return Math.Max(0, validStock);
            }
            catch
            {
                // In case of errors, return 0
                return 0;
            }
        }
    }

    public int TotalPhysicalStockQuantity
    {
        get
        {
            try
            {
                // Sum all remaining quantities regardless of expiry date
                var totalStock = GetAvailableStockIns()
                    .Sum(s => s.RemainingQuantity);

                // Ensure we don't return negative values
                return Math.Max(0, totalStock);
            }
            catch
            {
                // In case of errors, return 0
                return 0;
            }
        }
    }
    public DateOnly? CurrentExpiryDate
    {
        get
        {
            // Lấy danh sách các lô nhập kho có thông tin số lượng còn lại và còn hạn sử dụng
            var stockWithRemaining = GetAvailableStockIns()
                .Where(s => s.RemainingQuantity > 0)
                .ToList();

            if (!stockWithRemaining.Any())
            {
                // Nếu không còn lô nào còn hạn sử dụng và còn hàng, trả về ngày hết hạn của lô mới nhất
                var latestStockIn = StockIns?.OrderByDescending(si => si.ImportDate).FirstOrDefault();
                return latestStockIn?.ExpiryDate;
            }

            // Lấy lô cũ nhất còn hạn sử dụng và còn số lượng (FIFO)
            var oldestValidStock = stockWithRemaining
                .OrderBy(s => s.ImportDate)  // Sắp xếp theo thứ tự cũ -> mới (FIFO)
                .FirstOrDefault();

            // Nếu có lô phù hợp, trả về ngày hết hạn của lô đó
            if (oldestValidStock != null)
                return oldestValidStock.StockIn.ExpiryDate;

            // Nếu không tìm thấy, trả về ngày hết hạn của thuốc (nếu có)
            return ExpiryDate;
        }
    }
    public StockIn CurrentStockIn
    {
        get
        {
            // Lấy danh sách các lô nhập kho có thông tin số lượng còn lại và còn hạn sử dụng
            var stockWithRemaining = GetAvailableStockIns()
                .Where(s => s.RemainingQuantity > 0)
                .ToList();

            // Lấy lô cũ nhất còn hạn sử dụng và còn số lượng (FIFO)
            var oldestValidStock = stockWithRemaining
                .OrderBy(s => s.ImportDate)  // Sắp xếp theo thứ tự cũ -> mới (FIFO)
                .FirstOrDefault();

            // Nếu có lô phù hợp, trả về thông tin lô đó
            if (oldestValidStock != null)
                return oldestValidStock.StockIn;

            // Nếu không tìm thấy, trả về lô mới nhất
            return StockIns?.OrderByDescending(si => si.ImportDate).FirstOrDefault();
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
