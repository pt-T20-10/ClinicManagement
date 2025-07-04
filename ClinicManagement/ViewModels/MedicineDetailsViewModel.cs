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
    public class MedicineDetailsViewModel : BaseViewModel
    {
        #region Properties
        private readonly Window _window;
        private Medicine _medicine;
        private ObservableCollection<Medicine.StockInWithRemaining> _detailedStockList;

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

        public Medicine Medicine
        {
            get => _medicine;
            set
            {
                _medicine = value;
                OnPropertyChanged();
                // Only load initial data, not continuous refresh
                LoadInitialStockData();
            }
        }

        // Thêm property cho nhà cung cấp gần nhất
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

        public ObservableCollection<Medicine.StockInWithRemaining> DetailedStockList
        {
            get => _detailedStockList;
            set
            {
                _detailedStockList = value;
                OnPropertyChanged();
            }
        }

        // Selected items for ComboBoxes
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

        // StockIn properties
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

        private DateTime? _stockinExpiryDate = DateTime.Now.AddYears(1); // Default to one year ahead
        public DateTime? StockinExpiryDate
        {
            get => _stockinExpiryDate;
            set
            {
                _stockinExpiryDate = value;
                OnPropertyChanged();
            }
        }

        // Selected stock entry from the DataGrid
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

        // Flag to control visibility of the edit section
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

        // Properties for editing selected stock entry
        private int _editStockQuantity;
        public int EditStockQuantity
        {
            get => _editStockQuantity;
            set
            {
                _editStockQuantity = value;
                OnPropertyChanged();

                // Update the EditRemainQuantity when quantity changes
                if (SelectedStockEntry != null)
                {
                    int originalQuantity = SelectedStockEntry.StockIn.Quantity;
                    int originalRemain = SelectedStockEntry.StockIn.RemainQuantity;

                    // Calculate how many items have been sold from this batch
                    int soldItems = originalQuantity - originalRemain;

                    // Don't allow setting quantity below what's already been sold
                    if (value < soldItems)
                    {
                        MessageBoxService.ShowWarning(
                            $"Số lượng nhập không thể nhỏ hơn số đã bán ({soldItems}).",
                            "Cảnh báo"
                        );
                        _editStockQuantity = soldItems;
                        OnPropertyChanged();
                    }

                    // Update remain quantity based on new total quantity minus already sold items
                    EditRemainQuantity = Math.Max(0, _editStockQuantity - soldItems);
                }
            }
        }

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

        private decimal _editUnitPrice;
        public decimal EditUnitPrice
        {
            get => _editUnitPrice;
            set
            {
                _editUnitPrice = value;
                OnPropertyChanged();

                // If profit margin is set, recalculate sell price
                if (_editProfitMargin > 0)
                {
                    _editSellPrice = _editUnitPrice * (1 + (_editProfitMargin / 100));
                    OnPropertyChanged(nameof(EditSellPrice));
                }
                else if (_editSellPrice > 0)
                {
                    // Calculate profit margin based on unit price and sell price
                    CalculateEditProfitMargin();
                }
            }
        }

        private decimal _editProfitMargin;
        public decimal EditProfitMargin
        {
            get => _editProfitMargin;
            set
            {
                _editProfitMargin = value;
                OnPropertyChanged();

                // If unit price is set, recalculate sell price
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

        private decimal _editSellPrice;
        public decimal EditSellPrice
        {
            get => _editSellPrice;
            set
            {
                _editSellPrice = value;
                OnPropertyChanged();

                // If unit price is set, recalculate profit margin
                if (_editUnitPrice > 0)
                {
                    CalculateEditProfitMargin();
                }
            }
        }

        // Thêm property cho việc chỉnh sửa thông tin nhà cung cấp của lô thuốc
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

        // Helper method to calculate profit margin for edit mode
        private void CalculateEditProfitMargin()
        {
            if (_editUnitPrice > 0 && _editSellPrice > 0)
            {
                _editProfitMargin = ((_editSellPrice / _editUnitPrice) - 1) * 100;
                OnPropertyChanged(nameof(EditProfitMargin));
            }
        }

        #region Commands
        public ICommand CloseCommand { get; set; }
        public ICommand SaveChangesCommand { get; set; }
        public ICommand RefreshDataCommand { get; set; }
        public ICommand SetAsSellingBatchCommand { get; set; }
        // Commands for editing stock entries
        public ICommand EditStockEntryCommand { get; set; }
        public ICommand SaveStockEntryCommand { get; set; }
        public ICommand CancelEditStockEntryCommand { get; set; }
        #endregion
        #endregion

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
        public void InitializeCommands()
        {
            CloseCommand = new RelayCommand<object>(
                parameter => _window.Close(),
                parameter => true
            );

            SaveChangesCommand = new RelayCommand<object>(
               parameter => ExecuteSaveChanges(),
               parameter => CanSaveChanges()
           );

            RefreshDataCommand = new RelayCommand<object>(
                parameter => RefreshMedicineData(),
                parameter => Medicine != null
            );

            EditStockEntryCommand = new RelayCommand<Medicine.StockInWithRemaining>(
                parameter => BeginEditStockEntry(parameter),
                parameter => parameter != null
            );

            SaveStockEntryCommand = new RelayCommand<object>(
                parameter => SaveEditedStockEntry(),
                parameter => CanSaveStockEntry()
            );

            CancelEditStockEntryCommand = new RelayCommand<object>(
                parameter => CancelEditStockEntry(),
                parameter => IsEditingStockEntry
            );
            SetAsSellingBatchCommand = new RelayCommand<Medicine.StockInWithRemaining>(
                parameter => SetAsSellingBatch(parameter),
                parameter => parameter != null && parameter.RemainingQuantity > 0
            );
        }
        #endregion

        #region Data Loading Methods

        private void LoadRelatedData()
        {
            if (Medicine == null) return;

            try
            {
                // Get the latest supplier from the most recent StockIn
                var latestStockIn = DataProvider.Instance.Context.StockIns
                    .Include(si => si.Supplier)
                    .Where(si => si.MedicineId == Medicine.MedicineId)
                    .OrderByDescending(si => si.ImportDate)
                    .FirstOrDefault();

                // Update latest supplier name
                if (latestStockIn?.Supplier != null)
                {
                    LatestSupplierName = latestStockIn.Supplier.SupplierName;
                }
                else
                {
                    LatestSupplierName = "Chưa có thông tin nhà cung cấp";
                }

                // Update UI
                OnPropertyChanged(nameof(LatestSupplierName));
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải thông tin liên quan: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Load initial data when Medicine is initialized - Use RemainQuantity directly
        /// </summary>
        private void LoadInitialStockData()
        {
            try
            {
                if (Medicine != null)
                {
                    var context = DataProvider.Instance.Context;

                    // Get a refreshed medicine with all needed relationships
                    var refreshedMedicine = context.Medicines
                        .Include(m => m.StockIns)
                            .ThenInclude(si => si.Supplier)
                        .Include(m => m.Category)
                        .Include(m => m.Unit)
                        .FirstOrDefault(m => m.MedicineId == Medicine.MedicineId);

                    if (refreshedMedicine != null)
                    {
                        // Clear the cache to force recalculation
                        refreshedMedicine._availableStockInsCache = null;

                        // Create stock entries directly from StockIns using RemainQuantity
                        var allStockEntries = new List<Medicine.StockInWithRemaining>();
                        var stockIns = refreshedMedicine.StockIns.OrderByDescending(si => si.ImportDate).ToList();

                        foreach (var si in stockIns)
                        {
                            // Create a new StockInWithRemaining instance for each StockIn with selling status
                            var stockEntry = new Medicine.StockInWithRemaining
                            {
                                StockIn = si,
                                RemainingQuantity = si.RemainQuantity,
                                IsCurrentSellingBatch = si.IsSelling
                            };
                            allStockEntries.Add(stockEntry);
                        }

                        DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>(allStockEntries);

                        // Get latest supplier info
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
                        DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>();
                        LatestSupplierName = "Không tìm thấy thông tin thuốc";
                    }
                }
                else
                {
                    DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>();
                    LatestSupplierName = "Không có thông tin thuốc";
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi xảy ra trong quá trình tải dữ liệu: {ex.Message}", "Lỗi");
                DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>();
            }
        }


        /// <summary>
        /// Refresh data from database without causing loops
        /// </summary>
        private void RefreshMedicineData()
        {
            try
            {
                if (Medicine == null) return;

                // Get latest data from database
                var context = DataProvider.Instance.Context;
                var refreshedMedicine = context.Medicines
                    .Include(m => m.StockIns)
                        .ThenInclude(si => si.Supplier)
                    .Include(m => m.Category)
                    .Include(m => m.Unit)
                    .FirstOrDefault(m => m.MedicineId == Medicine.MedicineId);

                if (refreshedMedicine != null)
                {
                    // Update current Medicine properties
                    UpdateMedicineProperties(refreshedMedicine);

                    // Create stock entries directly from StockIns using RemainQuantity
                    var allStockEntries = new List<Medicine.StockInWithRemaining>();
                    var stockIns = refreshedMedicine.StockIns.OrderByDescending(si => si.ImportDate).ToList();

                    foreach (var si in stockIns)
                    {
                        // Create a new StockInWithRemaining instance for each StockIn
                        var stockEntry = new Medicine.StockInWithRemaining
                        {
                            StockIn = si,
                            RemainingQuantity = si.RemainQuantity,
                            IsCurrentSellingBatch = si.IsSelling
                        };
                        allStockEntries.Add(stockEntry);
                    }

                    DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>(allStockEntries);

                    // Update dropdown selections
                    UpdateDropdownSelections();

                    // Update latest supplier info
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
        /// Update Medicine properties without triggering setter
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

            // Notify UI about changes
            OnPropertyChanged(nameof(Medicine));
        }

        /// <summary>
        /// Load data for dropdowns
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

                // Only include suppliers that are both not deleted AND active
                SupplierList = new ObservableCollection<Supplier>(
                    context.Suppliers.Where(s => !(bool)s.IsDeleted && (bool)s.IsActive));
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu cho các ô chọn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Update dropdown selections
        /// </summary>
        private void UpdateDropdownSelections()
        {
            if (Medicine == null) return;

            SelectedCategory = CategoryList?.FirstOrDefault(c => c.CategoryId == Medicine.CategoryId);
            SelectedUnit = UnitList?.FirstOrDefault(u => u.UnitId == Medicine.UnitId);
        }

        /// <summary>
        /// Load medicine details for editing
        /// </summary>
        private void LoadMedicineDetailsForEdit(Medicine medicine)
        {
            if (medicine == null) return;

            // Update dropdown selections
            UpdateDropdownSelections();

            // Load additional details for StockIn
            StockinUnitPrice = medicine.CurrentUnitPrice;
            StockinSellPrice = medicine.CurrentSellPrice;

            // Get profit margin information from most recent StockIn
            var latestStockIn = medicine.StockIns?.OrderByDescending(si => si.ImportDate).FirstOrDefault();
            StockProfitMargin = latestStockIn?.ProfitMargin ?? 20; // Default to 20% if not found

            // Get latest import date
            ImportDate = medicine.LatestImportDate ?? DateTime.Now;

            // Default expiry date to one year from import date
            StockinExpiryDate = ImportDate?.AddYears(1);
        }
        #endregion

        #region Command Methods
        private bool CanSaveChanges()
        {
            return Medicine != null &&
                   !string.IsNullOrWhiteSpace(Medicine.Name) &&
                   SelectedCategory != null &&
                   SelectedUnit != null;
        }

        private void ExecuteSaveChanges()
        {
            try
            {
                var dbContext = DataProvider.Instance.Context;

                // Get medicine from database
                var medicineToUpdate = dbContext.Medicines.Find(Medicine.MedicineId);

                if (medicineToUpdate != null)
                {
                    // Update medicine fields
                    medicineToUpdate.Name = Medicine.Name.Trim();
                    medicineToUpdate.CategoryId = SelectedCategory?.CategoryId;
                    medicineToUpdate.UnitId = SelectedUnit?.UnitId;
                    medicineToUpdate.BarCode = Medicine.BarCode?.Trim();
                    medicineToUpdate.QrCode = Medicine.QrCode?.Trim();

                    // Check if we need to add a new StockIn entry
                    if (StockinQuantity > 0 && StockinUnitPrice > 0)
                    {
                        // Get supplier from latest stock entry or use any supplier
                        var latestStockIn = dbContext.StockIns
                            .Where(si => si.MedicineId == Medicine.MedicineId)
                            .OrderByDescending(si => si.ImportDate)
                            .FirstOrDefault();

                        // Use supplier from EditSupplier or latest stock entry
                        int? supplierId = EditSupplier?.SupplierId ?? latestStockIn?.SupplierId;

                        if (!supplierId.HasValue)
                        {
                            MessageBoxService.ShowError(
                                "Không thể thêm lô nhập mới vì thiếu thông tin nhà cung cấp.",
                                "Lỗi"
                            );
                            return;
                        }

                        // Convert expiry date if provided
                        DateOnly? expiryDateOnly = null;
                        if (StockinExpiryDate.HasValue)
                        {
                            expiryDateOnly = DateOnly.FromDateTime(StockinExpiryDate.Value);
                        }

                        // Create new StockIn entry with proper RemainQuantity
                        var newStockIn = new StockIn
                        {
                            MedicineId = Medicine.MedicineId,
                            Quantity = StockinQuantity,
                            RemainQuantity = StockinQuantity, // Initialize RemainQuantity equal to Quantity
                            ImportDate = ImportDate ?? DateTime.Now,
                            UnitPrice = StockinUnitPrice,
                            SellPrice = StockinSellPrice,
                            ProfitMargin = StockProfitMargin,
                            TotalCost = StockinUnitPrice * StockinQuantity,
                            ExpiryDate = expiryDateOnly,
                            SupplierId = supplierId.Value
                        };

                        dbContext.StockIns.Add(newStockIn);

                        // Update or create stock entry
                        var existingStock = dbContext.Stocks
                            .FirstOrDefault(s => s.MedicineId == Medicine.MedicineId);

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
                                MedicineId = Medicine.MedicineId,
                                Quantity = StockinQuantity,
                                UsableQuantity = isUsable ? StockinQuantity : 0,
                                LastUpdated = ImportDate ?? DateTime.Now
                            };
                            dbContext.Stocks.Add(newStock);
                        }
                    }

                    dbContext.SaveChanges();

                    MessageBoxService.ShowSuccess(
                        "Cập nhật thông tin thuốc thành công!",
                        "Thành công"
                    );

                    // Refresh data after saving
                    RefreshMedicineData();

                    // Reset StockIn fields for next entry
                    StockinQuantity = 1;
                    ImportDate = DateTime.Now;
                    StockinExpiryDate = DateTime.Now.AddYears(1);
                }
                else
                {
                    MessageBoxService.ShowError(
                        "Không tìm thấy thuốc trong cơ sở dữ liệu.",
                        "Lỗi"
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Lỗi trong khi đang lưu thông tin thuốc: {ex.Message}",
                    "Lỗi"
                );
            }
        }

        private void BeginEditStockEntry(Medicine.StockInWithRemaining stockEntry)
        {
            if (stockEntry == null) return;

            CurrentStockEntry = stockEntry;

            // Initialize edit fields with current values
            EditStockQuantity = stockEntry.StockIn.Quantity;
            EditRemainQuantity = stockEntry.StockIn.RemainQuantity;
            EditUnitPrice = stockEntry.StockIn.UnitPrice;
            EditProfitMargin = stockEntry.StockIn.ProfitMargin;
            EditSellPrice = stockEntry.StockIn.SellPrice ?? 0;
            EditSupplier = SupplierList.FirstOrDefault(s => s.SupplierId == stockEntry.StockIn.SupplierId);

            // Convert DateOnly to DateTime for the date picker
            EditExpiryDate = stockEntry.StockIn.ExpiryDate.HasValue ?
                new DateTime(stockEntry.StockIn.ExpiryDate.Value.Year, stockEntry.StockIn.ExpiryDate.Value.Month, stockEntry.StockIn.ExpiryDate.Value.Day) :
                null;

            IsEditingStockEntry = true;
        }

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
                        // Calculate the quantity difference
                        int quantityDiff = EditStockQuantity - stockInToUpdate.Quantity;
                        int remainQuantityDiff = EditRemainQuantity - stockInToUpdate.RemainQuantity;

                        // Check if this stock is usable based on expiry date
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var minimumExpiryDate = today.AddDays(Medicine.MinimumDaysBeforeExpiry);

                        // Convert EditExpiryDate to DateOnly if it has a value
                        DateOnly? expiryDateOnly = EditExpiryDate.HasValue ?
                            DateOnly.FromDateTime(EditExpiryDate.Value) : null;

                        bool isUsable = !expiryDateOnly.HasValue || expiryDateOnly.Value >= minimumExpiryDate;

                        // Update StockIn record
                        stockInToUpdate.Quantity = EditStockQuantity;
                        stockInToUpdate.RemainQuantity = EditRemainQuantity;
                        stockInToUpdate.UnitPrice = EditUnitPrice;
                        stockInToUpdate.SellPrice = EditSellPrice;
                        stockInToUpdate.ProfitMargin = EditProfitMargin;
                        stockInToUpdate.TotalCost = EditUnitPrice * EditStockQuantity;
                        stockInToUpdate.ExpiryDate = expiryDateOnly;

                    

                        // Update supplier if changed
                        if (EditSupplier != null)
                        {
                            stockInToUpdate.SupplierId = EditSupplier.SupplierId;
                        }

                        // Update Stock record if quantity changed
                        if (quantityDiff != 0 || remainQuantityDiff != 0)
                        {
                            var stockToUpdate = dbContext.Stocks
                                .FirstOrDefault(s => s.MedicineId == stockInToUpdate.MedicineId);

                            if (stockToUpdate != null)
                            {
                                stockToUpdate.Quantity += remainQuantityDiff; // Update by remain quantity diff
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

                        // Refresh data after saving
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

        private void CancelEditStockEntry()
        {
            IsEditingStockEntry = false;
        }

        // Phương thức để đặt lô được chọn làm lô đang bán
        private void SetAsSellingBatch(Medicine.StockInWithRemaining stockEntry)
        {
            if (stockEntry == null) return;

            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
            {
                try
                {
                    var context = DataProvider.Instance.Context;

                    // Reset tất cả các lô thuốc của medicine này về false
                    var allBatches = context.StockIns
                        .Where(si => si.MedicineId == Medicine.MedicineId)
                        .ToList();

                    foreach (var batch in allBatches)
                    {
                        batch.IsSelling = false;
                    }

                    // Đặt lô được chọn thành lô đang bán
                    var stockInToUpdate = context.StockIns.Find(stockEntry.StockIn.StockInId);
                    if (stockInToUpdate != null)
                    {
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
                catch (Exception ex)
                {
                    // Hoàn tác giao dịch nếu có lỗi xảy ra
                    transaction.Rollback();

          

                    MessageBoxService.ShowError($"Lỗi khi cập nhật lô thuốc đang bán: {ex.Message}", "Lỗi");
                }
            }
        }

        // Property để lưu StockEntry hiện tại
        private Medicine.StockInWithRemaining CurrentStockEntry { get; set; }

        #endregion
    }
}
