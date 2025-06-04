using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
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

        private Supplier _selectedSupplier;
        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
                OnPropertyChanged();
                if (value != null && Medicine != null)
                    Medicine.SupplierId = value.SupplierId;
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

        /// <summary>
        /// Load initial data when Medicine is initialized
        /// </summary>
        private void LoadInitialStockData()
        {
            try
            {
                if (Medicine != null)
                {
                    // Get stock details from current Medicine
                    var details = Medicine.GetDetailedStock();
                    DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>(
                        details.OrderByDescending(d => d.ImportDate));
                }
                else
                {
                    DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initial data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var refreshedMedicine = DataProvider.Instance.Context.Medicines
                    .Include(m => m.StockIns)
                    .Include(m => m.InvoiceDetails)
                    .Include(m => m.Category)
                    .Include(m => m.Unit)
                    .Include(m => m.Supplier)
                    .FirstOrDefault(m => m.MedicineId == Medicine.MedicineId);

                if (refreshedMedicine != null)
                {
                    // Update current Medicine properties
                    UpdateMedicineProperties(refreshedMedicine);

                    // Update DetailedStockList
                    var details = refreshedMedicine.GetDetailedStock();
                    DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>(
                        details.OrderByDescending(d => d.ImportDate));

                    // Update dropdown selections
                    UpdateDropdownSelections();
                }
                else
                {
                    MessageBox.Show("Medicine information not found in database.", "Notice",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            Medicine.SupplierId = refreshedMedicine.SupplierId;
            Medicine.ExpiryDate = refreshedMedicine.ExpiryDate;
            Medicine.StockIns = refreshedMedicine.StockIns;
            Medicine.InvoiceDetails = refreshedMedicine.InvoiceDetails;
            Medicine.Category = refreshedMedicine.Category;
            Medicine.Unit = refreshedMedicine.Unit;
            Medicine.Supplier = refreshedMedicine.Supplier;

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
                MessageBox.Show($"Error loading dropdown data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            SelectedSupplier = SupplierList?.FirstOrDefault(s => s.SupplierId == Medicine.SupplierId);
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
                   SelectedUnit != null &&
                   SelectedSupplier != null;
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
                    medicineToUpdate.SupplierId = SelectedSupplier?.SupplierId;

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
                        var newStockIn = new StockIn
                        {
                            MedicineId = Medicine.MedicineId,
                            Quantity = StockinQuantity,
                            ImportDate = ImportDate ?? DateTime.Now,
                            UnitPrice = StockinUnitPrice,
                            SellPrice = StockinSellPrice,
                            ProfitMargin = StockProfitMargin,
                            TotalCost = StockinUnitPrice * StockinQuantity
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

                    MessageBox.Show(
                        "Medicine information has been updated successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Refresh data after saving
                    RefreshMedicineData();
                }
                else
                {
                    MessageBox.Show(
                        "Medicine not found in database.",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error saving medicine information: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BeginEditStockEntry(Medicine.StockInWithRemaining stockEntry)
        {
            if (stockEntry == null) return;

            // Load data from the selected stock entry
            EditStockQuantity = stockEntry.StockIn.Quantity;
            EditUnitPrice = stockEntry.UnitPrice;
            EditSellPrice = stockEntry.SellPrice ?? 0;
            EditProfitMargin = stockEntry.StockIn.ProfitMargin;

            IsEditingStockEntry = true;
        }

        private bool CanSaveStockEntry()
        {
            return IsEditingStockEntry &&
                   SelectedStockEntry != null &&
                   EditStockQuantity > 0 &&
                   EditUnitPrice > 0 &&
                   EditSellPrice > 0;
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

                    MessageBox.Show(
                        "Thông tin lô nhập kho đã được cập nhật thành công!",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Refresh data after saving
                    RefreshMedicineData();
                    IsEditingStockEntry = false;
                }
                else
                {
                    MessageBox.Show(
                        "Không tìm thấy lô nhập kho trong cơ sở dữ liệu.",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi lưu thông tin lô nhập kho: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelEditStockEntry()
        {
            IsEditingStockEntry = false;
        }
        #endregion
    }
}
