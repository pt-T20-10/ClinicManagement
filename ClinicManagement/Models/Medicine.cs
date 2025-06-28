using ClinicManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models;

public partial class Medicine : BaseViewModel
{
    public const int MinimumDaysBeforeExpiry = 30;

    private int _tempQuantity = 1;

    public int MedicineId { get; set; }

    public string Name { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? UnitId { get; set; }

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

    public virtual ICollection<MonthlyStock> MonthlyStocks { get; set; } = new List<MonthlyStock>();

    public virtual Unit? Unit { get; set; }

    public DateTime? LatestImportDate
    {
        get
        {
            // If there are no stock-ins, return null
            if (StockIns == null || !StockIns.Any())
                return null;

            // Get the most recent import date directly from StockIns
            return StockIns.OrderByDescending(si => si.ImportDate).FirstOrDefault()?.ImportDate;
        }
    }

    // Get Current Unit Price based on the most recent stock-in that has remaining stock
    public decimal CurrentUnitPrice
    {
        get
        {
            // Get the oldest batch with remaining quantity
            var activeStockIn = ActiveStockIn;
            if (activeStockIn != null)
                return activeStockIn.UnitPrice;

            // If no active stock in, use the latest import's price
            var latestStockIn = StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();
            return latestStockIn?.UnitPrice ?? 0;
        }
    }

    public decimal CurrentSellPrice
    {
        get
        {
            // Get the oldest batch with remaining quantity
            var activeStockIn = ActiveStockIn;
            if (activeStockIn != null)
                return activeStockIn.SellPrice ?? 0;

            // If no active stock in, use the latest import's sell price
            var latestStockIn = StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();
            return latestStockIn?.SellPrice ?? 0;
        }
    }

    /// <summary>
    /// Cache for StockIn with remaining calculation
    /// </summary>
    public List<StockInWithRemaining> _availableStockInsCache;

    private bool _isCalculatingStockIns = false;

    /// <summary>
    /// Gets the currently active StockIn according to FIFO principles
    /// (oldest non-expired batch with remaining quantity)
    /// </summary>
    public StockIn ActiveStockIn
    {
        get
        {
            if (StockIns == null || !StockIns.Any())
                return null;

            // Get available stock entries with remaining quantity, in FIFO order
            var availableStockIns = StockIns
                .Where(si => si.RemainQuantity > 0)
                .OrderBy(si => si.ImportDate)
                .ToList();

            // Return the oldest batch that has remaining quantity
            return availableStockIns.FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets the currently active and non-expired StockIn according to FIFO principles
    /// </summary>
    public StockIn ActiveNonExpiredStockIn
    {
        get
        {
            if (StockIns == null || !StockIns.Any())
                return null;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var minimumExpiryDate = today.AddDays(MinimumDaysBeforeExpiry);

            // Get unexpired stock entries with remaining quantity, in FIFO order
            var usableStockIns = StockIns
                .Where(si => si.RemainQuantity > 0 &&
                        (!si.ExpiryDate.HasValue || si.ExpiryDate.Value >= minimumExpiryDate))
                .OrderBy(si => si.ImportDate)
                .ToList();

            // Return the oldest unexpired batch that has remaining quantity
            return usableStockIns.FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets stock-ins with remaining quantity information
    /// Uses StockIn.RemainQuantity which is maintained by the database
    /// </summary>
    public IEnumerable<StockInWithRemaining> GetStockInsWithRemaining()
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

            // Create StockInWithRemaining objects directly from RemainQuantity values
            var stockInsWithRemaining = StockIns
                .OrderBy(si => si.ImportDate)
                .Select(si => new StockInWithRemaining
                {
                    StockIn = si,
                    RemainingQuantity = si.RemainQuantity
                })
                .ToList();

            // Cache the result
            _availableStockInsCache = stockInsWithRemaining;
            return stockInsWithRemaining;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi khi tải thông tin lô hàng: {ex.Message}");
            return Enumerable.Empty<StockInWithRemaining>();
        }
        finally
        {
            _isCalculatingStockIns = false;
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết về các lô hàng có thể sử dụng (còn hạn)
    /// </summary>
    public IEnumerable<StockInWithRemaining> GetDetailedStock()
    {
        // Clear the cache to ensure we get fresh data
        _availableStockInsCache = null;

        // Get all stock ins with remaining quantity
        var allStockIns = GetStockInsWithRemaining();

        // Filter by expiry date for valid entries only
        var today = DateOnly.FromDateTime(DateTime.Today);
        var minimumExpiryDate = today.AddDays(MinimumDaysBeforeExpiry);

        // Return only items that aren't expired
        return allStockIns.Where(s =>
            !s.StockIn.ExpiryDate.HasValue ||
            s.StockIn.ExpiryDate.Value >= minimumExpiryDate);
    }

    /// <summary>
    /// Tổng số lượng tồn kho sử dụng được (còn hạn sử dụng)
    /// </summary>
    public int TotalStockQuantity
    {
        get
        {
            // First attempt: Use pre-calculated UsableQuantity from Stock table
            var stockRecord = Stocks?.FirstOrDefault();
            if (stockRecord != null && stockRecord.UsableQuantity >= 0)
            {
                return stockRecord.UsableQuantity;
            }

            // Calculate directly using RemainQuantity and filtering by expiry date
            var today = DateOnly.FromDateTime(DateTime.Today);
            var minimumExpiryDate = today.AddDays(MinimumDaysBeforeExpiry);

            return StockIns?
                .Where(si => !si.ExpiryDate.HasValue || si.ExpiryDate.Value >= minimumExpiryDate)
                .Sum(si => si.RemainQuantity) ?? 0;
        }
    }

    /// <summary>
    /// Tổng số lượng tồn kho vật lý (bao gồm cả thuốc sắp hết hạn)
    /// </summary>
    public int TotalPhysicalStockQuantity
    {
        get
        {
            // First attempt: Use pre-calculated Quantity from Stock table
            var stockRecord = Stocks?.FirstOrDefault();
            if (stockRecord != null && stockRecord.Quantity >= 0)
            {
                return stockRecord.Quantity;
            }

            // Calculate directly from RemainQuantity values
            return StockIns?.Sum(si => si.RemainQuantity) ?? 0;
        }
    }

    /// <summary>
    /// Ngày hết hạn của lô thuốc đang sử dụng (theo FIFO)
    /// </summary>
    public DateOnly? CurrentExpiryDate => ActiveStockIn?.ExpiryDate;

    /// <summary>
    /// Lô thuốc đang sử dụng (theo FIFO)
    /// </summary>
    public StockIn CurrentStockIn => ActiveStockIn;

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
        public DateOnly? ExpiryDate => StockIn.ExpiryDate;
        public bool IsExpired => StockIn.ExpiryDate.HasValue &&
                               StockIn.ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.Today);
        public bool IsNearExpiry
        {
            get
            {
                if (!StockIn.ExpiryDate.HasValue) return false;
                var today = DateOnly.FromDateTime(DateTime.Today);
                return StockIn.ExpiryDate.Value > today &&
                       StockIn.ExpiryDate.Value <= today.AddDays(Medicine.MinimumDaysBeforeExpiry);
            }
        }
    }
}
