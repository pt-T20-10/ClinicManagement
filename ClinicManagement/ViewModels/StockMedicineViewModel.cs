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
    public class StockMedicineViewModel : BaseViewModel, IDataErrorInfo
    {
     public class StockViewModel
{
    public Medicine Medicine { get; set; }
    public string Status { get; set; }
    public string StatusColor { get; set; }
    public string StatusMessage { get; set; }

    // Thuộc tính bổ sung để hỗ trợ UI
    public bool ShowLastBatchExpiryWarning => 
        Medicine?.IsLastestStockIn == true && Medicine?.HasNearExpiryStock == true;

    public bool ShowLastBatchQuantityWarning => 
        Medicine?.IsLastestStockIn == true && 
        (Medicine?.TotalPhysicalStockQuantity ?? 0) <= 10; // Giả sử 10 là ngưỡng tồn kho thấp
}

        private bool IsLoading;
        public string Error => null;


        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false; // Flag to control when to validate

        #region Properties
        #region Initial Data
        // Thêm thuộc tính mới để hiển thị trạng thái lô
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

        public int TempQuantity { get; set; }

        public string TotalQuantity => IsMonthlyView
    ? MonthlyTotalQuantity.ToString()
    : CurrentTotalQuantity.ToString();

        public string TotalValue => IsMonthlyView
            ? MonthlyTotalValue.ToString("N0")
            : CurrentTotalValue.ToString("N0");
        #endregion

        #region StockProperties

        private ObservableCollection<Stock> _allStockMedicine;
        // Thuộc tính tìm kiếm
        private string _SearchStockMedicine;
        public string SearchStockMedicine
        {
            get => _SearchStockMedicine;
            set
            {
                _SearchStockMedicine = value;
                OnPropertyChanged();

                // Có thể bỏ auto-filter ở đây nếu muốn chỉ filter khi nhấn nút tìm kiếm
                // Còn nếu muốn auto-filter khi nhập, giữ nguyên dòng bên dưới
                if (IsMonthlyView)
                    FilterMonthlyStock();
                else
                    FilterCurrentStock();
            }
        }

        private Unit _SelectedStockUnit;
        public Unit SelectedStockUnit
        {
            get => _SelectedStockUnit;
            set
            {
                _SelectedStockUnit = value;
                OnPropertyChanged();
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
        private int? _SelectedStockUnitId;
        public int? SelectedStockUnitId
        {
            get => _SelectedStockUnitId;
            set
            {
                _SelectedStockUnitId = value;
                OnPropertyChanged();

                // Lọc theo chế độ xem hiện tại
                if (IsMonthlyView)
                    FilterMonthlyStock();
                else
                    FilterCurrentStock();
            }
        }
        private Supplier _SelectedStockSupplier;
        public Supplier SelectedStockSupplier
        {
            get => _SelectedStockSupplier;
            set
            {
                _SelectedStockSupplier = value;
                OnPropertyChanged();
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

        private int? _SelectedStockSupplierId;
        public int? SelectedStockSupplierId
        {
            get => _SelectedStockSupplierId;
            set
            {
                _SelectedStockSupplierId = value;
                OnPropertyChanged();

                // Lọc theo chế độ xem hiện tại
                if (IsMonthlyView)
                    FilterMonthlyStock();
                else
                    FilterCurrentStock();
            }
        }


        private MedicineCategory _SelectedStockCategoryName;
        public MedicineCategory SelectedStockCategoryName
        {
            get => _SelectedStockCategoryName;
            set
            {
                _SelectedStockCategoryName = value;
                OnPropertyChanged();
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
        private int? _SelectedStockCategoryId;
        public int? SelectedStockCategoryId
        {
            get => _SelectedStockCategoryId;
            set
            {
                _SelectedStockCategoryId = value;
                OnPropertyChanged(nameof(_SelectedStockCategoryId));

                // Lọc theo chế độ xem hiện tại
                if (IsMonthlyView)
                    FilterMonthlyStock();
                else
                    FilterCurrentStock();
            }
        }



        #endregion

        #region Monthly Stock Properties

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

        // Properties để quản lý tổng số lượng và giá trị theo từng chế độ xem
        private int _monthlyTotalQuantity;
        public int MonthlyTotalQuantity
        {
            get => _monthlyTotalQuantity;
            set
            {
                _monthlyTotalQuantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalQuantity));
            }
        }

        private decimal _monthlyTotalValue;
        public decimal MonthlyTotalValue
        {
            get => _monthlyTotalValue;
            set
            {
                _monthlyTotalValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalValue));
            }
        }

        private int _currentTotalQuantity;
        public int CurrentTotalQuantity
        {
            get => _currentTotalQuantity;
            set
            {
                _currentTotalQuantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalQuantity));
            }
        }

        private decimal _currentTotalValue;
        public decimal CurrentTotalValue
        {
            get => _currentTotalValue;
            set
            {
                _currentTotalValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalValue));
            }
        }
  

        #endregion

        #region UnitProperties

        private string _UnitName;
        public string UnitName
        {
            get => _UnitName;
            set
            {
                _UnitName = value;
                OnPropertyChanged();
            }
        }

        private string _UnitDescription;
        public string UnitDescription
        {
            get => _UnitDescription;
            set
            {
                _UnitDescription = value;
                OnPropertyChanged();
            }
        }

        private Unit _SelectedUnit;
        public Unit SelectedUnit
        {
            get => _SelectedUnit;
            set
            {
                _SelectedUnit = value;
                OnPropertyChanged(nameof(_SelectedUnit));

                if (SelectedUnit != null)
                {
                    UnitName = SelectedUnit.UnitName;
                    UnitDescription = SelectedUnit.Description;
                }
            }
        }

        #endregion#region UnitProperties

        #region CategoryProperties

        private string _CategoryName;
        public string CategoryName
        {
            get => _CategoryName;
            set
            {
                _CategoryName = value;
                OnPropertyChanged();
            }
        }

        private string _CategoryDescription;
        public string CategoryDescription
        {
            get => _CategoryDescription;
            set
            {
                _CategoryDescription = value;
                OnPropertyChanged();
            }
        }

        private MedicineCategory _SelectedCategory;
        public MedicineCategory SelectedCategory
        {
            get => _SelectedCategory;
            set
            {
                _SelectedCategory = value;
                OnPropertyChanged(nameof(_SelectedCategory));

                if (SelectedCategory != null)
                {
                    CategoryName = SelectedCategory.CategoryName;
                    CategoryDescription = SelectedCategory.Description;
                }
            }
        }

        #endregion

        #region SupplierProperties
       
        private Supplier _SelectedSupplier;
        public Supplier SelectedSupplier
        {
            get => _SelectedSupplier;
            set
            {
                _SelectedSupplier = value;
                OnPropertyChanged(nameof(_SelectedSupplier));

                if (SelectedSupplier != null)
                {
                    SupplierCode = SelectedSupplier.SupplierCode;
                    SupplierAddress = SelectedSupplier.Address;
                    SupplierPhone = SelectedSupplier.Phone;
                    SupplierEmail = SelectedSupplier.Email;
                    SupplierName = SelectedSupplier.SupplierName;
                    SupplierTaxCode = SelectedSupplier.TaxCode;
                    ContactPerson = SelectedSupplier.ContactPerson;
                    if(SelectedSupplier.IsActive == true)
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
        private string _supplierCode;
        public string SupplierCode
        {
            get => _supplierCode;
            set
            {
                if (_supplierCode != value)
                {
                    // Mark field as touched when user starts typing
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_supplierCode))
                        _touchedFields.Add(nameof(SupplierCode));
                    else
                        _touchedFields.Remove(nameof(SupplierCode));

                    _supplierCode = value;
                    OnPropertyChanged(nameof(SupplierCode));
                }
            }
        }

        private string _supplierName;
        public string SupplierName
        {
            get => _supplierName;
            set
            {
                if (_supplierName != value)
                {
                    // Mark field as touched when user starts typing
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_supplierName))
                        _touchedFields.Add(nameof(SupplierName));
                    else
                        _touchedFields.Remove(nameof(SupplierName));

                    _supplierName = value;
                    OnPropertyChanged(nameof(SupplierName));
                }
            }
        }

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

        private string _supplierPhone;
        public string SupplierPhone
        {
            get => _supplierPhone;
            set
            {
                if (_supplierPhone != value)
                {
                    // Mark field as touched when user starts typing
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_supplierPhone))
                        _touchedFields.Add(nameof(SupplierPhone));
                    else
                        _touchedFields.Remove(nameof(SupplierPhone));

                    _supplierPhone = value;
                    OnPropertyChanged(nameof(SupplierPhone));
                }
            }
        }

        private string _supplierEmail;
        public string SupplierEmail
        {
            get => _supplierEmail;
            set
            {
                if (_supplierEmail != value)
                {
                    // Mark field as touched when user starts typing
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_supplierEmail))
                        _touchedFields.Add(nameof(SupplierEmail));
                    else
                        _touchedFields.Remove(nameof(SupplierEmail));

                    _supplierEmail = value;
                    OnPropertyChanged(nameof(SupplierEmail));
                }
            }
        }

        private string _supplierTaxCode;
        public string SupplierTaxCode
        {
            get => _supplierTaxCode;
            set
            {
                if (_supplierTaxCode != value)
                {
                    // Mark field as touched when user starts typing
                    if (!string.IsNullOrEmpty(value) || !string.IsNullOrEmpty(_supplierTaxCode))
                        _touchedFields.Add(nameof(SupplierTaxCode));
                    else
                        _touchedFields.Remove(nameof(SupplierTaxCode));

                    _supplierTaxCode = value;
                    OnPropertyChanged(nameof(SupplierTaxCode));
                }
            }
        }

        // Nếu cần thêm Address (thường có trong thông tin nhà cung cấp)
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


        // Trạng thái hoạt động
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
       // Trạng thái không hoạt động
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

        // Private backing fields for supplier filters
        private ObservableCollection<Supplier> _allSuppliers;
        private string _supplierSearchText;

        // Public property for supplier search text with auto-filtering
        public string SupplierSearchText
        {
            get => _supplierSearchText;
            set
            {
                _supplierSearchText = value;
                OnPropertyChanged();
                // Auto-filter when text changes
                ExecuteAutoSupplierFilter();
            }
        }

        // Supplier filter flags with auto-filtering
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

        public string DialogTitle => CurrentMedicine != null
            ? $"Chi tiết các lô thuốc: {CurrentMedicine.Name}"
            : "Chi tiết lô thuốc";

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

        private string _stockinMedicineName;
        public string StockinMedicineName
        {
            get => _stockinMedicineName;
            set
            {
                _stockinMedicineName = value;
                OnPropertyChanged();
            }
        }

        // Add new properties for BarCode and QRCode
        private string _stockinBarCode;
        public string StockinBarCode
        {
            get => _stockinBarCode;
            set
            {
                _stockinBarCode = value;
                OnPropertyChanged();
            }
        }

        private string _stockinQrCode;
        public string StockinQrCode
        {
            get => _stockinQrCode;
            set
            {
                _stockinQrCode = value;
                OnPropertyChanged();
            }
        }

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

        private decimal _stockinUnitPrice;
        public decimal StockinUnitPrice
        {
            get => _stockinUnitPrice;
            set
            {
                _stockinUnitPrice = value;
                OnPropertyChanged();
                
                // If profit margin is set, recalculate sell price
                if (_StockProfitMargin > 0)
                {
                    // Calculate sell price based on unit price and profit margin
                    _stockinSellPrice = _stockinUnitPrice * (1 + (_StockProfitMargin / 100));
                    OnPropertyChanged(nameof(StockinSellPrice));
                }
                else if (_stockinSellPrice > 0)
                {
                    // Calculate profit margin based on unit price and sell price
                    CalculateProfitMargin();
                }
            }
        }

        private decimal _StockProfitMargin;
        public decimal StockProfitMargin
        {
            get => _StockProfitMargin;
            set
            {
                _StockProfitMargin = value;
                OnPropertyChanged();
                
                // If unit price is set, recalculate sell price
                if (_stockinUnitPrice > 0)
                {
                    // Calculate sell price based on unit price and profit margin
                    _stockinSellPrice = _stockinUnitPrice * (1 + (_StockProfitMargin / 100));
                    OnPropertyChanged(nameof(StockinSellPrice));
                }
                else if (_stockinSellPrice > 0)
                {
                    // Calculate unit price based on sell price and profit margin
                    _stockinUnitPrice = _stockinSellPrice / (1 + (_StockProfitMargin / 100));
                    OnPropertyChanged(nameof(StockinUnitPrice));
                }
            }
        }

        private decimal _stockinSellPrice;
        public decimal StockinSellPrice
        {
            get => _stockinSellPrice;
            set
            {
                _stockinSellPrice = value;
                OnPropertyChanged();
                
                // If unit price is set, recalculate profit margin
                if (_stockinUnitPrice > 0)
                {
                    CalculateProfitMargin();
                }
            }
        }

        // Helper method to calculate profit margin based on unit price and sell price
        private void CalculateProfitMargin()
        {
            if (_stockinUnitPrice > 0 && _stockinSellPrice > 0)
            {
                // Calculate profit margin percentage
                _StockProfitMargin = ((_stockinSellPrice / _stockinUnitPrice) - 1) * 100;
                OnPropertyChanged(nameof(StockProfitMargin));
            }
        }

        private DateTime? _stockinExpiryDate = DateTime.Now.AddYears(3);
        public DateTime? StockinExpiryDate
        {
            get => _stockinExpiryDate;
            set
            {
                _stockinExpiryDate = value;
                OnPropertyChanged();
            }
        }

        private Stock _selectedMedicine;
        public Stock SelectedMedicine
        {
            get => _selectedMedicine;
            set
            {
                _selectedMedicine = value;
                OnPropertyChanged();
                if (value != null)
                {
                    LoadMedicineDetailsForStockIn(value.Medicine);
                }
            }
        }
        private DateTime? _importDate = DateTime.Now; // Default to current date
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
        // Add properties for the current account and role-based permissions
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                // Update permissions when account changes
                UpdatePermissions();
            }
        }

        // Property to check if user can edit stock
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

        // Property to check if user can add new medicines
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

        // Property to check if user can manage categories and units
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
        public ICommand AddUnitCommand { get; set; }
        public ICommand EditUnitCommand { get; set; }
        public ICommand DeleteUnitCommand { get; set; }
        public ICommand RefreshUnitCommand { get; set; }
        

        // Supplier Commands
        public ICommand AddSupplierCommand { get; set; }
        public ICommand EditSupplierCommand { get; set; }
        public ICommand DeleteSupplierCommand { get; set; }
        public ICommand SetActiveStatusCommand { get; set; }
        public ICommand SetNoActiveStatusCommand { get; set; }
        public ICommand SearchSupplierCommand { get; set; }
        public ICommand RefreshSulpierCommand { get; set; }


        // Category Commands
        public ICommand AddCategoryCommand { get; set; }
        public ICommand EditCategoryCommand { get; set; }
        public ICommand DeleteCategoryCommand { get; set; }
        public ICommand RefreshCatergoryCommand { get; set; }


        public ICommand OpenDoctorDetailsCommand { get; set; }

        // Specialty Commands
        public ICommand AddCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
      
        //StockMedicine Commands
        public ICommand SearchStockMedicineCommand { get; set; }
        public ICommand ResetStockFiltersCommand { get; set; }
        public ICommand DeleteMedicine { get; set; }
        public ICommand ExportStockExcelCommand { get;  set; }
        public ICommand ManualGenerateMonthlyStockCommand { get; private set; }
        public ICommand UndoMonthlyStockCommand { get; private set; }

        //StockIn Commands
        public ICommand AddNewMedicineCommand { get; set; }
        public ICommand RestartCommand { get; set; }
        public ICommand ShowMedicineDetailsCommand { get; set; }

        public ICommand LoadedUCCommand { get; set; }

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
        #region InitializeCommands
        private void InitializeCommands()
        {
            LoadedUCCommand = new RelayCommand<UserControl>(
            (userControl) => {
                if (userControl != null)
                {
                    // Get the MainViewModel from Application resources
                    var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                    if (mainVM != null && mainVM.CurrentAccount != null)
                    {
                        // Update current account
                        CurrentAccount = mainVM.CurrentAccount;
                        CheckMonthlyStockReminder();
                    }

                    // Run the expiry date check when loading the view
                    //UpdateUsableQuantitiesBasedOnExpiryDates();
                }
            },
            (userControl) => true
        );
            ManualGenerateMonthlyStockCommand = new RelayCommand<object>(
               (p) => ExecuteManualGenerateMonthlyStock(),
               (p) => CanManualGenerateMonthlyStock()
                );

            UndoMonthlyStockCommand = new RelayCommand<object>(
               (p) => ExecuteUndoMonthlyStock(),
               (p) => CanUndoMonthlyStock()
            );
            // Unit Commands
            AddUnitCommand = new RelayCommand<object>(
                (p) => AddUnit(),
                (p) => !string.IsNullOrEmpty(UnitName) && CanManageSettings
            );

            EditUnitCommand = new RelayCommand<object>(
                (p) => EditUnit(),
                (p) => SelectedUnit != null && !string.IsNullOrEmpty(UnitName) && CanManageSettings
            );

            DeleteUnitCommand = new RelayCommand<object>(
                (p) => DeleteUnit(),
                (p) => SelectedUnit != null && CanManageSettings
            );

            RefreshUnitCommand = new RelayCommand<object>(
              (p) => ExecuteUnitRefresh(),
              (p) => true
            );

            // Category Commands
            AddCategoryCommand = new RelayCommand<object>(
                (p) => AddCategory(),
                (p) => !string.IsNullOrEmpty(CategoryName) && CanManageSettings
            );

            EditCategoryCommand = new RelayCommand<object>(
                (p) => EditCategory(),
                (p) => SelectedCategory != null && !string.IsNullOrEmpty(CategoryName) && CanManageSettings
            );

            DeleteCategoryCommand = new RelayCommand<object>(
                (p) => DeleteCategory(),
                (p) => SelectedCategory != null && CanManageSettings
            );

            RefreshCatergoryCommand = new RelayCommand<object>(
                  (p) => ExecuteCategoryRefresh(),
                  (p) => true
              );

            // Supplier Commands
            AddSupplierCommand = new RelayCommand<object>(
                (p) => AddSupplier(),
                (p) => !string.IsNullOrEmpty(SupplierName) && CanManageSettings
            );

            EditSupplierCommand = new RelayCommand<object>(
                (p) => EditSupplier(),
                (p) => SelectedSupplier != null && !string.IsNullOrEmpty(SupplierName) && CanManageSettings
            );

            DeleteSupplierCommand = new RelayCommand<object>(
                (p) => DeleteSupplier(),
                (p) => SelectedSupplier != null && CanManageSettings
            );

            SetActiveStatusCommand = new RelayCommand<object>(
                (p) => {
                    IsActive = true;
                    IsNotActive = false;
                },
                (p) => CanManageSettings
            );

            SetNoActiveStatusCommand = new RelayCommand<object>(
                (p) => {
                    IsActive = false;
                    IsNotActive = true;
                },
                (p) => CanManageSettings
            );

            RefreshSulpierCommand = new RelayCommand<object>(
                (p) => ExecuteSupplierRefresh(),
                (p) => true
            );

            // Initialize supplier filter commands
            SearchSupplierCommand = new RelayCommand<object>(
                p => FilterSuppliers(),
                p => true
            );

            RefreshSulpierCommand = new RelayCommand<object>(
                p => ResetSupplierFilters(),
                p => true
            );

            ShowAllSuppliers = true;

            //StockMedicine Commands
            SearchStockMedicineCommand = new RelayCommand<object>(
                (p) => {
                    // Thực hiện tìm kiếm theo chế độ xem hiện tại
                    if (IsMonthlyView)
                        FilterMonthlyStock();
                    else
                        FilterCurrentStock();
                },
                (p) => true
            );

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

            // In the constructor, initialize the command:
            ExportStockExcelCommand = new RelayCommand<object>(
                p => ExportToExcel(),
                p => IsMonthlyView
                    ? MonthlyStockList?.Count > 0
                    : ListStockMedicine?.Count > 0
            );

            // Add these lines to the InitializeCommands method
            AddNewMedicineCommand = new RelayCommand<object>(
                (p) => ExecuteAddNewMedicine(),
                (p) => CanExecuteAddNewMedicine() && CanAddNewMedicine
            );

            RestartCommand = new RelayCommand<object>(
                (p) => ExecuteRestart(),
                (p) => true
            );

            ShowMedicineDetailsCommand = new RelayCommand<Medicine>(
                (medicine) => {
                    if (medicine != null)
                    {
                        var detailsWindow = new MedicineDetailsWindow(medicine);
                        detailsWindow.ShowDialog();
                        LoadData(); // Reload data after closing the details window   
                    }
                },
                (medicine) => medicine != null
            );

            DeleteMedicine = new RelayCommand<Medicine>(
                (medicine) => ExecuteDeleteMedicine(medicine),
                (medicine) => CanDeleteMedicine(medicine) && CanEdit
            );
        }

        
    

       
        #endregion

        #region Unit Methods
        private void ExecuteUnitRefresh()
        {
            UnitList = new ObservableCollection<Unit>(
                   DataProvider.Instance.Context.Units
                       .Where(s => (bool)!s.IsDeleted)
                       .ToList()
               );
            SelectedUnit = null;
            UnitName = "";
            UnitDescription = "";
        }
        private void AddUnit()
        {
            try
            {
                // Confirm dialog
                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn thêm đơn vị '{UnitName}' không?",
                    "Xác Nhận Thêm"
                     
                      );

                if ( !result)
                    return;

                // Check if specialty already exists
                bool isExist = DataProvider.Instance.Context.Units
                    .Any(s => s.UnitName.Trim().ToLower() == UnitName.Trim().ToLower() && (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBoxService.ShowWarning("Đơn vị này đã tồn tại.");
                    return;
                }

                // Add new specialty
                var newUnit = new Unit
                {
                    UnitName = UnitName,
                    Description = UnitDescription ?? "",
                    IsDeleted = false
                };

                DataProvider.Instance.Context.Units.Add(newUnit);
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                UnitList = new ObservableCollection<Unit>(
                    DataProvider.Instance.Context.Units
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Clear fields
                SelectedUnit = null;
                UnitName = ""; 
                UnitDescription = "";

                MessageBoxService.ShowSuccess(
                    "Đã thêm Đơn vị thành công!",
                    "Thành công"
                     
                      );
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể thêm Đơn vị: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu"
                     
                     );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }

        private void EditUnit()
        {
            try
            {
                // Confirm dialog
                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa đơn vị '{SelectedUnit.UnitName}' thành '{UnitName}' không?",
                    "Xác Nhận Sửa"
                     
                      );

                if ( !result)
                    return;

                // Check if specialty name already exists (except for current)
                bool isExist = DataProvider.Instance.Context.Units
                    .Any(s => s.UnitName.Trim().ToLower() == UnitName.Trim().ToLower() &&
                              s.UnitId != SelectedUnit.UnitId &&
                             (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBoxService.ShowWarning("Tên đơn vị này đã tồn tại.");
                    return;
                }

                // Update specialty
                var unitToUpdate = DataProvider.Instance.Context.Units
                    .FirstOrDefault(s => s.UnitId == SelectedUnit.UnitId);

                if (unitToUpdate == null)
                {
                    MessageBoxService.ShowWarning("Không tìm thấy đơn vị cần sửa.");
                    return;
                }

                unitToUpdate.UnitName = UnitName;
                unitToUpdate.Description = UnitDescription ?? "";
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                UnitList = new ObservableCollection<Unit>(
                    DataProvider.Instance.Context.Units
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Update doctor list as specialty names may have changed
                LoadData();

                MessageBoxService.ShowSuccess(
                    "Đã cập nhật đơn vị thành công!",
                    "Thành Công"
                     
                      );
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể sửa đơn vị: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu"
                     
                     );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }

        private void DeleteUnit()
        {
            try
            {
         
                // Confirm deletion
                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn xóa Đơn vị '{SelectedUnit.UnitName}' không?",
                    "Xác nhận xóa"
                     
                      );

                if ( !result)
                    return;

                // Soft delete the specialty
                var specialtyToDelete = DataProvider.Instance.Context.Units
                    .FirstOrDefault(s => s.UnitId == SelectedUnit.UnitId);

                if (specialtyToDelete == null)
                {
                    MessageBoxService.ShowWarning("Không tìm thấy Đơn vị cần xóa.");
                    return;
                }

                specialtyToDelete.IsDeleted = true;
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                UnitList = new ObservableCollection<Unit>(
                    DataProvider.Instance.Context.Units
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Clear selection and fields
                SelectedUnit = null;
                UnitName = "";
                UnitDescription = "";

                MessageBoxService.ShowSuccess(
                    "Đã xóa đơn vị thành công.",
                    "Thành công"
                     
                      );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa đơn vị: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }
        #endregion

        #region Category Methods
        private void ExecuteCategoryRefresh()
        {
            CategoryList = new ObservableCollection<MedicineCategory>(
                    DataProvider.Instance.Context.MedicineCategories
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );
            SelectedCategory = null;
            CategoryName = "";
            CategoryDescription = "";   
        }
        
        private void AddCategory()
        {
            try
            {
                // Confirm dialog
                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn thêm loại thuốc '{CategoryName}' không?",
                    "Xác Nhận Thêm"
                     
                      );

                if ( !result)
                    return;

                // Check if specialty already exists
                bool isExist = DataProvider.Instance.Context.MedicineCategories
                    .Any(s => s.CategoryName.Trim().ToLower() == CategoryName.Trim().ToLower() && (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBoxService.ShowWarning("Loại thuốc này đã tồn tại.");
                    return;
                }

                // Add new specialty
                var newCategory = new MedicineCategory
                {
                    CategoryName = CategoryName,
                    Description = UnitDescription ?? "",
                    IsDeleted = false
                };

                DataProvider.Instance.Context.MedicineCategories.Add(newCategory);
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                CategoryList = new ObservableCollection<MedicineCategory>(
                    DataProvider.Instance.Context.MedicineCategories
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Clear fields
                SelectedCategory = null;
                CategoryName = "";
                CategoryDescription = "";

                MessageBoxService.ShowSuccess(
                    "Đã thêm loại thuốc thành công!",
                    "Thành công"
                     
                      );
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể thêm loại thuốc: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu"
                     
                     );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }

        private void EditCategory()
        {
            try
            {
                // Confirm dialog
                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa loại thuốc '{SelectedCategory.CategoryName}' thành '{CategoryName}' không?",
                    "Xác Nhận Sửa"
                     
                      );

                if ( !result)
                    return;

                // Check if specialty name already exists (except for current)
                bool isExist = DataProvider.Instance.Context.MedicineCategories
                    .Any(s => s.CategoryName.Trim().ToLower() == CategoryName.Trim().ToLower() &&
                              s.CategoryId != SelectedCategory.CategoryId &&
                             (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBoxService.ShowWarning("Tên loại thuôc này đã tồn tại.");
                    return;
                }

                // Update specialty
                var categoryToUpdate = DataProvider.Instance.Context.MedicineCategories
                    .FirstOrDefault(s => s.CategoryId == SelectedCategory.CategoryId);

                if (categoryToUpdate == null)
                {
                    MessageBoxService.ShowWarning("Không tìm thấy loại thuốc cần sửa.");
                    return;
                }

                categoryToUpdate.CategoryName = CategoryName;
                categoryToUpdate.Description = CategoryDescription ?? "";
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                CategoryList = new ObservableCollection<MedicineCategory>(
                    DataProvider.Instance.Context.MedicineCategories
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Update doctor list as specialty names may have changed
                LoadData();

                MessageBoxService.ShowSuccess(
                    "Đã cập nhật loại thuốc thành công!",
                    "Thành công"
                     
                      );
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể sửa loại thuốc: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi cơ sở dữ liệu"
                     
                     );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }

        private void DeleteCategory()
        {
            try
            {
                // Check if specialty is in use by any Staffs
                bool isInUse = DataProvider.Instance.Context.MedicineCategories
                    .Any(d => d.CategoryId == SelectedCategory.CategoryId && (bool)d.IsDeleted == true);


                if (isInUse)
                {
                    MessageBoxService.ShowWarning(
                        "Không thể xóa Đơn vị này vì đang được sử dụng bởi một hoặc nhiều thuốc.",
                        "Cánh báo"
                         
                          );
                    return;
                }

                // Confirm deletion
                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn xóa loại thuốc '{SelectedCategory.CategoryName}' không?",
                    "Xác nhận xóa"
                     
                      );

                if ( !result)
                    return;

                // Soft delete the specialty
                var categoryToUpdate = DataProvider.Instance.Context.MedicineCategories
                    .FirstOrDefault(s => s.CategoryId == SelectedCategory.CategoryId);

                if (categoryToUpdate == null)
                {
                    MessageBoxService.ShowWarning("Không tìm thấy loại thuốc cần xóa.");
                    return;
                }

                categoryToUpdate.IsDeleted = true;
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                CategoryList = new ObservableCollection<MedicineCategory>(
                    DataProvider.Instance.Context.MedicineCategories
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Clear selection and fields
                SelectedCategory = null;
                CategoryName = "";
                CategoryDescription = "";

                MessageBoxService.ShowSuccess(
                    "Đã xóa loại thuốc thành công.",
                    "Thành công"
                     
                      );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa loại thuốc: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }
        #endregion

        #region Supplier Methods
        // Check if form has any errors
        public bool HasErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(SupplierCode)]) ||
                       !string.IsNullOrEmpty(this[nameof(SupplierName)]) ||
                       !string.IsNullOrEmpty(this[nameof(SupplierPhone)]) ||
                       !string.IsNullOrEmpty(this[nameof(SupplierEmail)]) ||
                       !string.IsNullOrEmpty(this[nameof(SupplierTaxCode)]);
            }
        }
        public string this[string columnName]
        {
            get
            {
                // Don't validate until user has interacted with the form
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(SupplierCode):
                        // Only show "required" error if field was touched and is empty
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
                        // Only show "required" error if field was touched and is empty
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
                        // Only validate if user has entered something
                        if (!string.IsNullOrWhiteSpace(SupplierPhone) &&
                            !Regex.IsMatch(SupplierPhone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            error = "Số điện thoại không đúng định dạng (VD: 0901234567)";
                        }
                        break;

                    case nameof(SupplierEmail):
                        // Only validate if user has entered something
                        if (!string.IsNullOrWhiteSpace(SupplierEmail) &&
                            !Regex.IsMatch(SupplierEmail.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            error = "Email không đúng định dạng";
                        }
                        break;

                    case nameof(SupplierTaxCode):
                        // Only validate if user has entered something
                        if (!string.IsNullOrWhiteSpace(SupplierTaxCode) &&
                            !Regex.IsMatch(SupplierTaxCode.Trim(), @"^\d{10}(-\d{3})?$"))
                        {
                            error = "Mã số thuế phải có 10 số hoặc 10-3 số (VD: 0123456789-001)";
                        }
                        break;
                }

                return error;
            }
        }

        private void AddSupplier()
        {
            try
            {
                // Enable validation for all fields when trying to submit
                _isValidating = true;
                _touchedFields.Add(nameof(SupplierCode));
                _touchedFields.Add(nameof(SupplierName));

                // Trigger validation check for required fields
                OnPropertyChanged(nameof(SupplierCode));
                OnPropertyChanged(nameof(SupplierName));

                // Check for validation errors
                if (HasErrors)
                {
                    MessageBoxService.ShowError(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm nhà cung cấp.",
                        "Lỗi thông tin"
                         
                          );
                    return;
                }

                // Check required fields
                if (string.IsNullOrWhiteSpace(SupplierCode) || string.IsNullOrWhiteSpace(SupplierName))
                {
                    MessageBoxService.ShowWarning(
                        "Mã nhà cung cấp và Tên nhà cung cấp là bắt buộc.",
                        "Thiếu Thông Tin"
                         
                          );
                    return;
                }

                // Confirm dialog
                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn thêm nhà cung cấp '{SupplierName}' không?",
                    "Xác Nhận Thêm"
                     
                      );

                if ( !result)
                    return;

                // Check if supplier already exists (by code or name)
                bool isExist = DataProvider.Instance.Context.Suppliers
                    .Any(s => (s.SupplierCode.Trim().ToLower() == SupplierCode.Trim().ToLower() ||
                              s.SupplierName.Trim().ToLower() == SupplierName.Trim().ToLower()) &&
                              (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBoxService.ShowWarning("Nhà cung cấp này đã tồn tại (trùng mã hoặc tên).");
                    return;
                }

                // Add new supplier
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

                DataProvider.Instance.Context.Suppliers.Add(newSupplier);
                DataProvider.Instance.Context.SaveChanges();

                // Clear fields and reset validation state
                ClearForm();

                MessageBoxService.ShowSuccess(
                    "Đã thêm nhà cung cấp thành công!",
                    "Thành công"
                     
                      );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }


        private void EditSupplier()
        {
            try
            {
                // Confirm dialog
                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn sửa nhà cung cấp '{SelectedSupplier.SupplierName}' thành '{SupplierName}' không?",
                    "Xác Nhận Sửa"
                     
                      );

                if ( !result)
                    return;

                // Check if supplier code or name already exists (except for current)
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

                // Update supplier
                var supplierToUpdate = DataProvider.Instance.Context.Suppliers
                    .FirstOrDefault(s => s.SupplierId == SelectedSupplier.SupplierId);

                if (supplierToUpdate == null)
                {
                    MessageBoxService.ShowWarning("Không tìm thấy nhà cung cấp cần sửa.");
                    return;
                }

                supplierToUpdate.SupplierCode = SupplierCode;
                supplierToUpdate.SupplierName = SupplierName;
                supplierToUpdate.Email = SupplierEmail ?? "";
                supplierToUpdate.Phone = SupplierPhone ?? "";
                supplierToUpdate.TaxCode = SupplierTaxCode ?? "";
                supplierToUpdate.ContactPerson = ContactPerson ?? "";
                supplierToUpdate.Address = SupplierAddress ?? "";
                supplierToUpdate.IsActive = IsActive;

                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                SupplierList = new ObservableCollection<Supplier>(
                    DataProvider.Instance.Context.Suppliers
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                // Update related data if needed
                LoadData();

                MessageBoxService.ShowSuccess(
                    "Đã cập nhật nhà cung cấp thành công!",
                    "Thành công"
                     
                      );
            }
            catch (DbUpdateException ex)
            {
                MessageBoxService.ShowError(
                    $"Không thể sửa nhà cung cấp: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi cơ sở dữ liệu"
                     
                     );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }

        private void DeleteSupplier()
        {
            try
            {
                

                // Confirm deletion
                 bool  result = MessageBoxService.ShowQuestion(
                    $"Bạn có chắc muốn xóa nhà cung cấp '{SelectedSupplier.SupplierName}' không?",
                    "Xác nhận xóa"
                     
                      );

                if ( !result)
                    return;

                // Soft delete the supplier
                var supplierToDelete = DataProvider.Instance.Context.Suppliers
                    .FirstOrDefault(s => s.SupplierId == SelectedSupplier.SupplierId);

                if (supplierToDelete == null)
                {
                    MessageBoxService.ShowWarning("Không tìm thấy nhà cung cấp cần xóa.");
                    return;
                }

                supplierToDelete.IsDeleted = true;
                DataProvider.Instance.Context.SaveChanges();

                // Refresh data
                SupplierList = new ObservableCollection<Supplier>(
                    DataProvider.Instance.Context.Suppliers
                        .Where(s => (bool)!s.IsDeleted)
                        .ToList()
                );

                ClearForm();

                MessageBoxService.ShowSuccess(
                    "Đã xóa nhà cung cấp thành công.",
                    "Thành công"
                     
                      );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi xóa nhà cung cấp: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }


        private void ExecuteAutoSupplierFilter()
        {
            FilterSuppliers();
        }


        private void FilterSuppliers()
        {
            if (_allSuppliers == null)
            {
                // Load all suppliers if not already loaded
                LoadAllSuppliers();
            }

            if (_allSuppliers == null || _allSuppliers.Count == 0)
            {
                SupplierList = new ObservableCollection<Supplier>();
                return;
            }

            IEnumerable<Supplier> filteredSuppliers = _allSuppliers;

            // Apply status filter
            if (ShowActiveSuppliers)
            {
                filteredSuppliers = filteredSuppliers.Where(s => (bool)s.IsActive);
            }
            else if (ShowInactiveSuppliers)
            {
                filteredSuppliers = filteredSuppliers.Where(s => (bool)!s.IsActive);
            }
            // If ShowAllSuppliers is true, we don't filter by active status

            // Apply search text filter
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

            // Update the SupplierList with filtered results
            SupplierList = new ObservableCollection<Supplier>(filteredSuppliers);
        }
        // Phương thức lọc tồn kho hiện tại
        public void FilterCurrentStock()
        {
            try
            {
                // Check if _allStockMedicine is null
                if (_allStockMedicine == null)
                {
                    ListStockMedicine = new ObservableCollection<Stock>();
                    UpdateCurrentTotals();
                    return;
                }

                var query = _allStockMedicine.AsEnumerable();

                // Áp dụng các bộ lọc
                if (SelectedStockCategoryId.HasValue && SelectedStockCategoryId.Value > 0)
                {
                    query = query.Where(s => s.Medicine?.CategoryId == SelectedStockCategoryId.Value);
                }

                if (SelectedStockSupplierId.HasValue && SelectedStockSupplierId.Value > 0)
                {
                    query = query.Where(s =>
                        s.Medicine != null &&
                        s.Medicine.StockIns != null &&
                        s.Medicine.StockIns.Any(si => si.SupplierId == SelectedStockSupplierId.Value));
                }

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
                MessageBoxService.ShowError($"Lỗi khi lọc dữ liệu tồn kho hiện tại: {ex.Message}", "Lỗi"    );

                // In case of error, make sure ListStockMedicine is not null
                if (ListStockMedicine == null)
                    ListStockMedicine = new ObservableCollection<Stock>();
            }
        }



        // Phương thức lọc tồn kho theo tháng
        // Suggested improvement for FilterMonthlyStock method
        public void FilterMonthlyStock()
        {
            try
            {
                // Format month/year string for filtering
                string monthYear = $"{SelectedYear:D4}-{SelectedMonth:D2}";

                // Use a fresh context to avoid stale data
                using (var context = new ClinicDbContext())
                {
                    // Query with all needed includes
                    var query = context.MonthlyStocks
                        .AsNoTracking() // For better performance
                        .Include(ms => ms.Medicine)
                        .ThenInclude(m => m.Category)
                        .Include(ms => ms.Medicine.Unit)
                        .Where(ms => ms.MonthYear == monthYear);

                    // Apply category filter if selected
                    if (SelectedStockCategoryId.HasValue && SelectedStockCategoryId.Value > 0)
                    {
                        query = query.Where(ms => ms.Medicine.CategoryId == SelectedStockCategoryId.Value);
                    }

                    // Apply unit filter if selected
                    if (SelectedStockUnitId.HasValue && SelectedStockUnitId.Value > 0)
                    {
                        query = query.Where(ms => ms.Medicine.UnitId == SelectedStockUnitId.Value);
                    }

                    // Apply supplier filter if needed
                    if (SelectedStockSupplierId.HasValue && SelectedStockSupplierId.Value > 0)
                    {
                        query = query.Where(ms =>
                            ms.Medicine.StockIns != null &&
                            ms.Medicine.StockIns.Any(si => si.SupplierId == SelectedStockSupplierId.Value));
                    }

                    // Apply name search filter
                    if (!string.IsNullOrWhiteSpace(SearchStockMedicine))
                    {
                        var searchTerm = SearchStockMedicine.ToLower().Trim();
                        query = query.Where(ms =>
                            ms.Medicine.Name.ToLower().Contains(searchTerm));
                    }

                    // Execute query and update list
                    var monthlyStockItems = query.ToList();
                    MonthlyStockList = new ObservableCollection<MonthlyStock>(monthlyStockItems);

                    // Update totals
                    UpdateMonthlyTotals();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi lọc dữ liệu tồn kho theo tháng: {ex.Message}", "Lỗi");

                // In case of error, make sure MonthlyStockList is not null
                if (MonthlyStockList == null)
                    MonthlyStockList = new ObservableCollection<MonthlyStock>();
            }
        }



        // Phương thức để cập nhật tổng số lượng và giá trị tồn kho hiện tại
        private void UpdateCurrentTotals()
        {
            if (ListStockMedicine == null)
            {
                CurrentTotalQuantity = 0;
                CurrentTotalValue = 0;
            }
            else
            {
                CurrentTotalQuantity = ListStockMedicine.Sum(x => x.Quantity);
                CurrentTotalValue = ListStockMedicine.Sum(x => x.Quantity * (x.Medicine?.CurrentUnitPrice ?? 0));
            }

            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(TotalValue));
        }

        // Phương thức để cập nhật tổng số lượng và giá trị tồn kho theo tháng
        private void UpdateMonthlyTotals()
        {
            if (MonthlyStockList == null)
            {
                MonthlyTotalQuantity = 0;
                MonthlyTotalValue = 0;
            }
            else
            {
                MonthlyTotalQuantity = MonthlyStockList.Sum(x => x.Quantity);
                MonthlyTotalValue = MonthlyStockList.Sum(x => x.Quantity * (x.Medicine?.CurrentUnitPrice ?? 0));
            }

            OnPropertyChanged(nameof(TotalQuantity));
            OnPropertyChanged(nameof(TotalValue));
        }

        /// <summary>
        /// Load all suppliers from database
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
        /// Load suppliers with filtering support
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
        private void ResetSupplierFilters()
        {
            SupplierSearchText = string.Empty;

            // Temporarily disable auto filtering to avoid redundant updates
            var tempShowAll = _showAllSuppliers;
            var tempShowActive = _showActiveSuppliers;
            var tempShowInactive = _showInactiveSuppliers;

            _showAllSuppliers = false;
            _showActiveSuppliers = false;
            _showInactiveSuppliers = false;

            // Set ShowAllSuppliers to true which will trigger filtering
            ShowAllSuppliers = true;

            // Reload all suppliers
            LoadSuppliers();
        }

        #endregion

        #region StockMedicine Methods


        // Method to check if manual generation is possible
        private bool CanManualGenerateMonthlyStock()
        {
            return CanManageSettings;
        }


        // Method to check if undo is possible
        private bool CanUndoMonthlyStock()
        {
            // Create a fresh context for this check
            using (var context = new ClinicDbContext())
            {
                return CanManageSettings && HasCurrentMonthStock(context);
            }
        }

        private void ExecuteSupplierRefresh()
        {
            SupplierList = new ObservableCollection<Supplier>(
                   DataProvider.Instance.Context.Suppliers
                       .Where(s => (bool)!s.IsDeleted)
                       .ToList()
               );
            ClearForm();

        }
 
        private void ExecuteDeleteMedicine(Medicine medicine)
        {
            try
            {
                var medicineId = medicine.MedicineId;
                // Confirm dialog
                 bool  result = MessageBoxService.ShowQuestion(
                        "Xác nhận xóa thuốc '" + medicine.Name + "'?",
                        "Xác nhận"
                         
                          
                    );


                if ( !result)
                    return;
                // Check if specialty already exists
                int isNotExist = DataProvider.Instance.Context.Medicines
                    .Count(s => s.MedicineId == medicineId);

                if (isNotExist == 0)
                {
                    MessageBoxService.ShowWarning("Thuốc này đã không tồn tại.");
                    return;
                }
                else
                {
                    // Soft delete the medicine
                    var medicineToDelete = DataProvider.Instance.Context.Medicines
                        .FirstOrDefault(s => s.MedicineId == medicineId);
                    if (medicineToDelete == null)
                    {
                        MessageBoxService.ShowWarning("Không tìm thấy thuốc cần xóa.");
                        return;
                    }
                    medicineToDelete.IsDeleted = true;
                    DataProvider.Instance.Context.SaveChanges();
                    // Refresh data
                    ListMedicine = new ObservableCollection<Stock>(
                DataProvider.Instance.Context.Stocks
                .Where(d => (bool)!d.Medicine.IsDeleted)
                .Include(d => d.Medicine)
                    .ThenInclude(m => m.InvoiceDetails) // Cần để tính số lượng đã bán
                .Include(d => d.Medicine.Category)
                .Include(d => d.Medicine.Unit)

                .Include(d => d.Medicine.StockIns)
                .ToList()
               );
                    MessageBoxService.ShowSuccess(
                        "Đã xóa thuốc thành công.",
                        "Thành công"
                         
                          );
                }

            }
            catch
            {

            }

        }

        private bool CanDeleteMedicine(Medicine medicine)
        {
            if (medicine == null)
                return false;
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (medicine.CurrentExpiryDate > today)
                return false;
            return true;
        }
        // Phương thức xuất Excel cập nhật để hỗ trợ cả hai chế độ xem
        /// <summary>
        /// Xuất dữ liệu tồn kho ra tập tin Excel
        /// </summary>
        /// <summary>
        /// Xuất dữ liệu tồn kho ra tập tin Excel, với bảng dữ liệu bắt đầu từ cột B
        /// </summary>
        private void ExportToExcel()
        {
            try
            {
                // Tạo hộp thoại lưu tập tin
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "Chọn vị trí lưu file Excel",
                    FileName = IsMonthlyView
                        ? $"TonKho_Thang_{SelectedMonth:D2}_{SelectedYear}.xlsx"  // Đặt tên file theo tháng/năm nếu đang xem theo tháng
                        : $"TonKho_HienTai_{DateTime.Now:yyyyMMdd}.xlsx"  // Đặt tên file theo ngày hiện tại nếu xem tồn kho hiện tại
                };

                // Kiểm tra người dùng đã chọn nơi lưu file chưa
                if (saveFileDialog.ShowDialog() == true)
                {
                    // Kiểm tra xem file đã tồn tại và có đang được sử dụng không
                    if (File.Exists(saveFileDialog.FileName))
                    {
                        try
                        {
                            // Thử mở file để kiểm tra xem nó có đang bị khóa không
                            using (FileStream fs = File.Open(saveFileDialog.FileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                            {
                                // File không bị khóa, có thể đóng luồng ngay
                                fs.Close();
                            }
                        }
                        catch (IOException)
                        {
                            // File đang bị khóa (đang được sử dụng bởi một tiến trình khác)
                            MessageBoxService.ShowError(
                                "File này đang được mở bởi một chương trình khác. Vui lòng đóng file hoặc chọn tên file khác.",
                                "Lỗi"
                            );
                            return;
                        }
                    }

                    // Hiển thị hộp thoại tiến trình xuất Excel
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Chạy quá trình xuất Excel trong một luồng riêng biệt để không làm đứng giao diện
                    Task.Run(() =>
                    {
                        try
                        {
                            // Sử dụng thư viện ClosedXML để tạo tập tin Excel
                            using (var workbook = new XLWorkbook())
                            {
                                // Tạo một worksheet mới có tên "Tồn kho"
                                var worksheet = workbook.Worksheets.Add("Tồn kho");

                                // Báo cáo tiến trình: 5% - Đã tạo workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // Đặt vị trí bắt đầu cho bảng là cột B (index 2)
                                int startColumn = 2;

                                // Xác định số cột dựa trên chế độ xem
                                int totalColumns = IsMonthlyView ? 6 : 8;

                                // Thêm tiêu đề (ô hợp nhất), bắt đầu từ cột B
                                worksheet.Cell(1, startColumn).Value = IsMonthlyView
                                    ? $"DANH SÁCH TỒN KHO THÁNG {SelectedMonth}/{SelectedYear}"
                                    : "DANH SÁCH TỒN KHO HIỆN TẠI";

                                // Định dạng tiêu đề: hợp nhất ô, đậm, cỡ chữ lớn, căn giữa
                                var titleRange = worksheet.Range(1, startColumn, 1, startColumn + totalColumns - 1);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Thêm ngày xuất báo cáo, bắt đầu từ cột B
                                worksheet.Cell(2, startColumn).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, startColumn, 2, startColumn + totalColumns - 1);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Báo cáo tiến trình: 10% - Đã thêm tiêu đề
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Thêm tiêu đề cột
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

                                // Định dạng hàng tiêu đề: in đậm, nền xám, căn giữa, viền
                                var headerRange = worksheet.Range(headerRow, startColumn, headerRow, startColumn + totalColumns - 1);
                                headerRange.Style.Font.Bold = true;
                                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                                // Báo cáo tiến trình: 20% - Đã thêm tiêu đề cột
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(20));

                                // Thêm dữ liệu
                                int row = headerRow + 1;  // Bắt đầu từ dòng sau tiêu đề cột
                                int totalItems = IsMonthlyView ? MonthlyStockList.Count : ListStockMedicine.Count;
                                var dataStartRow = row;

                                // Điền dữ liệu tùy theo chế độ xem
                                if (IsMonthlyView)  // Chế độ xem theo tháng
                                {
                                    for (int i = 0; i < totalItems; i++)
                                    {
                                        var item = MonthlyStockList[i];
                                        column = startColumn;  // Bắt đầu từ cột B

                                        // Điền các thông tin từ dữ liệu MonthlyStock
                                        worksheet.Cell(row, column++).Value = item.Medicine?.Name ?? "";
                                        worksheet.Cell(row, column++).Value = item.Medicine?.Category?.CategoryName ?? "";
                                        worksheet.Cell(row, column++).Value = item.Medicine?.Unit?.UnitName ?? "";
                                        worksheet.Cell(row, column++).Value = item.MonthYear;
                                        worksheet.Cell(row, column++).Value = item.Quantity;
                                        worksheet.Cell(row, column++).Value = item.CanUsed;

                                        row++;

                                        // Cập nhật tiến trình
                                        int progressValue = 20 + (i * 60 / totalItems);
                                        Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(progressValue));
                                        Thread.Sleep(5);  // Tạm dừng nhỏ để hiển thị tiến trình
                                    }
                                }
                                else  // Chế độ xem tồn kho hiện tại
                                {
                                    for (int i = 0; i < totalItems; i++)
                                    {
                                        var item = ListStockMedicine[i];
                                        var medicine = item.Medicine;
                                        column = startColumn;  // Bắt đầu từ cột B

                                        // Điền các thông tin từ dữ liệu Stock và Medicine
                                        worksheet.Cell(row, column++).Value = medicine?.Name ?? "";
                                        worksheet.Cell(row, column++).Value = medicine?.Category?.CategoryName ?? "";
                                        worksheet.Cell(row, column++).Value = medicine?.Unit?.UnitName ?? "";

                                        // Định dạng ngày nhập mới nhất
                                        if (medicine?.LatestImportDate.HasValue == true)
                                            worksheet.Cell(row, column++).Value = medicine.LatestImportDate.Value.ToString("dd/MM/yyyy");
                                        else
                                            worksheet.Cell(row, column++).Value = "";

                                        // Thêm thông tin tồn kho
                                        worksheet.Cell(row, column++).Value = item.Quantity;
                                        worksheet.Cell(row, column++).Value = medicine?.TotalStockQuantity ?? 0;

                                        // Thêm thông tin chi tiết lô thuốc
                                        if (medicine != null)
                                        {
                                            // Lấy tất cả các lô của thuốc và sắp xếp theo ngày nhập (mới nhất trước)
                                            var stockBatches = DataProvider.Instance.Context.StockIns
                                                .Where(si => si.MedicineId == medicine.MedicineId && si.RemainQuantity > 0)
                                                .OrderByDescending(si => si.ImportDate)
                                                .Select(si => new { Price = si.UnitPrice, Quantity = si.RemainQuantity })
                                                .ToList();

                                            // Tạo chuỗi thông tin chi tiết
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

                                        // Cập nhật tiến trình
                                        int progressValue = 20 + (i * 60 / totalItems);
                                        Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(progressValue));
                                        Thread.Sleep(5);  // Tạm dừng nhỏ để hiển thị tiến trình
                                    }
                                }

                                // Báo cáo tiến trình: 80% - Đã thêm dữ liệu
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(80));

                                // Thêm viền cho phần dữ liệu
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

                                // Xác định cột tổng số lượng dựa vào chế độ xem
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
                                    int valCol = startColumn + 7; // Cột "Thành tiền" 

                                    // Thêm nhãn "Tổng giá trị còn lại" trước giá trị tổng
                                    worksheet.Cell(row, valCol - 1).Value = "Tổng giá trị còn lại:";
                                    worksheet.Cell(row, valCol - 1).Style.Font.Bold = true;
                                    worksheet.Cell(row, valCol - 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                                    worksheet.Cell(row, valCol).Value = CurrentTotalValue;
                                    worksheet.Cell(row, valCol).Style.Font.Bold = true;
                                    worksheet.Cell(row, valCol).Style.NumberFormat.Format = "#,##0";
                                }

                                // Tự động điều chỉnh độ rộng cột cho phù hợp với nội dung
                                worksheet.Columns().AdjustToContents();

                                // Đặt độ rộng cho cột A để tạo khoảng trống bên trái
                                worksheet.Column(1).Width = 3;

                                // Đặt độ rộng tối đa cho cột Chi tiết lô thuốc (chỉ khi ở chế độ xem hiện tại)
                                if (!IsMonthlyView)
                                {
                                    int batchDetailsCol = startColumn + 6;
                                    if (worksheet.Column(batchDetailsCol).Width > 80)
                                        worksheet.Column(batchDetailsCol).Width = 80;
                                }

                                // Báo cáo tiến trình: 90% - Hoàn thành định dạng
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                try
                                {
                                    // Lưu tập tin Excel
                                    workbook.SaveAs(saveFileDialog.FileName);

                                    // Báo cáo tiến trình: 100% - Đã lưu tập tin
                                    Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                    Thread.Sleep(200);  // Tạm dừng nhỏ để hiển thị tiến trình hoàn thành

                                    // Đóng hộp thoại tiến trình và hiển thị thông báo thành công
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        progressDialog.Close();
                                        MessageBoxService.ShowSuccess(
                                            $"Đã xuất danh sách tồn kho thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                            "Thông báo"
                                        );
                                        // Open the Excel file with the default application
                                        if (MessageBoxService.ShowQuestion("Bạn có muốn mở file Excel không?", "Mở file"))
                                        {
                                            try
                                            {
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
                                    // Xử lý lỗi truy cập file khi lưu
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

                    // Hiển thị hộp thoại tiến trình - sẽ chặn cho đến khi hộp thoại được đóng
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi chung
                MessageBoxService.ShowError(
                    $"Lỗi khi xuất Excel: {ex.Message}",
                    "Lỗi"
                );
            }
        }



        // Method to check if there's already a stock summary for current month
        private bool HasCurrentMonthStock(ClinicDbContext context = null)
        {
            try
            {
                bool shouldDisposeContext = context == null;
                context = context ?? DataProvider.Instance.Context;

                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var monthYearString = $"{currentYear}-{currentMonth:D2}";

                bool result = context.MonthlyStocks.Any(ms => ms.MonthYear == monthYearString);

                // Only dispose if we created it
                if (shouldDisposeContext && context != DataProvider.Instance.Context)
                {
                    context.Dispose();
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking monthly stock: {ex.Message}");
                return false;
            }
        }

        // Method to execute manual generation of monthly stock
        /// <summary>
        /// Executes manual generation of monthly stock with improved month selection logic
        /// </summary>
        private void ExecuteManualGenerateMonthlyStock()
        {
            try
            {
                // Use the currently selected month and year from UI controls
                int targetYear = SelectedYear;
                int targetMonth = SelectedMonth;

                // Format month and year for display
                string monthYearFormat = $"{targetYear:D4}-{targetMonth:D2}";
                string monthName = new DateTime(targetYear, targetMonth, 1).ToString("MMMM yyyy");

                // Check if monthly stock data already exists for this month
                bool hasExistingData = false;
                using (var context = new ClinicDbContext())
                {
                    hasExistingData = context.MonthlyStocks
                        .Any(ms => ms.MonthYear == monthYearFormat);
                }

                // Prepare the confirmation message
                string confirmMessage;
                if (hasExistingData)
                {
                    confirmMessage = $"Đã tồn tại dữ liệu tổng kết tồn kho cho tháng {targetMonth}/{targetYear}.\n" +
                                    $"Bạn có chắc chắn muốn cập nhật lại không?";
                }
                else
                {
                    confirmMessage = $"Bạn đang chuẩn bị tổng kết tồn kho cho tháng {targetMonth}/{targetYear}.\n" +
                                    $"Bạn có chắc chắn muốn tiếp tục không?";
                }

                // Show confirmation dialog
                var result = MessageBoxService.ShowQuestion(
                    confirmMessage,
                    "Xác nhận tổng kết kho"
                );

                if (!result)
                    return;

                try
                {
                    // Generate or update the monthly stock records
                    GenerateOrUpdateMonthlyStock(targetYear, targetMonth);

                    MessageBoxService.ShowSuccess(
                        $"Đã tổng kết tồn kho cho tháng {targetMonth}/{targetYear} thành công!",
                        "Tổng kết tồn kho"
                    );
                    FilterMonthlyStock();
                   
                }
                catch (ObjectDisposedException)
                {
                    // Handle the specific case where the context was disposed
                    MessageBoxService.ShowError(
                        "Lỗi kết nối cơ sở dữ liệu. Hãy thử lại sau vài giây.",
                        "Lỗi cơ sở dữ liệu"
                    );

                    // Reset the data provider context
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
        // Method to delete current month's stock records
        private void DeleteCurrentMonthStock()
        {
            try
            {
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var monthYearString = $"{currentYear}-{currentMonth:D2}";

                // Use the DataProvider singleton for database operations
                var context = DataProvider.Instance.Context;

                var recordsToDelete = context.MonthlyStocks
                    .Where(ms => ms.MonthYear == monthYearString)
                    .ToList();

                if (recordsToDelete.Any())
                {
                    context.MonthlyStocks.RemoveRange(recordsToDelete);
                    context.SaveChanges();
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

        private void GenerateOrUpdateMonthlyStock(int year, int month)
        {
            try
            {
                // Format for MonthYear field
                string monthYearFormat = $"{year:D4}-{month:D2}";

                // Create a fresh context for this operation
                using (var context = new ClinicDbContext())
                {
                    // Begin a transaction for consistency
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        try
                        {
                            // Dictionary to track existing records for efficient updates
                            Dictionary<int, MonthlyStock> existingRecords = new Dictionary<int, MonthlyStock>();

                            // Check for existing records and load them into dictionary
                            var currentRecords = context.MonthlyStocks
                                .Where(ms => ms.MonthYear == monthYearFormat)
                                .ToList();

                            foreach (var record in currentRecords)
                            {
                                existingRecords[record.MedicineId] = record;
                            }

                            // Get all non-deleted medicines
                            var medicines = context.Medicines
                                .Where(m => m.IsDeleted != true)
                                .Include(m => m.StockIns)
                                .Include(m => m.Stocks)
                                .ToList();

                            // Track medicines with stock for cleanup
                            HashSet<int> medicinesWithStock = new HashSet<int>();

                            foreach (var medicine in medicines)
                            {
                                // Reset cache to ensure fresh calculations
                                medicine._availableStockInsCache = null;

                                // Calculate stock quantities
                                int totalPhysicalQuantity = medicine.TotalPhysicalStockQuantity;
                                int usableQuantity = medicine.TotalStockQuantity;

                                // Only process medicines with stock
                                if (totalPhysicalQuantity > 0)
                                {
                                    medicinesWithStock.Add(medicine.MedicineId);

                                    if (existingRecords.TryGetValue(medicine.MedicineId, out MonthlyStock existingRecord))
                                    {
                                        // Update existing record
                                        existingRecord.Quantity = totalPhysicalQuantity;
                                        existingRecord.CanUsed = usableQuantity;
                                        existingRecord.RecordedDate = DateTime.Now;
                                    }
                                    else
                                    {
                                        // Create new record
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

                                    // Update current month's Stock records if applicable
                                    if (month == DateTime.Now.Month && year == DateTime.Now.Year)
                                    {
                                        var stockRecord = context.Stocks.FirstOrDefault(s => s.MedicineId == medicine.MedicineId);
                                        if (stockRecord != null)
                                        {
                                            stockRecord.Quantity = totalPhysicalQuantity;
                                            stockRecord.UsableQuantity = usableQuantity;
                                            stockRecord.LastUpdated = DateTime.Now;
                                        }
                                        else
                                        {
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

                            // Remove records for medicines that no longer have stock
                            var recordsToDelete = currentRecords
                                .Where(r => !medicinesWithStock.Contains(r.MedicineId))
                                .ToList();

                            if (recordsToDelete.Any())
                            {
                                context.MonthlyStocks.RemoveRange(recordsToDelete);
                            }

                            // Save all changes
                            context.SaveChanges();
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
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

        private void ExecuteUndoMonthlyStock()
        {
            var confirmResult = MessageBoxService.ShowQuestion(
                "Bạn có chắc chắn muốn hủy bỏ tổng kết tồn kho tháng hiện tại không?",
                "Xác nhận hoàn tác"
            );

            if (!confirmResult)
                return;

            DeleteCurrentMonthStock();

            MessageBoxService.ShowSuccess(
                "Đã hủy bỏ tổng kết tồn kho tháng hiện tại thành công!",
                "Hoàn tác tổng kết kho"
            );

            // Refresh data if in monthly view
            if (IsMonthlyView)
            {
                FilterMonthlyStock();
            }
        }

        // Method to check if it's time to remind about monthly stock summary
        public void CheckMonthlyStockReminder()
        {
            try
            {
                var today = DateTime.Now;

                // Use DataProvider.Instance.Context instead of creating a new context
                var context = DataProvider.Instance.Context;

                // Remind on last day of month
                if (today.Day == DateTime.DaysInMonth(today.Year, today.Month))
                {
                    var currentMonth = today.Month;
                    var currentYear = today.Year;
                    var monthYearString = $"{currentYear}-{currentMonth:D2}";

                    bool hasCurrentMonthStock = context.MonthlyStocks.Any(ms => ms.MonthYear == monthYearString);

                    if (!hasCurrentMonthStock && CanManageSettings)
                    {
                        MessageBoxService.ShowInfo(
                            "Hôm nay là ngày cuối tháng. Bạn nên thực hiện tổng kết tồn kho!",
                            "Nhắc nhở tổng kết kho"
                        );
                    }
                }

                // Remind in first 2 days of new month if previous month not summarized
                else if (today.Day <= 2)
                {
                    var previousMonth = today.AddMonths(-1);
                    var prevMonth = previousMonth.Month;
                    var prevYear = previousMonth.Year;
                    var prevMonthYearString = $"{prevYear}-{prevMonth:D2}";

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
                Console.WriteLine($"Error in CheckMonthlyStockReminder: {ex.Message}");
                // Don't show a message box to avoid disrupting the UI
            }
        }

        #endregion

        #region StockIn Methods
        private bool CanExecuteAddNewMedicine()
        {
            // Basic validation
            return StockinSelectedCategory != null &&
                   StockinSelectedSupplier != null &&
                   StockinSelectedUnit != null &&
                   !string.IsNullOrWhiteSpace(StockinMedicineName) &&
                   StockinQuantity > 0 &&
                   StockinUnitPrice > 0 &&
                   StockinSellPrice > 0 &&
                   StockinExpiryDate.HasValue;
        }

        private void ExecuteAddNewMedicine()
        {
            try
            {
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        if (!IsSupplierWorking(StockinSelectedSupplier))
                        {
                            MessageBoxService.ShowWarning(
                                "Nhà cung cấp này không hoạt động. Vui lòng chọn nhà cung cấp khác.",
                                "Nhà Cung Cấp Không Hoạt Động"
                            );
                            return;
                        }

                        // Convert expiry date
                        DateOnly? expiryDateOnly = null;
                        if (StockinExpiryDate.HasValue)
                        {
                            expiryDateOnly = DateOnly.FromDateTime(StockinExpiryDate.Value);
                        }

                        DateTime importDateTime = ImportDate ?? DateTime.Now;

                        // Find existing medicine or create new one
                        var dbContext = DataProvider.Instance.Context;
                        Medicine medicine;
                        string resultMessage;

                        // Find medicine with exact matches (name, category, unit)
                        var existingExactMedicine = dbContext.Medicines
                            .Include(m => m.StockIns)
                            .FirstOrDefault(m => m.Name.ToLower() == StockinMedicineName.ToLower().Trim() &&
                                            m.UnitId == StockinSelectedUnit.UnitId &&
                                            m.CategoryId == StockinSelectedCategory.CategoryId &&
                                            (bool)!m.IsDeleted);

                        // Find any medicine with the same name (for warning)
                        var existingMedicineWithName = dbContext.Medicines
                            .FirstOrDefault(m => m.Name.ToLower() == StockinMedicineName.ToLower().Trim() &&
                                            (bool)!m.IsDeleted);

                        if (existingExactMedicine != null)
                        {
                            // Case 1: Found exact match - add StockIn to existing medicine
                            medicine = existingExactMedicine;
                            resultMessage = $"Đã nhập thêm '{StockinQuantity}' '{medicine.Unit?.UnitName ?? "đơn vị"}' cho thuốc '{medicine.Name}' hiện có.";
                        }
                        else if (existingMedicineWithName != null)
                        {
                            // Case 2: Found medicine with same name but different properties
                            string differences = GetDifferenceMessage(existingMedicineWithName);

                            bool result = MessageBoxService.ShowQuestion(
                               $"Đã tồn tại thuốc có tên '{StockinMedicineName.Trim()}' nhưng {differences}. " +
                               $"Bạn có muốn tạo thuốc mới không?",
                               "Xác nhận"
                           );

                            if (!result)
                                return;

                            // Create a new medicine
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
                            // Case 3: Completely new medicine
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

                        // Add new StockIn entry
                        var stockIn = new StockIn
                        {
                            MedicineId = medicine.MedicineId,
                            StaffId = CurrentAccount.StaffId,
                            Quantity = StockinQuantity,
                            RemainQuantity = StockinQuantity, // Initialize RemainQuantity equal to Quantity
                            ImportDate = importDateTime,
                            UnitPrice = StockinUnitPrice,
                            SellPrice = StockinSellPrice,
                            ProfitMargin = StockProfitMargin,
                            TotalCost = StockinUnitPrice * StockinQuantity,
                            ExpiryDate = expiryDateOnly,
                            SupplierId = StockinSelectedSupplier.SupplierId
                        };

                        dbContext.StockIns.Add(stockIn);

                        // Update or create stock record
                        var existingStock = dbContext.Stocks
                            .FirstOrDefault(s => s.MedicineId == medicine.MedicineId);

                        // Check if this new stock is usable based on expiry date
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var minimumExpiryDate = today.AddDays(Medicine.MinimumDaysBeforeExpiry);
                        bool isUsable = !expiryDateOnly.HasValue || expiryDateOnly.Value >= minimumExpiryDate;

                        if (existingStock != null)
                        {
                            // Update existing stock
                            existingStock.Quantity += StockinQuantity;
                            if (isUsable)
                                existingStock.UsableQuantity += StockinQuantity;
                            existingStock.LastUpdated = ImportDate ?? DateTime.Now;
                        }
                        else
                        {
                            // Create new stock entry
                            var newStock = new Stock
                            {
                                MedicineId = medicine.MedicineId,
                                Quantity = StockinQuantity,
                                UsableQuantity = isUsable ? StockinQuantity : 0,
                                LastUpdated = ImportDate ?? DateTime.Now
                            };
                            dbContext.Stocks.Add(newStock);
                        }

                        dbContext.SaveChanges();
                        transaction.Commit();

                        MessageBoxService.ShowSuccess(
                            resultMessage,
                            "Thêm thuốc thành công"
                        );

                        // Refresh data
                        LoadData();
                        ExecuteRestart();
                    }
                    catch (Exception ex)
                    {
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



        // Helper method to generate a message describing differences
        private string GetDifferenceMessage(Medicine existingMedicine)
        {
            List<string> differences = new List<string>();

            if (existingMedicine.CategoryId != StockinSelectedCategory.CategoryId)
                differences.Add("khác loại thuốc");

            if (existingMedicine.UnitId != StockinSelectedUnit.UnitId)
                differences.Add("khác đơn vị tính");

            // Không còn kiểm tra SupplierId ở Medicine nữa
            // Thay vào đó, chúng ta có thể kiểm tra nhà cung cấp của lần nhập gần nhất
            var latestStockIn = existingMedicine.StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();

            if (latestStockIn != null && latestStockIn.SupplierId != StockinSelectedSupplier.SupplierId)
                differences.Add("khác nhà cung cấp gần đây");

            if (!differences.Any())
                return "có thông tin khác";

            return string.Join(", ", differences);
        }




        private void ExecuteRestart()
        {
            // Clear all form fields
            StockinMedicineName = string.Empty;
            StockinBarCode = string.Empty;     // Clear BarCode
            StockinQrCode = string.Empty;      // Clear QRCode
            StockinQuantity = 0;
            StockinUnitPrice = 0;
            StockinSellPrice = 0;
            StockinExpiryDate = null;
            StockinSelectedCategory = null;
            StockinSelectedSupplier = null;
            StockinSelectedUnit = null;
            SelectedMedicine = null;
            ImportDate = DateTime.Now;
        }

        private void LoadMedicineDetailsForStockIn(Medicine medicine)
        {
            if (medicine == null) return;

            // Clear any cached data to ensure we get fresh information
            medicine._availableStockInsCache = null;

            // Basic medicine information
            StockinMedicineName = medicine.Name;
            StockinBarCode = medicine.BarCode;
            StockinQrCode = medicine.QrCode;
            StockinSelectedCategory = medicine.Category;
            StockinSelectedUnit = medicine.Unit;

            // Get the selling StockIn following our new logic
            var sellingStockIn = medicine.SellingStockIn;

            // For new stock entries, use most recent stock entry as defaults
            var latestStockIn = medicine.StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();

            if (latestStockIn != null)
            {
                // Use the latest StockIn's values for default pricing/supplier
                StockinUnitPrice = latestStockIn.UnitPrice;
                StockinSellPrice = latestStockIn.SellPrice ?? latestStockIn.UnitPrice * 1.2m;
                StockProfitMargin = latestStockIn.ProfitMargin;

                // For supplier, use latest StockIn's supplier as default
                StockinSelectedSupplier = SupplierList.FirstOrDefault(s => s.SupplierId == latestStockIn.SupplierId);

                // Use current date as default for new entries
                ImportDate = DateTime.Now;
            }
            else
            {
                // Default values if no previous StockIn exists
                StockinUnitPrice = 0;
                StockinSellPrice = 0;
                StockProfitMargin = 20; // Default 20% markup
                ImportDate = DateTime.Now;
            }

            // Expiry date - try to get from selling StockIn if exists
            if (sellingStockIn?.ExpiryDate.HasValue == true)
            {
                try
                {
                    StockinExpiryDate = new DateTime(
                        sellingStockIn.ExpiryDate.Value.Year,
                        sellingStockIn.ExpiryDate.Value.Month,
                        sellingStockIn.ExpiryDate.Value.Day);

                    // Show specific warnings based on stock status
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
                    // Default to one year from now if conversion fails
                    StockinExpiryDate = DateTime.Now.AddYears(1);
                }
            }
            else
            {
                // Default to one year from now if no active batch or no expiry date
                StockinExpiryDate = DateTime.Now.AddYears(1);
            }

            // Show low stock warning if the active batch is running low
            if (sellingStockIn != null && sellingStockIn.RemainQuantity <= 10)
            {
                MessageBoxService.ShowWarning(
                    $"Thuốc {medicine.Name} đang sử dụng chỉ còn {sellingStockIn.RemainQuantity} {medicine.Unit?.UnitName ?? "đơn vị"}.",
                    "Cảnh báo số lượng thấp"
                );
            }
        }





        private bool IsSupplierWorking(Supplier supplier)
        {
            if (supplier == null)
                return false;

            return supplier.IsActive == true;
        }


        #endregion

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

        #region Helper Methods
        private void ClearForm()
        {
            SelectedSupplier = null;
            SupplierCode = "";
            SupplierName = "";
            SupplierEmail = "";
            SupplierPhone = "";
            SupplierTaxCode = "";
            ContactPerson = "";
            SupplierAddress = "";
            IsActive = false;
            IsNotActive = false;

     
            _touchedFields.Clear();
            _isValidating = false;
        }
        private int? GetCategoryIdByName(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return null;

            var category = DataProvider.Instance.Context.MedicineCategories
                .FirstOrDefault(c => c.CategoryName.Trim().ToLower() == categoryName.Trim().ToLower() && (bool)!c.IsDeleted);

            return category?.CategoryId;
        }
        private int? GetSupplierIdByName(string supplierName)
        {
            if (string.IsNullOrWhiteSpace(supplierName))
                return null;

            var supplier = DataProvider.Instance.Context.Suppliers
                .FirstOrDefault(c => c.SupplierName.Trim().ToLower() == supplierName.Trim().ToLower() && (bool)!c.IsDeleted);

            return supplier?.SupplierId;
        }
        private int? GetUnitIdByName(string unitName)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                return null;
            var unit = DataProvider.Instance.Context.Units
                .FirstOrDefault(c => c.UnitName.Trim().ToLower() == unitName.Trim().ToLower() && (bool)!c.IsDeleted);
            return unit?.UnitId;
        }
        #endregion

        #endregion
    }
}
