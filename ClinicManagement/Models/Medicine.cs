using ClinicManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagement.Models;

public partial class Medicine : BaseViewModel
{
    // Thời gian tối thiểu trước khi hết hạn để cảnh báo (60 ngày)
    public const int MinimumDaysBeforeExpiry = 60;

    // Thời gian tối thiểu trước khi chuyển sang lô mới (20 ngày)
    public const int MinimumDaysBeforeSwitchingBatch = 20;

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
    /// <summary>
    /// Lấy giá nhập của lô mới nhất (dùng cho màn hình nhập kho)
    /// </summary>
    public decimal LatestUnitPrice
    {
        get
        {
            var latestStockIn = StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();
            return latestStockIn?.UnitPrice ?? 0;
        }
    }

    /// <summary>
    /// Lấy giá bán của lô mới nhất (dùng cho màn hình nhập kho)
    /// </summary>
    public decimal LatestSellPrice
    {
        get
        {
            var latestStockIn = StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();
            return latestStockIn?.SellPrice ?? 0;
        }
    }

    /// <summary>
    /// Lấy lợi nhuận của lô mới nhất (dùng cho màn hình nhập kho)
    /// </summary>
    public decimal LatestProfitMargin
    {
        get
        {
            var latestStockIn = StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();
            return latestStockIn?.ProfitMargin ?? 20; // Mặc định 20% nếu không có dữ liệu
        }
    }

    /// <summary>
    /// Lấy ngày hết hạn của lô mới nhất (dùng cho màn hình nhập kho)
    /// </summary>
    public DateOnly? LatestExpiryDate
    {
        get
        {
            var latestStockIn = StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();
            return latestStockIn?.ExpiryDate;
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
            // Nếu không có lô nhập, trả về null
            if (StockIns == null || !StockIns.Any())
                return null;

            // Lấy ngày nhập gần nhất trực tiếp từ StockIns
            return StockIns.OrderByDescending(si => si.ImportDate).FirstOrDefault()?.ImportDate;
        }
    }

    /// <summary>
    /// Lấy giá nhập hiện tại dựa trên lô đang bán
    /// </summary>
    public decimal CurrentUnitPrice
    {
        get
        {
            // Ưu tiên lô đánh dấu là đang bán
            var sellingStockIn = SellingStockIn;
            if (sellingStockIn != null)
                return sellingStockIn.UnitPrice;

            // Nếu không có lô đang bán, sử dụng giá của lô nhập gần nhất
            var latestStockIn = StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();
            return latestStockIn?.UnitPrice ?? 0;
        }
    }

    /// <summary>
    /// Lấy giá bán hiện tại dựa trên lô đang bán
    /// </summary>
    public decimal CurrentSellPrice
    {
        get
        {
            // Ưu tiên lô đánh dấu là đang bán
            var sellingStockIn = SellingStockIn;
            if (sellingStockIn != null)
                return sellingStockIn.SellPrice ?? 0;

            // Nếu không có lô đang bán, sử dụng giá bán của lô nhập gần nhất
            var latestStockIn = StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();
            return latestStockIn?.SellPrice ?? 0;
        }
    }


    /// <summary>
    /// Kiểm tra xem lô đang sử dụng có phải là lô cuối cùng hay không
    /// </summary>
    public bool IsLastestStockIn
    {
        get
        {
            if (StockIns == null || !StockIns.Any())
                return false;

            var currentStockIn = ActiveStockIn;
            if (currentStockIn == null)
                return false;

            // Lấy tất cả các lô có số lượng còn lại
            var availableStockIns = StockIns
                .Where(si => si.RemainQuantity > 0)
                .OrderBy(si => si.ImportDate)
                .ToList();

            // Nếu chỉ có một lô có số lượng còn lại, thì đó là lô cuối cùng
            if (availableStockIns.Count == 1)
                return true;

            // Kiểm tra xem lô hiện tại có phải là lô mới nhất không
            var latestStockIn = availableStockIns
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();

            return currentStockIn.StockInId == latestStockIn?.StockInId;
        }
    }

    /// <summary>
    /// Kiểm tra xem thuốc có lô nào sắp hết hạn không
    /// </summary>
    public bool HasNearExpiryStock
    {
        get
        {
            if (StockIns == null || !StockIns.Any())
                return false;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var minimumExpiryDate = today.AddDays(MinimumDaysBeforeExpiry);

            return StockIns.Any(si =>
                si.RemainQuantity > 0 &&
                si.ExpiryDate.HasValue &&
                si.ExpiryDate.Value > today &&
                si.ExpiryDate.Value <= minimumExpiryDate);
        }
    }

    /// <summary>
    /// Kiểm tra xem thuốc có lô nào đã hết hạn nhưng vẫn còn số lượng không
    /// </summary>
    public bool HasExpiredStock
    {
        get
        {
            if (StockIns == null || !StockIns.Any())
                return false;

            var today = DateOnly.FromDateTime(DateTime.Today);

            return StockIns.Any(si =>
                si.RemainQuantity > 0 &&
                si.ExpiryDate.HasValue &&
                si.ExpiryDate.Value <= today);
        }
    }

    /// <summary>
    /// Kiểm tra xem lô thuốc hiện tại có cần phải được chuyển sang lô mới không
    /// Nếu lô hiện tại là lô cuối cùng, thì không cần chuyển
    /// </summary>
    public bool ShouldSwitchToNewBatch
    {
        get
        {
            // Nếu là lô cuối cùng, không cần chuyển
            if (IsLastestStockIn)
                return false;

            var currentStockIn = ActiveStockIn;
            if (currentStockIn == null)
                return false;

            // Kiểm tra hạn sử dụng của lô hiện tại
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Nếu không có hạn sử dụng, không cần chuyển
            if (!currentStockIn.ExpiryDate.HasValue)
                return false;

            // Nếu số ngày còn lại ít hơn ngưỡng tối thiểu để chuyển lô
            int daysUntilExpiry = currentStockIn.ExpiryDate.Value.DayNumber - today.DayNumber;

            // Nếu còn ít hơn số ngày tối thiểu hoặc đã hết hàng, cần chuyển lô
            return daysUntilExpiry < MinimumDaysBeforeSwitchingBatch || currentStockIn.RemainQuantity <= 0;
        }
    }

    /// <summary>
    /// Lấy lô thuốc hiện có cuối cùng (bao gồm cả lô đã hết hạn hoặc sắp hết hạn)
    /// để hiển thị và cảnh báo người dùng
    /// </summary>
    public StockIn LastAvailableStockIn
    {
        get
        {
            if (StockIns == null || !StockIns.Any())
                return null;

            // Lấy tất cả các lô còn số lượng, sắp xếp theo FIFO
            var availableStockIns = StockIns
                .Where(si => si.RemainQuantity > 0)
                .OrderBy(si => si.ImportDate)
                .ToList();

            // Trả về lô có số lượng còn lại theo FIFO
            var fifoStock = availableStockIns.FirstOrDefault();

            // Nếu không có lô nào theo FIFO, trả về lô cuối cùng được nhập vào
            if (fifoStock == null)
            {
                return StockIns.OrderByDescending(si => si.ImportDate).FirstOrDefault();
            }

            return fifoStock;
        }
    }

    /// <summary>
    /// Lô thuốc sẽ được sử dụng để bán hàng
    /// Ưu tiên lô được đánh dấu là IsSelling=true, nếu không có thì sử dụng logic FIFO
    /// </summary>
    public StockIn SellingStockIn
    {
        get
        {
            if (StockIns == null || !StockIns.Any())
                return null;

            // Ưu tiên sử dụng lô được đánh dấu IsSelling
            var sellingStockIn = StockIns
                .FirstOrDefault(si => si.IsSelling && si.RemainQuantity > 0);

            if (sellingStockIn != null)
                return sellingStockIn;

            // Nếu không có lô nào được đánh dấu hoặc lô được đánh dấu đã hết hàng, 
            // sử dụng logic FIFO mặc định
            return ActiveStockIn;
        }
    }

    /// <summary>
    /// Bộ nhớ đệm cho tính toán lô hàng còn lại
    /// </summary>
    public List<StockInWithRemaining> _availableStockInsCache;

    private bool _isCalculatingStockIns = false;

    /// <summary>
    /// Lấy lô thuốc đang hoạt động theo nguyên tắc FIFO
    /// (lô cũ nhất chưa hết hạn có số lượng còn lại)
    /// </summary>
    public StockIn ActiveStockIn
    {
        get
        {
            if (StockIns == null || !StockIns.Any())
                return null;

            // Lấy các lô có số lượng còn lại, sắp xếp theo ngày nhập (FIFO)
            var availableStockIns = StockIns
                .Where(si => si.RemainQuantity > 0)
                .OrderBy(si => si.ImportDate)
                .ToList();

            // Trả về lô cũ nhất có số lượng còn lại
            return availableStockIns.FirstOrDefault();
        }
    }

    /// <summary>
    /// Lấy lô thuốc đang hoạt động và chưa hết hạn theo nguyên tắc FIFO
    /// </summary>
    public StockIn ActiveNonExpiredStockIn
    {
        get
        {
            if (StockIns == null || !StockIns.Any())
                return null;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var minimumExpiryDate = today.AddDays(MinimumDaysBeforeExpiry);

            // Lấy các lô chưa hết hạn có số lượng còn lại, sắp xếp theo ngày nhập (FIFO)
            var usableStockIns = StockIns
                .Where(si => si.RemainQuantity > 0 &&
                        (!si.ExpiryDate.HasValue || si.ExpiryDate.Value >= minimumExpiryDate))
                .OrderBy(si => si.ImportDate)
                .ToList();

            // Trả về lô cũ nhất chưa hết hạn có số lượng còn lại
            return usableStockIns.FirstOrDefault();
        }
    }

    /// <summary>
    /// Lấy thông tin lô nhập kèm số lượng còn lại
    /// Sử dụng StockIn.RemainQuantity được duy trì bởi cơ sở dữ liệu
    /// </summary>
    public IEnumerable<StockInWithRemaining> GetStockInsWithRemaining()
    {
        // Trả về bộ đệm nếu có sẵn để tránh gọi đệ quy
        if (_availableStockInsCache != null)
            return _availableStockInsCache;

        // Ngăn chặn đệ quy
        if (_isCalculatingStockIns)
            return Enumerable.Empty<StockInWithRemaining>();

        _isCalculatingStockIns = true;

        try
        {
            if (StockIns == null || !StockIns.Any())
                return Enumerable.Empty<StockInWithRemaining>();

            // Tạo đối tượng StockInWithRemaining trực tiếp từ giá trị RemainQuantity
            var stockInsWithRemaining = StockIns
                .OrderBy(si => si.ImportDate)
                .Select(si => new StockInWithRemaining
                {
                    StockIn = si,
                    RemainingQuantity = si.RemainQuantity,
                    IsCurrentSellingBatch = si.IsSelling
                })
                .ToList();

            // Lưu kết quả vào bộ đệm
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
        // Xóa bộ đệm để đảm bảo lấy dữ liệu mới
        _availableStockInsCache = null;

        // Lấy tất cả lô có số lượng còn lại
        var allStockIns = GetStockInsWithRemaining();

        // Lọc theo ngày hết hạn để chỉ lấy các mục còn hạn
        var today = DateOnly.FromDateTime(DateTime.Today);
        var minimumExpiryDate = today.AddDays(MinimumDaysBeforeExpiry);

        // Chỉ trả về các mục chưa hết hạn
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
            // Ưu tiên sử dụng UsableQuantity từ bảng Stock
            var stockRecord = Stocks?.FirstOrDefault();
            if (stockRecord != null && stockRecord.UsableQuantity >= 0)
            {
                return stockRecord.UsableQuantity;
            }

            // Tính trực tiếp sử dụng RemainQuantity và lọc theo ngày hết hạn
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
            // Ưu tiên sử dụng Quantity từ bảng Stock
            var stockRecord = Stocks?.FirstOrDefault();
            if (stockRecord != null && stockRecord.Quantity >= 0)
            {
                return stockRecord.Quantity;
            }

            // Tính trực tiếp từ giá trị RemainQuantity
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
    /// Lớp hỗ trợ lưu thông tin lô hàng còn lại
    /// </summary>
    public class StockInWithRemaining
    {
        public StockIn StockIn { get; set; }
        public int RemainingQuantity { get; set; }
        public bool IsCurrentSellingBatch { get; set; }

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

        public string StatusDescription
        {
            get
            {
                if (IsCurrentSellingBatch)
                    return "Đang bán";
                if (IsExpired)
                    return "Hết hạn";
                if (IsNearExpiry)
                    return "Sắp hết hạn";
                return "Còn hạn";
            }
        }
    }
}
