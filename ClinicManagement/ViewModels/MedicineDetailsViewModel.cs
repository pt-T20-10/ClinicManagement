using ClinicManagement.Models;
using ClinicManagement.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel quản lý chi tiết thuốc - cung cấp chức năng xem, chỉnh sửa thông tin thuốc
    /// và quản lý các lô nhập kho (StockIn) bao gồm chỉnh sửa, tiêu hủy và đặt lô đang bán
    /// </summary>
    public class MedicineDetailsViewModel : BaseViewModel
    {
        #region Properties

        /// <summary>
        /// Tham chiếu đến cửa sổ chứa ViewModel này
        /// Được sử dụng để đóng cửa sổ khi cần thiết
        /// </summary>
        private readonly Window _window;
        
        /// <summary>
        /// Đối tượng Medicine chính được hiển thị và chỉnh sửa
        /// </summary>
        private Medicine _medicine;
        
        /// <summary>
        /// Lô nhập kho hiện đang được chỉnh sửa
        /// Được sử dụng trong quá trình edit lô thuốc
        /// </summary>
        private Medicine.StockInWithRemaining CurrentStockEntry { get; set; }
        
        /// <summary>
        /// Danh sách chi tiết các lô nhập kho của thuốc
        /// Hiển thị thông tin đầy đủ về từng lô nhập bao gồm số lượng còn lại
        /// </summary>
        private ObservableCollection<Medicine.StockInWithRemaining> _detailedStockList;

        /// <summary>
        /// Danh sách nhà cung cấp để lựa chọn
        /// Chỉ bao gồm các nhà cung cấp đang hoạt động và chưa bị xóa
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

        /// <summary>
        /// Danh sách đơn vị tính để lựa chọn
        /// Được tải từ cơ sở dữ liệu, loại trừ các đơn vị đã bị xóa
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
        /// Danh sách loại thuốc để phân loại
        /// Được tải từ cơ sở dữ liệu, loại trừ các loại đã bị xóa
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
        /// Thuốc chính được quản lý trong ViewModel
        /// Khi thay đổi sẽ tự động tải dữ liệu lô nhập kho
        /// </summary>
        public Medicine Medicine
        {
            get => _medicine;
            set
            {
                _medicine = value;
                OnPropertyChanged();
                // Chỉ tải dữ liệu ban đầu, không refresh liên tục
                RefreshMedicineData();
            }
        }

        /// <summary>
        /// Tên nhà cung cấp gần nhất (lô nhập mới nhất)
        /// Được hiển thị để người dùng biết nguồn gốc thuốc
        /// </summary>
        private string _latestSupplierName;
        public string LatestSupplierName
        {
            get => _latestSupplierName;
            set
            {
                _latestSupplierName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách chi tiết các lô nhập kho với thông tin số lượng còn lại
        /// Được sử dụng để hiển thị trong DataGrid và quản lý các lô
        /// </summary>
        public ObservableCollection<Medicine.StockInWithRemaining> DetailedStockList
        {
            get => _detailedStockList;
            set
            {
                _detailedStockList = value;
                OnPropertyChanged();
            }
        }

        // === CÁC THUỘC TÍNH LỰA CHỌN CHO COMBOBOX ===

        /// <summary>
        /// Loại thuốc được chọn trong ComboBox
        /// Khi thay đổi sẽ cập nhật CategoryId của thuốc
        /// </summary>
        private MedicineCategory _selectedCategory;
        public MedicineCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                if (value != null && Medicine != null)
                    Medicine.CategoryId = value.CategoryId;
            }
        }

        /// <summary>
        /// Đơn vị tính được chọn trong ComboBox
        /// Khi thay đổi sẽ cập nhật UnitId của thuốc
        /// </summary>
        private Unit _selectedUnit;
        public Unit SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                _selectedUnit = value;
                OnPropertyChanged();
                if (value != null && Medicine != null)
                    Medicine.UnitId = value.UnitId;
            }
        }

        // === CÁC THUỘC TÍNH CHO NHẬP KHO MỚI ===

        /// <summary>
        /// Số lượng thuốc nhập vào trong lô mới
        /// Mặc định là 1, phải lớn hơn 0
        /// </summary>
        private int _stockinQuantity = 1;
        public int StockinQuantity
        {
            get => _stockinQuantity;
            set
            {
                _stockinQuantity = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Giá nhập của thuốc cho lô mới
        /// Khi thay đổi sẽ tự động tính lại giá bán nếu có margin lợi nhuận
        /// </summary>
        private decimal _stockinUnitPrice;
        public decimal StockinUnitPrice
        {
            get => _stockinUnitPrice;
            set
            {
                _stockinUnitPrice = value;
                OnPropertyChanged();

                // Nếu margin lợi nhuận đã được thiết lập, tính lại giá bán
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
            }
        }

        /// <summary>
        /// Phần trăm lợi nhuận của lô thuốc
        /// Khi thay đổi sẽ tự động tính lại giá bán hoặc giá nhập tương ứng
        /// </summary>
        private decimal _StockProfitMargin;
        public decimal StockProfitMargin
        {
            get => _StockProfitMargin;
            set
            {
                _StockProfitMargin = value;
                OnPropertyChanged();

                // Nếu giá nhập đã có, tính lại giá bán
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
        /// Giá bán của thuốc cho lô mới
        /// Khi thay đổi sẽ tự động tính lại margin lợi nhuận nếu có giá nhập
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
        /// Ngày nhập kho cho lô mới
        /// Mặc định là ngày hiện tại
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

        /// <summary>
        /// Ngày hết hạn của lô thuốc mới
        /// Mặc định là một năm sau ngày nhập
        /// </summary>
        private DateTime? _stockinExpiryDate;
        public DateTime? StockinExpiryDate
        {
            get => _stockinExpiryDate;
            set
            {
                _stockinExpiryDate = value;
                OnPropertyChanged();
            }
        }

        // === CÁC THUỘC TÍNH CHO CHỈNH SỬA LÔ THUỐC ===

        /// <summary>
        /// Lô thuốc được chọn từ DataGrid để chỉnh sửa
        /// </summary>
        private Medicine.StockInWithRemaining _selectedStockEntry;
        public Medicine.StockInWithRemaining SelectedStockEntry
        {
            get => _selectedStockEntry;
            set
            {
                _selectedStockEntry = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Cờ hiệu kiểm soát hiển thị phần chỉnh sửa lô thuốc
        /// True = đang ở chế độ chỉnh sửa, False = chế độ xem
        /// </summary>
        private bool _isEditingStockEntry;
        public bool IsEditingStockEntry
        {
            get => _isEditingStockEntry;
            set
            {
                _isEditingStockEntry = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Số lượng tổng của lô đang chỉnh sửa
        /// Không được nhỏ hơn số lượng đã bán
        /// </summary>
        private int _editStockQuantity;
        public int EditStockQuantity
        {
            get => _editStockQuantity;
            set
            {
                _editStockQuantity = value;
                OnPropertyChanged();

                // Cập nhật số lượng còn lại khi số lượng tổng thay đổi
                if (SelectedStockEntry != null)
                {
                    int originalQuantity = SelectedStockEntry.StockIn.Quantity;
                    int originalRemain = SelectedStockEntry.StockIn.RemainQuantity;

                    // Tính số lượng đã bán từ lô này
                    int soldItems = originalQuantity - originalRemain;

                    // Không cho phép thiết lập số lượng nhỏ hơn số đã bán
                    if (value < soldItems)
                    {
                        MessageBoxService.ShowWarning(
                            $"Số lượng nhập không thể nhỏ hơn số đã bán ({soldItems}).",
                            "Cảnh báo"
                        );
                        _editStockQuantity = soldItems;
                        OnPropertyChanged();
                    }

                    // Cập nhật số lượng còn lại dựa trên tổng số lượng mới trừ đi số đã bán
                    EditRemainQuantity = Math.Max(0, _editStockQuantity - soldItems);
                }
            }
        }

        /// <summary>
        /// Số lượng còn lại của lô đang chỉnh sửa
        /// Được tính tự động dựa trên EditStockQuantity và số lượng đã bán
        /// </summary>
        private int _editRemainQuantity;
        public int EditRemainQuantity
        {
            get => _editRemainQuantity;
            set
            {
                _editRemainQuantity = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Giá nhập của lô đang chỉnh sửa
        /// Khi thay đổi sẽ tự động tính lại giá bán dựa trên margin
        /// </summary>
        private decimal _editUnitPrice;
        public decimal EditUnitPrice
        {
            get => _editUnitPrice;
            set
            {
                _editUnitPrice = value;
                OnPropertyChanged();

                // Nếu margin đã được thiết lập, tính lại giá bán
                if (_editProfitMargin > 0)
                {
                    _editSellPrice = _editUnitPrice * (1 + (_editProfitMargin / 100));
                    OnPropertyChanged(nameof(EditSellPrice));
                }
                else if (_editSellPrice > 0)
                {
                    // Tính margin dựa trên giá nhập và giá bán
                    CalculateEditProfitMargin();
                }
            }
        }

        /// <summary>
        /// Phần trăm lợi nhuận của lô đang chỉnh sửa
        /// Tương tự như StockProfitMargin nhưng dành cho chế độ edit
        /// </summary>
        private decimal _editProfitMargin;
        public decimal EditProfitMargin
        {
            get => _editProfitMargin;
            set
            {
                _editProfitMargin = value;
                OnPropertyChanged();

                // Nếu giá nhập đã có, tính lại giá bán
                if (_editUnitPrice > 0)
                {
                    _editSellPrice = _editUnitPrice * (1 + (_editProfitMargin / 100));
                    OnPropertyChanged(nameof(EditSellPrice));
                }
                else if (_editSellPrice > 0)
                {
                    _editUnitPrice = _editSellPrice / (1 + (_editProfitMargin / 100));
                    OnPropertyChanged(nameof(EditUnitPrice));
                }
            }
        }

        /// <summary>
        /// Giá bán của lô đang chỉnh sửa
        /// Khi thay đổi sẽ tự động tính lại margin nếu có giá nhập
        /// </summary>
        private decimal _editSellPrice;
        public decimal EditSellPrice
        {
            get => _editSellPrice;
            set
            {
                _editSellPrice = value;
                OnPropertyChanged();

                // Nếu giá nhập đã có, tính lại margin lợi nhuận
                if (_editUnitPrice > 0)
                {
                    CalculateEditProfitMargin();
                }
            }
        }

        /// <summary>
        /// Nhà cung cấp của lô đang chỉnh sửa
        /// Cho phép thay đổi nhà cung cấp của lô thuốc
        /// </summary>
        private Supplier _editSupplier;
        public Supplier EditSupplier
        {
            get => _editSupplier;
            set
            {
                _editSupplier = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Ngày hết hạn của lô đang chỉnh sửa
        /// Sử dụng DateTime? để tương thích với DatePicker
        /// </summary>
        private DateTime? _editExpiryDate;
        public DateTime? EditExpiryDate
        {
            get => _editExpiryDate;
            set
            {
                _editExpiryDate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Phương thức helper tính margin lợi nhuận cho chế độ chỉnh sửa
        /// </summary>
        private void CalculateEditProfitMargin()
        {
            if (_editUnitPrice > 0 && _editSellPrice > 0)
            {
                _editProfitMargin = ((_editSellPrice / _editUnitPrice) - 1) * 100;
                OnPropertyChanged(nameof(EditProfitMargin));
            }
        }

        #region Commands
        /// <summary>
        /// Lệnh đóng cửa sổ
        /// </summary>
        public ICommand CloseCommand { get; set; }
        
        /// <summary>
        /// Lệnh lưu các thay đổi thông tin thuốc và thêm lô nhập mới (nếu có)
        /// </summary>
        public ICommand SaveChangesCommand { get; set; }
        
        /// <summary>
        /// Lệnh làm mới dữ liệu từ cơ sở dữ liệu
        /// </summary>
        public ICommand RefreshDataCommand { get; set; }
        
        /// <summary>
        /// Lệnh đặt lô được chọn làm lô đang bán
        /// </summary>
        public ICommand SetAsSellingBatchCommand { get; set; }
        
        // === CÁC LỆNH CHO CHỈNH SỬA LÔ THUỐC ===
        
        /// <summary>
        /// Lệnh bắt đầu chỉnh sửa lô thuốc được chọn
        /// </summary>
        public ICommand EditStockEntryCommand { get; set; }
        
        /// <summary>
        /// Lệnh lưu thông tin lô thuốc đã chỉnh sửa
        /// </summary>
        public ICommand SaveStockEntryCommand { get; set; }
        
        /// <summary>
        /// Lệnh tiêu hủy lô thuốc (đánh dấu IsTerminated = true)
        /// </summary>
        public ICommand TerminateStockBatchCommand { get; set; }
        
        /// <summary>
        /// Lệnh hủy chỉnh sửa lô thuốc và quay về chế độ xem
        /// </summary>
        public ICommand CancelEditStockEntryCommand { get; set; }
        #endregion
        #endregion

        /// <summary>
        /// Constructor khởi tạo ViewModel với thuốc và cửa sổ cụ thể
        /// </summary>
        /// <param name="medicine">Thuốc cần quản lý</param>
        /// <param name="window">Cửa sổ chứa ViewModel</param>
        public MedicineDetailsViewModel(Medicine medicine, Window window)
        {
            _window = window;
            Medicine = medicine;
            InitializeCommands();
            LoadDropdownData();
            LoadMedicineDetailsForEdit(medicine);
            LoadRelatedData();
        }

        #region InitializeCommands
        /// <summary>
        /// Khởi tạo tất cả các command với logic thực thi và điều kiện kích hoạt
        /// </summary>
        public void InitializeCommands()
        {
            // Lệnh đóng cửa sổ - luôn khả dụng
            CloseCommand = new RelayCommand<object>(
                parameter => _window.Close(),
                parameter => true
            );
            // Lệnh làm mới dữ liệu - chỉ khả dụng khi có thuốc
            RefreshDataCommand = new RelayCommand<object>(
                parameter => RefreshMedicineData(),
                parameter => Medicine != null
            );

            // Lệnh bắt đầu chỉnh sửa lô thuốc - chỉ khả dụng khi có lô được chọn
            EditStockEntryCommand = new RelayCommand<Medicine.StockInWithRemaining>(
                parameter => BeginEditStockEntry(parameter),
                parameter => parameter != null
            );

            // Lệnh lưu chỉnh sửa lô - chỉ khả dụng khi có thể lưu
            SaveStockEntryCommand = new RelayCommand<object>(
                parameter => SaveEditedStockEntry(),
                parameter => CanSaveStockEntry()
            );

            // Lệnh hủy chỉnh sửa - chỉ khả dụng khi đang chỉnh sửa
            CancelEditStockEntryCommand = new RelayCommand<object>(
                parameter => CancelEditStockEntry(),
                parameter => IsEditingStockEntry
            );
            
            // Lệnh đặt lô đang bán - chỉ khả dụng khi lô còn hàng
            SetAsSellingBatchCommand = new RelayCommand<Medicine.StockInWithRemaining>(
                parameter => SetAsSellingBatch(parameter),
                parameter => parameter != null && parameter.RemainingQuantity > 0
            );
            
            // Lệnh tiêu hủy lô - chỉ khả dụng khi có thể tiêu hủy
            TerminateStockBatchCommand = new RelayCommand<Medicine.StockInWithRemaining>(
                 parameter => TerminateStockBatch(parameter),
                 parameter => CanTerminateStockBatch(parameter)
             );
        }
        #endregion

        #region Data Loading Methods

        /// <summary>
        /// Tải thông tin liên quan như nhà cung cấp mới nhất
        /// </summary>
        private void LoadRelatedData()
        {
            if (Medicine == null) return;

            try
            {

                LatestSupplierName = Medicine.SellingStockIn.Supplier.SupplierName;

                // Lấy ngày hết hạn của lô đang bán và gán cho StockinExpiryDate
                var sellingStockIn = Medicine.SellingStockIn;
                if (sellingStockIn != null && sellingStockIn.ExpiryDate.HasValue)
                {
                    // Chuyển đổi DateOnly sang DateTime
                    StockinExpiryDate = new DateTime(
                        sellingStockIn.ExpiryDate.Value.Year,
                        sellingStockIn.ExpiryDate.Value.Month,
                        sellingStockIn.ExpiryDate.Value.Day
                    );
                }
                else
                {
                    StockinExpiryDate = null;
                }

                // Cập nhật UI
                OnPropertyChanged(nameof(LatestSupplierName));
                OnPropertyChanged(nameof(StockinExpiryDate));
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải thông tin liên quan: {ex.Message}", "Lỗi");
            }
        }
        /// <summary>
        /// Làm mới dữ liệu từ cơ sở dữ liệu mà không gây ra vòng lặp
        /// </summary>
        private void RefreshMedicineData()
        {
            try
            {
                if (Medicine == null) return;

                // Lấy dữ liệu mới nhất từ cơ sở dữ liệu
                var context = DataProvider.Instance.Context;
                var refreshedMedicine = context.Medicines
                    .Include(m => m.StockIns)
                        .ThenInclude(si => si.Supplier)
                    .Include(m => m.Category)
                    .Include(m => m.Unit)
                    .FirstOrDefault(m => m.MedicineId == Medicine.MedicineId);

                if (refreshedMedicine != null)
                {
                    // Cập nhật thuộc tính Medicine hiện tại
                    UpdateMedicineProperties(refreshedMedicine);

                    // Tạo các entry lô thuốc trực tiếp từ StockIns sử dụng RemainQuantity
                    var allStockEntries = new List<Medicine.StockInWithRemaining>();
                    var stockIns = refreshedMedicine.StockIns.OrderByDescending(si => si.ImportDate).ToList();

                    foreach (var si in stockIns)
                    {
                        // Tạo instance StockInWithRemaining mới cho mỗi StockIn
                        var stockEntry = new Medicine.StockInWithRemaining
                        {
                            StockIn = si,
                            RemainingQuantity = si.RemainQuantity,
                            IsCurrentSellingBatch = si.IsSelling
                        };
                        allStockEntries.Add(stockEntry);
                    }

                    DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>(allStockEntries);

                    // Cập nhật lựa chọn dropdown
                    UpdateDropdownSelections();

                    // Cập nhật thông tin nhà cung cấp mới nhất
                    var latestStockIn = refreshedMedicine.StockIns
                        .OrderByDescending(si => si.ImportDate)
                        .FirstOrDefault();

                    if (latestStockIn?.Supplier != null)
                    {
                        LatestSupplierName = latestStockIn.Supplier.SupplierName;
                    }
                    else
                    {
                        LatestSupplierName = "Chưa có thông tin nhà cung cấp";
                    }
                }
                else
                {
                    MessageBoxService.ShowWarning("Không tìm thấy thông tin thuốc", "Cảnh báo");
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải lại dữ liệu: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Cập nhật thuộc tính Medicine mà không kích hoạt setter
        /// </summary>
        private void UpdateMedicineProperties(Medicine refreshedMedicine)
        {
            if (Medicine == null || refreshedMedicine == null) return;

            Medicine.Name = refreshedMedicine.Name;
            Medicine.CategoryId = refreshedMedicine.CategoryId;
            Medicine.UnitId = refreshedMedicine.UnitId;
            Medicine.BarCode = refreshedMedicine.BarCode;
            Medicine.QrCode = refreshedMedicine.QrCode;
            Medicine.StockIns = refreshedMedicine.StockIns;
            Medicine.Category = refreshedMedicine.Category;
            Medicine.Unit = refreshedMedicine.Unit;

            // Thông báo UI về các thay đổi
            OnPropertyChanged(nameof(Medicine));
        }

        /// <summary>
        /// Tải dữ liệu cho các dropdown
        /// </summary>
        private void LoadDropdownData()
        {
            try
            {
                var context = DataProvider.Instance.Context;

                CategoryList = new ObservableCollection<MedicineCategory>(
                    context.MedicineCategories.Where(c => !(bool)c.IsDeleted));

                UnitList = new ObservableCollection<Unit>(
                    context.Units.Where(u => !(bool)u.IsDeleted));

                // Chỉ bao gồm nhà cung cấp vừa chưa bị xóa VÀ đang hoạt động
                SupplierList = new ObservableCollection<Supplier>(
                    context.Suppliers.Where(s => !(bool)s.IsDeleted && (bool)s.IsActive));
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu cho các ô chọn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Cập nhật các lựa chọn dropdown
        /// </summary>
        private void UpdateDropdownSelections()
        {
            if (Medicine == null) return;

            SelectedCategory = CategoryList?.FirstOrDefault(c => c.CategoryId == Medicine.CategoryId);
            SelectedUnit = UnitList?.FirstOrDefault(u => u.UnitId == Medicine.UnitId);
        }

        /// <summary>
        /// Tải chi tiết thuốc để chỉnh sửa
        /// </summary>
        private void LoadMedicineDetailsForEdit(Medicine medicine)
        {
            if (medicine == null) return;

            // Cập nhật lựa chọn dropdown
            UpdateDropdownSelections();

            // Tải chi tiết bổ sung cho StockIn
            StockinUnitPrice = medicine.CurrentUnitPrice;
            StockinSellPrice = medicine.CurrentSellPrice;

            // Lấy thông tin margin lợi nhuận từ lô nhập gần nhất
            var latestStockIn = medicine.StockIns?.OrderByDescending(si => si.ImportDate).FirstOrDefault();
            StockProfitMargin = latestStockIn?.ProfitMargin ?? 20; // Mặc định 20% nếu không tìm thấy

            // Lấy ngày nhập gần nhất
            ImportDate = medicine.LatestImportDate ?? DateTime.Now;

     
        }
        #endregion

        #region Command Methods
        /// <summary>
        /// Bắt đầu chỉnh sửa lô thuốc được chọn
        /// </summary>
        private void BeginEditStockEntry(Medicine.StockInWithRemaining stockEntry)
        {
            if (stockEntry == null) return;

            CurrentStockEntry = stockEntry;

            // Khởi tạo các trường chỉnh sửa với giá trị hiện tại
            EditStockQuantity = stockEntry.StockIn.Quantity;
            EditRemainQuantity = stockEntry.StockIn.RemainQuantity;
            EditUnitPrice = stockEntry.StockIn.UnitPrice;
            EditProfitMargin = stockEntry.StockIn.ProfitMargin;
            EditSellPrice = stockEntry.StockIn.SellPrice ?? 0;
            EditSupplier = SupplierList.FirstOrDefault(s => s.SupplierId == stockEntry.StockIn.SupplierId);

            // Chuyển đổi DateOnly sang DateTime cho date picker
            EditExpiryDate = stockEntry.StockIn.ExpiryDate.HasValue ?
                new DateTime(stockEntry.StockIn.ExpiryDate.Value.Year, stockEntry.StockIn.ExpiryDate.Value.Month, stockEntry.StockIn.ExpiryDate.Value.Day) :
                null;

            IsEditingStockEntry = true;
        }

        /// <summary>
        /// Kiểm tra xem có thể lưu lô thuốc đang chỉnh sửa hay không
        /// </summary>
        private bool CanSaveStockEntry()
        {
            return IsEditingStockEntry &&
                   SelectedStockEntry != null &&
                   EditStockQuantity > 0 &&
                   EditRemainQuantity >= 0 &&
                   EditStockQuantity >= EditRemainQuantity &&
                   EditUnitPrice > 0 &&
                   EditSellPrice > 0 &&
                   EditSupplier != null;
        }

        /// <summary>
        /// Lưu thông tin lô thuốc đã chỉnh sửa
        /// </summary>
        private void SaveEditedStockEntry()
        {
            if (SelectedStockEntry == null || !IsEditingStockEntry) return;

            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
            {
                try
                {
                    var dbContext = DataProvider.Instance.Context;

                    var stockInToUpdate = dbContext.StockIns.Find(SelectedStockEntry.StockIn.StockInId);

                    if (stockInToUpdate != null)
                    {
                        // Tính toán sự khác biệt về số lượng
                        int quantityDiff = EditStockQuantity - stockInToUpdate.Quantity;
                        int remainQuantityDiff = EditRemainQuantity - stockInToUpdate.RemainQuantity;

                        // Kiểm tra xem lô này có thể sử dụng được dựa trên ngày hết hạn
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var minimumExpiryDate = today.AddDays(Medicine.MinimumDaysBeforeExpiry);

                        // Chuyển đổi EditExpiryDate sang DateOnly nếu có giá trị
                            DateOnly? expiryDateOnly = EditExpiryDate.HasValue ?
                            DateOnly.FromDateTime(EditExpiryDate.Value) : null;

                        bool isUsable = !expiryDateOnly.HasValue || expiryDateOnly.Value >= minimumExpiryDate;

                        // Cập nhật bản ghi StockIn
                        stockInToUpdate.Quantity = EditStockQuantity;
                        stockInToUpdate.RemainQuantity = EditRemainQuantity;
                        stockInToUpdate.UnitPrice = EditUnitPrice;
                        stockInToUpdate.SellPrice = EditSellPrice;
                        stockInToUpdate.ProfitMargin = EditProfitMargin;
                        stockInToUpdate.TotalCost = EditUnitPrice * EditStockQuantity;
                        stockInToUpdate.ExpiryDate = expiryDateOnly;

                        // Cập nhật nhà cung cấp nếu thay đổi
                        if (EditSupplier != null)
                        {
                            stockInToUpdate.SupplierId = EditSupplier.SupplierId;
                        }

                        // Cập nhật bản ghi Stock nếu số lượng thay đổi
                        if (quantityDiff != 0 || remainQuantityDiff != 0)
                        {
                            var stockToUpdate = dbContext.Stocks
                                .FirstOrDefault(s => s.MedicineId == stockInToUpdate.MedicineId);

                            if (stockToUpdate != null)
                            {
                                stockToUpdate.Quantity += remainQuantityDiff; // Cập nhật theo sự khác biệt về số lượng còn lại
                                if (isUsable)
                                    stockToUpdate.UsableQuantity += remainQuantityDiff;
                                stockToUpdate.LastUpdated = DateTime.Now;
                            }
                        }

                        // Lưu các thay đổi vào cơ sở dữ liệu
                        dbContext.SaveChanges();

                        // Hoàn thành giao dịch nếu mọi thứ thành công
                        transaction.Commit();

                        MessageBoxService.ShowSuccess(
                            "Thông tin lô nhập kho đã được cập nhật thành công!",
                            "Thành công"
                        );

                        // Làm mới dữ liệu sau khi lưu
                        RefreshMedicineData();
                        IsEditingStockEntry = false;
                    }
                    else
                    {
                        MessageBoxService.ShowError(
                            "Không tìm thấy lô nhập kho trong cơ sở dữ liệu.",
                            "Lỗi"
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Hoàn tác giao dịch nếu có lỗi xảy ra
                    transaction.Rollback();

                    // Ghi log lỗi
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu thông tin lô thuốc: {ex.Message}");

                    MessageBoxService.ShowError(
                        $"Lỗi khi lưu thông tin lô nhập kho: {ex.Message}",
                        "Lỗi"
                    );
                }
            }
        }

        /// <summary>
        /// Hủy chỉnh sửa lô thuốc và quay về chế độ xem
        /// </summary>
        private void CancelEditStockEntry()
        {
            IsEditingStockEntry = false;
        }

        /// <summary>
        /// Đặt lô được chọn làm lô đang bán
        /// </summary>
        private void SetAsSellingBatch(Medicine.StockInWithRemaining stockEntry)
        {
            if (stockEntry == null) return;

            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
            {
                try
                {
                    var context = DataProvider.Instance.Context;

                    var stockInToUpdate = context.StockIns.Find(stockEntry.StockIn.StockInId);
                    if (stockInToUpdate != null)
                    {
                        // Kiểm tra nếu lô này đã được đánh dấu là đang bán
                        if (stockInToUpdate.IsSelling)
                        {
                            var result = MessageBoxService.ShowQuestion(
                                "Lô thuốc này đã được đánh dấu là lô đang bán. Bạn có chắc muốn hủy đánh dấu không?",
                                "Xác nhận hủy đánh dấu"
                            );
                            if (!result)
                                return;
                            // Nếu người dùng xác nhận, bỏ đánh dấu lô này
                            stockInToUpdate.IsSelling = false;
                            // Lưu thay đổi vào cơ sở dữ liệu
                            context.SaveChanges();

                            // Hoàn thành giao dịch nếu mọi thứ thành công
                            transaction.Commit();

                            MessageBoxService.ShowInfo(
                                "Đã hủy đánh dấu lô thuốc này là lô đang bán.",
                                "Thông báo"
                            );
                            // Cập nhật lại thông tin hiển thị
                            RefreshMedicineData();
                            return;

                        }
                        else
                        {
                            // Reset tất cả các lô thuốc của medicine này về false
                            var allBatches = context.StockIns
                            .Where(si => si.MedicineId == Medicine.MedicineId)
                            .ToList();

                            foreach (var batch in allBatches)
                            {
                                batch.IsSelling = false;
                            }
                            stockInToUpdate.IsSelling = true;

                            // Cập nhật trạng thái trong UI trước khi lưu database
                            foreach (var item in DetailedStockList)
                            {
                                item.IsCurrentSellingBatch = (item.StockIn.StockInId == stockEntry.StockIn.StockInId);
                            }

                            // Kiểm tra nếu lô này gần hết hạn hoặc đã hết hạn
                            if (stockEntry.IsExpired)
                            {
                                MessageBoxService.ShowWarning(
                                    "Bạn đang chọn một lô thuốc đã hết hạn làm lô đang bán.\n" +
                                    "Việc này có thể ảnh hưởng đến an toàn thuốc.",
                                    "Cảnh báo: Lô hết hạn"
                                );
                            }
                            else if (stockEntry.IsNearExpiry)
                            {
                                MessageBoxService.ShowWarning(
                                    "Bạn đang chọn một lô thuốc sắp hết hạn làm lô đang bán.\n" +
                                    $"Lô thuốc này sẽ hết hạn vào ngày {stockEntry.ExpiryDate:dd/MM/yyyy}.",
                                    "Cảnh báo: Lô gần hết hạn"
                                );
                            }

                            // Lưu thay đổi vào cơ sở dữ liệu
                            context.SaveChanges();

                            // Hoàn thành giao dịch nếu mọi thứ thành công
                            transaction.Commit();

                            MessageBoxService.ShowSuccess(
                                $"Đã đặt lô thuốc (mã: {stockInToUpdate.StockInId}) làm lô đang bán.",
                                "Thành công"
                            );

                            // Cập nhật lại thông tin hiển thị
                            RefreshMedicineData();
                        }
                    }
                    else
                    {
                        MessageBoxService.ShowError(
                            "Không tìm thấy lô thuốc trong cơ sở dữ liệu.",
                            "Lỗi"
                        );
                    }
                }
                catch (Exception ex)
                {
                    // Hoàn tác giao dịch nếu có lỗi xảy ra
                    transaction.Rollback();

                    MessageBoxService.ShowError($"Lỗi khi cập nhật lô thuốc đang bán: {ex.Message}", "Lỗi");
                }
            }
        }

        /// <summary>
        /// Kiểm tra xem có thể tiêu hủy lô thuốc hay không
        /// </summary>
        private bool CanTerminateStockBatch(Medicine.StockInWithRemaining stockEntry)
        {
            return stockEntry != null &&
                   !stockEntry.StockIn.IsTerminated &&
                   stockEntry.RemainingQuantity > 0;
        }

        /// <summary>
        /// Tiêu hủy lô thuốc (đánh dấu IsTerminated = true)
        /// </summary>
        private void TerminateStockBatch(Medicine.StockInWithRemaining stockEntry)
        {
            if (stockEntry == null) return;

            // Xác nhận với người dùng
            bool confirm = MessageBoxService.ShowQuestion(
                $"Bạn có chắc chắn muốn tiêu hủy lô thuốc này không?\n" +
                $"Lô nhập ngày: {stockEntry.ImportDate:dd/MM/yyyy}\n" +
                $"Số lượng còn lại: {stockEntry.RemainingQuantity}\n\n" +
                "Hành động này không thể hoàn tác!",
                "Xác nhận tiêu hủy"
            );

            if (!confirm) return;

            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
            {
                try
                {
                    var context = DataProvider.Instance.Context;

                    // Tìm lô thuốc cần tiêu hủy
                    var stockInToTerminate = context.StockIns.Find(stockEntry.StockIn.StockInId);

                    if (stockInToTerminate == null)
                    {
                        MessageBoxService.ShowError(
                            "Không tìm thấy lô thuốc trong cơ sở dữ liệu.",
                            "Lỗi"
                        );
                        return;
                    }

                    // Đánh dấu lô thuốc là đã tiêu hủy
                    stockInToTerminate.IsTerminated = true;

                    // Nếu lô này đang là lô đang bán, reset lại để không bán lô này nữa
                    if (stockInToTerminate.IsSelling)
                    {
                        stockInToTerminate.IsSelling = false;

                        // Tìm lô khác có thể bán để chuyển đến
                        var availableBatch = context.StockIns
                            .Where(si => si.MedicineId == Medicine.MedicineId &&
                                         si.StockInId != stockInToTerminate.StockInId &&
                                         si.RemainQuantity > 0 &&
                                         !si.IsTerminated)
                            .OrderBy(si => si.ImportDate)  // FIFO
                            .FirstOrDefault();

                        if (availableBatch != null)
                        {
                            availableBatch.IsSelling = true;

                            // Hiển thị thông báo
                            MessageBoxService.ShowWarning(
                                $"Lô vừa tiêu hủy là lô đang bán. Hệ thống đã tự động chuyển sang lô mới " +
                                $"(Mã: {availableBatch.StockInId}, ngày nhập: {availableBatch.ImportDate:dd/MM/yyyy}).",
                                "Thông báo"
                            );
                        }
                    }

                    // Cập nhật số lượng trong bảng Stock
                    var stockRecord = context.Stocks
                        .FirstOrDefault(s => s.MedicineId == stockInToTerminate.MedicineId);

                    if (stockRecord != null)
                    {
                        // Cập nhật số lượng tồn kho vật lý và có thể sử dụng
                        stockRecord.Quantity -= stockInToTerminate.RemainQuantity;
                        stockRecord.UsableQuantity -= stockInToTerminate.RemainQuantity;
                        stockRecord.LastUpdated = DateTime.Now;
                    }

                    // Lưu thay đổi vào cơ sở dữ liệu
                    context.SaveChanges();

                    // Hoàn thành giao dịch
                    transaction.Commit();

                    MessageBoxService.ShowSuccess(
                        $"Đã tiêu hủy lô thuốc (mã: {stockInToTerminate.StockInId}) thành công!",
                        "Thành công"
                    );

                    // Cập nhật lại thông tin hiển thị
                    RefreshMedicineData();
                }
                catch (Exception ex)
                {
                    // Hoàn tác giao dịch nếu có lỗi xảy ra
                    transaction.Rollback();

                    MessageBoxService.ShowError(
                        $"Lỗi khi tiêu hủy lô thuốc: {ex.Message}",
                        "Lỗi"
                    );
                }
            }
        }

        #endregion
    }
}
