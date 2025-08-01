using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel quản lý kho thuốc của phòng khám
    /// Cung cấp các chức năng: nhập thuốc, tìm kiếm, lọc tồn kho, xuất báo cáo
    /// Hỗ trợ quản lý đơn vị tính, danh mục thuốc và nhà cung cấp
    /// Triển khai IDataErrorInfo để validation dữ liệu đầu vào
    /// </summary>
    public class StockMedicineViewModel : BaseViewModel, IDataErrorInfo
    {


        #region Properties

        #region Validation 
        /// <summary>
        /// Lớp trợ giúp hiển thị thông tin trạng thái tồn kho
        /// Cung cấp thông tin chi tiết về tình trạng thuốc trong kho
        /// Bao gồm cảnh báo về hạn sử dụng và số lượng tồn kho
        /// </summary>
        public class StockViewModel
        {
            /// <summary>
            /// Đối tượng thuốc được hiển thị
            /// Chứa toàn bộ thông tin của thuốc từ database
            /// </summary>
            public Medicine Medicine { get; set; }

            /// <summary>
            /// Trạng thái hiện tại của thuốc
            /// Các giá trị: "Bình thường", "Sắp hết hạn", "Đã hết hạn", "Hết hàng", "Lô cuối"
            /// </summary>
            public string Status { get; set; }

            /// <summary>
            /// Mã màu hiển thị trạng thái trên giao diện
            /// Green = Bình thường, Yellow = Sắp hết, Red = Hết hạn, Orange = Sắp hết hạn, Blue = Lô cuối
            /// </summary>
            public string StatusColor { get; set; }

            /// <summary>
            /// Thông báo chi tiết về trạng thái thuốc
            /// Mô tả cụ thể tình trạng để người dùng hiểu rõ hơn
            /// </summary>
            public string StatusMessage { get; set; }

            /// <summary>
            /// Cảnh báo khi thuốc đang sử dụng lô cuối cùng và sắp hết hạn
            /// Trường hợp nguy hiểm nhất cần ưu tiên xử lý
            /// True = Hiển thị cảnh báo đỏ trên UI
            /// </summary>
            public bool ShowLastBatchExpiryWarning =>
                Medicine?.IsLastestStockIn == true && Medicine?.HasNearExpiryStock == true;

            /// <summary>
            /// Cảnh báo khi thuốc đang sử dụng lô cuối cùng và số lượng thấp
            /// Cần nhập thêm hàng ngay để tránh hết thuốc
            /// True = Hiển thị cảnh báo cam trên UI (ngưỡng mặc định ≤ 10)
            /// </summary>
            public bool ShowLastBatchQuantityWarning =>
                Medicine?.IsLastestStockIn == true &&
                (Medicine?.TotalPhysicalStockQuantity ?? 0) <= 10; // Ngưỡng tồn kho thấp là 10 đơn vị
        }

        // === CÁC TRƯỜNG TRẠNG THÁI VÀ VALIDATION ===

        /// <summary>
        /// Cờ hiệu cho biết hệ thống đang trong quá trình tải dữ liệu
        /// Sử dụng để hiển thị loading spinner hoặc vô hiệu hóa UI tạm thời
        /// </summary>
        private bool IsLoading;

        /// <summary>
        /// Error property bắt buộc của IDataErrorInfo
        /// Trả về null vì validation được thực hiện ở từng property riêng biệt
        /// </summary>
        public string Error => null;

        // === VALIDATION THEO NHÓM CHỨC NĂNG ===

        /// <summary>
        /// Danh sách các trường thuộc nhóm "Thông tin thuốc" đã được người dùng tương tác
        /// Bao gồm: Tên thuốc, Mã vạch, Mã QR, Số lượng, Giá nhập, Giá bán, Ngày hết hạn
        /// Chỉ validate các trường này khi người dùng đã chạm vào để tránh hiển thị lỗi sớm
        /// </summary>
        private HashSet<string> _medicineFieldsTouched = new HashSet<string>();

        /// <summary>
        /// Cờ kích hoạt validation cho nhóm thuốc
        /// True = Hiển thị tất cả lỗi validation trong form nhập thuốc
        /// False = Chỉ hiển thị lỗi cho các field đã được touched
        /// </summary>
        private bool _isMedicineValidating = false;

        /// <summary>
        /// Danh sách các trường thuộc nhóm "Đơn vị tính" đã được người dùng tương tác
        /// Bao gồm: Tên đơn vị, Mô tả đơn vị
        /// Validation riêng biệt để không ảnh hưởng đến các form khác
        /// </summary>
        private HashSet<string> _unitFieldsTouched = new HashSet<string>();

        /// <summary>
        /// Danh sách các trường thuộc nhóm "Danh mục thuốc" đã được người dùng tương tác  
        /// Bao gồm: Tên danh mục, Mô tả danh mục
        /// Validation riêng biệt để không ảnh hưởng đến các form khác
        /// </summary>
        private HashSet<string> _categoryFieldsTouched = new HashSet<string>();

        /// <summary>
        /// Cờ kích hoạt validation cho nhóm đơn vị tính
        /// True = Hiển thị tất cả lỗi validation trong form quản lý đơn vị
        /// False = Chỉ hiển thị lỗi cho các field đã được touched
        /// </summary>
        private bool _isUnitValidating = false;

        /// <summary>
        /// Cờ kích hoạt validation cho nhóm danh mục thuốc
        /// True = Hiển thị tất cả lỗi validation trong form quản lý danh mục
        /// False = Chỉ hiển thị lỗi cho các field đã được touched
        /// </summary>
        private bool _isCategoryValidating = false;

        /// <summary>
        /// Danh sách các trường dữ liệu chung đã được người dùng tương tác
        /// Sử dụng cho các form không thuộc nhóm cụ thể nào ở trên
        /// Ví dụ: Form thông tin nhà cung cấp, form tìm kiếm, v.v.
        /// </summary>
        private HashSet<string> _touchedFields = new HashSet<string>();

        /// <summary>
        /// Cờ kiểm soát validation chung cho toàn bộ ViewModel
        /// True = Bật validation cho tất cả các form
        /// False = Chỉ validate theo từng nhóm field đã touched
        /// Thường được bật khi người dùng nhấn nút Submit/Save
        /// </summary>
        private bool _isValidating = false;
        #endregion

        #region Initial Data
        /// <summary>
        /// Danh sách ViewModel hiển thị trạng thái của các thuốc trong kho
        /// Bao gồm thông tin cảnh báo về hạn sử dụng và tình trạng tồn kho
        /// Được sử dụng để hiển thị màu sắc và thông báo cảnh báo trên giao diện
        /// </summary>
        private ObservableCollection<StockViewModel> _stockViewModels;
        public ObservableCollection<StockViewModel> StockViewModels
        {
            get => _stockViewModels;
            set
            {
                _stockViewModels = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách tồn kho thuốc gốc từ cơ sở dữ liệu
        /// Chứa thông tin đầy đủ về thuốc, loại thuốc, đơn vị tính và các lô nhập
        /// Được sử dụng làm nguồn dữ liệu chính cho việc hiển thị và lọc
        /// </summary>
        private ObservableCollection<Stock> _ListMedicine;
        public ObservableCollection<Stock> ListMedicine
        {
            get => _ListMedicine;
            set
            {
                _ListMedicine = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách tồn kho thuốc đã qua lọc và phân trang
        /// Chứa dữ liệu hiển thị trên DataGrid sau khi áp dụng các bộ lọc
        /// Được cập nhật tự động khi thay đổi điều kiện lọc hoặc chuyển trang
        /// </summary>
        private ObservableCollection<Stock> _ListStockMedicine;
        public ObservableCollection<Stock> ListStockMedicine
        {
            get => _ListStockMedicine;
            set
            {
                _ListStockMedicine = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách nhà cung cấp hoạt động trong hệ thống
        /// Được sử dụng cho ComboBox chọn nhà cung cấp trong các form nhập thuốc
        /// Chỉ bao gồm các nhà cung cấp chưa bị xóa và đang trong trạng thái hoạt động
        /// </summary>
        private ObservableCollection<Supplier> _SupplierList;
        public ObservableCollection<Supplier> SupplierList
        {
            get => _SupplierList;
            set
            {
                _SupplierList = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Supplier> _SupplierListStockIn;
        public ObservableCollection<Supplier> SupplierListStockIn
        {
            get => _SupplierListStockIn;
            set
            {
                _SupplierListStockIn = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Danh sách đơn vị tính có sẵn trong hệ thống
        /// Được sử dụng cho ComboBox chọn đơn vị tính khi thêm/sửa thuốc
        /// Hỗ trợ việc chuẩn hóa đơn vị đo lường trong quản lý kho
        /// </summary>
        private ObservableCollection<Unit> _UnitList;
        public ObservableCollection<Unit> UnitList
        {
            get => _UnitList;
            set
            {
                _UnitList = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách phân loại thuốc trong hệ thống
        /// Được sử dụng cho ComboBox chọn loại thuốc khi thêm/sửa thuốc
        /// Hỗ trợ việc phân loại và tổ chức thuốc theo nhóm chức năng
        /// </summary>
        private ObservableCollection<MedicineCategory> _CategoryList;
        public ObservableCollection<MedicineCategory> CategoryList
        {
            get => _CategoryList;
            set
            {
                _CategoryList = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Số lượng tạm thời để tính toán
        /// Được sử dụng trong các thao tác tính toán số lượng trung gian
        /// </summary>
        public int TempQuantity { get; set; }

        /// <summary>
        /// Tổng số lượng thuốc hiển thị dưới dạng chuỗi
        /// Tự động chuyển đổi giữa tổng tồn kho hiện tại và tổng tồn kho theo tháng
        /// Phụ thuộc vào chế độ xem đang được chọn (IsMonthlyView)
        /// </summary>
        public string TotalQuantity => IsMonthlyView
            ? MonthlyTotalQuantity.ToString()
            : CurrentTotalQuantity.ToString();

        /// <summary>
        /// Tổng giá trị tồn kho hiển thị dưới dạng chuỗi đã định dạng
        /// Tự động chuyển đổi giữa giá trị tồn kho hiện tại và giá trị tồn kho theo tháng
        /// Sử dụng định dạng "N0" để hiển thị số với dấu phân cách hàng nghìn
        /// </summary>
        public string TotalValue => IsMonthlyView
            ? MonthlyTotalValue.ToString("N0")
            : CurrentTotalValue.ToString("N0");
        #endregion

        #region StockProperties

        /// <summary>
        /// Bộ nhớ đệm chứa toàn bộ danh sách tồn kho thuốc
        /// Được sử dụng để thực hiện lọc mà không cần truy vấn database nhiều lần
        /// Cải thiện hiệu suất khi người dùng thay đổi điều kiện lọc liên tục
        /// </summary>
        private ObservableCollection<Stock> _allStockMedicine;

        /// <summary>
        /// Từ khóa tìm kiếm thuốc trong kho
        /// Hỗ trợ tìm kiếm theo tên thuốc, mã thuốc, barcode
        /// Tự động kích hoạt lọc khi người dùng nhập liệu (real-time search)
        /// </summary>
        private string _SearchStockMedicine;
        public string SearchStockMedicine
        {
            get => _SearchStockMedicine;
            set
            {
                _SearchStockMedicine = value;
                OnPropertyChanged();

                // Tự động lọc dữ liệu khi từ khóa tìm kiếm thay đổi
                // Có thể tắt tính năng này nếu muốn chỉ lọc khi nhấn nút tìm kiếm
                if (IsMonthlyView)
                    FilterMonthlyStock();
                else
                    FilterCurrentStock();
            }
        }

        /// <summary>
        /// Đơn vị tính được chọn để lọc tồn kho
        /// Cho phép người dùng xem chỉ các thuốc có đơn vị tính cụ thể
        /// Khi thay đổi sẽ tự động chuyển đổi sang ID tương ứng và kích hoạt lọc
        /// </summary>
        private Unit _SelectedStockUnit;
        public Unit SelectedStockUnit
        {
            get => _SelectedStockUnit;
            set
            {
                _SelectedStockUnit = value;
                OnPropertyChanged();

                // Chuyển đổi từ object Unit sang ID để sử dụng trong query lọc
                if (value != null)
                {
                    SelectedStockUnitId = GetUnitIdByName(value.UnitName);
                }
                else
                {
                    SelectedStockUnitId = null;
                }
            }
        }

        /// <summary>
        /// ID của đơn vị tính được chọn để lọc
        /// Được sử dụng trong query database để lọc theo đơn vị tính
        /// Tự động kích hoạt lọc dữ liệu khi ID thay đổi
        /// </summary>
        private int? _SelectedStockUnitId;
        public int? SelectedStockUnitId
        {
            get => _SelectedStockUnitId;
            set
            {
                _SelectedStockUnitId = value;
                OnPropertyChanged();

                // Lọc dữ liệu theo chế độ xem hiện tại
                if (IsMonthlyView)
                    FilterMonthlyStock();
                else
                    FilterCurrentStock();
            }
        }

        /// <summary>
        /// Nhà cung cấp được chọn để lọc tồn kho
        /// Cho phép xem chỉ các thuốc từ nhà cung cấp cụ thể
        /// Hữu ích khi cần kiểm tra tồn kho theo từng nhà cung cấp
        /// </summary>
        private Supplier _SelectedStockSupplier;
        public Supplier SelectedStockSupplier
        {
            get => _SelectedStockSupplier;
            set
            {
                _SelectedStockSupplier = value;
                OnPropertyChanged();

                // Chuyển đổi từ object Supplier sang ID để sử dụng trong query lọc
                if (value != null)
                {
                    SelectedStockSupplierId = GetSupplierIdByName(value.SupplierName);
                }
                else
                {
                    SelectedStockSupplierId = null;
                }
            }
        }

        /// <summary>
        /// ID của nhà cung cấp được chọn để lọc
        /// Được sử dụng trong query database để lọc theo nhà cung cấp
        /// Lọc dựa trên các lô nhập (StockIn) có cùng SupplierId
        /// </summary>
        private int? _SelectedStockSupplierId;
        public int? SelectedStockSupplierId
        {
            get => _SelectedStockSupplierId;
            set
            {
                _SelectedStockSupplierId = value;
                OnPropertyChanged();

                // Lọc dữ liệu theo chế độ xem hiện tại
                if (IsMonthlyView)
                    FilterMonthlyStock();
                else
                    FilterCurrentStock();
            }
        }

        /// <summary>
        /// Loại thuốc được chọn để lọc tồn kho
        /// Cho phép xem chỉ các thuốc thuộc danh mục/loại cụ thể
        /// Hỗ trợ việc quản lý tồn kho theo phân loại chức năng của thuốc
        /// </summary>
        private MedicineCategory _SelectedStockCategoryName;
        public MedicineCategory SelectedStockCategoryName
        {
            get => _SelectedStockCategoryName;
            set
            {
                _SelectedStockCategoryName = value;
                OnPropertyChanged();

                // Chuyển đổi từ object MedicineCategory sang ID để sử dụng trong query lọc
                if (value != null)
                {
                    SelectedStockCategoryId = GetCategoryIdByName(value.CategoryName);
                }
                else
                {
                    SelectedStockCategoryId = null;
                }
            }
        }

        /// <summary>
        /// ID của loại thuốc được chọn để lọc
        /// Được sử dụng trong query database để lọc theo CategoryId của thuốc
        /// Tự động kích hoạt lọc dữ liệu khi ID thay đổi
        /// </summary>
        private int? _SelectedStockCategoryId;
        public int? SelectedStockCategoryId
        {
            get => _SelectedStockCategoryId;
            set
            {
                _SelectedStockCategoryId = value;
                // Lưu ý: Sử dụng tên backing field thay vì property name để tránh vòng lặp
                OnPropertyChanged(nameof(SelectedStockCategoryId));

                // Lọc dữ liệu theo chế độ xem hiện tại
                if (IsMonthlyView)
                    FilterMonthlyStock();
                else
                    FilterCurrentStock();
            }
        }

        #endregion

        #region Monthly Stock Properties

        /// <summary>
        /// Cờ hiệu chế độ xem tồn kho theo tháng
        /// True = Xem báo cáo tồn kho theo tháng đã tổng kết
        /// False = Xem tồn kho hiện tại (real-time)
        /// Tự động kích hoạt lọc dữ liệu khi thay đổi
        /// </summary>
        private bool _isMonthlyView;
        public bool IsMonthlyView
        {
            get => _isMonthlyView;
            set
            {
                _isMonthlyView = value;
                OnPropertyChanged();
                // Cập nhật dữ liệu khi thay đổi chế độ xem
                if (_isMonthlyView)
                    FilterMonthlyStock();
                else
                    FilterCurrentStock();
            }
        }

        /// <summary>
        /// Danh sách các tháng trong năm (1-12) để lựa chọn
        /// Được khởi tạo tự động từ 1 đến 12 sử dụng Enumerable.Range
        /// Dùng cho ComboBox chọn tháng trong báo cáo tồn kho theo tháng
        /// </summary>
        private ObservableCollection<int> _monthOptions = new ObservableCollection<int>(
            Enumerable.Range(1, 12).ToList()
        );
        public ObservableCollection<int> MonthOptions
        {
            get => _monthOptions;
            set
            {
                _monthOptions = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách các năm để lựa chọn trong báo cáo
        /// Bao gồm 5 năm trước và năm hiện tại (tổng cộng 6 năm)
        /// Được tính toán động dựa trên năm hiện tại
        /// </summary>
        private ObservableCollection<int> _yearOptions = new ObservableCollection<int>(
            Enumerable.Range(DateTime.Now.Year - 5, 6).ToList() // 5 năm trước và năm hiện tại
        );
        public ObservableCollection<int> YearOptions
        {
            get => _yearOptions;
            set
            {
                _yearOptions = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tháng được chọn để xem báo cáo tồn kho
        /// Mặc định là tháng hiện tại
        /// Tự động kích hoạt lọc dữ liệu khi thay đổi (nếu đang ở chế độ xem theo tháng)
        /// </summary>
        private int _selectedMonth = DateTime.Now.Month;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set
            {
                _selectedMonth = value;
                OnPropertyChanged();
                if (IsMonthlyView) FilterMonthlyStock();
            }
        }

        /// <summary>
        /// Năm được chọn để xem báo cáo tồn kho
        /// Mặc định là năm hiện tại
        /// Tự động kích hoạt lọc dữ liệu khi thay đổi (nếu đang ở chế độ xem theo tháng)
        /// </summary>
        private int _selectedYear = DateTime.Now.Year;
        public int SelectedYear
        {
            get => _selectedYear;
            set
            {
                _selectedYear = value;
                OnPropertyChanged();
                if (IsMonthlyView) FilterMonthlyStock();
            }
        }

        /// <summary>
        /// Danh sách tồn kho theo tháng đã được tổng kết
        /// Chứa dữ liệu snapshot của tồn kho tại thời điểm cuối tháng
        /// Được tải từ bảng MonthlyStock trong database
        /// </summary>
        private ObservableCollection<MonthlyStock> _monthlyStockList = new ObservableCollection<MonthlyStock>();
        public ObservableCollection<MonthlyStock> MonthlyStockList
        {
            get => _monthlyStockList;
            set
            {
                _monthlyStockList = value;
                OnPropertyChanged();
            }
        }

        // === CÁC THUỘC TÍNH TỔNG HỢP THEO CHȘTE ĐỘ XEM ===

        /// <summary>
        /// Tổng số lượng tồn kho trong tháng được chọn
        /// Chỉ hiển thị khi ở chế độ xem theo tháng
        /// Được tính từ dữ liệu MonthlyStockList
        /// </summary>
        private int _monthlyTotalQuantity;
        public int MonthlyTotalQuantity
        {
            get => _monthlyTotalQuantity;
            set
            {
                _monthlyTotalQuantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalQuantity)); // Cập nhật computed property
            }
        }

        /// <summary>
        /// Tổng giá trị tồn kho trong tháng được chọn
        /// Chỉ hiển thị khi ở chế độ xem theo tháng
        /// Tính bằng cách nhân số lượng với giá hiện tại của từng thuốc
        /// </summary>
        private decimal _monthlyTotalValue;
        public decimal MonthlyTotalValue
        {
            get => _monthlyTotalValue;
            set
            {
                _monthlyTotalValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalValue)); // Cập nhật computed property
            }
        }

        /// <summary>
        /// Tổng số lượng tồn kho hiện tại (real-time)
        /// Chỉ hiển thị khi ở chế độ xem tồn kho hiện tại
        /// Được tính từ dữ liệu ListStockMedicine thời gian thực
        /// </summary>
        private int _currentTotalQuantity;
        public int CurrentTotalQuantity
        {
            get => _currentTotalQuantity;
            set
            {
                _currentTotalQuantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalQuantity)); // Cập nhật computed property
            }
        }

        /// <summary>
        /// Tổng giá trị tồn kho hiện tại (real-time)
        /// Chỉ hiển thị khi ở chế độ xem tồn kho hiện tại
        /// Tính bằng cách nhân số lượng với giá hiện tại của từng thuốc
        /// </summary>
        private decimal _currentTotalValue;
        public decimal CurrentTotalValue
        {
            get => _currentTotalValue;
            set
            {
                _currentTotalValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalValue)); // Cập nhật computed property
            }
        }

        #endregion

        #region UnitProperties

        /// <summary>
        /// Tên đơn vị tính trong form quản lý đơn vị
        /// Hỗ trợ validation: không được trống, tối thiểu 2 ký tự, tối đa 50 ký tự
        /// Tự động theo dõi trạng thái touched để hiển thị validation phù hợp
        /// </summary>
        private string _UnitName;
        public string UnitName
        {
            get => _UnitName;
            set
            {
                if (_UnitName != value)
                {
                    // Theo dõi trạng thái empty -> có giá trị hoặc ngược lại
                    bool wasEmpty = string.IsNullOrWhiteSpace(_UnitName);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    // Đánh dấu field đã được touched khi người dùng bắt đầu nhập
                    if (wasEmpty && !isEmpty)
                        _unitFieldsTouched.Add(nameof(UnitName));
                    // Xóa khỏi danh sách touched nếu người dùng xóa hết
                    else if (!wasEmpty && isEmpty)
                    {
                        _unitFieldsTouched.Remove(nameof(UnitName));
                        OnPropertyChanged(nameof(Error)); // Trigger validation update
                    }

                    _UnitName = value;
                    OnPropertyChanged();

                    // Chỉ validate nếu field đã được touched
                    if (_unitFieldsTouched.Contains(nameof(UnitName)))
                        OnPropertyChanged(nameof(Error));

                    // Cập nhật trạng thái CanExecute của các command liên quan
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Mô tả đơn vị tính trong form quản lý đơn vị
        /// Validation: không bắt buộc, nhưng nếu có thì tối đa 255 ký tự
        /// Tự động theo dõi trạng thái touched để hiển thị validation phù hợp
        /// </summary>
        private string _UnitDescription;
        public string UnitDescription
        {
            get => _UnitDescription;
            set
            {
                if (_UnitDescription != value)
                {
                    // Theo dõi trạng thái empty -> có giá trị hoặc ngược lại
                    bool wasEmpty = string.IsNullOrWhiteSpace(_UnitDescription);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    // Đánh dấu field đã được touched khi người dùng bắt đầu nhập
                    if (wasEmpty && !isEmpty)
                        _unitFieldsTouched.Add(nameof(UnitDescription));
                    // Xóa khỏi danh sách touched nếu người dùng xóa hết
                    else if (!wasEmpty && isEmpty)
                    {
                        _unitFieldsTouched.Remove(nameof(UnitDescription));
                        OnPropertyChanged(nameof(Error)); // Trigger validation update
                    }

                    _UnitDescription = value;
                    OnPropertyChanged();

                    // Chỉ validate nếu field đã được touched
                    if (_unitFieldsTouched.Contains(nameof(UnitDescription)))
                        OnPropertyChanged(nameof(Error));
                }
            }
        }

        /// <summary>
        /// Đơn vị tính được chọn trong DataGrid/ComboBox
        /// Khi thay đổi sẽ tự động load thông tin vào UnitName và UnitDescription
        /// Sử dụng trong chức năng chỉnh sửa đơn vị tính
        /// </summary>
        private Unit _SelectedUnit;
        public Unit SelectedUnit
        {
            get => _SelectedUnit;
            set
            {
                _SelectedUnit = value;
                OnPropertyChanged(nameof(SelectedUnit)); // Sử dụng nameof thay vì backing field

                // Tự động điền thông tin khi chọn đơn vị để chỉnh sửa
                if (SelectedUnit != null)
                {
                    UnitName = SelectedUnit.UnitName;
                    UnitDescription = SelectedUnit.Description;
                }
            }
        }

        #endregion

        #region CategoryProperties

        /// <summary>
        /// Tên loại thuốc trong form quản lý danh mục
        /// Hỗ trợ validation: không được trống, tối thiểu 2 ký tự, tối đa 50 ký tự
        /// Tự động theo dõi trạng thái touched để hiển thị validation phù hợp
        /// </summary>
        private string _CategoryName;
        public string CategoryName
        {
            get => _CategoryName;
            set
            {
                if (_CategoryName != value)
                {
                    // Theo dõi trạng thái empty -> có giá trị hoặc ngược lại
                    bool wasEmpty = string.IsNullOrWhiteSpace(_CategoryName);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    // Đánh dấu field đã được touched khi người dùng bắt đầu nhập
                    if (wasEmpty && !isEmpty)
                        _categoryFieldsTouched.Add(nameof(CategoryName));
                    // Xóa khỏi danh sách touched nếu người dùng xóa hết
                    else if (!wasEmpty && isEmpty)
                    {
                        _categoryFieldsTouched.Remove(nameof(CategoryName));
                        OnPropertyChanged(nameof(Error)); // Trigger validation update
                    }

                    _CategoryName = value;
                    OnPropertyChanged();

                    // Chỉ validate nếu field đã được touched
                    if (_categoryFieldsTouched.Contains(nameof(CategoryName)))
                        OnPropertyChanged(nameof(Error));

                    // Cập nhật trạng thái CanExecute của các command liên quan
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Mô tả loại thuốc trong form quản lý danh mục
        /// Validation: không bắt buộc, nhưng nếu có thì tối đa 255 ký tự
        /// Tự động theo dõi trạng thái touched để hiển thị validation phù hợp
        /// </summary>
        private string _CategoryDescription;
        public string CategoryDescription
        {
            get => _CategoryDescription;
            set
            {
                if (_CategoryDescription != value)
                {
                    // Theo dõi trạng thái empty -> có giá trị hoặc ngược lại
                    bool wasEmpty = string.IsNullOrWhiteSpace(_CategoryDescription);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    // Đánh dấu field đã được touched khi người dùng bắt đầu nhập
                    if (wasEmpty && !isEmpty)
                        _categoryFieldsTouched.Add(nameof(CategoryDescription));
                    // Xóa khỏi danh sách touched nếu người dùng xóa hết
                    else if (!wasEmpty && isEmpty)
                    {
                        _categoryFieldsTouched.Remove(nameof(CategoryDescription));
                        OnPropertyChanged(nameof(Error)); // Trigger validation update
                    }

                    _CategoryDescription = value;
                    OnPropertyChanged();

                    // Chỉ validate nếu field đã được touched
                    if (_categoryFieldsTouched.Contains(nameof(CategoryDescription)))
                        OnPropertyChanged(nameof(Error));
                }
            }
        }

        /// <summary>
        /// Loại thuốc được chọn trong DataGrid/ComboBox
        /// Khi thay đổi sẽ tự động load thông tin vào CategoryName và CategoryDescription
        /// Sử dụng trong chức năng chỉnh sửa loại thuốc
        /// </summary>
        private MedicineCategory _SelectedCategory;
        public MedicineCategory SelectedCategory
        {
            get => _SelectedCategory;
            set
            {
                _SelectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory)); // Sử dụng nameof thay vì backing field

                // Tự động điền thông tin khi chọn loại thuốc để chỉnh sửa
                if (SelectedCategory != null)
                {
                    CategoryName = SelectedCategory.CategoryName;
                    CategoryDescription = SelectedCategory.Description;
                }
            }
        }

        #endregion

        #region SupplierProperties

        /// <summary>
        /// Nhà cung cấp được chọn từ DataGrid/ComboBox
        /// Khi thay đổi sẽ tự động load tất cả thông tin vào các field tương ứng
        /// Sử dụng trong chức năng chỉnh sửa thông tin nhà cung cấp
        /// </summary>
        private Supplier _SelectedSupplier;
        public Supplier SelectedSupplier
        {
            get => _SelectedSupplier;
            set
            {
                _SelectedSupplier = value;
                OnPropertyChanged(nameof(SelectedSupplier)); // Sử dụng nameof thay vì backing field

                // Tự động điền thông tin khi chọn nhà cung cấp để chỉnh sửa
                if (SelectedSupplier != null)
                {
                    SupplierCode = SelectedSupplier.SupplierCode;
                    SupplierAddress = SelectedSupplier.Address;
                    SupplierPhone = SelectedSupplier.Phone;
                    SupplierEmail = SelectedSupplier.Email;
                    SupplierName = SelectedSupplier.SupplierName;
                    SupplierTaxCode = SelectedSupplier.TaxCode;
                    ContactPerson = SelectedSupplier.ContactPerson;

                    // Thiết lập trạng thái hoạt động dựa trên dữ liệu từ database
                    if (SelectedSupplier.IsActive == true)
                    {
                        IsActive = true;
                        IsNotActive = false;
                    }
                    else
                    {
                        IsActive = false;
                        IsNotActive = true;
                    }
                }
            }
        }

        /// <summary>
        /// Mã nhà cung cấp - có validation và theo dõi touched state
        /// Định dạng yêu cầu: NCC + số (VD: NCC001, NCC123)
        /// Trường bắt buộc khi thêm/sửa nhà cung cấp
        /// </summary>
        private string _supplierCode;
        public string SupplierCode
        {
            get => _supplierCode;
            set
            {
                if (_supplierCode != value)
                {
                    // Đánh dấu field đã được touched khi người dùng bắt đầu nhập
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_supplierCode))
                        _touchedFields.Add(nameof(SupplierCode));
                    else
                        _touchedFields.Remove(nameof(SupplierCode));

                    _supplierCode = value;
                    OnPropertyChanged(nameof(SupplierCode));
                }
            }
        }

        /// <summary>
        /// Tên nhà cung cấp - có validation và theo dõi touched state
        /// Trường bắt buộc với độ dài tối thiểu 2 ký tự
        /// Dùng để hiển thị và tìm kiếm nhà cung cấp
        /// </summary>
        private string _supplierName;
        public string SupplierName
        {
            get => _supplierName;
            set
            {
                if (_supplierName != value)
                {
                    // Đánh dấu field đã được touched khi người dùng bắt đầu nhập
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_supplierName))
                        _touchedFields.Add(nameof(SupplierName));
                    else
                        _touchedFields.Remove(nameof(SupplierName));

                    _supplierName = value;
                    OnPropertyChanged(nameof(SupplierName));
                }
            }
        }

        /// <summary>
        /// Người liên hệ của nhà cung cấp - trường tùy chọn
        /// Không có validation đặc biệt, chỉ lưu trữ thông tin
        /// </summary>
        private string _contactPerson;
        public string ContactPerson
        {
            get => _contactPerson;
            set
            {
                _contactPerson = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Số điện thoại nhà cung cấp - có validation và theo dõi touched state
        /// Định dạng số điện thoại Việt Nam (VD: 0901234567)
        /// Trường bắt buộc khi thêm nhà cung cấp
        /// </summary>
        private string _supplierPhone;
        public string SupplierPhone
        {
            get => _supplierPhone;
            set
            {
                if (_supplierPhone != value)
                {
                    // Đánh dấu field đã được touched khi người dùng bắt đầu nhập hoặc xóa
                    if (!string.IsNullOrEmpty(value))
                        _touchedFields.Add(nameof(SupplierPhone));
                    else
                        _touchedFields.Remove(nameof(SupplierPhone));

                    _supplierPhone = value;
                    OnPropertyChanged(nameof(SupplierPhone));
                }
            }
        }

        /// <summary>
        /// Email nhà cung cấp - có validation và theo dõi touched state
        /// Định dạng email chuẩn (VD: contact@company.com)
        /// Trường tùy chọn nhưng nếu nhập phải đúng định dạng
        /// </summary>
        private string _supplierEmail;
        public string SupplierEmail
        {
            get => _supplierEmail;
            set
            {
                if (_supplierEmail != value)
                {
                    // Đánh dấu field đã được touched khi người dùng bắt đầu nhập
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_supplierEmail))
                        _touchedFields.Add(nameof(SupplierEmail));
                    else
                        _touchedFields.Remove(nameof(SupplierEmail));

                    _supplierEmail = value;
                    OnPropertyChanged(nameof(SupplierEmail));
                }
            }
        }

        /// <summary>
        /// Mã số thuế của nhà cung cấp - có validation và theo dõi touched state
        /// Định dạng: 10 số hoặc 10-3 số (VD: 0123456789 hoặc 0123456789-001)
        /// Trường tùy chọn nhưng nếu nhập phải đúng định dạng
        /// </summary>
        private string _supplierTaxCode;
        public string SupplierTaxCode
        {
            get => _supplierTaxCode;
            set
            {
                if (_supplierTaxCode != value)
                {
                    // Đánh dấu field đã được touched khi người dùng bắt đầu nhập
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_supplierTaxCode))
                        _touchedFields.Add(nameof(SupplierTaxCode));
                    else
                        _touchedFields.Remove(nameof(SupplierTaxCode));

                    _supplierTaxCode = value;
                    OnPropertyChanged(nameof(SupplierTaxCode));
                }
            }
        }

        /// <summary>
        /// Địa chỉ nhà cung cấp - trường tùy chọn
        /// Không có validation đặc biệt, chỉ lưu trữ thông tin
        /// </summary>
        private string _supplierAddress;
        public string SupplierAddress
        {
            get => _supplierAddress;
            set
            {
                _supplierAddress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Trạng thái hoạt động của nhà cung cấp
        /// True = nhà cung cấp đang hoạt động, có thể nhập hàng
        /// False = nhà cung cấp tạm ngưng hoạt động
        /// </summary>
        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Trạng thái không hoạt động của nhà cung cấp
        /// Ngược lại với IsActive, dùng cho UI binding (RadioButton)
        /// True = nhà cung cấp không hoạt động
        /// </summary>
        private bool _isNoActive;
        public bool IsNotActive
        {
            get => _isNoActive;
            set
            {
                _isNoActive = value;
                OnPropertyChanged();
            }
        }

        // === BỘ LỌC VÀ TÌM KIẾM NHÀ CUNG CẤP ===

        /// <summary>
        /// Bộ nhớ đệm chứa tất cả nhà cung cấp
        /// Sử dụng để thực hiện lọc mà không cần truy vấn database nhiều lần
        /// </summary>
        private ObservableCollection<Supplier> _allSuppliers;

        /// <summary>
        /// Từ khóa tìm kiếm nhà cung cấp
        /// Tìm theo tên, mã, số điện thoại hoặc người liên hệ
        /// Tự động kích hoạt lọc khi thay đổi
        /// </summary>
        private string _supplierSearchText;
        public string SupplierSearchText
        {
            get => _supplierSearchText;
            set
            {
                _supplierSearchText = value;
                OnPropertyChanged();
                // Tự động lọc khi từ khóa thay đổi
                ExecuteAutoSupplierFilter();
            }
        }

        // === CÁC CHECKBOX FILTER NHÀ CUNG CẤP ===

        /// <summary>
        /// Checkbox hiển thị tất cả nhà cung cấp
        /// Khi chọn sẽ bỏ filter theo trạng thái hoạt động
        /// Mặc định được chọn khi khởi tạo
        /// </summary>
        private bool _showAllSuppliers = true;
        public bool ShowAllSuppliers
        {
            get => _showAllSuppliers;
            set
            {
                if (_showAllSuppliers != value)
                {
                    _showAllSuppliers = value;
                    OnPropertyChanged();

                    // Khi chọn "Tất cả", bỏ chọn các checkbox khác
                    if (value)
                    {
                        _showActiveSuppliers = false;
                        _showInactiveSuppliers = false;
                        OnPropertyChanged(nameof(ShowActiveSuppliers));
                        OnPropertyChanged(nameof(ShowInactiveSuppliers));
                        ExecuteAutoSupplierFilter();
                    }
                }
            }
        }

        /// <summary>
        /// Checkbox lọc chỉ nhà cung cấp đang hoạt động
        /// Khi chọn sẽ uncheck các checkbox khác và kích hoạt filter
        /// </summary>
        private bool _showActiveSuppliers;
        public bool ShowActiveSuppliers
        {
            get => _showActiveSuppliers;
            set
            {
                if (_showActiveSuppliers != value)
                {
                    _showActiveSuppliers = value;
                    OnPropertyChanged();

                    // Khi chọn "Hoạt động", bỏ chọn các checkbox khác
                    if (value)
                    {
                        _showAllSuppliers = false;
                        _showInactiveSuppliers = false;
                        OnPropertyChanged(nameof(ShowAllSuppliers));
                        OnPropertyChanged(nameof(ShowInactiveSuppliers));
                        ExecuteAutoSupplierFilter();
                    }
                }
            }
        }

        /// <summary>
        /// Checkbox lọc chỉ nhà cung cấp không hoạt động
        /// Khi chọn sẽ uncheck các checkbox khác và kích hoạt filter
        /// </summary>
        private bool _showInactiveSuppliers;
        public bool ShowInactiveSuppliers
        {
            get => _showInactiveSuppliers;
            set
            {
                if (_showInactiveSuppliers != value)
                {
                    _showInactiveSuppliers = value;
                    OnPropertyChanged();

                    // Khi chọn "Không hoạt động", bỏ chọn các checkbox khác
                    if (value)
                    {
                        _showAllSuppliers = false;
                        _showActiveSuppliers = false;
                        OnPropertyChanged(nameof(ShowAllSuppliers));
                        OnPropertyChanged(nameof(ShowActiveSuppliers));
                        ExecuteAutoSupplierFilter();
                    }
                }
            }
        }
        #endregion

        #region StockinMedicine Properties

        /// <summary>
        /// Thuốc hiện tại đang được xem chi tiết lô nhập
        /// Sử dụng trong cửa sổ chi tiết thuốc để hiển thị thông tin
        /// </summary>
        private Medicine _currentMedicine;
        public Medicine CurrentMedicine
        {
            get => _currentMedicine;
            set
            {
                _currentMedicine = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách chi tiết các lô nhập của thuốc hiện tại
        /// Hiển thị thông tin số lượng còn lại, ngày nhập, hạn sử dụng của từng lô
        /// </summary>
        private ObservableCollection<Medicine.StockInWithRemaining> _currentMedicineDetails;
        public ObservableCollection<Medicine.StockInWithRemaining> CurrentMedicineDetails
        {
            get => _currentMedicineDetails;
            set
            {
                _currentMedicineDetails = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tiêu đề dialog chi tiết thuốc
        /// Tự động cập nhật dựa trên tên thuốc hiện tại
        /// </summary>
        public string DialogTitle => CurrentMedicine != null
            ? $"Chi tiết các lô thuốc: {CurrentMedicine.Name}"
            : "Chi tiết lô thuốc";

        // === CÁC THUỘC TÍNH CHO FORM NHẬP KHO ===

        /// <summary>
        /// Loại thuốc được chọn trong form nhập kho
        /// Bắt buộc phải chọn khi thêm thuốc mới
        /// </summary>
        private MedicineCategory _stockinSelectedCategory;
        public MedicineCategory StockinSelectedCategory
        {
            get => _stockinSelectedCategory;
            set
            {
                _stockinSelectedCategory = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Nhà cung cấp được chọn trong form nhập kho
        /// Bắt buộc phải chọn khi thêm lô thuốc mới
        /// </summary>
        private Supplier _stockinSelectedSupplier;
        public Supplier StockinSelectedSupplier
        {
            get => _stockinSelectedSupplier;
            set
            {
                _stockinSelectedSupplier = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Đơn vị tính được chọn trong form nhập kho
        /// Bắt buộc phải chọn khi thêm thuốc mới
        /// </summary>
        private Unit _stockinSelectedUnit;
        public Unit StockinSelectedUnit
        {
            get => _stockinSelectedUnit;
            set
            {
                _stockinSelectedUnit = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tên thuốc trong form nhập kho - có validation và theo dõi touched state
        /// Trường bắt buộc với độ dài tối thiểu 2 ký tự
        /// Tự động refresh command availability khi thay đổi
        /// </summary>
        private string _stockinMedicineName;
        public string StockinMedicineName
        {
            get => _stockinMedicineName;
            set
            {
                if (_stockinMedicineName != value)
                {
                    // Theo dõi trạng thái touched để validation thông minh
                    bool wasEmpty = string.IsNullOrWhiteSpace(_stockinMedicineName);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _medicineFieldsTouched.Add(nameof(StockinMedicineName));
                    else if (!wasEmpty && isEmpty)
                    {
                        _medicineFieldsTouched.Remove(nameof(StockinMedicineName));
                        OnPropertyChanged(nameof(Error));
                    }

                    _stockinMedicineName = value;
                    OnPropertyChanged();

                    // Kích hoạt validation nếu field đã được touched
                    if (_medicineFieldsTouched.Contains(nameof(StockinMedicineName)))
                        OnPropertyChanged(nameof(Error));

                    // Refresh khả năng thực thi command
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Mã vạch thuốc - có validation và theo dõi touched state
        /// Trường tùy chọn nhưng nếu nhập phải có ít nhất 8 ký tự
        /// </summary>
        private string _stockinBarCode;
        public string StockinBarCode
        {
            get => _stockinBarCode;
            set
            {
                if (_stockinBarCode != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_stockinBarCode);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _medicineFieldsTouched.Add(nameof(StockinBarCode));
                    else if (!wasEmpty && isEmpty)
                    {
                        _medicineFieldsTouched.Remove(nameof(StockinBarCode));
                        OnPropertyChanged(nameof(Error));
                    }

                    _stockinBarCode = value;
                    OnPropertyChanged();

                    if (_medicineFieldsTouched.Contains(nameof(StockinBarCode)))
                        OnPropertyChanged(nameof(Error));
                }
            }
        }

        /// <summary>
        /// Mã QR thuốc - có validation và theo dõi touched state
        /// Trường tùy chọn, không có validation đặc biệt
        /// </summary>
        private string _stockinQrCode;
        public string StockinQrCode
        {
            get => _stockinQrCode;
            set
            {
                if (_stockinQrCode != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_stockinQrCode);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _medicineFieldsTouched.Add(nameof(StockinQrCode));
                    else if (!wasEmpty && isEmpty)
                    {
                        _medicineFieldsTouched.Remove(nameof(StockinQrCode));
                        OnPropertyChanged(nameof(Error));
                    }

                    _stockinQrCode = value;
                    OnPropertyChanged();

                    if (_medicineFieldsTouched.Contains(nameof(StockinQrCode)))
                        OnPropertyChanged(nameof(Error));
                }
            }
        }

        /// <summary>
        /// Số lượng thuốc nhập kho - có validation và theo dõi touched state
        /// Trường bắt buộc, phải lớn hơn 0
        /// Mặc định là 1 để thuận tiện cho người dùng
        /// </summary>
        private int _stockinQuantity = 1;
        public int StockinQuantity
        {
            get => _stockinQuantity;
            set
            {
                if (_stockinQuantity != value)
                {
                    // Đánh dấu touched khi có giá trị khác 0
                    if (value != 0)
                        _medicineFieldsTouched.Add(nameof(StockinQuantity));
                    else
                        _medicineFieldsTouched.Remove(nameof(StockinQuantity));

                    _stockinQuantity = value;
                    OnPropertyChanged();

                    if (_medicineFieldsTouched.Contains(nameof(StockinQuantity)))
                        OnPropertyChanged(nameof(Error));

                    // Refresh command availability khi số lượng thay đổi
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Giá nhập của thuốc - có validation và theo dõi touched state
        /// Trường bắt buộc, phải lớn hơn 0
        /// Khi thay đổi sẽ tự động tính lại giá bán dựa trên margin lợi nhuận
        /// </summary>
        private decimal _stockinUnitPrice;
        public decimal StockinUnitPrice
        {
            get => _stockinUnitPrice;
            set
            {
                if (_stockinUnitPrice != value)
                {
                    // Đánh dấu touched khi có giá trị khác 0
                    if (value != 0)
                        _medicineFieldsTouched.Add(nameof(StockinUnitPrice));
                    else
                        _medicineFieldsTouched.Remove(nameof(StockinUnitPrice));

                    _stockinUnitPrice = value;
                    OnPropertyChanged();

                    // Tự động tính lại giá bán khi có margin lợi nhuận
                    if (_StockProfitMargin > 0)
                    {
                        // Tính giá bán dựa trên giá nhập và margin lợi nhuận
                        _stockinSellPrice = _stockinUnitPrice * (1 + (_StockProfitMargin / 100));
                        OnPropertyChanged(nameof(StockinSellPrice));
                    }
                    else if (_stockinSellPrice > 0)
                    {
                        // Tính margin lợi nhuận dựa trên giá nhập và giá bán
                        CalculateProfitMargin();
                    }

                    if (_medicineFieldsTouched.Contains(nameof(StockinUnitPrice)))
                        OnPropertyChanged(nameof(Error));

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Ngày hết hạn của lô thuốc - có validation và theo dõi touched state
        /// Trường bắt buộc, phải sau ngày hiện tại
        /// Mặc định là 3 năm sau ngày hiện tại
        /// </summary>
        private DateTime? _stockinExpiryDate = DateTime.Now.AddYears(3);
        public DateTime? StockinExpiryDate
        {
            get => _stockinExpiryDate;
            set
            {
                if (_stockinExpiryDate != value)
                {
                    // Đánh dấu touched khi có giá trị
                    if (value.HasValue)
                        _medicineFieldsTouched.Add(nameof(StockinExpiryDate));
                    else
                        _medicineFieldsTouched.Remove(nameof(StockinExpiryDate));

                    _stockinExpiryDate = value;
                    OnPropertyChanged();

                    if (_medicineFieldsTouched.Contains(nameof(StockinExpiryDate)))
                        OnPropertyChanged(nameof(Error));

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// Tỷ lệ lợi nhuận của lô thuốc (%)
        /// Khi thay đổi sẽ tự động tính lại giá bán hoặc giá nhập tương ứng
        /// Tạo sự linh hoạt trong việc thiết lập giá
        /// </summary>
        private decimal _StockProfitMargin;
        public decimal StockProfitMargin
        {
            get => _StockProfitMargin;
            set
            {
                _StockProfitMargin = value;
                OnPropertyChanged();

                // Nếu giá nhập đã có, tính lại giá bán dựa trên margin
                if (_stockinUnitPrice > 0)
                {
                    // Tính giá bán dựa trên giá nhập và margin lợi nhuận
                    _stockinSellPrice = _stockinUnitPrice * (1 + (_StockProfitMargin / 100));
                    OnPropertyChanged(nameof(StockinSellPrice));
                }
                else if (_stockinSellPrice > 0)
                {
                    // Tính giá nhập dựa trên giá bán và margin lợi nhuận
                    _stockinUnitPrice = _stockinSellPrice / (1 + (_StockProfitMargin / 100));
                    OnPropertyChanged(nameof(StockinUnitPrice));
                }
            }
        }

        /// <summary>
        /// Giá bán của thuốc
        /// Khi thay đổi sẽ tự động tính lại margin lợi nhuận nếu có giá nhập
        /// Phải lớn hơn hoặc bằng giá nhập
        /// </summary>
        private decimal _stockinSellPrice;
        public decimal StockinSellPrice
        {
            get => _stockinSellPrice;
            set
            {
                _stockinSellPrice = value;
                OnPropertyChanged();

                // Nếu giá nhập đã có, tính lại margin lợi nhuận
                if (_stockinUnitPrice > 0)
                {
                    CalculateProfitMargin();
                }
            }
        }

        /// <summary>
        /// Phương thức helper tính toán margin lợi nhuận dựa trên giá nhập và giá bán
        /// Công thức: ((giá bán / giá nhập) - 1) * 100
        /// </summary>
        private void CalculateProfitMargin()
        {
            if (_stockinUnitPrice > 0 && _stockinSellPrice > 0)
            {
                // Tính phần trăm lợi nhuận
                _StockProfitMargin = ((_stockinSellPrice / _stockinUnitPrice) - 1) * 100;
                OnPropertyChanged(nameof(StockProfitMargin));
            }
        }

        /// <summary>
        /// Thuốc được chọn từ danh sách để thêm vào lô nhập
        /// Khi chọn sẽ tự động load thông tin thuốc vào form
        /// </summary>
        private Stock _selectedMedicine;
        public Stock SelectedMedicine
        {
            get => _selectedMedicine;
            set
            {
                _selectedMedicine = value;
                OnPropertyChanged();
                // Tự động load chi tiết thuốc khi chọn
                if (value != null)
                {
                    LoadMedicineDetailsForStockIn(value.Medicine);
                }
            }
        }

        /// <summary>
        /// Ngày nhập kho của lô thuốc
        /// Mặc định là ngày hiện tại, có thể thay đổi khi cần thiết
        /// </summary>
        private DateTime? _importDate = DateTime.Now;
        public DateTime? ImportDate
        {
            get => _importDate;
            set
            {
                _importDate = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Authentication Properties

        /// <summary>
        /// Tài khoản hiện tại đang đăng nhập hệ thống
        /// Được sử dụng để kiểm tra quyền thao tác với kho thuốc
        /// Khi thay đổi sẽ tự động cập nhật quyền thông qua UpdatePermissions()
        /// </summary>
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                // Cập nhật quyền mỗi khi tài khoản thay đổi
                UpdatePermissions();
            }
        }

        /// <summary>
        /// Quyền chỉnh sửa thông tin tồn kho
        /// True = có thể chỉnh sửa lô thuốc, cập nhật số lượng, giá cả
        /// False = chỉ có thể xem, không được thay đổi dữ liệu
        /// Phụ thuộc vào vai trò: Admin, Manager, Pharmacist được phép
        /// </summary>
        private bool _canEdit;
        public bool CanEdit
        {
            get => _canEdit;
            set
            {
                _canEdit = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Quyền thêm thuốc mới vào hệ thống
        /// True = có thể tạo thuốc mới và nhập lô đầu tiên
        /// False = chỉ có thể nhập thêm lô cho thuốc đã có
        /// Phụ thuộc vào vai trò: Admin, Manager, Pharmacist, Doctor được phép
        /// </summary>
        private bool _canAddNewMedicine;
        public bool CanAddNewMedicine
        {
            get => _canAddNewMedicine;
            set
            {
                _canAddNewMedicine = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Quyền quản lý cài đặt hệ thống kho
        /// True = có thể thêm/sửa/xóa đơn vị tính, loại thuốc, nhà cung cấp
        /// False = chỉ có thể sử dụng dữ liệu có sẵn
        /// Phụ thuộc vào vai trò: Chỉ Admin và Manager được phép
        /// </summary>
        private bool _canManageSettings;
        public bool CanManageSettings
        {
            get => _canManageSettings;
            set
            {
                _canManageSettings = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands
        // Unit Commands
        public ICommand AddUnitCommand { get; set; } //Lệnh thêm đơn vị thuốc
        public ICommand EditUnitCommand { get; set; } //Lệnh sửa đơn vị thuốc
        public ICommand DeleteUnitCommand { get; set; } //Lệnh xóa đơn vị thuốc
        public ICommand RefreshUnitCommand { get; set; } // Lệnh làm mới danh sách đơn vị thuốc


        // Supplier Commands
        public ICommand AddSupplierCommand { get; set; } //Lệnh thêm nhà cung cấp
        public ICommand EditSupplierCommand { get; set; } // Lệnh sửa nhà cung cấp
        public ICommand DeleteSupplierCommand { get; set; } // Lệnh xóa nhà cung cấp
        public ICommand SetActiveStatusCommand { get; set; } // Lệnh đặt trạng thái nhà cung cấp là hoạt động
        public ICommand SetNoActiveStatusCommand { get; set; } // Lệnh đặt trạng thái nhà cung cấp là không hoạt động
        public ICommand SearchSupplierCommand { get; set; } // Lệnh tìm kiếm nhà cung cấp
        public ICommand RefreshSulpierCommand { get; set; } // Lệnh làm mới danh sách nhà cung cấp


        // Category Commands
        public ICommand AddCategoryCommand { get; set; } // Lệnh thêm danh mục thuốc
        public ICommand EditCategoryCommand { get; set; } //Lệnh sửa danh mục thuốc
        public ICommand DeleteCategoryCommand { get; set; } // Lệnh xóa danh mục thuốc
        public ICommand RefreshCatergoryCommand { get; set; } // Lệnh làm mới danh sách danh mục thuốc


        public ICommand OpenDoctorDetailsCommand { get; set; } //Lệnh mở chi tiết bác sĩ

        //StockMedicine Commands
        public ICommand SearchStockMedicineCommand { get; set; } // Lệnh tìm kiếm thuốc trong kho
        public ICommand ResetStockFiltersCommand { get; set; } // Lệnh đặt lại bộ lọc thuốc trong kho
        public ICommand DeleteMedicine { get; set; } // Lệnh xóa thuốc khỏi kho
        public ICommand ExportStockExcelCommand { get;  set; } // Lệnh xuất tồn kho ra file Excel
        public ICommand ManualGenerateMonthlyStockCommand { get; private set; } // Lệnh tạo tồn kho tháng thủ công
        public ICommand UndoMonthlyStockCommand { get; private set; }// Lệnh hoàn tác tồn kho tháng

        //StockIn Commands
        public ICommand AddNewMedicineCommand { get; set; }// Lệnh thêm thuốc mới vào kho
        public ICommand RestartCommand { get; set; } // Lệnh khởi tạo lại form nhập kho
        public ICommand ShowMedicineDetailsCommand { get; set; } //Lệnh hiển thị chi tiết thuốc
        public ICommand LoadedUCCommand { get; set; }// Lệnh thực thi khi UserControl được tải

        #endregion
        #endregion

        public StockMedicineViewModel()
        {
            // Khởi tạo các thuộc tính mặc định
            MonthOptions = new ObservableCollection<int>(Enumerable.Range(1, 12));
            YearOptions = new ObservableCollection<int>(
                Enumerable.Range(DateTime.Now.Year - 5, 6)
            );

            SelectedMonth = DateTime.Now.Month-1;
            SelectedYear = DateTime.Now.Year;

            // Đặt chế độ xem mặc định là tồn kho hiện tại
            IsMonthlyView = false;

            // Khởi tạo các lệnh và tải dữ liệu
            LoadData();
            InitializeCommands();

        
        }


        #region Methods

        #region LoadData
        public void LoadData()
        {
            try
            {
                // Load medicines with proper eager loading
              ListMedicine = new ObservableCollection<Stock>(
                DataProvider.Instance.Context.Stocks
                .Where(d => (bool)!d.Medicine.IsDeleted)
                .Include(d => d.Medicine)
                    .ThenInclude(m => m.InvoiceDetails)
                .Include(d => d.Medicine.Category)
                .Include(d => d.Medicine.Unit)
                .Include(d => d.Medicine.StockIns)
                    .ThenInclude(si => si.Supplier) // Đảm bảo load Supplier
                .ToList()
            );



                UpdateCurrentTotals();

                // Load other data
                UnitList = new ObservableCollection<Unit>(
                    DataProvider.Instance.Context.Units
                    .Where(u => (bool)!u.IsDeleted)
                    .ToList()
                );

                SupplierList = new ObservableCollection<Supplier>(
                    DataProvider.Instance.Context.Suppliers
                    .Where(u => (bool)!u.IsDeleted)
                    .ToList()
                );
                SupplierListStockIn = new ObservableCollection<Supplier>(
                  DataProvider.Instance.Context.Suppliers
                  .Where(u => (bool)!u.IsDeleted && (bool)u.IsActive)
                  .ToList()
              );
                CategoryList = new ObservableCollection<MedicineCategory>(
                    DataProvider.Instance.Context.MedicineCategories
                    .Where(u => (bool)!u.IsDeleted)
                    .ToList()
                );

                // Initialize stock medicine list and cache
                _allStockMedicine = new ObservableCollection<Stock>(ListMedicine);
                ListStockMedicine = new ObservableCollection<Stock>(ListMedicine);
                UpdateStockStatus();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi"    );
            }
        }
        // Method dùng để phần quyền dựa trên Account 
        private void UpdatePermissions()
        {
            if (CurrentAccount == null)
            {
                CanEdit = false;
                CanAddNewMedicine = false;
                CanManageSettings = false;
                return;
            }

            string role = CurrentAccount.Role?.Trim() ?? "";

            // Admin và Quản lí có thể có tất cả quyền
            if (role == UserRoles.Admin || role == UserRoles.Manager)
            {
                CanEdit = true;
                CanAddNewMedicine = true;
                CanManageSettings = true;
            }
            // Dược sĩ có thể chỉnh sửa và thêm thuốc
            else if (role == UserRoles.Pharmacist)
            {
                CanEdit = true;
                CanAddNewMedicine = true;
                CanManageSettings = false;
            }
            // Thu ngân chỉ có thể xem
            else if (role == UserRoles.Cashier)
            {
                CanEdit = false;
                CanAddNewMedicine = false;
                CanManageSettings = false;
            }
            // Doctor can only view
            else if (role == UserRoles.Doctor)
            {
                CanEdit = false;
                CanAddNewMedicine = false;
                CanManageSettings = false;
            }
           

            // Refresh command can-execute state
            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateStockStatus()
        {
            var stockViewModels = new ObservableCollection<StockViewModel>();

            foreach (var stock in ListStockMedicine)
            {
                var medicine = stock.Medicine;
                var statusViewModel = new StockViewModel { Medicine = medicine };

                // Xác định trạng thái
                if (medicine.HasExpiredStock)
                {
                    statusViewModel.Status = "Hết hạn";
                    statusViewModel.StatusColor = "#E53935"; // Red
                    statusViewModel.StatusMessage = "Có lô thuốc đã hết hạn nhưng vẫn còn hàng";
                }
                else if (medicine.HasNearExpiryStock)
                {
                    statusViewModel.Status = "Sắp hết hạn";
                    statusViewModel.StatusColor = "#FFC107"; // Amber
                    statusViewModel.StatusMessage = $"Có lô thuốc sẽ hết hạn trong {Medicine.MinimumDaysBeforeExpiry} ngày tới";
                }
                else if (medicine.IsLastestStockIn)
                {
                    statusViewModel.Status = "Lô cuối";
                    statusViewModel.StatusColor = "#2196F3"; // Blue
                    statusViewModel.StatusMessage = "Đang sử dụng lô cuối cùng, cần nhập thêm hàng";
                }
                else
                {
                    statusViewModel.Status = "Bình thường";
                    statusViewModel.StatusColor = "#4CAF50"; // Green
                    statusViewModel.StatusMessage = "Thuốc đang ở trạng thái bình thường";
                }

                stockViewModels.Add(statusViewModel);
            }

            StockViewModels = stockViewModels;
        }


        #endregion

        #region InitializeCommands
        /// <summary>
        /// Khởi tạo tất cả các lệnh (commands) trong ViewModel
        /// Thiết lập logic thực thi và điều kiện kích hoạt cho từng command
        /// </summary>
        private void InitializeCommands()
        {
            // Command xử lý khi UserControl được tải - lấy tài khoản hiện tại từ MainViewModel
            LoadedUCCommand = new RelayCommand<UserControl>(
            (userControl) => {
                if (userControl != null)
                {
                    // Lấy MainViewModel từ Application resources
                    var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                    if (mainVM != null && mainVM.CurrentAccount != null)
                    {
                        // Cập nhật tài khoản hiện tại
                        CurrentAccount = mainVM.CurrentAccount;
                        // Kiểm tra và nhắc nhở về tồn kho hàng tháng
                        CheckMonthlyStockReminder();
                    }

                    // Chạy kiểm tra ngày hết hạn khi tải view
                    //UpdateUsableQuantitiesBasedOnExpiryDates();
                }
            },
            (userControl) => true // Luôn có thể thực thi
        );

            // Command tạo thủ công báo cáo tồn kho hàng tháng
            ManualGenerateMonthlyStockCommand = new RelayCommand<object>(
               (p) => ExecuteManualGenerateMonthlyStock(),
               (p) => CanManualGenerateMonthlyStock() // Kiểm tra điều kiện có thể tạo báo cáo
                );

            // Command hoàn tác báo cáo tồn kho hàng tháng
            UndoMonthlyStockCommand = new RelayCommand<object>(
               (p) => ExecuteUndoMonthlyStock(),
               (p) => CanUndoMonthlyStock() // Kiểm tra điều kiện có thể hoàn tác
            );

            // === COMMANDS CHO QUẢN LÝ ĐƠN VỊ ===

            // Command thêm đơn vị mới - yêu cầu tên không rỗng và quyền quản lý
            AddUnitCommand = new RelayCommand<object>(
                (p) => AddUnit(),
                (p) => !string.IsNullOrEmpty(UnitName) && CanManageSettings
            );

            // Command chỉnh sửa đơn vị - yêu cầu có đơn vị được chọn, tên không rỗng và quyền quản lý
            EditUnitCommand = new RelayCommand<object>(
                (p) => EditUnit(),
                (p) => SelectedUnit != null && !string.IsNullOrEmpty(UnitName) && CanManageSettings
            );

            // Command xóa đơn vị - yêu cầu có đơn vị được chọn và quyền quản lý
            DeleteUnitCommand = new RelayCommand<object>(
                (p) => DeleteUnit(),
                (p) => SelectedUnit != null && CanManageSettings
            );

            // Command làm mới danh sách đơn vị
            RefreshUnitCommand = new RelayCommand<object>(
              (p) => ExecuteUnitRefresh(),
              (p) => true // Luôn có thể thực thi
            );

            // === COMMANDS CHO QUẢN LÝ DANH MỤC THUỐC ===

            // Command thêm danh mục thuốc mới - yêu cầu tên không rỗng và quyền quản lý
            AddCategoryCommand = new RelayCommand<object>(
                (p) => AddCategory(),
                (p) => !string.IsNullOrEmpty(CategoryName) && CanManageSettings
            );

            // Command chỉnh sửa danh mục thuốc - yêu cầu có danh mục được chọn, tên không rỗng và quyền quản lý
            EditCategoryCommand = new RelayCommand<object>(
                (p) => EditCategory(),
                (p) => SelectedCategory != null && !string.IsNullOrEmpty(CategoryName) && CanManageSettings
            );

            // Command xóa danh mục thuốc - yêu cầu có danh mục được chọn và quyền quản lý
            DeleteCategoryCommand = new RelayCommand<object>(
                (p) => DeleteCategory(),
                (p) => SelectedCategory != null && CanManageSettings
            );

            // Command làm mới danh sách danh mục thuốc
            RefreshCatergoryCommand = new RelayCommand<object>(
                  (p) => ExecuteCategoryRefresh(),
                  (p) => true // Luôn có thể thực thi
              );

            // === COMMANDS CHO QUẢN LÝ NHÀ CUNG CẤP ===

            // Command thêm nhà cung cấp mới - yêu cầu tên không rỗng và quyền quản lý
            AddSupplierCommand = new RelayCommand<object>(
                (p) => AddSupplier(),
                (p) => !string.IsNullOrEmpty(SupplierName) && CanManageSettings
            );

            // Command chỉnh sửa nhà cung cấp - yêu cầu có nhà cung cấp được chọn, tên không rỗng và quyền quản lý
            EditSupplierCommand = new RelayCommand<object>(
                (p) => EditSupplier(),
                (p) => SelectedSupplier != null && !string.IsNullOrEmpty(SupplierName) && CanManageSettings
            );

            // Command xóa nhà cung cấp - yêu cầu có nhà cung cấp được chọn và quyền quản lý
            DeleteSupplierCommand = new RelayCommand<object>(
                (p) => DeleteSupplier(),
                (p) => SelectedSupplier != null && CanManageSettings
            );

            // Command đặt trạng thái hoạt động cho nhà cung cấp
            SetActiveStatusCommand = new RelayCommand<object>(
                (p) => {
                    IsActive = true;
                    IsNotActive = false;
                },
                (p) => CanManageSettings
            );

            // Command đặt trạng thái không hoạt động cho nhà cung cấp
            SetNoActiveStatusCommand = new RelayCommand<object>(
                (p) => {
                    IsActive = false;
                    IsNotActive = true;
                },
                (p) => CanManageSettings
            );

            // Command làm mới danh sách nhà cung cấp
            RefreshSulpierCommand = new RelayCommand<object>(
                (p) => ExecuteSupplierRefresh(),
                (p) => true // Luôn có thể thực thi
            );

            // Khởi tạo commands cho filter nhà cung cấp
            SearchSupplierCommand = new RelayCommand<object>(
                p => FilterSuppliers(), // Lọc nhà cung cấp
                p => true
            );

            // Command làm mới filter nhà cung cấp (đặt lại bộ lọc)
            RefreshSulpierCommand = new RelayCommand<object>(
                p => ResetSupplierFilters(), // Đặt lại bộ lọc nhà cung cấp
                p => true
            );

            // Mặc định hiển thị tất cả nhà cung cấp
            ShowAllSuppliers = true;

            // === COMMANDS CHO QUẢN LÝ TỒN KHO THUỐC ===

            // Command tìm kiếm tồn kho thuốc theo chế độ xem hiện tại
            SearchStockMedicineCommand = new RelayCommand<object>(
                (p) => {
                    // Thực hiện tìm kiếm theo chế độ xem hiện tại
                    if (IsMonthlyView)
                        FilterMonthlyStock(); // Lọc tồn kho hàng tháng
                    else
                        FilterCurrentStock(); // Lọc tồn kho hiện tại
                },
                (p) => true
            );

            // Command đặt lại các bộ lọc tồn kho
            ResetStockFiltersCommand = new RelayCommand<object>(
                (p) => {
                    // Đặt lại các filter
                    SearchStockMedicine = "";
                    SelectedStockCategoryName = null;
                    SelectedStockSupplier = null;
                    SelectedStockUnit = null;

                    // Làm mới dữ liệu theo chế độ xem hiện tại
                    if (IsMonthlyView)
                        FilterMonthlyStock();
                    else
                        FilterCurrentStock();
                },
                (p) => true
            );

            // Command xuất báo cáo tồn kho ra Excel
            ExportStockExcelCommand = new RelayCommand<object>(
                p => ExportToExcel(),
                p => IsMonthlyView
                    ? MonthlyStockList?.Count > 0   // Có dữ liệu tồn kho hàng tháng
                    : ListStockMedicine?.Count > 0  // Có dữ liệu tồn kho hiện tại
            );

            // Command thêm thuốc mới vào hệ thống
            AddNewMedicineCommand = new RelayCommand<object>(
                (p) => ExecuteAddNewMedicine(),
                (p) => CanExecuteAddNewMedicine() && CanAddNewMedicine // Kiểm tra quyền thêm thuốc
            );

            // Command khởi động lại hệ thống
            RestartCommand = new RelayCommand<object>(
                (p) => ExecuteRestart(),
                (p) => true
            );

            // Command hiển thị chi tiết thuốc
            ShowMedicineDetailsCommand = new RelayCommand<Medicine>(
                (medicine) => {
                    if (medicine != null)
                    {
                        // Mở cửa sổ chi tiết thuốc
                        var detailsWindow = new MedicineDetailsWindow(medicine);
                        detailsWindow.ShowDialog();
                        LoadData(); // Tải lại dữ liệu sau khi đóng cửa sổ chi tiết   
                    }
                },
                (medicine) => medicine != null // Chỉ khi có thuốc được chọn
            );

            // Command xóa thuốc khỏi hệ thống
            DeleteMedicine = new RelayCommand<Medicine>(
                (medicine) => ExecuteDeleteMedicine(medicine),
                (medicine) => CanDeleteMedicine(medicine) && CanEdit // Kiểm tra quyền xóa thuốc
            );
        }

        #endregion

        #region Unit Methods
        /// <summary>
        /// Thực hiện làm mới danh sách đơn vị
        /// Xóa form và đặt lại trạng thái validation
        /// </summary>
        private void ExecuteUnitRefresh()
        {
            // Tải lại danh sách đơn vị từ cơ sở dữ liệu (chỉ những đơn vị chưa bị xóa)
            UnitList = new ObservableCollection<Unit>(
                DataProvider.Instance.Context.Units
                    .Where(s => (bool)!s.IsDeleted)
                    .ToList()
            );

            // Xóa các trường nhập liệu trong form
            SelectedUnit = null;
            UnitName = "";
            UnitDescription = "";

            // Xóa trạng thái validation
            _isUnitValidating = false;
            _unitFieldsTouched.Clear();
        }

        /// <summary>
        /// Thêm đơn vị mới vào hệ thống
        /// Bao gồm validation, kiểm tra trùng lặp và sử dụng transaction
        /// </summary>
        private void AddUnit()
        {
            try
            {
                // Bật validation cho các trường đơn vị
                _isUnitValidating = true;
                _unitFieldsTouched.Add(nameof(UnitName));
                _unitFieldsTouched.Add(nameof(UnitDescription));

                // Kích hoạt validation bằng cách thông báo thay đổi thuộc tính
                OnPropertyChanged(nameof(UnitName));
                OnPropertyChanged(nameof(UnitDescription));

                // Kiểm tra lỗi validation trong các trường đơn vị
                if (!string.IsNullOrEmpty(this[nameof(UnitName)]) ||
                    !string.IsNullOrEmpty(this[nameof(UnitDescription)]))
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm đơn vị.",
                        "Lỗi thông tin");
                    return;
                }

                // Hiển thị hộp thoại xác nhận trước khi thêm đơn vị
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn thêm đơn vị '{UnitName}' không?",
                    "Xác Nhận Thêm");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra đơn vị đã tồn tại chưa (so sánh không phân biệt hoa thường)
                        bool isExist = DataProvider.Instance.Context.Units
                            .Any(s => s.UnitName.Trim().ToLower() == UnitName.Trim().ToLower() && (bool)!s.IsDeleted);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Đơn vị này đã tồn tại.");
                            return;
                        }

                        // Tạo đối tượng đơn vị mới
                        var newUnit = new Unit
                        {
                            UnitName = UnitName,
                            Description = UnitDescription ?? "",
                            IsDeleted = false
                        };

                        // Thêm đơn vị vào cơ sở dữ liệu
                        DataProvider.Instance.Context.Units.Add(newUnit);
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn thành giao dịch nếu mọi thứ thành công
                        transaction.Commit();

                        // Làm mới danh sách đơn vị
                        UnitList = new ObservableCollection<Unit>(
                            DataProvider.Instance.Context.Units
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Xóa các trường nhập liệu
                        SelectedUnit = null;
                        UnitName = "";
                        UnitDescription = "";

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã thêm Đơn vị thành công!",
                            "Thành công");
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                // Xử lý lỗi cơ sở dữ liệu
                MessageBoxService.ShowError(
                    $"Không thể thêm Đơn vị: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu");
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Chỉnh sửa thông tin đơn vị đã có
        /// Kiểm tra trùng lặp (ngoại trừ chính nó) và sử dụng transaction
        /// </summary>
        private void EditUnit()
        {
            try
            {
                // Bật validation cho các trường đơn vị
                _isUnitValidating = true;
                _unitFieldsTouched.Add(nameof(UnitName));
                _unitFieldsTouched.Add(nameof(UnitDescription));

                // Kích hoạt validation bằng cách thông báo thay đổi thuộc tính
                OnPropertyChanged(nameof(UnitName));
                OnPropertyChanged(nameof(UnitDescription));

                // Kiểm tra lỗi validation trong các trường đơn vị
                if (!string.IsNullOrEmpty(this[nameof(UnitName)]) ||
                    !string.IsNullOrEmpty(this[nameof(UnitDescription)]))
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi sửa đơn vị.",
                        "Lỗi thông tin");
                    return;
                }

                // Hiển thị hộp thoại xác nhận trước khi sửa đơn vị
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa đơn vị '{SelectedUnit.UnitName}' thành '{UnitName}' không?",
                    "Xác Nhận Sửa");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra tên đơn vị đã tồn tại chưa (trừ chính đơn vị đang sửa)
                        bool isExist = DataProvider.Instance.Context.Units
                            .Any(s => s.UnitName.Trim().ToLower() == UnitName.Trim().ToLower() &&
                                    s.UnitId != SelectedUnit.UnitId &&
                                    (bool)!s.IsDeleted);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Tên đơn vị này đã tồn tại.");
                            return;
                        }

                        // Tìm đơn vị cần cập nhật trong cơ sở dữ liệu
                        var unitToUpdate = DataProvider.Instance.Context.Units
                            .FirstOrDefault(s => s.UnitId == SelectedUnit.UnitId);

                        if (unitToUpdate == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy đơn vị cần sửa.");
                            return;
                        }

                        // Cập nhật thông tin đơn vị
                        unitToUpdate.UnitName = UnitName;
                        unitToUpdate.Description = UnitDescription ?? "";
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn thành giao dịch nếu mọi thứ thành công
                        transaction.Commit();

                        // Làm mới danh sách đơn vị
                        UnitList = new ObservableCollection<Unit>(
                            DataProvider.Instance.Context.Units
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Cập nhật danh sách thuốc vì tên đơn vị có thể đã thay đổi
                        LoadData();

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã cập nhật đơn vị thành công!",
                            "Thành Công");

                        // Xóa trạng thái validation
                        _isUnitValidating = false;
                        _unitFieldsTouched.Clear();
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                // Xử lý lỗi cơ sở dữ liệu
                MessageBoxService.ShowError(
                    $"Không thể sửa đơn vị: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu");
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Xóa mềm đơn vị khỏi hệ thống
        /// Kiểm tra ràng buộc với thuốc trước khi xóa và sử dụng transaction
        /// </summary>
        private void DeleteUnit()
        {
            try
            {
                // Hiển thị hộp thoại xác nhận trước khi xóa đơn vị
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn xóa Đơn vị '{SelectedUnit.UnitName}' không?",
                    "Xác nhận xóa");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra xem đơn vị có đang được sử dụng bởi thuốc nào không
                        bool isInUse = DataProvider.Instance.Context.Medicines
                            .Any(m => m.UnitId == SelectedUnit.UnitId && (bool)!m.IsDeleted);

                        if (isInUse)
                        {
                            MessageBoxService.ShowWarning(
                                "Không thể xóa đơn vị này vì đang được sử dụng bởi một hoặc nhiều thuốc.",
                                "Cảnh báo");
                            return;
                        }

                        // Tìm đơn vị cần xóa trong cơ sở dữ liệu
                        var unitToDelete = DataProvider.Instance.Context.Units
                            .FirstOrDefault(s => s.UnitId == SelectedUnit.UnitId);

                        if (unitToDelete == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy Đơn vị cần xóa.");
                            return;
                        }

                        // Thực hiện xóa mềm bằng cách đánh dấu IsDeleted = true
                        unitToDelete.IsDeleted = true;
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn thành giao dịch nếu mọi thứ thành công
                        transaction.Commit();

                        // Làm mới danh sách đơn vị
                        UnitList = new ObservableCollection<Unit>(
                            DataProvider.Instance.Context.Units
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Xóa các trường nhập liệu
                        SelectedUnit = null;
                        UnitName = "";
                        UnitDescription = "";

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã xóa đơn vị thành công.",
                            "Thành công");
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để xử lý ở khối catch bên ngoài
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa đơn vị: {ex.Message}",
                    "Lỗi");
            }
        }

        #endregion

        #region Category Methods
        /// <summary>
        /// Thực hiện làm mới danh sách danh mục thuốc
        /// Xóa form và đặt lại trạng thái validation
        /// </summary>
        private void ExecuteCategoryRefresh()
        {
            // Tải lại danh sách danh mục thuốc từ cơ sở dữ liệu (chỉ những danh mục chưa bị xóa)
            CategoryList = new ObservableCollection<MedicineCategory>(
                DataProvider.Instance.Context.MedicineCategories
                    .Where(s => (bool)!s.IsDeleted)
                    .ToList()
            );

            // Xóa các trường nhập liệu trong form
            SelectedCategory = null;
            CategoryName = "";
            CategoryDescription = "";

            // Xóa trạng thái validation
            _isCategoryValidating = false;
            _categoryFieldsTouched.Clear();
        }

        /// <summary>
        /// Thêm danh mục thuốc mới vào hệ thống
        /// Bao gồm validation, kiểm tra trùng lặp và sử dụng transaction
        /// </summary>
        private void AddCategory()
        {
            try
            {
                // Bật validation cho các trường danh mục
                _isCategoryValidating = true;
                _categoryFieldsTouched.Add(nameof(CategoryName));
                _categoryFieldsTouched.Add(nameof(CategoryDescription));

                // Kích hoạt validation bằng cách thông báo thay đổi thuộc tính
                OnPropertyChanged(nameof(CategoryName));
                OnPropertyChanged(nameof(CategoryDescription));

                // Kiểm tra lỗi validation trong các trường danh mục
                if (!string.IsNullOrEmpty(this[nameof(CategoryName)]) ||
                    !string.IsNullOrEmpty(this[nameof(CategoryDescription)]))
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm loại thuốc.",
                        "Lỗi thông tin");
                    return;
                }

                // Hiển thị hộp thoại xác nhận trước khi thêm danh mục
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn thêm loại thuốc '{CategoryName}' không?",
                    "Xác Nhận Thêm");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra danh mục đã tồn tại chưa (so sánh không phân biệt hoa thường)
                        bool isExist = DataProvider.Instance.Context.MedicineCategories
                            .Any(s => s.CategoryName.Trim().ToLower() == CategoryName.Trim().ToLower() && (bool)!s.IsDeleted);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Loại thuốc này đã tồn tại.", "Trùng dữ liệu");
                            return;
                        }

                        // Tạo đối tượng danh mục mới
                        var newCategory = new MedicineCategory
                        {
                            CategoryName = CategoryName.Trim(),
                            Description = CategoryDescription?.Trim() ?? "",
                            IsDeleted = false
                        };

                        // Thêm danh mục vào cơ sở dữ liệu
                        DataProvider.Instance.Context.MedicineCategories.Add(newCategory);
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn thành giao dịch sau khi lưu thành công
                        transaction.Commit();

                        // Làm mới danh sách danh mục
                        CategoryList = new ObservableCollection<MedicineCategory>(
                            DataProvider.Instance.Context.MedicineCategories
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Xóa các trường nhập liệu
                        SelectedCategory = null;
                        CategoryName = "";
                        CategoryDescription = "";

                        // Xóa trạng thái validation
                        _isCategoryValidating = false;
                        _categoryFieldsTouched.Clear();

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã thêm loại thuốc thành công!",
                            "Thành công");
                    }
                    catch (DbUpdateException ex)
                    {
                        // Hoàn tác giao dịch trong trường hợp có lỗi cơ sở dữ liệu
                        transaction.Rollback();

                        MessageBoxService.ShowError(
                            $"Không thể thêm loại thuốc: {ex.InnerException?.Message ?? ex.Message}",
                            "Lỗi Cơ Sở Dữ Liệu");
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch cho các lỗi khác
                        transaction.Rollback();

                        MessageBoxService.ShowError(
                            $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                            "Lỗi");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Chỉnh sửa thông tin danh mục thuốc đã có
        /// Kiểm tra trùng lặp (ngoại trừ chính nó) và sử dụng transaction
        /// </summary>
        private void EditCategory()
        {
            try
            {
                // Bật validation cho các trường danh mục
                _isCategoryValidating = true;
                _categoryFieldsTouched.Add(nameof(CategoryName));
                _categoryFieldsTouched.Add(nameof(CategoryDescription));

                // Kích hoạt validation bằng cách thông báo thay đổi thuộc tính
                OnPropertyChanged(nameof(CategoryName));
                OnPropertyChanged(nameof(CategoryDescription));

                // Kiểm tra lỗi validation trong các trường danh mục
                if (!string.IsNullOrEmpty(this[nameof(CategoryName)]) ||
                    !string.IsNullOrEmpty(this[nameof(CategoryDescription)]))
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi sửa loại thuốc.",
                        "Lỗi thông tin");
                    return;
                }

                // Kiểm tra xem có danh mục nào được chọn hay không
                if (SelectedCategory == null)
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng chọn loại thuốc cần sửa.",
                        "Thiếu thông tin");
                    return;
                }

                // Hiển thị hộp thoại xác nhận trước khi sửa danh mục
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa loại thuốc '{SelectedCategory.CategoryName}' thành '{CategoryName}' không?",
                    "Xác Nhận Sửa");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra tên danh mục đã tồn tại chưa (trừ chính danh mục đang sửa)
                        bool isExist = DataProvider.Instance.Context.MedicineCategories
                            .Any(s => s.CategoryName.Trim().ToLower() == CategoryName.Trim().ToLower() &&
                                    s.CategoryId != SelectedCategory.CategoryId &&
                                    (bool)!s.IsDeleted);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Tên loại thuốc này đã tồn tại.", "Trùng dữ liệu");
                            return;
                        }

                        // Tìm danh mục cần cập nhật trong cơ sở dữ liệu
                        var categoryToUpdate = DataProvider.Instance.Context.MedicineCategories
                            .FirstOrDefault(s => s.CategoryId == SelectedCategory.CategoryId);

                        if (categoryToUpdate == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy loại thuốc cần sửa.", "Dữ liệu không tồn tại");
                            return;
                        }

                        // Cập nhật thông tin danh mục
                        categoryToUpdate.CategoryName = CategoryName.Trim();
                        categoryToUpdate.Description = CategoryDescription?.Trim() ?? "";
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn thành giao dịch sau khi lưu thành công
                        transaction.Commit();

                        // Làm mới danh sách danh mục
                        CategoryList = new ObservableCollection<MedicineCategory>(
                            DataProvider.Instance.Context.MedicineCategories
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Cập nhật danh sách thuốc vì tên danh mục có thể đã thay đổi
                        LoadData();

                        // Xóa trạng thái validation
                        _isCategoryValidating = false;
                        _categoryFieldsTouched.Clear();

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã cập nhật loại thuốc thành công!",
                            "Thành công");
                    }
                    catch (DbUpdateException ex)
                    {
                        // Hoàn tác giao dịch trong trường hợp có lỗi cơ sở dữ liệu
                        transaction.Rollback();

                        MessageBoxService.ShowError(
                            $"Không thể sửa loại thuốc: {ex.InnerException?.Message ?? ex.Message}",
                            "Lỗi cơ sở dữ liệu");
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch cho các lỗi khác
                        transaction.Rollback();

                        MessageBoxService.ShowError(
                            $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                            "Lỗi");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Xóa mềm danh mục thuốc khỏi hệ thống
        /// Kiểm tra ràng buộc với thuốc trước khi xóa và sử dụng transaction
        /// </summary>
        private void DeleteCategory()
        {
            try
            {
                // Kiểm tra xem có danh mục nào được chọn hay không
                if (SelectedCategory == null)
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng chọn loại thuốc cần xóa.",
                        "Thiếu thông tin");
                    return;
                }

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra xem danh mục có đang được sử dụng bởi thuốc nào không
                        bool isInUse = DataProvider.Instance.Context.Medicines
                            .Any(m => m.CategoryId == SelectedCategory.CategoryId && (bool)!m.IsDeleted);

                        if (isInUse)
                        {
                            MessageBoxService.ShowWarning(
                                "Không thể xóa loại thuốc này vì đang được sử dụng bởi một hoặc nhiều thuốc.",
                                "Ràng buộc dữ liệu");
                            return;
                        }

                        // Hiển thị hộp thoại xác nhận xóa
                        bool result = MessageBoxService.ShowQuestion(
                            $"Bạn có chắc muốn xóa loại thuốc '{SelectedCategory.CategoryName}' không?",
                            "Xác nhận xóa");

                        if (!result)
                            return;

                        // Tìm danh mục cần xóa trong cơ sở dữ liệu
                        var categoryToUpdate = DataProvider.Instance.Context.MedicineCategories
                            .FirstOrDefault(s => s.CategoryId == SelectedCategory.CategoryId);

                        if (categoryToUpdate == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy loại thuốc cần xóa.", "Dữ liệu không tồn tại");
                            return;
                        }

                        // Thực hiện xóa mềm bằng cách đánh dấu IsDeleted = true
                        categoryToUpdate.IsDeleted = true;
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn thành giao dịch sau khi lưu thành công
                        transaction.Commit();

                        // Làm mới danh sách danh mục
                        CategoryList = new ObservableCollection<MedicineCategory>(
                            DataProvider.Instance.Context.MedicineCategories
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Xóa các trường nhập liệu
                        SelectedCategory = null;
                        CategoryName = "";
                        CategoryDescription = "";

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã xóa loại thuốc thành công.",
                            "Thành công");
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch trong trường hợp có lỗi
                        transaction.Rollback();

                        MessageBoxService.ShowError(
                            $"Đã xảy ra lỗi khi xóa loại thuốc: {ex.Message}",
                            "Lỗi"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi: {ex.Message}",
                    "Lỗi");
            }
        }

        #endregion

        #region Supplier Methods
        /// <summary>
        /// Thuộc tính kiểm tra xem form có lỗi nào không
        /// Tổng hợp tất cả các validation errors từ các nhóm fields khác nhau
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(SupplierCode)]) ||
                       !string.IsNullOrEmpty(this[nameof(SupplierName)]) ||
                       !string.IsNullOrEmpty(this[nameof(SupplierPhone)]) ||
                       !string.IsNullOrEmpty(this[nameof(SupplierEmail)]) ||
                       !string.IsNullOrEmpty(this[nameof(SupplierTaxCode)]) ||
                       // Thêm kiểm tra validation cho các trường đơn vị
                       !string.IsNullOrEmpty(this[nameof(UnitName)]) ||
                       !string.IsNullOrEmpty(this[nameof(UnitDescription)]) ||
                       // Thêm kiểm tra validation cho các trường danh mục
                       !string.IsNullOrEmpty(this[nameof(CategoryName)]) ||
                       !string.IsNullOrEmpty(this[nameof(CategoryDescription)]) ||
                       // Bao gồm kiểm tra validation cho các trường thuốc nếu tồn tại
                       (_medicineFieldsTouched != null && (_medicineFieldsTouched.Any() &&
                         (!string.IsNullOrEmpty(this[nameof(StockinMedicineName)]) ||
                          !string.IsNullOrEmpty(this[nameof(StockinBarCode)]) ||
                          !string.IsNullOrEmpty(this[nameof(StockinQrCode)]) ||
                          !string.IsNullOrEmpty(this[nameof(StockinQuantity)]) ||
                          !string.IsNullOrEmpty(this[nameof(StockinUnitPrice)]) ||
                          !string.IsNullOrEmpty(this[nameof(StockinSellPrice)]) ||
                          !string.IsNullOrEmpty(this[nameof(StockinExpiryDate)]))));
            }
        }

        /// <summary>
        /// Indexer triển khai IDataErrorInfo để validation từng field riêng biệt
        /// Trả về thông báo lỗi cho field cụ thể hoặc null nếu không có lỗi
        /// </summary>
        public string this[string columnName]
        {
            get
            {
                // Không validate cho đến khi người dùng tương tác với form
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(SupplierCode):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(SupplierCode))
                        {
                            error = "Mã nhà cung cấp không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(SupplierCode) &&
                                 !Regex.IsMatch(SupplierCode.Trim(), @"^NCC\d{3,}$"))
                        {
                            error = "Mã phải có định dạng NCC + số (VD: NCC001)";
                        }
                        break;

                    case nameof(SupplierName):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(SupplierName))
                        {
                            error = "Tên nhà cung cấp không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(SupplierName) &&
                                 SupplierName.Trim().Length < 2)
                        {
                            error = "Tên phải có ít nhất 2 ký tự";
                        }
                        break;

                    case nameof(SupplierPhone):
                        // Bắt buộc nhập số điện thoại khi thêm nhà cung cấp
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(SupplierPhone))
                        {
                            error = "Số điện thoại không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(SupplierPhone) &&
                            !Regex.IsMatch(SupplierPhone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            error = "Số điện thoại không đúng định dạng (VD: 0901234567)";
                        }
                        break;

                    case nameof(SupplierEmail):
                        if (!string.IsNullOrWhiteSpace(SupplierEmail) &&
                            !Regex.IsMatch(SupplierEmail.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            error = "Email không đúng định dạng";
                        }
                        break;

                    case nameof(SupplierTaxCode):
                        if (!string.IsNullOrWhiteSpace(SupplierTaxCode) &&
                            !Regex.IsMatch(SupplierTaxCode.Trim(), @"^\d{10}(-\d{3})?$"))
                        {
                            error = "Mã số thuế phải có 10 số hoặc 10-3 số (VD: 0123456789-001)";
                        }
                        break;

                    case nameof(StockinMedicineName):
                        if (_medicineFieldsTouched.Contains(columnName) && string.IsNullOrWhiteSpace(StockinMedicineName))
                        {
                            error = "Tên thuốc không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(StockinMedicineName) && StockinMedicineName.Trim().Length < 2)
                        {
                            error = "Tên thuốc phải có ít nhất 2 ký tự";
                        }
                        break;

                    case nameof(StockinBarCode):
                        if (!string.IsNullOrWhiteSpace(StockinBarCode) && StockinBarCode.Length < 8)
                        {
                            error = "Mã vạch phải có ít nhất 8 ký tự";
                        }
                        break;

                    case nameof(StockinQrCode):
                        // Thêm quy tắc validation cụ thể cho mã QR nếu cần
                        break;

                    case nameof(StockinQuantity):
                        if (_medicineFieldsTouched.Contains(columnName) && StockinQuantity <= 0)
                        {
                            error = "Số lượng phải lớn hơn 0";
                        }
                        break;

                    case nameof(StockinUnitPrice):
                        if (_medicineFieldsTouched.Contains(columnName) && StockinUnitPrice <= 0)
                        {
                            error = "Giá nhập phải lớn hơn 0";
                        }
                        break;

                    case nameof(StockinSellPrice):
                        if (_medicineFieldsTouched.Contains(columnName) && StockinSellPrice <= 0)
                        {
                            error = "Giá bán phải lớn hơn 0";
                        }
                        else if (StockinSellPrice < StockinUnitPrice)
                        {
                            error = "Giá bán không được thấp hơn giá nhập";
                        }
                        break;

                    case nameof(StockinExpiryDate):
                        if (!StockinExpiryDate.HasValue)
                        {
                            error = "Ngày hết hạn không được để trống";
                        }
                        else
                        {
                            var today = DateTime.Today;
                            if (StockinExpiryDate.Value.Date <= today)
                            {
                                error = "Ngày hết hạn phải sau ngày hôm nay";
                            }
                        }
                        break;

                    case nameof(UnitName):
                        if (_unitFieldsTouched.Contains(columnName) && string.IsNullOrWhiteSpace(UnitName))
                        {
                            error = "Tên đơn vị không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(UnitName))
                        {
                            if (UnitName.Trim().Length < 2)
                                error = "Tên đơn vị phải có ít nhất 2 ký tự";
                            else if (UnitName.Trim().Length > 50)
                                error = "Tên đơn vị không được vượt quá 50 ký tự";
                        }
                        break;

                    case nameof(UnitDescription):
                        if (!string.IsNullOrWhiteSpace(UnitDescription) && UnitDescription.Trim().Length > 255)
                        {
                            error = "Mô tả đơn vị không được vượt quá 255 ký tự";
                        }
                        break;

                    case nameof(CategoryName):
                        if (_categoryFieldsTouched.Contains(columnName) && string.IsNullOrWhiteSpace(CategoryName))
                        {
                            error = "Tên loại thuốc không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(CategoryName))
                        {
                            if (CategoryName.Trim().Length < 2)
                                error = "Tên loại thuốc phải có ít nhất 2 ký tự";
                            else if (CategoryName.Trim().Length > 50)
                                error = "Tên loại thuốc không được vượt quá 50 ký tự";
                        }
                        break;

                    case nameof(CategoryDescription):
                        if (!string.IsNullOrWhiteSpace(CategoryDescription) && CategoryDescription.Trim().Length > 255)
                        {
                            error = "Mô tả loại thuốc không được vượt quá 255 ký tự";
                        }
                        break;
                }

                return error;
            }
        }

        /// <summary>
        /// Thêm nhà cung cấp mới vào hệ thống
        /// Bao gồm validation đầy đủ, kiểm tra trùng lặp và sử dụng transaction
        /// </summary>
        private void AddSupplier()
        {
            try
            {
                // Bật chế độ validation cho tất cả các trường khi thử submit
                _isValidating = true;
                _touchedFields.Add(nameof(SupplierCode));
                _touchedFields.Add(nameof(SupplierName));
                _touchedFields.Add(nameof(SupplierPhone)); // Đảm bảo luôn validate số điện thoại

                // Kích hoạt kiểm tra validation cho các trường bắt buộc
                OnPropertyChanged(nameof(SupplierCode));
                OnPropertyChanged(nameof(SupplierName));
                OnPropertyChanged(nameof(SupplierPhone));

                // Kiểm tra lỗi validation
                if (HasErrors)
                {
                    MessageBoxService.ShowError(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm nhà cung cấp.",
                        "Lỗi thông tin");
                    return;
                }

                // Kiểm tra các trường bắt buộc
                if (string.IsNullOrWhiteSpace(SupplierCode) ||
                    string.IsNullOrWhiteSpace(SupplierName) ||
                    string.IsNullOrWhiteSpace(SupplierPhone))
                {
                    MessageBoxService.ShowWarning(
                        "Mã nhà cung cấp, Tên nhà cung cấp và Số điện thoại là bắt buộc.",
                        "Thiếu Thông Tin");
                    return;
                }

                // Hiển thị hộp thoại xác nhận trước khi thêm
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn thêm nhà cung cấp '{SupplierName}' không?",
                    "Xác Nhận Thêm");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra nhà cung cấp đã tồn tại chưa (theo mã hoặc tên)
                        bool isExist = DataProvider.Instance.Context.Suppliers
                            .Any(s => (s.SupplierCode.Trim().ToLower() == SupplierCode.Trim().ToLower() ||
                                      s.SupplierName.Trim().ToLower() == SupplierName.Trim().ToLower()) &&
                                      (bool)!s.IsDeleted);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Nhà cung cấp này đã tồn tại (trùng mã hoặc tên).");
                            return;
                        }

                        // Kiểm tra số điện thoại đã tồn tại chưa
                        bool phoneExists = DataProvider.Instance.Context.Suppliers
                            .Any(s => s.Phone == SupplierPhone.Trim() && (bool)!s.IsDeleted);

                        if (phoneExists)
                        {
                            MessageBoxService.ShowWarning(
                                "Số điện thoại này đã được sử dụng bởi nhà cung cấp khác.",
                                "Trùng số điện thoại");
                            return;
                        }

                        // Tạo đối tượng nhà cung cấp mới
                        var newSupplier = new Supplier
                        {
                            SupplierCode = SupplierCode.Trim(),
                            SupplierName = SupplierName.Trim(),
                            Email = SupplierEmail?.Trim() ?? "",
                            Phone = SupplierPhone?.Trim() ?? "",
                            TaxCode = SupplierTaxCode?.Trim() ?? "",
                            ContactPerson = ContactPerson?.Trim() ?? "",
                            Address = SupplierAddress?.Trim() ?? "",
                            IsActive = IsActive,
                            IsDeleted = false
                        };

                        // Thêm nhà cung cấp vào cơ sở dữ liệu
                        DataProvider.Instance.Context.Suppliers.Add(newSupplier);
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn thành giao dịch nếu mọi thứ thành công
                        transaction.Commit();

                        // Xóa các trường và đặt lại trạng thái validation
                        ClearForm();

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã thêm nhà cung cấp thành công!",
                            "Thành công");
                        LoadSuppliers();
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để xử lý ở catch bên ngoài
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể thêm nhà cung cấp: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi cơ sở dữ liệu");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Chỉnh sửa thông tin nhà cung cấp đã có
        /// Kiểm tra trùng lặp (ngoại trừ chính nó) và sử dụng transaction
        /// </summary>
        private void EditSupplier()
        {
            try
            {
                // Bật chế độ validation cho tất cả các trường
                _isValidating = true;
                _touchedFields.Add(nameof(SupplierCode));
                _touchedFields.Add(nameof(SupplierName));
                _touchedFields.Add(nameof(SupplierPhone));

                // Kích hoạt kiểm tra validation cho các trường bắt buộc
                OnPropertyChanged(nameof(SupplierCode));
                OnPropertyChanged(nameof(SupplierName));
                OnPropertyChanged(nameof(SupplierPhone));

                // Kiểm tra số điện thoại đã tồn tại chưa (chỉ so sánh với các supplier khác)
                if (!string.IsNullOrWhiteSpace(SupplierPhone))
                {
                    bool phoneExists = DataProvider.Instance.Context.Suppliers
                        .Any(s => s.SupplierId != SelectedSupplier.SupplierId &&
                                  s.Phone == SupplierPhone.Trim() &&
                                  (bool)!s.IsDeleted);

                    if (phoneExists)
                    {
                        MessageBoxService.ShowWarning(
                            "Số điện thoại này đã được sử dụng bởi nhà cung cấp khác.",
                            "Trùng số điện thoại");
                        return;
                    }
                }

                // Kiểm tra lỗi validation
                if (HasErrors)
                {
                    MessageBoxService.ShowError(
                        "Vui lòng sửa các lỗi nhập liệu trước khi cập nhật nhà cung cấp.",
                        "Lỗi thông tin");
                    return;
                }

                // Hiển thị hộp thoại xác nhận trước khi sửa
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa nhà cung cấp '{SelectedSupplier.SupplierName}' không?",
                    "Xác Nhận Sửa");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra mã hoặc tên nhà cung cấp đã tồn tại chưa (trừ chính nó)
                        bool isExist = DataProvider.Instance.Context.Suppliers
                            .Any(s => (s.SupplierCode.Trim().ToLower() == SupplierCode.Trim().ToLower() ||
                                      s.SupplierName.Trim().ToLower() == SupplierName.Trim().ToLower()) &&
                                      s.SupplierId != SelectedSupplier.SupplierId &&
                                      (bool)!s.IsDeleted);

                        if (isExist)
                        {
                            MessageBoxService.ShowWarning("Mã hoặc tên nhà cung cấp này đã tồn tại.");
                            return;
                        }

                        // Tìm nhà cung cấp cần cập nhật trong cơ sở dữ liệu
                        var supplierToUpdate = DataProvider.Instance.Context.Suppliers
                            .FirstOrDefault(s => s.SupplierId == SelectedSupplier.SupplierId);

                        if (supplierToUpdate == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy nhà cung cấp cần sửa.");
                            return;
                        }

                        // Cập nhật thông tin nhà cung cấp
                        supplierToUpdate.SupplierCode = SupplierCode;
                        supplierToUpdate.SupplierName = SupplierName;
                        supplierToUpdate.Email = SupplierEmail ?? "";
                        supplierToUpdate.Phone = SupplierPhone ?? "";
                        supplierToUpdate.TaxCode = SupplierTaxCode ?? "";
                        supplierToUpdate.ContactPerson = ContactPerson ?? "";
                        supplierToUpdate.Address = SupplierAddress ?? "";
                        supplierToUpdate.IsActive = IsActive;

                        // Lưu thay đổi vào cơ sở dữ liệu
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn thành giao dịch nếu mọi thứ thành công
                        transaction.Commit();

                        // Làm mới danh sách nhà cung cấp
                        SupplierList = new ObservableCollection<Supplier>(
                            DataProvider.Instance.Context.Suppliers
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );
                        SupplierListStockIn = new ObservableCollection<Supplier>(
                           DataProvider.Instance.Context.Suppliers
                           .Where(u => (bool)!u.IsDeleted && (bool)u.IsActive)
                           .ToList()
                       );
                        ClearForm();
                     

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã cập nhật nhà cung cấp thành công!",
                            "Thành công");
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để xử lý ở catch bên ngoài
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể sửa nhà cung cấp: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi cơ sở dữ liệu");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Xóa mềm nhà cung cấp khỏi hệ thống
        /// Kiểm tra ràng buộc với các lô nhập trước khi xóa và sử dụng transaction
        /// </summary>
        private void DeleteSupplier()
        {
            try
            {
                // Kiểm tra xem nhà cung cấp có đang được sử dụng trong các lô nhập không
                bool isInUse = DataProvider.Instance.Context.StockIns
                    .Any(s => s.SupplierId == SelectedSupplier.SupplierId);

                if (isInUse)
                {
                    MessageBoxService.ShowWarning(
                        "Không thể xóa nhà cung cấp này vì đang được sử dụng trong các lô thuốc đã nhập.",
                        "Ràng buộc dữ liệu");
                    return;
                }

                // Hiển thị hộp thoại xác nhận xóa
                bool result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn xóa nhà cung cấp '{SelectedSupplier.SupplierName}' không?",
                    "Xác nhận xóa");

                if (!result)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Tìm nhà cung cấp cần xóa trong cơ sở dữ liệu
                        var supplierToDelete = DataProvider.Instance.Context.Suppliers
                            .FirstOrDefault(s => s.SupplierId == SelectedSupplier.SupplierId);

                        if (supplierToDelete == null)
                        {
                            MessageBoxService.ShowWarning("Không tìm thấy nhà cung cấp cần xóa.");
                            return;
                        }

                        // Thực hiện xóa mềm bằng cách đánh dấu IsDeleted = true
                        supplierToDelete.IsDeleted = true;
                        DataProvider.Instance.Context.SaveChanges();

                        // Hoàn thành giao dịch nếu mọi thứ thành công
                        transaction.Commit();

                        // Làm mới danh sách nhà cung cấp
                        SupplierList = new ObservableCollection<Supplier>(
                            DataProvider.Instance.Context.Suppliers
                                .Where(s => (bool)!s.IsDeleted)
                                .ToList()
                        );

                        // Xóa các trường nhập liệu
                        ClearForm();

                        // Thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã xóa nhà cung cấp thành công.",
                            "Thành công");
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để xử lý ở catch bên ngoài
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa nhà cung cấp: {ex.Message}",
                    "Lỗi");
            }
        }

        /// <summary>
        /// Thực hiện lọc nhà cung cấp tự động
        /// Wrapper method để gọi FilterSuppliers()
        /// </summary>
        private void ExecuteAutoSupplierFilter()
        {
            FilterSuppliers();
        }

        /// <summary>
        /// Lọc danh sách nhà cung cấp theo các tiêu chí đã chọn
        /// Áp dụng filter theo trạng thái hoạt động và từ khóa tìm kiếm
        /// </summary>
        private void FilterSuppliers()
        {
            // Tải tất cả nhà cung cấp nếu chưa được tải
            if (_allSuppliers == null)
            {
                LoadAllSuppliers();
            }

            // Kiểm tra dữ liệu có sẵn không
            if (_allSuppliers == null || _allSuppliers.Count == 0)
            {
                SupplierList = new ObservableCollection<Supplier>();
                return;
            }

            IEnumerable<Supplier> filteredSuppliers = _allSuppliers;

            // Áp dụng bộ lọc theo trạng thái
            if (ShowActiveSuppliers)
            {
                // Chỉ hiển thị nhà cung cấp đang hoạt động
                filteredSuppliers = filteredSuppliers.Where(s => (bool)s.IsActive);
            }
            else if (ShowInactiveSuppliers)
            {
                // Chỉ hiển thị nhà cung cấp không hoạt động
                filteredSuppliers = filteredSuppliers.Where(s => (bool)!s.IsActive);
            }
            // Nếu ShowAllSuppliers = true, không lọc theo trạng thái hoạt động

            // Áp dụng bộ lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(SupplierSearchText))
            {
                string searchTerm = SupplierSearchText.ToLower().Trim();
                filteredSuppliers = filteredSuppliers.Where(s =>
                    (s.SupplierName != null && s.SupplierName.ToLower().Contains(searchTerm)) ||
                    (s.SupplierCode != null && s.SupplierCode.ToLower().Contains(searchTerm)) ||
                    (s.Phone != null && s.Phone.ToLower().Contains(searchTerm)) ||
                    (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(searchTerm))
                );
            }

            // Cập nhật danh sách nhà cung cấp với kết quả đã lọc
            SupplierList = new ObservableCollection<Supplier>(filteredSuppliers);
        }

        /// <summary>
        /// Phương thức lọc tồn kho hiện tại theo các điều kiện đã chọn
        /// Áp dụng filter theo danh mục, nhà cung cấp, đơn vị và từ khóa tìm kiếm
        /// </summary>
        public void FilterCurrentStock()
        {
            try
            {
                // Kiểm tra nếu dữ liệu cache null
                if (_allStockMedicine == null)
                {
                    ListStockMedicine = new ObservableCollection<Stock>();
                    UpdateCurrentTotals();
                    return;
                }

                var query = _allStockMedicine.AsEnumerable();

                // Áp dụng bộ lọc theo danh mục thuốc
                if (SelectedStockCategoryId.HasValue && SelectedStockCategoryId.Value > 0)
                {
                    query = query.Where(s => s.Medicine?.CategoryId == SelectedStockCategoryId.Value);
                }

                // Áp dụng bộ lọc theo nhà cung cấp
                if (SelectedStockSupplierId.HasValue && SelectedStockSupplierId.Value > 0)
                {
                    query = query.Where(s =>
                        s.Medicine != null &&
                        s.Medicine.StockIns != null &&
                        s.Medicine.StockIns.Any(si => si.SupplierId == SelectedStockSupplierId.Value));
                }

                // Áp dụng bộ lọc theo đơn vị tính
                if (SelectedStockUnitId.HasValue && SelectedStockUnitId.Value > 0)
                {
                    query = query.Where(s => s.Medicine?.UnitId == SelectedStockUnitId.Value);
                }

                // Tìm kiếm theo tên thuốc
                if (!string.IsNullOrWhiteSpace(SearchStockMedicine))
                {
                    var searchTerm = SearchStockMedicine.ToLower().Trim();
                    query = query.Where(s =>
                        s.Medicine?.Name != null &&
                        s.Medicine.Name.ToLower().Contains(searchTerm));
                }

                // Cập nhật danh sách và tính tổng
                ListStockMedicine = new ObservableCollection<Stock>(query.ToList());

                // Tính tổng số lượng và giá trị
                UpdateCurrentTotals();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lọc dữ liệu tồn kho hiện tại: {ex.Message}", "Lỗi");

                // Trong trường hợp lỗi, đảm bảo ListStockMedicine không null
                if (ListStockMedicine == null)
                    ListStockMedicine = new ObservableCollection<Stock>();
            }
        }

        /// <summary>
        /// Phương thức lọc tồn kho theo tháng với cải tiến hiệu suất
        /// Sử dụng context mới và AsNoTracking để tránh dữ liệu cũ
        /// </summary>
        public void FilterMonthlyStock()
        {
            try
            {
                // Định dạng chuỗi tháng/năm để lọc
                string monthYear = $"{SelectedYear:D4}-{SelectedMonth:D2}";

                // Sử dụng context mới để tránh dữ liệu cũ
                using (var context = new ClinicDbContext())
                {
                    // Query với tất cả includes cần thiết
                    var query = context.MonthlyStocks
                        .AsNoTracking() // Để hiệu suất tốt hơn
                        .Include(ms => ms.Medicine)
                        .ThenInclude(m => m.Category)
                        .Include(ms => ms.Medicine.Unit)
                        .Where(ms => ms.MonthYear == monthYear);

                    // Áp dụng bộ lọc theo danh mục nếu được chọn
                    if (SelectedStockCategoryId.HasValue && SelectedStockCategoryId.Value > 0)
                    {
                        query = query.Where(ms => ms.Medicine.CategoryId == SelectedStockCategoryId.Value);
                    }

                    // Áp dụng bộ lọc theo đơn vị nếu được chọn
                    if (SelectedStockUnitId.HasValue && SelectedStockUnitId.Value > 0)
                    {
                        query = query.Where(ms => ms.Medicine.UnitId == SelectedStockUnitId.Value);
                    }

                    // Áp dụng bộ lọc theo nhà cung cấp nếu cần
                    if (SelectedStockSupplierId.HasValue && SelectedStockSupplierId.Value > 0)
                    {
                        query = query.Where(ms =>
                            ms.Medicine.StockIns != null &&
                            ms.Medicine.StockIns.Any(si => si.SupplierId == SelectedStockSupplierId.Value));
                    }

                    // Áp dụng bộ lọc tìm kiếm theo tên
                    if (!string.IsNullOrWhiteSpace(SearchStockMedicine))
                    {
                        var searchTerm = SearchStockMedicine.ToLower().Trim();
                        query = query.Where(ms =>
                            ms.Medicine.Name.ToLower().Contains(searchTerm));
                    }

                    // Thực thi query và cập nhật danh sách
                    var monthlyStockItems = query.ToList();
                    MonthlyStockList = new ObservableCollection<MonthlyStock>(monthlyStockItems);

                    // Cập nhật tổng cộng
                    UpdateMonthlyTotals();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lọc dữ liệu tồn kho theo tháng: {ex.Message}", "Lỗi");

                // Trong trường hợp lỗi, đảm bảo MonthlyStockList không null
                if (MonthlyStockList == null)
                    MonthlyStockList = new ObservableCollection<MonthlyStock>();
            }
        }

        /// <summary>
        /// Cập nhật tổng số lượng và giá trị tồn kho hiện tại
        /// Tính toán từ danh sách ListStockMedicine
        /// </summary>
        private void UpdateCurrentTotals()
        {
            if (ListStockMedicine == null)
            {
                CurrentTotalQuantity = 0;
                CurrentTotalValue = 0;
            }
            else
            {
                // Tính tổng số lượng
                CurrentTotalQuantity = ListStockMedicine.Sum(x => x.Quantity);
                // Tính tổng giá trị (số lượng × giá hiện tại)
                CurrentTotalValue = ListStockMedicine.Sum(x => x.Quantity * (x.Medicine?.CurrentUnitPrice ?? 0));
            }

            // Cập nhật UI với giá trị tổng mới
            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(TotalValue));
        }

        /// <summary>
        /// Cập nhật tổng số lượng và giá trị tồn kho theo tháng
        /// Tính toán từ danh sách MonthlyStockList
        /// </summary>
        private void UpdateMonthlyTotals()
        {
            if (MonthlyStockList == null)
            {
                MonthlyTotalQuantity = 0;
                MonthlyTotalValue = 0;
            }
            else
            {
                // Tính tổng số lượng
                MonthlyTotalQuantity = MonthlyStockList.Sum(x => x.Quantity);
                // Tính tổng giá trị (số lượng × giá hiện tại)
                MonthlyTotalValue = MonthlyStockList.Sum(x => x.Quantity * (x.Medicine?.CurrentUnitPrice ?? 0));
            }

            // Cập nhật UI với giá trị tổng mới
            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(TotalValue));
        }

        /// <summary>
        /// Tải tất cả nhà cung cấp từ cơ sở dữ liệu vào cache
        /// Chỉ lấy những nhà cung cấp chưa bị xóa
        /// </summary>
        private void LoadAllSuppliers()
        {
            try
            {
                _allSuppliers = new ObservableCollection<Supplier>(
                    DataProvider.Instance.Context.Suppliers
                        .Where(s => s.IsDeleted != true)
                        .ToList()
                );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Lỗi khi tải danh sách nhà cung cấp: {ex.Message}",
                    "Lỗi"
                );
            }
        }

        /// <summary>
        /// Tải nhà cung cấp với hỗ trợ lọc
        /// Kết hợp LoadAllSuppliers() và FilterSuppliers()
        /// </summary>
        public void LoadSuppliers()
        {
            try
            {
                LoadAllSuppliers();
                FilterSuppliers();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Lỗi khi tải danh sách nhà cung cấp: {ex.Message}",
                    "Lỗi"
                );
            }
        }

        /// <summary>
        /// Đặt lại tất cả bộ lọc nhà cung cấp về trạng thái mặc định
        /// Xóa từ khóa tìm kiếm và chọn hiển thị tất cả nhà cung cấp
        /// </summary>
        private void ResetSupplierFilters()
        {
            // Xóa từ khóa tìm kiếm
            SupplierSearchText = string.Empty;

            // Tạm thời vô hiệu hóa auto filtering để tránh cập nhật dư thừa
            var tempShowAll = _showAllSuppliers;
            var tempShowActive = _showActiveSuppliers;
            var tempShowInactive = _showInactiveSuppliers;

            _showAllSuppliers = false;
            _showActiveSuppliers = false;
            _showInactiveSuppliers = false;

            // Đặt ShowAllSuppliers = true sẽ kích hoạt filtering
            ShowAllSuppliers = true;

            // Tải lại tất cả nhà cung cấp
            LoadSuppliers();
        }

        #endregion

        #region StockMedicine Methods

        /// <summary>
        /// Kiểm tra xem có thể thực hiện tổng kết tồn kho thủ công hay không
        /// Chỉ những người có quyền quản lý cài đặt mới có thể thực hiện
        /// </summary>
        /// <returns>True nếu có quyền quản lý cài đặt</returns>
        private bool CanManualGenerateMonthlyStock()
        {
            return CanManageSettings;
        }

        /// <summary>
        /// Kiểm tra xem có thể hoàn tác tổng kết tồn kho tháng hiện tại hay không
        /// Yêu cầu có quyền quản lý và đã có dữ liệu tổng kết cho tháng hiện tại
        /// </summary>
        /// <returns>True nếu có quyền và có dữ liệu tháng hiện tại</returns>
        private bool CanUndoMonthlyStock()
        {
            // Tạo context mới để kiểm tra tránh xung đột
            using (var context = new ClinicDbContext())
            {
                return CanManageSettings && HasCurrentMonthStock(context);
            }
        }

        /// <summary>
        /// Làm mới danh sách nhà cung cấp từ cơ sở dữ liệu
        /// Chỉ lấy những nhà cung cấp chưa bị xóa và xóa form hiện tại
        /// </summary>
        private void ExecuteSupplierRefresh()
        {
            // Tải lại danh sách nhà cung cấp từ database, loại trừ những nhà cung cấp đã bị xóa
            SupplierList = new ObservableCollection<Supplier>(
                   DataProvider.Instance.Context.Suppliers
                       .Where(s => (bool)!s.IsDeleted) // Chỉ lấy nhà cung cấp chưa bị xóa
                       .ToList()
               );

            // Xóa các trường nhập liệu trên form
            ClearForm();
        }

        /// <summary>
        /// Thực hiện xóa mềm thuốc khỏi hệ thống
        /// Kiểm tra các điều kiện và yêu cầu xác nhận từ người dùng
        /// </summary>
        /// <param name="medicine">Thuốc cần xóa</param>
        private void ExecuteDeleteMedicine(Medicine medicine)
        {
            try
            {
                var medicineId = medicine.MedicineId;

                // Hiển thị hộp thoại xác nhận xóa thuốc
                bool result = MessageBoxService.ShowQuestion(
                        "Xác nhận xóa thuốc '" + medicine.Name + "'?",
                        "Xác nhận"
                    );

                // Nếu người dùng không xác nhận, thoát khỏi phương thức
                if (!result)
                    return;

                // Kiểm tra xem thuốc có tồn tại trong cơ sở dữ liệu không
                int isNotExist = DataProvider.Instance.Context.Medicines
                    .Count(s => s.MedicineId == medicineId);

                if (isNotExist == 0)
                {
                    // Thuốc không tồn tại
                    MessageBoxService.ShowWarning("Thuốc này đã không tồn tại.");
                    return;
                }
                else
                {
                    // Tìm thuốc cần xóa trong database
                    var medicineToDelete = DataProvider.Instance.Context.Medicines
                        .FirstOrDefault(s => s.MedicineId == medicineId);

                    if (medicineToDelete == null)
                    {
                        MessageBoxService.ShowWarning("Không tìm thấy thuốc cần xóa.");
                        return;
                    }

                    // Thực hiện xóa mềm bằng cách đánh dấu IsDeleted = true
                    medicineToDelete.IsDeleted = true;
                    DataProvider.Instance.Context.SaveChanges();

                    // Làm mới danh sách thuốc sau khi xóa
                    ListMedicine = new ObservableCollection<Stock>(
                DataProvider.Instance.Context.Stocks
                .Where(d => (bool)!d.Medicine.IsDeleted) // Chỉ lấy thuốc chưa bị xóa
                .Include(d => d.Medicine)
                    .ThenInclude(m => m.InvoiceDetails) // Bao gồm chi tiết hóa đơn để tính số lượng đã bán
                .Include(d => d.Medicine.Category)      // Bao gồm thông tin loại thuốc
                .Include(d => d.Medicine.Unit)          // Bao gồm thông tin đơn vị tính
                .Include(d => d.Medicine.StockIns)      // Bao gồm thông tin lô nhập kho
                .ToList()
               );

                    // Hiển thị thông báo thành công
                    MessageBoxService.ShowSuccess(
                        "Đã xóa thuốc thành công.",
                        "Thành công"
                    );
                }
            }
            catch
            {
                // Xử lý lỗi (có thể thêm logging hoặc thông báo lỗi cụ thể)
            }
        }

        /// <summary>
        /// Kiểm tra xem thuốc có thể được xóa hay không
        /// Thuốc chỉ có thể xóa nếu không có lô nhập nào hoặc tất cả lô đã hết hạn/bị tiêu hủy
        /// </summary>
        /// <param name="medicine">Thuốc cần kiểm tra</param>
        /// <returns>True nếu có thể xóa thuốc</returns>
        private bool CanDeleteMedicine(Medicine medicine)
        {
            if (medicine == null)
                return false;

            // Kiểm tra xem thuốc có lô nhập nào không
            if (medicine.StockIns == null || !medicine.StockIns.Any())
                return true; // Có thể xóa nếu không có lô nhập nào

            // Lấy ngày hiện tại để so sánh ngày hết hạn
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Kiểm tra xem tất cả lô thuốc đã bị tiêu hủy hoặc hết hạn chưa
            bool allBatchesTerminatedOrExpired = medicine.StockIns.All(si =>
                si.IsTerminated || // Lô đã bị tiêu hủy
                (si.ExpiryDate.HasValue && si.ExpiryDate.Value <= today) // Hoặc đã hết hạn
            );

            return allBatchesTerminatedOrExpired;
        }

        /// <summary>
        /// Xuất dữ liệu tồn kho ra file Excel với giao diện tiến trình
        /// Hỗ trợ cả chế độ xem tháng và xem hiện tại, bảng dữ liệu bắt đầu từ cột B
        /// </summary>
        private void ExportToExcel()
        {
            try
            {
                // Tạo hộp thoại chọn vị trí lưu file Excel
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "Chọn vị trí lưu file Excel",
                    FileName = IsMonthlyView
                        ? $"TonKho_Thang_{SelectedMonth:D2}_{SelectedYear}.xlsx"  // Tên file cho chế độ xem tháng
                        : $"TonKho_HienTai_{DateTime.Now:yyyyMMdd}.xlsx"        // Tên file cho chế độ xem hiện tại
                };

                // Kiểm tra người dùng có chọn nơi lưu file không
                if (saveFileDialog.ShowDialog() == true)
                {
                    // Kiểm tra file có đang được sử dụng bởi chương trình khác không
                    if (File.Exists(saveFileDialog.FileName))
                    {
                        try
                        {
                            // Thử mở file để kiểm tra xem có bị khóa không
                            using (FileStream fs = File.Open(saveFileDialog.FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                            {
                                fs.Close(); // File không bị khóa
                            }
                        }
                        catch (IOException)
                        {
                            // File đang bị sử dụng bởi chương trình khác
                            MessageBoxService.ShowError(
                                "File này đang được mở bởi một chương trình khác. Vui lòng đóng file hoặc chọn tên file khác.",
                                "Lỗi"
                            );
                            return;
                        }
                    }

                    // Hiển thị dialog tiến trình xuất Excel
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Chạy quá trình xuất Excel trong thread riêng để không làm đứng giao diện
                    Task.Run(() =>
                    {
                        try
                        {
                            // Tạo workbook Excel bằng thư viện ClosedXML
                            using (var workbook = new XLWorkbook())
                            {
                                // Tạo worksheet có tên "Tồn kho"
                                var worksheet = workbook.Worksheets.Add("Tồn kho");

                                // Cập nhật tiến trình: 5% - Đã tạo workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // Thiết lập vị trí bắt đầu cho bảng (cột B = index 2)
                                int startColumn = 2;

                                // Xác định số cột dựa trên chế độ xem
                                int totalColumns = IsMonthlyView ? 6 : 8;

                                // Thêm tiêu đề chính (merged cells) bắt đầu từ cột B
                                worksheet.Cell(1, startColumn).Value = IsMonthlyView
                                    ? $"DANH SÁCH TỒN KHO THÁNG {SelectedMonth}/{SelectedYear}"
                                    : "DANH SÁCH TỒN KHO HIỆN TẠI";

                                // Định dạng tiêu đề: merge cells, in đậm, cỡ chữ lớn, căn giữa
                                var titleRange = worksheet.Range(1, startColumn, 1, startColumn + totalColumns - 1);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Thêm ngày xuất báo cáo
                                worksheet.Cell(2, startColumn).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, startColumn, 2, startColumn + totalColumns - 1);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Cập nhật tiến trình: 10% - Đã thêm tiêu đề
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Thêm tiêu đề các cột
                                int headerRow = 4;  // Bắt đầu từ dòng 4
                                int column = startColumn;

                                // Tiêu đề cột khác nhau tùy theo chế độ xem
                                if (IsMonthlyView)  // Chế độ xem theo tháng
                                {
                                    worksheet.Cell(headerRow, column++).Value = "Tên thuốc";
                                    worksheet.Cell(headerRow, column++).Value = "Loại";
                                    worksheet.Cell(headerRow, column++).Value = "Đơn vị tính";
                                    worksheet.Cell(headerRow, column++).Value = "Tháng/năm";
                                    worksheet.Cell(headerRow, column++).Value = "Tồn kho tổng";
                                    worksheet.Cell(headerRow, column++).Value = "Sử dụng được";
                                }
                                else  // Chế độ xem hiện tại
                                {
                                    worksheet.Cell(headerRow, column++).Value = "Tên thuốc";
                                    worksheet.Cell(headerRow, column++).Value = "Loại";
                                    worksheet.Cell(headerRow, column++).Value = "Đơn vị tính";
                                    worksheet.Cell(headerRow, column++).Value = "Ngày nhập mới nhất";
                                    worksheet.Cell(headerRow, column++).Value = "Tồn kho tổng";
                                    worksheet.Cell(headerRow, column++).Value = "Sử dụng được";
                                    worksheet.Cell(headerRow, column++).Value = "Chi tiết lô thuốc (giá tiền: số lượng sử dụng được)";
                                    worksheet.Cell(headerRow, column++).Value = "Giá trị còn lại";
                                }

                                // Định dạng hàng tiêu đề: in đậm, nền xám, căn giữa, có viền
                                var headerRange = worksheet.Range(headerRow, startColumn, headerRow, startColumn + totalColumns - 1);
                                headerRange.Style.Font.Bold = true;
                                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                                // Cập nhật tiến trình: 20% - Đã thêm tiêu đề cột
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(20));

                                // Bắt đầu thêm dữ liệu
                                int row = headerRow + 1;  // Dòng đầu tiên của dữ liệu
                                int totalItems = IsMonthlyView ? MonthlyStockList.Count : ListStockMedicine.Count;
                                var dataStartRow = row;

                                // Điền dữ liệu tùy theo chế độ xem
                                if (IsMonthlyView)  // Chế độ xem theo tháng
                                {
                                    for (int i = 0; i < totalItems; i++)
                                    {
                                        var item = MonthlyStockList[i];
                                        column = startColumn;  // Reset về cột B

                                        // Điền thông tin từ dữ liệu MonthlyStock
                                        worksheet.Cell(row, column++).Value = item.Medicine?.Name ?? "";
                                        worksheet.Cell(row, column++).Value = item.Medicine?.Category?.CategoryName ?? "";
                                        worksheet.Cell(row, column++).Value = item.Medicine?.Unit?.UnitName ?? "";
                                        worksheet.Cell(row, column++).Value = item.MonthYear;
                                        worksheet.Cell(row, column++).Value = item.Quantity;
                                        worksheet.Cell(row, column++).Value = item.CanUsed;

                                        row++;

                                        // Cập nhật tiến trình xuất dữ liệu
                                        int progressValue = 20 + (i * 60 / totalItems);
                                        Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(progressValue));
                                        Thread.Sleep(5);  // Tạm dừng để hiển thị tiến trình
                                    }
                                }
                                else  // Chế độ xem tồn kho hiện tại
                                {
                                    for (int i = 0; i < totalItems; i++)
                                    {
                                        var item = ListStockMedicine[i];
                                        var medicine = item.Medicine;
                                        column = startColumn;  // Reset về cột B

                                        // Điền thông tin từ dữ liệu Stock và Medicine
                                        worksheet.Cell(row, column++).Value = medicine?.Name ?? "";
                                        worksheet.Cell(row, column++).Value = medicine?.Category?.CategoryName ?? "";
                                        worksheet.Cell(row, column++).Value = medicine?.Unit?.UnitName ?? "";

                                        // Định dạng ngày nhập mới nhất
                                        if (medicine?.LatestImportDate.HasValue == true)
                                            worksheet.Cell(row, column++).Value = medicine.LatestImportDate.Value.ToString("dd/MM/yyyy");
                                        else
                                            worksheet.Cell(row, column++).Value = "";

                                        // Thông tin tồn kho
                                        worksheet.Cell(row, column++).Value = item.Quantity;
                                        worksheet.Cell(row, column++).Value = medicine?.TotalStockQuantity ?? 0;

                                        // Chi tiết các lô thuốc
                                        if (medicine != null)
                                        {
                                            // Lấy tất cả lô thuốc còn hàng, sắp xếp theo ngày nhập mới nhất
                                            var stockBatches = DataProvider.Instance.Context.StockIns
                                                .Where(si => si.MedicineId == medicine.MedicineId && si.RemainQuantity > 0)
                                                .OrderByDescending(si => si.ImportDate)
                                                .Select(si => new { Price = si.UnitPrice, Quantity = si.RemainQuantity })
                                                .ToList();

                                            // Tạo chuỗi thông tin chi tiết: "giá: số lượng"
                                            string batchDetails = string.Join(", ",
                                                stockBatches.Select(b => $"{b.Price:#,##0}: {b.Quantity}"));

                                            worksheet.Cell(row, column++).Value = batchDetails;
                                        }
                                        else
                                        {
                                            worksheet.Cell(row, column++).Value = "";
                                        }

                                        // Tính và định dạng giá trị tồn kho
                                        decimal totalValue = item.Quantity * (medicine?.CurrentUnitPrice ?? 0);
                                        worksheet.Cell(row, column++).Value = totalValue;
                                        worksheet.Cell(row, column - 1).Style.NumberFormat.Format = "#,##0";

                                        row++;

                                        // Cập nhật tiến trình xuất dữ liệu
                                        int progressValue = 20 + (i * 60 / totalItems);
                                        Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(progressValue));
                                        Thread.Sleep(5);  // Tạm dừng để hiển thị tiến trình
                                    }
                                }

                                // Cập nhật tiến trình: 80% - Đã thêm xong dữ liệu
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(80));

                                // Thêm viền cho vùng dữ liệu
                                if (totalItems > 0)
                                {
                                    var dataRange = worksheet.Range(dataStartRow, startColumn, row - 1, startColumn + totalColumns - 1);
                                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                                }

                                // Thêm dòng tổng cộng
                                row++;  // Tăng dòng để thêm dòng tổng
                                worksheet.Cell(row, startColumn).Value = "Tổng cộng";
                                worksheet.Cell(row, startColumn).Style.Font.Bold = true;

                                // Xác định vị trí cột tổng dựa vào chế độ xem
                                int totalQtyCol = startColumn + 4;  // Cột "Tồn kho tổng" 
                                int usableQtyCol = startColumn + 5; // Cột "Sử dụng được"

                                // Thêm tổng số lượng tồn kho
                                worksheet.Cell(row, totalQtyCol).Value = IsMonthlyView ? MonthlyTotalQuantity : CurrentTotalQuantity;
                                worksheet.Cell(row, totalQtyCol).Style.Font.Bold = true;

                                // Thêm tổng số lượng sử dụng được
                                int totalUsableQty = IsMonthlyView
                                    ? MonthlyStockList.Sum(ms => ms.CanUsed)
                                    : ListStockMedicine.Sum(s => s.Medicine?.TotalStockQuantity ?? 0);

                                worksheet.Cell(row, usableQtyCol).Value = totalUsableQty;
                                worksheet.Cell(row, usableQtyCol).Style.Font.Bold = true;

                                // Chỉ thêm giá trị tổng cho chế độ xem hiện tại
                                if (!IsMonthlyView)
                                {
                                    int valCol = startColumn + 7; // Cột "Giá trị còn lại" 

                                    // Thêm nhãn và giá trị tổng
                                    worksheet.Cell(row, valCol - 1).Value = "Tổng giá trị còn lại:";
                                    worksheet.Cell(row, valCol - 1).Style.Font.Bold = true;
                                    worksheet.Cell(row, valCol - 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                                    worksheet.Cell(row, valCol).Value = CurrentTotalValue;
                                    worksheet.Cell(row, valCol).Style.Font.Bold = true;
                                    worksheet.Cell(row, valCol).Style.NumberFormat.Format = "#,##0";
                                }

                                // Tự động điều chỉnh độ rộng cột
                                worksheet.Columns().AdjustToContents();

                                // Thiết lập độ rộng cụ thể cho một số cột
                                worksheet.Column(1).Width = 3; // Cột A để tạo khoảng trống

                                // Giới hạn độ rộng cột chi tiết lô thuốc (chỉ ở chế độ hiện tại)
                                if (!IsMonthlyView)
                                {
                                    int batchDetailsCol = startColumn + 6;
                                    if (worksheet.Column(batchDetailsCol).Width > 80)
                                        worksheet.Column(batchDetailsCol).Width = 80;
                                }

                                // Cập nhật tiến trình: 90% - Hoàn thành định dạng
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                try
                                {
                                    // Lưu file Excel
                                    workbook.SaveAs(saveFileDialog.FileName);

                                    // Cập nhật tiến trình: 100% - Đã lưu file
                                    Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                    Thread.Sleep(200);  // Tạm dừng để hiển thị hoàn thành

                                    // Đóng dialog tiến trình và hiển thị thông báo thành công
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        progressDialog.Close();
                                        MessageBoxService.ShowSuccess(
                                            $"Đã xuất danh sách tồn kho thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                            "Thông báo"
                                        );

                                        // Hỏi người dùng có muốn mở file Excel không
                                        if (MessageBoxService.ShowQuestion("Bạn có muốn mở file Excel không?", "Mở file"))
                                        {
                                            try
                                            {
                                                // Mở file Excel bằng ứng dụng mặc định
                                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                                {
                                                    FileName = saveFileDialog.FileName,
                                                    UseShellExecute = true
                                                });
                                            }
                                            catch (Exception ex)
                                            {
                                                MessageBoxService.ShowError($"Không thể mở file: {ex.Message}", "Lỗi");
                                            }
                                        }
                                    });
                                }
                                catch (IOException ex)
                                {
                                    // Xử lý lỗi khi không thể lưu file
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        progressDialog.Close();
                                        MessageBoxService.ShowError(
                                            $"Không thể lưu file Excel. File đang được sử dụng bởi chương trình khác: {ex.Message}",
                                            "Lỗi truy cập file"
                                        );
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Xử lý lỗi trong quá trình xuất Excel
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError(
                                    $"Lỗi khi xuất Excel: {ex.Message}",
                                    "Lỗi"
                                );
                            });
                        }
                    });

                    // Hiển thị dialog tiến trình - sẽ block cho đến khi đóng
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung của phương thức
                MessageBoxService.ShowError(
                    $"Lỗi khi xuất Excel: {ex.Message}",
                    "Lỗi"
                );
            }
        }

        /// <summary>
        /// Kiểm tra xem đã có dữ liệu tổng kết tồn kho cho tháng hiện tại chưa
        /// </summary>
        /// <param name="context">Context cơ sở dữ liệu (tùy chọn)</param>
        /// <returns>True nếu đã có dữ liệu tháng hiện tại</returns>
        private bool HasCurrentMonthStock(ClinicDbContext context = null)
        {
            try
            {
                bool shouldDisposeContext = context == null;
                context = context ?? DataProvider.Instance.Context;

                // Lấy thông tin tháng và năm hiện tại
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var monthYearString = $"{currentYear}-{currentMonth:D2}";

                // Kiểm tra có bản ghi nào trong MonthlyStocks cho tháng hiện tại không
                bool result = context.MonthlyStocks.Any(ms => ms.MonthYear == monthYearString);

                // Chỉ dispose nếu context được tạo trong phương thức này
                if (shouldDisposeContext && context != DataProvider.Instance.Context)
                {
                    context.Dispose();
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi kiểm tra tồn kho tháng: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Thực hiện tổng kết tồn kho tháng một cách thủ công với xác nhận từ người dùng
        /// Cho phép tạo mới hoặc cập nhật dữ liệu tồn kho cho tháng đã chọn
        /// </summary>
        private void ExecuteManualGenerateMonthlyStock()
        {
            try
            {
                // Lấy tháng và năm đã chọn từ giao diện người dùng
                int targetYear = SelectedYear;
                int targetMonth = SelectedMonth;

                // Định dạng chuỗi tháng/năm để hiển thị và lưu trữ
                string monthYearFormat = $"{targetYear:D4}-{targetMonth:D2}";
                string monthName = new DateTime(targetYear, targetMonth, 1).ToString("MMMM yyyy");

                // Kiểm tra xem đã có dữ liệu tổng kết cho tháng này chưa
                bool hasExistingData = false;
                using (var context = new ClinicDbContext())
                {
                    hasExistingData = context.MonthlyStocks
                        .Any(ms => ms.MonthYear == monthYearFormat);
                }

                // Chuẩn bị thông báo xác nhận phù hợp
                string confirmMessage = hasExistingData
                    ? $"Đã tồn tại dữ liệu tổng kết tồn kho cho tháng {targetMonth}/{targetYear}.\n" +
                      $"Bạn có chắc chắn muốn cập nhật lại không?"
                    : $"Bạn đang chuẩn bị tổng kết tồn kho cho tháng {targetMonth}/{targetYear}.\n" +
                      $"Bạn có chắc chắn muốn tiếp tục không?";

                // Hiển thị hộp thoại xác nhận từ người dùng
                bool result = MessageBoxService.ShowQuestion(
                    confirmMessage,
                    "Xác nhận tổng kết kho"
                );

                if (!result)
                    return;

                try
                {
                    // Thực hiện tổng kết hoặc cập nhật dữ liệu tồn kho tháng
                    GenerateOrUpdateMonthlyStock(targetYear, targetMonth);

                    // Hiển thị thông báo thành công
                    MessageBoxService.ShowSuccess(
                        $"Đã tổng kết tồn kho cho tháng {targetMonth}/{targetYear} thành công!",
                        "Tổng kết tồn kho"
                    );

                    // Cập nhật lại dữ liệu hiển thị
                    FilterMonthlyStock();
                }
                catch (ObjectDisposedException)
                {
                    // Xử lý trường hợp đặc biệt khi context bị dispose
                    MessageBoxService.ShowError(
                        "Lỗi kết nối cơ sở dữ liệu. Hãy thử lại sau vài giây.",
                        "Lỗi cơ sở dữ liệu"
                    );

                    // Reset lại context của DataProvider
                    DataProvider.Instance.ResetContext();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Lỗi khi thực hiện tổng kết kho: {ex.Message}",
                    "Lỗi"
                );
            }
        }

        /// <summary>
        /// Xóa dữ liệu tổng kết tồn kho của tháng hiện tại
        /// Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
        /// </summary>
        private void DeleteCurrentMonthStock()
        {
            try
            {
                // Lấy thông tin tháng và năm hiện tại
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var monthYearString = $"{currentYear}-{currentMonth:D2}";

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Lấy danh sách các bản ghi cần xóa
                        var recordsToDelete = DataProvider.Instance.Context.MonthlyStocks
                            .Where(ms => ms.MonthYear == monthYearString)
                            .ToList();

                        // Kiểm tra có bản ghi nào để xóa không
                        if (recordsToDelete.Any())
                        {
                            // Xóa các bản ghi khỏi database
                            DataProvider.Instance.Context.MonthlyStocks.RemoveRange(recordsToDelete);
                            DataProvider.Instance.Context.SaveChanges();

                            // Hoàn thành giao dịch khi thành công
                            transaction.Commit();

                            // Ghi log thành công (cho mục đích debug)
                            System.Diagnostics.Debug.WriteLine($"Đã xóa {recordsToDelete.Count} bản ghi tổng kết kho tháng {monthYearString}");
                        }
                        else
                        {
                            // Không có bản ghi nào để xóa, vẫn commit transaction
                            transaction.Commit();
                            System.Diagnostics.Debug.WriteLine($"Không có bản ghi nào để xóa cho tháng {monthYearString}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch khi có lỗi
                        transaction.Rollback();
                        throw new Exception($"Lỗi khi xóa dữ liệu tổng kết tồn kho: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Lỗi khi xóa dữ liệu tồn kho tháng hiện tại: {ex.Message}",
                    "Lỗi"
                );
            }
        }

        /// <summary>
        /// Tạo mới hoặc cập nhật dữ liệu tổng kết tồn kho cho một tháng cụ thể
        /// Tính toán lại số lượng tồn kho từ các lô nhập kho hiện có
        /// </summary>
        /// <param name="year">Năm cần tổng kết</param>
        /// <param name="month">Tháng cần tổng kết</param>
        private void GenerateOrUpdateMonthlyStock(int year, int month)
        {
            try
            {
                // Định dạng chuỗi tháng-năm để lưu trong database
                string monthYearFormat = $"{year:D4}-{month:D2}";

                // Tạo context mới để tránh vấn đề với entity tracking
                using (var context = new ClinicDbContext())
                {
                    // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        try
                        {
                            // Dictionary để theo dõi các bản ghi hiện có nhằm cập nhật hiệu quả
                            Dictionary<int, MonthlyStock> existingRecords = new Dictionary<int, MonthlyStock>();

                            // Kiểm tra và tải các bản ghi hiện có vào dictionary
                            var currentRecords = context.MonthlyStocks
                                .Where(ms => ms.MonthYear == monthYearFormat)
                                .ToList();

                            foreach (var record in currentRecords)
                            {
                                existingRecords[record.MedicineId] = record;
                            }

                            // Lấy tất cả thuốc chưa bị xóa cùng với thông tin liên quan
                            var medicines = context.Medicines
                                .Where(m => m.IsDeleted != true)
                                .Include(m => m.StockIns)  // Bao gồm thông tin lô nhập kho
                                .Include(m => m.Stocks)   // Bao gồm thông tin tồn kho
                                .ToList();

                            // Set để theo dõi các thuốc có tồn kho
                            HashSet<int> medicinesWithStock = new HashSet<int>();

                            foreach (var medicine in medicines)
                            {
                                // Xóa cache để đảm bảo tính toán mới
                                medicine._availableStockInsCache = null;

                                // Tính toán số lượng tồn kho vật lý và có thể sử dụng
                                int totalPhysicalQuantity = medicine.TotalPhysicalStockQuantity;
                                int usableQuantity = medicine.TotalStockQuantity;

                                // Chỉ xử lý các thuốc có tồn kho
                                if (totalPhysicalQuantity > 0)
                                {
                                    medicinesWithStock.Add(medicine.MedicineId);

                                    if (existingRecords.TryGetValue(medicine.MedicineId, out MonthlyStock existingRecord))
                                    {
                                        // Cập nhật bản ghi hiện có
                                        existingRecord.Quantity = totalPhysicalQuantity;
                                        existingRecord.CanUsed = usableQuantity;
                                        existingRecord.RecordedDate = DateTime.Now;
                                    }
                                    else
                                    {
                                        // Tạo bản ghi mới
                                        var monthlyStock = new MonthlyStock
                                        {
                                            MedicineId = medicine.MedicineId,
                                            MonthYear = monthYearFormat,
                                            Quantity = totalPhysicalQuantity,
                                            CanUsed = usableQuantity,
                                            RecordedDate = DateTime.Now
                                        };
                                        context.MonthlyStocks.Add(monthlyStock);
                                    }

                                    // Cập nhật bản ghi Stock cho tháng hiện tại nếu cần
                                    if (month == DateTime.Now.Month && year == DateTime.Now.Year)
                                    {
                                        var stockRecord = context.Stocks.FirstOrDefault(s => s.MedicineId == medicine.MedicineId);
                                        if (stockRecord != null)
                                        {
                                            // Cập nhật bản ghi Stock hiện có
                                            stockRecord.Quantity = totalPhysicalQuantity;
                                            stockRecord.UsableQuantity = usableQuantity;
                                            stockRecord.LastUpdated = DateTime.Now;
                                        }
                                        else
                                        {
                                            // Tạo bản ghi Stock mới
                                            context.Stocks.Add(new Stock
                                            {
                                                MedicineId = medicine.MedicineId,
                                                Quantity = totalPhysicalQuantity,
                                                UsableQuantity = usableQuantity,
                                                LastUpdated = DateTime.Now
                                            });
                                        }
                                    }
                                }
                            }

                            // Xóa các bản ghi cho thuốc không còn tồn kho
                            var recordsToDelete = currentRecords
                                .Where(r => !medicinesWithStock.Contains(r.MedicineId))
                                .ToList();

                            if (recordsToDelete.Any())
                            {
                                context.MonthlyStocks.RemoveRange(recordsToDelete);
                            }

                            // Lưu tất cả thay đổi vào database
                            context.SaveChanges();
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            // Hoàn tác giao dịch khi có lỗi
                            transaction.Rollback();
                            throw new Exception($"Lỗi khi tạo hoặc cập nhật dữ liệu tồn kho tháng: {ex.Message}", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Lỗi khi xử lý tồn kho tháng: {ex.Message}",
                    "Lỗi"
                );
            }
        }

        /// <summary>
        /// Hoàn tác việc tổng kết tồn kho tháng hiện tại
        /// Xóa dữ liệu tổng kết đã tạo và cập nhật giao diện
        /// </summary>
        private void ExecuteUndoMonthlyStock()
        {
            try
            {
                // Hiển thị hộp thoại xác nhận từ người dùng
                bool confirmResult = MessageBoxService.ShowQuestion(
                    "Bạn có chắc chắn muốn hủy bỏ tổng kết tồn kho tháng hiện tại không?",
                    "Xác nhận hoàn tác"
                );

                if (!confirmResult)
                    return;

                // Thực hiện xóa dữ liệu trong transaction
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Gọi phương thức xóa dữ liệu tổng kết
                        DeleteCurrentMonthStock();

                        // Hoàn thành giao dịch
                        transaction.Commit();

                        // Hiển thị thông báo thành công
                        MessageBoxService.ShowSuccess(
                            "Đã hủy bỏ tổng kết tồn kho tháng hiện tại thành công!",
                            "Hoàn tác tổng kết kho"
                        );

                        // Cập nhật dữ liệu hiển thị nếu đang ở chế độ xem tháng
                        if (IsMonthlyView)
                        {
                            FilterMonthlyStock();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác giao dịch khi có lỗi
                        transaction.Rollback();

                        // Hiển thị thông báo lỗi
                        MessageBoxService.ShowError(
                            $"Lỗi khi hoàn tác tổng kết kho: {ex.Message}",
                            "Lỗi"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi không dự kiến
                MessageBoxService.ShowError(
                    $"Lỗi không mong muốn khi hoàn tác tổng kết kho: {ex.Message}",
                    "Lỗi hệ thống"
                );
            }
        }

        /// <summary>
        /// Kiểm tra và nhắc nhở về việc thực hiện tổng kết tồn kho tháng
        /// Nhắc nhở vào ngày cuối tháng hoặc 2 ngày đầu tháng mới nếu chưa tổng kết tháng trước
        /// </summary>
        public void CheckMonthlyStockReminder()
        {
            try
            {
                var today = DateTime.Now;

                // Sử dụng context hiện có thay vì tạo mới
                var context = DataProvider.Instance.Context;

                // Nhắc nhở vào ngày cuối tháng
                if (today.Day == DateTime.DaysInMonth(today.Year, today.Month))
                {
                    var currentMonth = today.Month;
                    var currentYear = today.Year;
                    var monthYearString = $"{currentYear}-{currentMonth:D2}";

                    // Kiểm tra đã có tổng kết tháng hiện tại chưa
                    bool hasCurrentMonthStock = context.MonthlyStocks.Any(ms => ms.MonthYear == monthYearString);

                    if (!hasCurrentMonthStock && CanManageSettings)
                    {
                        MessageBoxService.ShowInfo(
                            "Hôm nay là ngày cuối tháng. Bạn nên thực hiện tổng kết tồn kho!",
                            "Nhắc nhở tổng kết kho"
                        );
                    }
                }
                // Nhắc nhở trong 2 ngày đầu tháng mới nếu tháng trước chưa tổng kết
                else if (today.Day <= 2)
                {
                    var previousMonth = today.AddMonths(-1);
                    var prevMonth = previousMonth.Month;
                    var prevYear = previousMonth.Year;
                    var prevMonthYearString = $"{prevYear}-{prevMonth:D2}";

                    // Kiểm tra đã có tổng kết tháng trước chưa
                    bool hasPrevMonthStock = context.MonthlyStocks.Any(ms => ms.MonthYear == prevMonthYearString);

                    if (!hasPrevMonthStock && CanManageSettings)
                    {
                        MessageBoxService.ShowInfo(
                            $"Tháng {prevMonth}/{prevYear} chưa được tổng kết tồn kho. Bạn nên thực hiện tổng kết!",
                            "Nhắc nhở tổng kết kho"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi trong CheckMonthlyStockReminder: {ex.Message}");
                // Không hiển thị MessageBox để tránh làm gián đoạn giao diện người dùng
            }
        }

        #endregion

        #region StockIn Methods

        /// <summary>
        /// Kiểm tra xem có thể thực hiện thêm thuốc mới hay không
        /// Validation cơ bản cho tất cả các trường bắt buộc
        /// </summary>
        /// <returns>True nếu tất cả thông tin hợp lệ để thêm thuốc</returns>
        private bool CanExecuteAddNewMedicine()
        {
            // Kiểm tra validation cơ bản - tất cả các trường bắt buộc phải có giá trị
            return StockinSelectedCategory != null &&           // Phải chọn loại thuốc
                   StockinSelectedSupplier != null &&           // Phải chọn nhà cung cấp
                   StockinSelectedUnit != null &&               // Phải chọn đơn vị tính
                   !string.IsNullOrWhiteSpace(StockinMedicineName) && // Tên thuốc không được trống
                   StockinQuantity > 0 &&                       // Số lượng phải lớn hơn 0
                   StockinUnitPrice > 0 &&                      // Giá nhập phải lớn hơn 0
                   StockinSellPrice > 0 &&                      // Giá bán phải lớn hơn 0
                   StockinExpiryDate.HasValue;                  // Phải có ngày hết hạn
        }

        /// <summary>
        /// Thực hiện thêm thuốc mới hoặc nhập thêm lô cho thuốc hiện có
        /// Bao gồm validation đầy đủ, kiểm tra trùng lặp và xử lý 3 trường hợp:
        /// 1. Thuốc hoàn toàn trùng khớp (tên, loại, đơn vị) - thêm lô mới
        /// 2. Thuốc trùng tên nhưng khác thông tin - hỏi người dùng có tạo mới không
        /// 3. Thuốc hoàn toàn mới - tạo thuốc và lô mới
        /// </summary>
        private void ExecuteAddNewMedicine()
        {
            try
            {
                // Bật chế độ validation cho tất cả các trường thuốc
                _isMedicineValidating = true;
                _medicineFieldsTouched.Add(nameof(StockinMedicineName));
                _medicineFieldsTouched.Add(nameof(StockinQuantity));
                _medicineFieldsTouched.Add(nameof(StockinUnitPrice));
                _medicineFieldsTouched.Add(nameof(StockinSellPrice));
                _medicineFieldsTouched.Add(nameof(StockinExpiryDate));

                // Kích hoạt validation bằng cách thông báo thay đổi thuộc tính
                OnPropertyChanged(nameof(StockinMedicineName));
                OnPropertyChanged(nameof(StockinBarCode));
                OnPropertyChanged(nameof(StockinQrCode));
                OnPropertyChanged(nameof(StockinQuantity));
                OnPropertyChanged(nameof(StockinUnitPrice));
                OnPropertyChanged(nameof(StockinSellPrice));
                OnPropertyChanged(nameof(StockinExpiryDate));

                // Kiểm tra lỗi validation cho các trường thuốc
                if (!string.IsNullOrEmpty(this[nameof(StockinMedicineName)]) ||
                    !string.IsNullOrEmpty(this[nameof(StockinBarCode)]) ||
                    !string.IsNullOrEmpty(this[nameof(StockinQrCode)]) ||
                    !string.IsNullOrEmpty(this[nameof(StockinQuantity)]) ||
                    !string.IsNullOrEmpty(this[nameof(StockinUnitPrice)]) ||
                    !string.IsNullOrEmpty(this[nameof(StockinSellPrice)]) ||
                    !string.IsNullOrEmpty(this[nameof(StockinExpiryDate)]))
                {
                    MessageBoxService.ShowWarning(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm thuốc.",
                        "Lỗi thông tin");
                    return;
                }

                // Validation riêng cho loại thuốc
                if (StockinSelectedCategory == null)
                {
                    MessageBoxService.ShowWarning("Vui lòng chọn loại thuốc", "Thiếu thông tin");
                    return;
                }

                // Validation riêng cho đơn vị tính
                if (StockinSelectedUnit == null)
                {
                    MessageBoxService.ShowWarning("Vui lòng chọn đơn vị tính", "Thiếu thông tin");
                    return;
                }

                // Validation riêng cho nhà cung cấp
                if (StockinSelectedSupplier == null)
                {
                    MessageBoxService.ShowWarning("Vui lòng chọn nhà cung cấp", "Thiếu thông tin");
                    return;
                }

                var resultt = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc chắn muốn nhập thuốc '{StockinMedicineName}' với số lượng '{StockinQuantity}' không ?",
                    "Xác nhận thêm thuốc"
                );
                if (!resultt)
                    return;

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                      

                        // Chuyển đổi ngày hết hạn từ DateTime sang DateOnly
                        DateOnly? expiryDateOnly = null;
                        if (StockinExpiryDate.HasValue)
                        {
                            expiryDateOnly = DateOnly.FromDateTime(StockinExpiryDate.Value);
                        }

                        // Sử dụng ngày nhập hoặc ngày hiện tại
                        DateTime importDateTime = ImportDate ?? DateTime.Now;

                        // Tìm thuốc hiện có hoặc tạo mới
                        var dbContext = DataProvider.Instance.Context;
                        Medicine medicine;
                        string resultMessage;

                        // Tìm thuốc khớp hoàn toàn (tên, loại, đơn vị)
                        var existingExactMedicine = dbContext.Medicines
                            .Include(m => m.StockIns)
                            .FirstOrDefault(m => m.Name.ToLower() == StockinMedicineName.ToLower().Trim() &&
                                            m.UnitId == StockinSelectedUnit.UnitId &&
                                            m.CategoryId == StockinSelectedCategory.CategoryId &&
                                            (bool)!m.IsDeleted);

                        // Tìm thuốc chỉ trùng tên (để cảnh báo)
                        var existingMedicineWithName = dbContext.Medicines
                            .FirstOrDefault(m => m.Name.ToLower() == StockinMedicineName.ToLower().Trim() &&
                                            (bool)!m.IsDeleted);

                        if (existingExactMedicine != null)
                        {
                            // Trường hợp 1: Tìm thấy thuốc khớp hoàn toàn - thêm lô nhập mới
                            medicine = existingExactMedicine;
                            resultMessage = $"Đã nhập thêm '{StockinQuantity}' '{medicine.Unit?.UnitName ?? "đơn vị"}' cho thuốc '{medicine.Name}' hiện có.";
                        }
                        else if (existingMedicineWithName != null)
                        {
                            // Trường hợp 2: Tìm thấy thuốc trùng tên nhưng khác thông tin
                            string differences = GetDifferenceMessage(existingMedicineWithName);

                            bool result = MessageBoxService.ShowQuestion(
                               $"Đã tồn tại thuốc có tên '{StockinMedicineName.Trim()}' nhưng {differences}. " +
                               $"Bạn có muốn tạo thuốc mới không?",
                               "Xác nhận"
                           );

                            if (!result)
                                return;

                            // Tạo thuốc mới với thông tin khác
                            medicine = new Medicine
                            {
                                Name = StockinMedicineName.Trim(),
                                CategoryId = StockinSelectedCategory.CategoryId,
                                UnitId = StockinSelectedUnit.UnitId,
                                BarCode = StockinBarCode?.Trim(),
                                QrCode = StockinQrCode?.Trim(),
                                IsDeleted = false
                            };

                            dbContext.Medicines.Add(medicine);
                            dbContext.SaveChanges();

                            resultMessage = $"Đã thêm thuốc '{medicine.Name}' mới với thông tin khác.";
                        }
                        else
                        {
                            // Trường hợp 3: Thuốc hoàn toàn mới
                            medicine = new Medicine
                            {
                                Name = StockinMedicineName.Trim(),
                                CategoryId = StockinSelectedCategory.CategoryId,
                                UnitId = StockinSelectedUnit.UnitId,
                                BarCode = StockinBarCode?.Trim(),
                                QrCode = StockinQrCode?.Trim(),
                                IsDeleted = false
                            };

                            dbContext.Medicines.Add(medicine);
                            dbContext.SaveChanges();

                            resultMessage = $"Đã thêm thuốc mới '{medicine.Name}'.";
                        }

                        // Thêm bản ghi lô nhập mới (StockIn)
                        var stockIn = new StockIn
                        {
                            MedicineId = medicine.MedicineId,
                            StaffId = CurrentAccount.StaffId,
                            Quantity = StockinQuantity,
                            RemainQuantity = StockinQuantity, // Khởi tạo số lượng còn lại = số lượng nhập
                            ImportDate = importDateTime,
                            UnitPrice = StockinUnitPrice,
                            SellPrice = StockinSellPrice,
                            ProfitMargin = StockProfitMargin,
                            TotalCost = StockinUnitPrice * StockinQuantity,
                            ExpiryDate = expiryDateOnly,
                            SupplierId = StockinSelectedSupplier.SupplierId
                        };

                        dbContext.StockIns.Add(stockIn);

                        // Cập nhật hoặc tạo bản ghi tồn kho (Stock)
                        var existingStock = dbContext.Stocks
                            .FirstOrDefault(s => s.MedicineId == medicine.MedicineId);

                        // Kiểm tra lô thuốc mới có thể sử dụng được dựa trên ngày hết hạn
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var minimumExpiryDate = today.AddDays(Medicine.MinimumDaysBeforeExpiry);
                        bool isUsable = !expiryDateOnly.HasValue || expiryDateOnly.Value >= minimumExpiryDate;

                        if (existingStock != null)
                        {
                            // Cập nhật bản ghi Stock hiện có
                            existingStock.Quantity += StockinQuantity;
                            if (isUsable)
                                existingStock.UsableQuantity += StockinQuantity;
                            existingStock.LastUpdated = ImportDate ?? DateTime.Now;
                        }
                        else
                        {
                            // Tạo bản ghi Stock mới
                            var newStock = new Stock
                            {
                                MedicineId = medicine.MedicineId,
                                Quantity = StockinQuantity,
                                UsableQuantity = isUsable ? StockinQuantity : 0,
                                LastUpdated = ImportDate ?? DateTime.Now
                            };
                            dbContext.Stocks.Add(newStock);
                        }

                        // Lưu tất cả thay đổi vào database
                        dbContext.SaveChanges();
                        transaction.Commit();

                        // Hiển thị thông báo thành công
                        MessageBoxService.ShowSuccess(
                            resultMessage,
                            "Thêm thuốc thành công"
                        );

                        // Reset trạng thái validation sau khi lưu thành công
                        _isMedicineValidating = false;
                        _medicineFieldsTouched.Clear();

                        // Làm mới dữ liệu và reset form
                        LoadData();
                        ExecuteRestart();
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác transaction khi có lỗi
                        transaction.Rollback();
                        throw new Exception("Lỗi khi lưu dữ liệu: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi: {ex.Message}",
                    "Lỗi"
                );
            }
        }

        /// <summary>
        /// Phương thức helper tạo thông báo mô tả sự khác biệt giữa thuốc hiện có và thuốc đang nhập
        /// So sánh loại thuốc, đơn vị tính và nhà cung cấp gần nhất
        /// </summary>
        /// <param name="existingMedicine">Thuốc hiện có trong hệ thống</param>
        /// <returns>Chuỗi mô tả các điểm khác biệt</returns>
        private string GetDifferenceMessage(Medicine existingMedicine)
        {
            List<string> differences = new List<string>();

            // So sánh loại thuốc
            if (existingMedicine.CategoryId != StockinSelectedCategory.CategoryId)
                differences.Add("khác loại thuốc");

            // So sánh đơn vị tính
            if (existingMedicine.UnitId != StockinSelectedUnit.UnitId)
                differences.Add("khác đơn vị tính");

            // Không còn kiểm tra SupplierId ở bảng Medicine nữa
            // Thay vào đó, kiểm tra nhà cung cấp của lần nhập gần nhất
            var latestStockIn = existingMedicine.StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();

            if (latestStockIn != null && latestStockIn.SupplierId != StockinSelectedSupplier.SupplierId)
                differences.Add("khác nhà cung cấp gần đây");

            // Trả về thông báo tổng hợp hoặc thông báo mặc định
            if (!differences.Any())
                return "có thông tin khác";

            return string.Join(", ", differences);
        }

        /// <summary>
        /// Khởi động lại form nhập kho về trạng thái ban đầu
        /// Xóa tất cả dữ liệu đã nhập và reset validation
        /// </summary>
        private void ExecuteRestart()
        {
            // Xóa trạng thái validation
            _isMedicineValidating = false;
            _medicineFieldsTouched.Clear();

            // Xóa tất cả các trường nhập liệu và reset về giá trị mặc định
            StockinMedicineName = string.Empty;
            StockinBarCode = string.Empty;
            StockinQrCode = string.Empty;
            StockinQuantity = 1;  // Reset về 1 thay vì 0 để dễ sử dụng
            StockinUnitPrice = 0;
            StockinSellPrice = 0;
            StockProfitMargin = 20; // Reset về margin lợi nhuận mặc định 20%
            StockinExpiryDate = DateTime.Now.AddYears(1); // Reset về ngày hết hạn mặc định 1 năm
            StockinSelectedCategory = null;
            StockinSelectedSupplier = null;
            StockinSelectedUnit = null;
            SelectedMedicine = null;
            ImportDate = DateTime.Now;
        }

        /// <summary>
        /// Tải thông tin chi tiết thuốc vào form nhập kho để thêm lô mới
        /// Tự động điền thông tin cơ bản và giá cả từ lô nhập gần nhất
        /// Hiển thị cảnh báo về tình trạng tồn kho và hạn sử dụng
        /// </summary>
        /// <param name="medicine">Thuốc cần tải thông tin</param>
        private void LoadMedicineDetailsForStockIn(Medicine medicine)
        {
            if (medicine == null) return;

            // Xóa cache để đảm bảo lấy thông tin mới nhất
            medicine._availableStockInsCache = null;

            // Điền thông tin cơ bản của thuốc
            StockinMedicineName = medicine.Name;
            StockinBarCode = medicine.BarCode;
            StockinQrCode = medicine.QrCode;
            StockinSelectedCategory = medicine.Category;
            StockinSelectedUnit = medicine.Unit;

            // Lấy lô đang được sử dụng để bán
            var sellingStockIn = medicine.SellingStockIn;

            // Đối với lô nhập mới, sử dụng thông tin từ lô nhập gần nhất làm mặc định
            var latestStockIn = medicine.StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();

            if (latestStockIn != null)
            {
                // Sử dụng giá từ lô nhập gần nhất làm mặc định
                StockinUnitPrice = latestStockIn.UnitPrice;
                StockinSellPrice = latestStockIn.SellPrice ?? latestStockIn.UnitPrice * 1.2m;
                StockProfitMargin = latestStockIn.ProfitMargin;

                // Đối với nhà cung cấp, sử dụng nhà cung cấp của lô nhập gần nhất
                StockinSelectedSupplier = SupplierListStockIn.FirstOrDefault(s => s.SupplierId == latestStockIn.SupplierId);

                // Sử dụng ngày hiện tại cho lô nhập mới
                ImportDate = DateTime.Now;
            }
            else
            {
                // Giá trị mặc định nếu không có lô nhập trước đó
                StockinUnitPrice = 0;
                StockinSellPrice = 0;
                StockProfitMargin = 20; // Mặc định 20% markup
                ImportDate = DateTime.Now;
            }

            // Xử lý ngày hết hạn - cố gắng lấy từ lô đang bán nếu có
            if (sellingStockIn?.ExpiryDate.HasValue == true)
            {
                try
                {
                    // Chuyển đổi DateOnly sang DateTime cho DatePicker
                    StockinExpiryDate = new DateTime(
                        sellingStockIn.ExpiryDate.Value.Year,
                        sellingStockIn.ExpiryDate.Value.Month,
                        sellingStockIn.ExpiryDate.Value.Day);

                    // Hiển thị cảnh báo cụ thể dựa trên tình trạng tồn kho
                    var today = DateOnly.FromDateTime(DateTime.Today);

                    if (medicine.IsLastestStockIn && medicine.HasNearExpiryStock)
                    {
                        MessageBoxService.ShowWarning(
                            $"Thuốc {medicine.Name} đang sử dụng lô cuối cùng và sẽ hết hạn trong " +
                            $"{(sellingStockIn.ExpiryDate.Value.DayNumber - today.DayNumber)} ngày. " +
                            $"Cần nhập thêm hàng ngay!",
                            "Cảnh báo tồn kho và hạn sử dụng"
                        );
                    }
                    else if (medicine.IsLastestStockIn)
                    {
                        MessageBoxService.ShowWarning(
                            $"Thuốc {medicine.Name} đang sử dụng lô cuối cùng. Cần nhập thêm hàng.",
                            "Cảnh báo tồn kho"
                        );
                    }
                    else if (medicine.HasNearExpiryStock)
                    {
                        MessageBoxService.ShowWarning(
                            $"Thuốc {medicine.Name} có lô sắp hết hạn. Hệ thống sẽ tự động chuyển sang lô mới " +
                            $"khi còn {Medicine.MinimumDaysBeforeSwitchingBatch} ngày hoặc ít hơn.",
                            "Cảnh báo hạn sử dụng"
                        );
                    }
                }
                catch
                {
                    // Mặc định một năm từ hiện tại nếu chuyển đổi thất bại
                    StockinExpiryDate = DateTime.Now.AddYears(1);
                }
            }
            else
            {
                // Mặc định một năm từ hiện tại nếu không có lô hoạt động hoặc không có ngày hết hạn
                StockinExpiryDate = DateTime.Now.AddYears(1);
            }

            // Hiển thị cảnh báo số lượng thấp nếu lô đang bán sắp hết
            if (sellingStockIn != null && sellingStockIn.RemainQuantity <= 10)
            {
                MessageBoxService.ShowWarning(
                    $"Thuốc {medicine.Name} đang sử dụng chỉ còn {sellingStockIn.RemainQuantity} {medicine.Unit?.UnitName ?? "đơn vị"}.",
                    "Cảnh báo số lượng thấp"
                );
            }
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có đang hoạt động hay không
        /// Chỉ cho phép nhập hàng từ những nhà cung cấp đang hoạt động
        /// </summary>
        /// <param name="supplier">Nhà cung cấp cần kiểm tra</param>
        /// <returns>True nếu nhà cung cấp đang hoạt động, False nếu không</returns>
        private bool IsSupplierWorking(Supplier supplier)
        {
            if (supplier == null)
                return false;

            return supplier.IsActive == true;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Đặt lại form sau khi thêm nhà cung cấp thành công
        /// Xóa trạng thái touched và tắt validation để không hiển thị lỗi không cần thiết
        /// </summary>
        private void ClearForm()
        {
            SupplierCode = string.Empty;
            SupplierName = string.Empty;
            SupplierEmail = string.Empty;
            SupplierPhone = string.Empty;
            SupplierTaxCode = string.Empty;
            ContactPerson = string.Empty;
            SupplierAddress = string.Empty;
            IsActive = true;
            IsNotActive = false;

            // Xóa trạng thái touched và tắt validation
            _touchedFields.Clear();
            _isValidating = false;

            // Kích hoạt PropertyChanged để cập nhật UI và xóa lỗi
            OnPropertyChanged(nameof(SupplierCode));
            OnPropertyChanged(nameof(SupplierName));
            OnPropertyChanged(nameof(SupplierEmail));
            OnPropertyChanged(nameof(SupplierPhone));
            OnPropertyChanged(nameof(SupplierTaxCode));
            OnPropertyChanged(nameof(ContactPerson));
            OnPropertyChanged(nameof(SupplierAddress));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(IsNotActive));
        }

        /// <summary>
        /// Tìm ID của loại thuốc dựa trên tên loại thuốc
        /// Sử dụng để chuyển đổi từ tên hiển thị sang ID lưu trữ trong database
        /// Hỗ trợ tìm kiếm không phân biệt hoa thường và loại trừ các loại đã bị xóa
        /// </summary>
        /// <param name="categoryName">Tên loại thuốc cần tìm ID</param>
        /// <returns>ID của loại thuốc nếu tìm thấy, null nếu không tìm thấy hoặc tên rỗng</returns>
        private int? GetCategoryIdByName(string categoryName)
        {
            // Kiểm tra tên loại thuốc có rỗng hoặc null không
            if (string.IsNullOrWhiteSpace(categoryName))
                return null;

            // Tìm loại thuốc trong database theo tên
            // Sử dụng Trim() để loại bỏ khoảng trắng thừa
            // Sử dụng ToLower() để so sánh không phân biệt hoa thường
            // Chỉ lấy các loại thuốc chưa bị xóa (IsDeleted != true)
            var category = DataProvider.Instance.Context.MedicineCategories
                .FirstOrDefault(c => c.CategoryName.Trim().ToLower() == categoryName.Trim().ToLower() && (bool)!c.IsDeleted);

            // Trả về CategoryId nếu tìm thấy, null nếu không tìm thấy
            return category?.CategoryId;
        }

        /// <summary>
        /// Tìm ID của nhà cung cấp dựa trên tên nhà cung cấp
        /// Sử dụng để chuyển đổi từ tên hiển thị sang ID lưu trữ trong database
        /// Hỗ trợ tìm kiếm không phân biệt hoa thường và loại trừ các nhà cung cấp đã bị xóa
        /// </summary>
        /// <param name="supplierName">Tên nhà cung cấp cần tìm ID</param>
        /// <returns>ID của nhà cung cấp nếu tìm thấy, null nếu không tìm thấy hoặc tên rỗng</returns>
        private int? GetSupplierIdByName(string supplierName)
        {
            // Kiểm tra tên nhà cung cấp có rỗng hoặc null không
            if (string.IsNullOrWhiteSpace(supplierName))
                return null;

            // Tìm nhà cung cấp trong database theo tên
            // Sử dụng Trim() để loại bỏ khoảng trắng thừa
            // Sử dụng ToLower() để so sánh không phân biệt hoa thường
            // Chỉ lấy các nhà cung cấp chưa bị xóa (IsDeleted != true)
            var supplier = DataProvider.Instance.Context.Suppliers
                .FirstOrDefault(c => c.SupplierName.Trim().ToLower() == supplierName.Trim().ToLower() && (bool)!c.IsDeleted);

            // Trả về SupplierId nếu tìm thấy, null nếu không tìm thấy
            return supplier?.SupplierId;
        }

        /// <summary>
        /// Tìm ID của đơn vị tính dựa trên tên đơn vị tính
        /// Sử dụng để chuyển đổi từ tên hiển thị sang ID lưu trữ trong database
        /// Hỗ trợ tìm kiếm không phân biệt hoa thường và loại trừ các đơn vị đã bị xóa
        /// </summary>
        /// <param name="unitName">Tên đơn vị tính cần tìm ID</param>
        /// <returns>ID của đơn vị tính nếu tìm thấy, null nếu không tìm thấy hoặc tên rỗng</returns>
        private int? GetUnitIdByName(string unitName)
        {
            // Kiểm tra tên đơn vị tính có rỗng hoặc null không
            if (string.IsNullOrWhiteSpace(unitName))
                return null;

            // Tìm đơn vị tính trong database theo tên
            // Sử dụng Trim() để loại bỏ khoảng trắng thừa
            // Sử dụng ToLower() để so sánh không phân biệt hoa thường
            // Chỉ lấy các đơn vị tính chưa bị xóa (IsDeleted != true)
            var unit = DataProvider.Instance.Context.Units
                .FirstOrDefault(c => c.UnitName.Trim().ToLower() == unitName.Trim().ToLower() && (bool)!c.IsDeleted);

            // Trả về UnitId nếu tìm thấy, null nếu không tìm thấy
            return unit?.UnitId;
        }

        #endregion
        #endregion
    }
}
