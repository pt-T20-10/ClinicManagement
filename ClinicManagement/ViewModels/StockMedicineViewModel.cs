﻿using ClinicManagement.Models;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel;
using Microsoft.Win32;
namespace ClinicManagement.ViewModels
{
    public class StockMedicineViewModel : BaseViewModel, IDataErrorInfo
    {
        public string Error => null;

        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false; // Flag to control when to validate

        #region Properties
        #region Initial Data
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
        public ICommand ExportStockExcelCommand { get; private set; }

        //StockIn Commands
        public ICommand AddNewMedicineCommand { get; set; }
        public ICommand RestartCommand { get; set; }
    
        public ICommand ShowMedicineDetailsCommand { get; set; }

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
            // Unit Commands
            AddUnitCommand = new RelayCommand<object>(
                (p) => AddUnit(),
                (p) => !string.IsNullOrEmpty(UnitName)
            );

            EditUnitCommand = new RelayCommand<object>(
                (p) => EditUnit(),
                (p) => SelectedUnit != null && !string.IsNullOrEmpty(UnitName)
            );

            DeleteUnitCommand = new RelayCommand<object>(
                (p) => DeleteUnit(),
                (p) => SelectedUnit != null
            );
            RefreshUnitCommand = new RelayCommand<object>(
              (p) => ExecuteUnitRefresh(),
              (p) => true
          );

            // Category Commands
            AddCategoryCommand = new RelayCommand<object>(
                (p) => AddCategory(),
                (p) => !string.IsNullOrEmpty(CategoryName)
            );
            EditCategoryCommand = new RelayCommand<object>(
                (p) => EditCategory(),
                (p) => SelectedCategory != null && !string.IsNullOrEmpty(CategoryName)
            );
            DeleteCategoryCommand = new RelayCommand<object>(
                (p) => DeleteCategory(),
                (p) => SelectedCategory != null
            );

            RefreshCatergoryCommand = new RelayCommand<object>(
                  (p) => ExecuteCategoryRefresh(),
                  (p) => true
              );

            // Supplier Commands
            AddSupplierCommand = new RelayCommand<object>(
            (p) => AddSupplier(),
            (p) => !string.IsNullOrEmpty(SupplierName)
        );

            EditSupplierCommand = new RelayCommand<object>(
                (p) => EditSupplier(),
                (p) => SelectedSupplier != null && !string.IsNullOrEmpty(SupplierName)
            );

            DeleteSupplierCommand = new RelayCommand<object>(
                (p) => DeleteSupplier(),
                (p) => SelectedSupplier != null
            );
            SetActiveStatusCommand = new RelayCommand<object>(
           (p) =>
           {
               IsActive = true;
               IsNotActive = false;
           } 
           ,
           (p) => true
            );
            SetNoActiveStatusCommand = new RelayCommand<object>(
          (p) =>
             {
                 IsActive = false;
                 IsNotActive = true;
             }
             ,
             (p) => true
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
            SearchStockMedicineCommand = new RelayCommand<object>
 (
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
                (p) => CanExecuteAddNewMedicine()
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
                (medicine) => CanDeleteMedicine(medicine)
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
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn thêm đơn vị '{UnitName}' không?",
                    "Xác Nhận Thêm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if specialty already exists
                bool isExist = DataProvider.Instance.Context.Units
                    .Any(s => s.UnitName.Trim().ToLower() == UnitName.Trim().ToLower() && (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Đơn vị này đã tồn tại.");
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

                MessageBox.Show(
                    "Đã thêm Đơn vị thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể thêm Đơn vị: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EditUnit()
        {
            try
            {
                // Confirm dialog
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn sửa đơn vị '{SelectedUnit.UnitName}' thành '{UnitName}' không?",
                    "Xác Nhận Sửa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if specialty name already exists (except for current)
                bool isExist = DataProvider.Instance.Context.Units
                    .Any(s => s.UnitName.Trim().ToLower() == UnitName.Trim().ToLower() &&
                              s.UnitId != SelectedUnit.UnitId &&
                             (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Tên đơn vị này đã tồn tại.");
                    return;
                }

                // Update specialty
                var unitToUpdate = DataProvider.Instance.Context.Units
                    .FirstOrDefault(s => s.UnitId == SelectedUnit.UnitId);

                if (unitToUpdate == null)
                {
                    MessageBox.Show("Không tìm thấy đơn vị cần sửa.");
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

                MessageBox.Show(
                    "Đã cập nhật đơn vị thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể sửa đơn vị: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteUnit()
        {
            try
            {
                // Check if specialty is in use by any doctors
                bool isInUse = DataProvider.Instance.Context.Units
                    .Any(d => d.UnitId == SelectedUnit.UnitId && (bool)!d.IsDeleted);

                if (isInUse)
                {
                    MessageBox.Show(
                        "Không thể xóa Đơn vị này vì đang được sử dụng bởi một hoặc nhiều thuốc.",
                        "Cảnh báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Confirm deletion
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn xóa Đơn vị '{SelectedUnit.UnitName}' không?",
                    "Xác Nhận Xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                // Soft delete the specialty
                var specialtyToDelete = DataProvider.Instance.Context.Units
                    .FirstOrDefault(s => s.UnitId == SelectedUnit.UnitId);

                if (specialtyToDelete == null)
                {
                    MessageBox.Show("Không tìm thấy Đơn vị cần xóa.");
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

                MessageBox.Show(
                    "Đã xóa đơn vị thành công.",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi xóa đơn vị: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn thêm loại thuốc '{CategoryName}' không?",
                    "Xác Nhận Thêm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if specialty already exists
                bool isExist = DataProvider.Instance.Context.MedicineCategories
                    .Any(s => s.CategoryName.Trim().ToLower() == CategoryName.Trim().ToLower() && (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Loại thuốc này đã tồn tại.");
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

                MessageBox.Show(
                    "Đã thêm loại thuốc thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể thêm loại thuốc: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EditCategory()
        {
            try
            {
                // Confirm dialog
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn sửa loại thuốc '{SelectedCategory.CategoryName}' thành '{CategoryName}' không?",
                    "Xác Nhận Sửa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if specialty name already exists (except for current)
                bool isExist = DataProvider.Instance.Context.MedicineCategories
                    .Any(s => s.CategoryName.Trim().ToLower() == CategoryName.Trim().ToLower() &&
                              s.CategoryId != SelectedCategory.CategoryId &&
                             (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Tên loại thuôc này đã tồn tại.");
                    return;
                }

                // Update specialty
                var categoryToUpdate = DataProvider.Instance.Context.MedicineCategories
                    .FirstOrDefault(s => s.CategoryId == SelectedCategory.CategoryId);

                if (categoryToUpdate == null)
                {
                    MessageBox.Show("Không tìm thấy loại thuốc cần sửa.");
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

                MessageBox.Show(
                    "Đã cập nhật loại thuốc thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể sửa loại thuốc: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteCategory()
        {
            try
            {
                // Check if specialty is in use by any doctors
                bool isInUse = DataProvider.Instance.Context.MedicineCategories
                    .Any(d => d.CategoryId == SelectedCategory.CategoryId && (bool)d.IsDeleted == true);


                if (isInUse)
                {
                    MessageBox.Show(
                        "Không thể xóa Đơn vị này vì đang được sử dụng bởi một hoặc nhiều thuốc.",
                        "Cảnh báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Confirm deletion
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn xóa loại thuốc '{SelectedCategory.CategoryName}' không?",
                    "Xác Nhận Xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                // Soft delete the specialty
                var categoryToUpdate = DataProvider.Instance.Context.MedicineCategories
                    .FirstOrDefault(s => s.CategoryId == SelectedCategory.CategoryId);

                if (categoryToUpdate == null)
                {
                    MessageBox.Show("Không tìm thấy loại thuốc cần xóa.");
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

                MessageBox.Show(
                    "Đã xóa loại thuốc thành công.",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi xóa loại thuốc: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                    MessageBox.Show(
                        "Vui lòng sửa các lỗi nhập liệu trước khi thêm nhà cung cấp.",
                        "Lỗi Validation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Check required fields
                if (string.IsNullOrWhiteSpace(SupplierCode) || string.IsNullOrWhiteSpace(SupplierName))
                {
                    MessageBox.Show(
                        "Mã nhà cung cấp và Tên nhà cung cấp là bắt buộc.",
                        "Thiếu Thông Tin",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Confirm dialog
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn thêm nhà cung cấp '{SupplierName}' không?",
                    "Xác Nhận Thêm",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if supplier already exists (by code or name)
                bool isExist = DataProvider.Instance.Context.Suppliers
                    .Any(s => (s.SupplierCode.Trim().ToLower() == SupplierCode.Trim().ToLower() ||
                              s.SupplierName.Trim().ToLower() == SupplierName.Trim().ToLower()) &&
                              (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Nhà cung cấp này đã tồn tại (trùng mã hoặc tên).");
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

                MessageBox.Show(
                    "Đã thêm nhà cung cấp thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private void EditSupplier()
        {
            try
            {
                // Confirm dialog
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn sửa nhà cung cấp '{SelectedSupplier.SupplierName}' thành '{SupplierName}' không?",
                    "Xác Nhận Sửa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Check if supplier code or name already exists (except for current)
                bool isExist = DataProvider.Instance.Context.Suppliers
                    .Any(s => (s.SupplierCode.Trim().ToLower() == SupplierCode.Trim().ToLower() ||
                              s.SupplierName.Trim().ToLower() == SupplierName.Trim().ToLower()) &&
                              s.SupplierId != SelectedSupplier.SupplierId &&
                             (bool)!s.IsDeleted);

                if (isExist)
                {
                    MessageBox.Show("Mã hoặc tên nhà cung cấp này đã tồn tại.");
                    return;
                }

                // Update supplier
                var supplierToUpdate = DataProvider.Instance.Context.Suppliers
                    .FirstOrDefault(s => s.SupplierId == SelectedSupplier.SupplierId);

                if (supplierToUpdate == null)
                {
                    MessageBox.Show("Không tìm thấy nhà cung cấp cần sửa.");
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

                MessageBox.Show(
                    "Đã cập nhật nhà cung cấp thành công!",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show(
                    $"Không thể sửa nhà cung cấp: {ex.InnerException?.Message ?? ex.Message}",
                    "Lỗi Cơ Sở Dữ Liệu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi không mong muốn: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteSupplier()
        {
            try
            {
                

                // Confirm deletion
                MessageBoxResult result = MessageBox.Show(
                    $"Bạn có chắc muốn xóa nhà cung cấp '{SelectedSupplier.SupplierName}' không?",
                    "Xác Nhận Xóa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                // Soft delete the supplier
                var supplierToDelete = DataProvider.Instance.Context.Suppliers
                    .FirstOrDefault(s => s.SupplierId == SelectedSupplier.SupplierId);

                if (supplierToDelete == null)
                {
                    MessageBox.Show("Không tìm thấy nhà cung cấp cần xóa.");
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

                MessageBox.Show(
                    "Đã xóa nhà cung cấp thành công.",
                    "Thành Công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Đã xảy ra lỗi khi xóa nhà cung cấp: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                MessageBox.Show($"Lỗi khi lọc dữ liệu tồn kho hiện tại: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

                // In case of error, make sure ListStockMedicine is not null
                if (ListStockMedicine == null)
                    ListStockMedicine = new ObservableCollection<Stock>();
            }
        }



        // Phương thức lọc tồn kho theo tháng
        public void FilterMonthlyStock()
        {
            try
            {
                // Tạo định dạng tháng/năm
                string monthYear = $"{SelectedYear:D4}-{SelectedMonth:D2}";

                // Lấy dữ liệu từ database
                var query = DataProvider.Instance.Context.MonthlyStocks
                    .Include(ms => ms.Medicine)
                    .ThenInclude(m => m.Category)
                    .Include(ms => ms.Medicine.Unit)
                    .Where(ms => ms.MonthYear == monthYear);

                // Áp dụng các bộ lọc
                if (SelectedStockCategoryId.HasValue && SelectedStockCategoryId.Value > 0)
                {
                    query = query.Where(ms => ms.Medicine.CategoryId == SelectedStockCategoryId.Value);
                }

                if (SelectedStockUnitId.HasValue && SelectedStockUnitId.Value > 0)
                {
                    query = query.Where(ms => ms.Medicine.UnitId == SelectedStockUnitId.Value);
                }

                if (SelectedStockSupplierId.HasValue && SelectedStockSupplierId.Value > 0)
                {
                    // Với tồn kho tháng, cần kiểm tra qua Medicine
                    query = query.Where(ms =>
                        ms.Medicine.StockIns != null &&
                        ms.Medicine.StockIns.Any(si => si.SupplierId == SelectedStockSupplierId.Value));
                }

                // Tìm kiếm theo tên thuốc
                if (!string.IsNullOrWhiteSpace(SearchStockMedicine))
                {
                    var searchTerm = SearchStockMedicine.ToLower().Trim();
                    query = query.Where(ms =>
                        ms.Medicine.Name.ToLower().Contains(searchTerm));
                }

                // Tải dữ liệu và cập nhật danh sách
                var monthlyStockItems = query.ToList();
                MonthlyStockList = new ObservableCollection<MonthlyStock>(monthlyStockItems);

                // Cập nhật tổng số lượng và giá trị
                UpdateMonthlyTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lọc dữ liệu tồn kho theo tháng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);

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
                MessageBox.Show(
                    $"Lỗi khi tải danh sách nhà cung cấp: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                MessageBox.Show(
                    $"Lỗi khi tải danh sách nhà cung cấp: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                MessageBoxResult result = MessageBox.Show(
                        "Xác nhận xóa thuốc '" + medicine.Name + "'?",
                        "Xác nhận",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );


                if (result != MessageBoxResult.Yes)
                    return;
                // Check if specialty already exists
                int isNotExist = DataProvider.Instance.Context.Medicines
                    .Count(s => s.MedicineId == medicineId);

                if (isNotExist == 0)
                {
                    MessageBox.Show("Thuốc này đã không tồn tại.");
                    return;
                }
                else
                {
                    // Soft delete the medicine
                    var medicineToDelete = DataProvider.Instance.Context.Medicines
                        .FirstOrDefault(s => s.MedicineId == medicineId);
                    if (medicineToDelete == null)
                    {
                        MessageBox.Show("Không tìm thấy thuốc cần xóa.");
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
                    MessageBox.Show(
                        "Đã xóa thuốc thành công.",
                        "Thành Công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
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
        private void ExportToExcel()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "Chọn vị trí lưu file Excel",
                    FileName = IsMonthlyView
                        ? $"TonKho_Thang_{SelectedMonth:D2}_{SelectedYear}.xlsx"
                        : $"TonKho_HienTai_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Hiển thị dialog tiến trình
                    ProgressDialog progressDialog = new ProgressDialog();

                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                var worksheet = workbook.Worksheets.Add("Tồn kho");

                                // Report progress: 5% - Created workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // Add title (merged cells)
                                worksheet.Cell(1, 1).Value = IsMonthlyView
                                    ? $"DANH SÁCH TỒN KHO THÁNG {SelectedMonth}/{SelectedYear}"
                                    : "DANH SÁCH TỒN KHO HIỆN TẠI";

                                var titleRange = worksheet.Range(1, 1, 1, IsMonthlyView ? 8 : 9);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Add current date
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, IsMonthlyView ? 8 : 9);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Report progress: 10% - Added title
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Add headers
                                int headerRow = 4;
                                int column = 1;

                                if (IsMonthlyView)
                                {
                                    worksheet.Cell(headerRow, column++).Value = "Tên thuốc";
                                    worksheet.Cell(headerRow, column++).Value = "Loại";
                                    worksheet.Cell(headerRow, column++).Value = "Đơn vị tính";
                                    worksheet.Cell(headerRow, column++).Value = "Tháng/năm";
                                    worksheet.Cell(headerRow, column++).Value = "Tồn kho tổng";
                                    worksheet.Cell(headerRow, column++).Value = "Sử dụng được";
                                    worksheet.Cell(headerRow, column++).Value = "Đơn giá";
                                    worksheet.Cell(headerRow, column++).Value = "Giá trị tồn";
                                }
                                else
                                {
                                    worksheet.Cell(headerRow, column++).Value = "Tên thuốc";
                                    worksheet.Cell(headerRow, column++).Value = "Loại";
                                    worksheet.Cell(headerRow, column++).Value = "Đơn vị tính";
                                    worksheet.Cell(headerRow, column++).Value = "Ngày nhập mới nhất";
                                    worksheet.Cell(headerRow, column++).Value = "Đơn giá hiện tại";
                                    worksheet.Cell(headerRow, column++).Value = "Tồn kho tổng";
                                    worksheet.Cell(headerRow, column++).Value = "Sử dụng được";
                                    worksheet.Cell(headerRow, column++).Value = "Lô mới nhất";
                                    worksheet.Cell(headerRow, column++).Value = "Thành tiền";
                                }

                                // Style header row
                                var headerRange = worksheet.Range(headerRow, 1, headerRow, IsMonthlyView ? 8 : 9);
                                headerRange.Style.Font.Bold = true;
                                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                                // Report progress: 20% - Headers added
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(20));

                                // Add data
                                int row = headerRow + 1;
                                int totalItems = IsMonthlyView ? MonthlyStockList.Count : ListStockMedicine.Count;
                                var dataStartRow = row;

                                if (IsMonthlyView)
                                {
                                    for (int i = 0; i < totalItems; i++)
                                    {
                                        var item = MonthlyStockList[i];
                                        column = 1;

                                        worksheet.Cell(row, column++).Value = item.Medicine?.Name ?? "";
                                        worksheet.Cell(row, column++).Value = item.Medicine?.Category?.CategoryName ?? "";
                                        worksheet.Cell(row, column++).Value = item.Medicine?.Unit?.UnitName ?? "";
                                        worksheet.Cell(row, column++).Value = item.MonthYear;
                                        worksheet.Cell(row, column++).Value = item.Quantity;
                                        worksheet.Cell(row, column++).Value = item.CanUsed;

                                        decimal unitPrice = item.Medicine?.CurrentUnitPrice ?? 0;
                                        worksheet.Cell(row, column++).Value = unitPrice;
                                        worksheet.Cell(row, column).Style.NumberFormat.Format = "#,##0";

                                        decimal stockValue = item.Quantity * unitPrice;
                                        worksheet.Cell(row, column++).Value = stockValue;
                                        worksheet.Cell(row, column).Style.NumberFormat.Format = "#,##0";

                                        row++;

                                        // Update progress
                                        int progressValue = 20 + (i * 60 / totalItems);
                                        Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(progressValue));
                                        Thread.Sleep(5); // Slight delay for visual feedback
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < totalItems; i++)
                                    {
                                        var item = ListStockMedicine[i];
                                        var medicine = item.Medicine;
                                        column = 1;

                                        worksheet.Cell(row, column++).Value = medicine?.Name ?? "";
                                        worksheet.Cell(row, column++).Value = medicine?.Category?.CategoryName ?? "";
                                        worksheet.Cell(row, column++).Value = medicine?.Unit?.UnitName ?? "";

                                        if (medicine?.LatestImportDate.HasValue == true)
                                            worksheet.Cell(row, column++).Value = medicine.LatestImportDate.Value.ToString("dd/MM/yyyy");
                                        else
                                            worksheet.Cell(row, column++).Value = "";

                                        worksheet.Cell(row, column++).Value = medicine?.CurrentSellPrice ?? 0;
                                        worksheet.Cell(row, column).Style.NumberFormat.Format = "#,##0";

                                        worksheet.Cell(row, column++).Value = item.Quantity;
                                        worksheet.Cell(row, column++).Value = medicine?.TotalStockQuantity ?? 0;
                                        worksheet.Cell(row, column++).Value = medicine?.CalculatedRemainingQuantity ?? 0;

                                        decimal totalValue = item.Quantity * (medicine?.CurrentUnitPrice ?? 0);
                                        worksheet.Cell(row, column++).Value = totalValue;
                                        worksheet.Cell(row, column).Style.NumberFormat.Format = "#,##0";

                                        row++;

                                        // Update progress
                                        int progressValue = 20 + (i * 60 / totalItems);
                                        Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(progressValue));
                                        Thread.Sleep(5); // Slight delay for visual feedback
                                    }
                                }

                                // Report progress: 80% - Data added
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(80));

                                // Apply borders to data
                                if (totalItems > 0)
                                {
                                    var dataRange = worksheet.Range(dataStartRow, 1, row - 1, IsMonthlyView ? 8 : 9);
                                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                                }

                                // Add totals row
                                int totalCol = IsMonthlyView ? 5 : 6;
                                int valueCol = IsMonthlyView ? 8 : 9;

                                row++;
                                worksheet.Cell(row, 1).Value = "Tổng cộng";
                                worksheet.Cell(row, 1).Style.Font.Bold = true;

                                // Tổng số lượng
                                worksheet.Cell(row, totalCol).Value = IsMonthlyView ? MonthlyTotalQuantity : CurrentTotalQuantity;
                                worksheet.Cell(row, totalCol).Style.Font.Bold = true;

                                // Tổng giá trị
                                worksheet.Cell(row, valueCol).Value = IsMonthlyView ? MonthlyTotalValue : CurrentTotalValue;
                                worksheet.Cell(row, valueCol).Style.Font.Bold = true;
                                worksheet.Cell(row, valueCol).Style.NumberFormat.Format = "#,##0";

                                // Auto-fit columns
                                worksheet.Columns().AdjustToContents();

                                // Report progress: 90% - Formatting complete
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                // Save the workbook
                                workbook.SaveAs(saveFileDialog.FileName);

                                // Report progress: 100% - File saved
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                Thread.Sleep(200); // Slight delay for visual feedback

                                // Close progress dialog and show success message
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressDialog.Close();
                                    MessageBox.Show(
                                        $"Đã xuất danh sách tồn kho thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thông báo",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBox.Show(
                                    $"Lỗi khi xuất Excel: {ex.Message}",
                                    "Lỗi",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            });
                        }
                    });

                    // Show progress dialog
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi xuất Excel: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                            MessageBox.Show(
                                "Nhà cung cấp này không hoạt động. Vui lòng chọn nhà cung cấp khác.",
                                "Nhà Cung Cấp Không Hoạt Động",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        // Convert expiry date
                        DateOnly expiryDateOnly = DateOnly.FromDateTime(StockinExpiryDate.Value);
                        DateTime importDateTime = ImportDate ?? DateTime.Now;

                        // Find medicine with exact matches (name, category, unit - KHÔNG bao gồm supplier)
                        var existingExactMedicine = DataProvider.Instance.Context.Medicines
                            .Include(m => m.StockIns)
                            .FirstOrDefault(m => m.Name.ToLower() == StockinMedicineName.ToLower().Trim() &&
                                            m.UnitId == StockinSelectedUnit.UnitId &&
                                            m.CategoryId == StockinSelectedCategory.CategoryId &&
                                            (bool)!m.IsDeleted);

                        // Find any medicine with the same name (regardless of other properties)
                        var existingMedicineWithName = DataProvider.Instance.Context.Medicines
                            .FirstOrDefault(m => m.Name.ToLower() == StockinMedicineName.ToLower().Trim() &&
                                            (bool)!m.IsDeleted);

                        Medicine medicine;
                        string resultMessage = string.Empty;

                        if (existingExactMedicine != null)
                        {
                            // Case 1: Found exact match (name, category, unit match)
                            // Check if import date matches the existing medicine's latest import date
                            DateTime? latestImportDate = existingExactMedicine.LatestImportDate;
                            bool sameImportDate = latestImportDate.HasValue &&
                                importDateTime.Date == latestImportDate.Value.Date;

                            if (sameImportDate)
                            {
                                // If import dates match, ask if user wants to update with current date
                                MessageBoxResult dateResult = MessageBox.Show(
                                    $"Ngày nhập hàng hiện tại ({importDateTime:dd/MM/yyyy}) trùng với ngày nhập gần nhất của thuốc này. " +
                                    $"Bạn có muốn đổi sang ngày hiện tại không?",
                                    "Xác nhận ngày nhập hàng",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question);

                                if (dateResult == MessageBoxResult.Yes)
                                {
                                    // Update to current date if user confirms
                                    importDateTime = DateTime.Now;
                                }
                            }

                            // Check if the existing medicine's expiry date needs updating
                            if (!existingExactMedicine.ExpiryDate.HasValue ||
                                expiryDateOnly > existingExactMedicine.ExpiryDate.Value)
                            {
                                // Update the medicine's expiry date if the new one is further in the future
                                existingExactMedicine.ExpiryDate = expiryDateOnly;
                            }

                            // Update BarCode and QRCode if provided
                            if (!string.IsNullOrWhiteSpace(StockinBarCode))
                            {
                                existingExactMedicine.BarCode = StockinBarCode.Trim();
                            }

                            if (!string.IsNullOrWhiteSpace(StockinQrCode))
                            {
                                existingExactMedicine.QrCode = StockinQrCode.Trim();
                            }

                            MessageBoxResult result = MessageBox.Show(
                                $"Bạn có muốn nhập thêm '{StockinQuantity}' '{StockinSelectedCategory.CategoryName}' cho thuốc '{existingExactMedicine.Name}' hiện có không?",
                                "Xác nhận",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result != MessageBoxResult.Yes)
                                return;

                            medicine = existingExactMedicine;

                            // Force refresh of the medicine's cache
                            var refreshedDetails = medicine.GetDetailedStock();

                            resultMessage = $"Đã nhập thêm '{StockinQuantity}' '{StockinSelectedCategory.CategoryName}' cho thuốc '{medicine.Name}' hiện có.";
                        }
                        else if (existingMedicineWithName != null)
                        {
                            // Case 2: Found medicine with same name but different properties
                            string differences = GetDifferenceMessage(existingMedicineWithName);

                            MessageBoxResult result = MessageBox.Show(
                                $"Đã tồn tại thuốc có tên '{StockinMedicineName.Trim()}' nhưng {differences}. " +
                                $"Bạn có muốn tạo thuốc mới không?",
                                "Xác nhận",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result != MessageBoxResult.Yes)
                                return;

                            // Create a new medicine (không bao gồm SupplierId)
                            medicine = new Medicine
                            {
                                Name = StockinMedicineName.Trim(),
                                CategoryId = StockinSelectedCategory.CategoryId,
                                UnitId = StockinSelectedUnit.UnitId,
                                ExpiryDate = expiryDateOnly,
                                BarCode = StockinBarCode?.Trim(),  // Add BarCode
                                QrCode = StockinQrCode?.Trim(),    // Add QRCode
                                IsDeleted = false
                            };
                            DataProvider.Instance.Context.Medicines.Add(medicine);
                            DataProvider.Instance.Context.SaveChanges();

                            resultMessage = $"Đã thêm thuốc '{medicine.Name}' mới với thông tin khác.";
                        }
                        else
                        {
                            // Case 3: This is a completely new medicine
                            medicine = new Medicine
                            {
                                Name = StockinMedicineName.Trim(),
                                CategoryId = StockinSelectedCategory.CategoryId,
                                UnitId = StockinSelectedUnit.UnitId,
                                ExpiryDate = expiryDateOnly,
                                BarCode = StockinBarCode?.Trim(),  // Add BarCode
                                QrCode = StockinQrCode?.Trim(),    // Add QRCode
                                IsDeleted = false
                            };
                            DataProvider.Instance.Context.Medicines.Add(medicine);
                            DataProvider.Instance.Context.SaveChanges();

                            resultMessage = $"Đã thêm thuốc mới '{medicine.Name}'.";
                        }

                        // Add new stock-in entry with potentially updated importDateTime
                        // và bây giờ cung cấp SupplierId
                        var stockIn = new StockIn
                        {
                            MedicineId = medicine.MedicineId,
                            Quantity = StockinQuantity,
                            ImportDate = importDateTime,
                            UnitPrice = StockinUnitPrice,
                            SellPrice = StockinSellPrice,
                            ProfitMargin = StockProfitMargin,
                            TotalCost = StockinUnitPrice * StockinQuantity,
                            ExpiryDate = expiryDateOnly,  // Make sure this matches the medicine's expiry date
                            SupplierId = StockinSelectedSupplier.SupplierId // Thêm SupplierId vào StockIn
                        };

                        DataProvider.Instance.Context.StockIns.Add(stockIn);
                        DataProvider.Instance.Context.SaveChanges();

                        // Update or create stock entry
                        var existingStock = DataProvider.Instance.Context.Stocks
                            .FirstOrDefault(s => s.MedicineId == medicine.MedicineId);

                        if (existingStock != null)
                        {
                            // Update existing stock
                            existingStock.Quantity += StockinQuantity;
                            existingStock.LastUpdated = importDateTime;
                        }
                        else
                        {
                            // Create new stock entry
                            var newStock = new Stock
                            {
                                MedicineId = medicine.MedicineId,
                                Quantity = StockinQuantity,
                                LastUpdated = importDateTime
                            };
                            DataProvider.Instance.Context.Stocks.Add(newStock);
                        }

                        DataProvider.Instance.Context.SaveChanges();
                        transaction.Commit();

                        MessageBox.Show(
                            resultMessage,
                            "Thêm thuốc thành công",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

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
                MessageBox.Show(
                    $"Đã xảy ra lỗi: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

            // Clear any cached data to ensure we get the fresh latest import date
            var freshDetails = medicine.GetDetailedStock(); // This calls _availableStockInsCache = null first

            StockinMedicineName = medicine.Name;
            StockinBarCode = medicine.BarCode; // Set BarCode from medicine
            StockinQrCode = medicine.QrCode; // Set QRCode from medicine
            StockinSelectedCategory = medicine.Category;
            StockinSelectedUnit = medicine.Unit;
            StockinUnitPrice = medicine.CurrentUnitPrice;
            StockinSellPrice = medicine.CurrentSellPrice;

            // Get the most recent stock-in for profit margin information and supplier
            var latestStockIn = medicine.StockIns?
                .OrderByDescending(si => si.ImportDate)
                .FirstOrDefault();

            StockProfitMargin = latestStockIn?.ProfitMargin ?? 0;

            // Lấy nhà cung cấp từ lần StockIn gần đây nhất
            if (latestStockIn?.SupplierId != null)
            {
                StockinSelectedSupplier = SupplierList.FirstOrDefault(s => s.SupplierId == latestStockIn.SupplierId);
            }

            // Get the latest import date after refreshing the cache
            ImportDate = medicine.LatestImportDate ?? DateTime.Now;

            if (medicine.ExpiryDate.HasValue)
            {
                try
                {
                    StockinExpiryDate = new DateTime(
                        medicine.ExpiryDate.Value.Year,
                        medicine.ExpiryDate.Value.Month,
                        medicine.ExpiryDate.Value.Day);
                }
                catch
                {
                    StockinExpiryDate = DateTime.Now.AddYears(1);
                }
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
