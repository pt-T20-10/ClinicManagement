using ClinicManagement.Models;
using ClinicManagement.Services;
using Microsoft.EntityFrameworkCore;
using System;
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

        // For DatePicker - convert DateOnly to DateTime
        private DateTime? _expiryDateDateTime;
        public DateTime? ExpiryDateDateTime
        {
            get => _expiryDateDateTime;
            set
            {
                _expiryDateDateTime = value;
                OnPropertyChanged();
                if (value.HasValue && Medicine != null)
                {
                    Medicine.ExpiryDate = DateOnly.FromDateTime(value.Value);
                }
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
            LoadRelatedData(); // Thêm phương thức mới để load dữ liệu liên quan
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
        }
        #endregion

        #region Data Loading Methods

        // Thêm phương thức mới để load dữ liệu liên quan
        private void LoadRelatedData()
        {
            if (Medicine == null) return;

            try
            {
                // Lấy nhà cung cấp từ StockIn gần nhất
                var latestStockIn = DataProvider.Instance.Context.StockIns
                    .Include(si => si.Supplier)
                    .Where(si => si.MedicineId == Medicine.MedicineId)
                    .OrderByDescending(si => si.ImportDate)
                    .FirstOrDefault();

                // Cập nhật tên nhà cung cấp mới nhất
                if (latestStockIn?.Supplier != null)
                {
                    LatestSupplierName = latestStockIn.Supplier.SupplierName;
                }
                else
                {
                    LatestSupplierName = "Chưa có thông tin nhà cung cấp";
                }

                // Cập nhật UI
                OnPropertyChanged(nameof(LatestSupplierName));
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải thông tin liên quan: {ex.Message}", "Lỗi"    );
            }
        }

        /// <summary>
        /// Load initial data when Medicine is initialized
        /// </summary>
        private void LoadInitialStockData()
        {
            try
            {
                if (Medicine != null)
                {
                    // Instead of using GetDetailedStock which filters by expiry date,
                    // we'll directly access all StockIns and calculate remaining quantities
                    var context = DataProvider.Instance.Context;

                    // Get a refreshed medicine with all needed relationships
                    var refreshedMedicine = context.Medicines
                        .Include(m => m.StockIns)
                            .ThenInclude(si => si.Supplier) // Thêm dòng này để include Supplier
                        .Include(m => m.InvoiceDetails)
                        .Include(m => m.Category)
                        .Include(m => m.Unit)
                        .FirstOrDefault(m => m.MedicineId == Medicine.MedicineId);

                    if (refreshedMedicine != null)
                    {
                        // Temporarily clear the cache to force recalculation
                        refreshedMedicine._availableStockInsCache = null;

                        // Force calculation of all stock entries without expiry filtering
                        var allStockEntries = new List<Medicine.StockInWithRemaining>();

                        // Calculate total sold quantity
                        var totalSold = refreshedMedicine.InvoiceDetails?.Sum(id => id.Quantity) ?? 0;
                        var remainingToSubtract = totalSold;

                        // Process all stock entries in FIFO order without filtering by expiry
                        foreach (var stockIn in refreshedMedicine.StockIns.OrderBy(si => si.ImportDate))
                        {
                            var remainingInThisLot = stockIn.Quantity - Math.Min(remainingToSubtract, stockIn.Quantity);

                            allStockEntries.Add(new Medicine.StockInWithRemaining
                            {
                                StockIn = stockIn,
                                RemainingQuantity = Math.Max(0, remainingInThisLot),
                                // Đảm bảo UnitPrice và SellPrice được set đúng
                        
                            });

                            remainingToSubtract = Math.Max(0, remainingToSubtract - stockIn.Quantity);
                        }

                        DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>(
                            allStockEntries.OrderByDescending(d => d.ImportDate));

                        // Lấy thông tin nhà cung cấp mới nhất
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
                MessageBoxService.ShowError("Lỗi xảy ra trong quá trình tải dữ liệu {ex.Message}", "Lỗi"    );
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
                        .ThenInclude(si => si.Supplier) // Thêm dòng này để include Supplier
                    .Include(m => m.InvoiceDetails)
                    .Include(m => m.Category)
                    .Include(m => m.Unit)
                    .FirstOrDefault(m => m.MedicineId == Medicine.MedicineId);

                if (refreshedMedicine != null)
                {
                    // Update current Medicine properties
                    UpdateMedicineProperties(refreshedMedicine);

                    // Calculate all stock entries, including expired ones
                    var allStockEntries = new List<Medicine.StockInWithRemaining>();

                    // Calculate total sold quantity
                    var totalSold = refreshedMedicine.InvoiceDetails?.Sum(id => id.Quantity) ?? 0;
                    var remainingToSubtract = totalSold;

                    // Process all stock entries in FIFO order without filtering by expiry
                    foreach (var stockIn in refreshedMedicine.StockIns.OrderBy(si => si.ImportDate))
                    {
                        var remainingInThisLot = stockIn.Quantity - Math.Min(remainingToSubtract, stockIn.Quantity);

                        allStockEntries.Add(new Medicine.StockInWithRemaining
                        {
                            StockIn = stockIn,
                            RemainingQuantity = Math.Max(0, remainingInThisLot),
                            // Đảm bảo UnitPrice và SellPrice được set đúng
                       
                        });

                        remainingToSubtract = Math.Max(0, remainingToSubtract - stockIn.Quantity);
                    }

                    DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>(
                        allStockEntries.OrderByDescending(d => d.ImportDate));

                    // Update dropdown selections
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
                    MessageBoxService.ShowWarning("Không tìm thấy thông tin thuốc", "Cảnh báo"
                            );
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải lại dữ liệu: {ex.Message}", "Lỗi"    );
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
            // Medicine.SupplierId đã bị loại bỏ, nên không cần cập nhật
            Medicine.ExpiryDate = refreshedMedicine.ExpiryDate;
            Medicine.StockIns = refreshedMedicine.StockIns;
            Medicine.InvoiceDetails = refreshedMedicine.InvoiceDetails;
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

                // Only include suppliers that are both not deleted AND active - for editing stock entries
                SupplierList = new ObservableCollection<Supplier>(
                    context.Suppliers.Where(s => !(bool)s.IsDeleted && (bool)s.IsActive));
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu cho các ô chọn: {ex.Message}", "Lỗi"    );
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
            // Không cần cập nhật SelectedSupplier vì Medicine không còn thuộc tính SupplierId
        }

        /// <summary>
        /// Update ExpiryDateDateTime from Medicine.ExpiryDate
        /// </summary>
        private void UpdateExpiryDateTime()
        {
            if (Medicine?.ExpiryDate.HasValue == true)
            {
                ExpiryDateDateTime = new DateTime(
                    Medicine.ExpiryDate.Value.Year,
                    Medicine.ExpiryDate.Value.Month,
                    Medicine.ExpiryDate.Value.Day);
            }
            else
            {
                ExpiryDateDateTime = null;
            }
        }

        /// <summary>
        /// Load medicine details for editing
        /// </summary>
        private void LoadMedicineDetailsForEdit(Medicine medicine)
        {
            if (medicine == null) return;

            // Update dropdown selections
            UpdateDropdownSelections();
            UpdateExpiryDateTime();

            // Load additional details for StockIn
            StockinUnitPrice = medicine.CurrentUnitPrice;
            StockinSellPrice = medicine.CurrentSellPrice;

            // Get profit margin information from most recent StockIn
            var latestStockIn = medicine.StockIns?.OrderByDescending(si => si.ImportDate).FirstOrDefault();
            StockProfitMargin = latestStockIn?.ProfitMargin ?? 0;

            // Get latest import date
            ImportDate = medicine.LatestImportDate ?? DateTime.Now;
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
                    medicineToUpdate.Name = Medicine.Name;
                    medicineToUpdate.CategoryId = SelectedCategory?.CategoryId;
                    medicineToUpdate.UnitId = SelectedUnit?.UnitId;
                    // Không còn cập nhật SupplierId nữa vì đã chuyển sang StockIn

                    if (ExpiryDateDateTime.HasValue)
                    {
                        medicineToUpdate.ExpiryDate = DateOnly.FromDateTime(ExpiryDateDateTime.Value);
                    }
                    else
                    {
                        medicineToUpdate.ExpiryDate = null;
                    }

                    // Check if we need to add a new StockIn entry
                    if (StockinQuantity > 0 && StockinUnitPrice > 0)
                    {
                        // Lấy thông tin nhà cung cấp từ lô nhập kho gần đây nhất
                        var latestStockIn = dbContext.StockIns
                            .Where(si => si.MedicineId == Medicine.MedicineId)
                            .OrderByDescending(si => si.ImportDate)
                            .FirstOrDefault();

                        // Lấy SupplierId từ EditSupplier nếu có, ngược lại sử dụng SupplierId từ StockIn gần nhất
                        int? supplierId = EditSupplier?.SupplierId ?? latestStockIn?.SupplierId;

                        if (!supplierId.HasValue)
                        {
                            MessageBoxService.ShowError(
                                "Không thể thêm lô nhập mới vì thiếu thông tin nhà cung cấp.",
                                "Lỗi"
                                 
                                 );
                            return;
                        }

                        var newStockIn = new StockIn
                        {
                            MedicineId = Medicine.MedicineId,
                            Quantity = StockinQuantity,
                            ImportDate = ImportDate ?? DateTime.Now,
                            UnitPrice = StockinUnitPrice,
                            SellPrice = StockinSellPrice,
                            ProfitMargin = StockProfitMargin,
                            TotalCost = StockinUnitPrice * StockinQuantity,
                            SupplierId = supplierId.Value // Thêm SupplierId vào StockIn
                        };

                        dbContext.StockIns.Add(newStockIn);

                        // Update or create stock entry
                        var existingStock = dbContext.Stocks
                            .FirstOrDefault(s => s.MedicineId == Medicine.MedicineId);

                        if (existingStock != null)
                        {
                            // Update existing stock
                            existingStock.Quantity += StockinQuantity;
                            existingStock.LastUpdated = ImportDate ?? DateTime.Now;
                        }
                        else
                        {
                            // Create new stock entry
                            var newStock = new Stock
                            {
                                MedicineId = Medicine.MedicineId,
                                Quantity = StockinQuantity,
                                LastUpdated = ImportDate ?? DateTime.Now
                            };
                            dbContext.Stocks.Add(newStock);
                        }
                    }

                    dbContext.SaveChanges();

                    MessageBoxService.ShowSuccess(
                        "Câp nhật thông tin thuốc thành công!",
                        "Thành công"
                         
                          );

                    // Refresh data after saving
                    RefreshMedicineData();
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
            EditStockQuantity = stockEntry.StockIn.Quantity;
            EditUnitPrice = stockEntry.UnitPrice;
            EditProfitMargin = stockEntry.StockIn.ProfitMargin;
            EditSellPrice = stockEntry.SellPrice ?? 0;

            // Cập nhật chọn nhà cung cấp
            EditSupplier = SupplierList.FirstOrDefault(s => s.SupplierId == stockEntry.StockIn.SupplierId);

            IsEditingStockEntry = true;
        }

        private bool CanSaveStockEntry()
        {
            return IsEditingStockEntry &&
                   SelectedStockEntry != null &&
                   EditStockQuantity > 0 &&
                   EditUnitPrice > 0 &&
                   EditSellPrice > 0 &&
                   EditSupplier != null; // Thêm kiểm tra có chọn nhà cung cấp hay không
        }

        private void SaveEditedStockEntry()
        {
            if (SelectedStockEntry == null || !IsEditingStockEntry) return;

            try
            {
                var dbContext = DataProvider.Instance.Context;

                var stockInToUpdate = dbContext.StockIns.Find(SelectedStockEntry.StockIn.StockInId);

                if (stockInToUpdate != null)
                {
                    // Calculate the quantity difference
                    int quantityDiff = EditStockQuantity - stockInToUpdate.Quantity;

                    // Update StockIn record
                    stockInToUpdate.Quantity = EditStockQuantity;
                    stockInToUpdate.UnitPrice = EditUnitPrice;
                    stockInToUpdate.SellPrice = EditSellPrice;
                    stockInToUpdate.ProfitMargin = EditProfitMargin;
                    stockInToUpdate.TotalCost = EditUnitPrice * EditStockQuantity;

                    // Cập nhật nhà cung cấp
                    if (EditSupplier != null)
                    {
                        stockInToUpdate.SupplierId = EditSupplier.SupplierId;
                    }

                    // Update Stock record if quantity changed
                    if (quantityDiff != 0)
                    {
                        var stockToUpdate = dbContext.Stocks
                            .FirstOrDefault(s => s.MedicineId == stockInToUpdate.MedicineId);

                        if (stockToUpdate != null)
                        {
                            stockToUpdate.Quantity += quantityDiff;
                            stockToUpdate.LastUpdated = DateTime.Now;
                        }
                    }

                    dbContext.SaveChanges();

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
                MessageBoxService.ShowError(
                    $"Lỗi khi lưu thông tin lô nhập kho: {ex.Message}",
                    "Lỗi"
                     
                     );
            }
        }

        private void CancelEditStockEntry()
        {
            IsEditingStockEntry = false;
        }

        // Property để lưu StockEntry hiện tại
        private Medicine.StockInWithRemaining CurrentStockEntry { get; set; }

        #endregion
    }
}
