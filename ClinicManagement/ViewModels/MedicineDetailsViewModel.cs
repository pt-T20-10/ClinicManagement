using ClinicManagement.Models;
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
                // Chỉ load dữ liệu ban đầu, không refresh liên tục
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

        public string DialogTitle => Medicine != null
            ? $"Chi tiết các lô thuốc: {Medicine.Name}"
            : "Chi tiết lô thuốc";

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

        // For DatePicker - convert DateOnly to DateTime and back
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

        #region Commands
        public ICommand CloseCommand { get; set; }
        public ICommand SaveChangesCommand { get; set; }
        public ICommand RefreshDataCommand { get; set; }
        #endregion
        #endregion

        public MedicineDetailsViewModel(Medicine medicine, Window window)
        {
            _window = window;
            Medicine = medicine;
            InitializeCommands();
            LoadDropdownData();
            UpdateMedicineInfo();
        }

        #region InitializateCommands
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
        }
        #endregion

        #region Data Loading Methods

        /// <summary>
        /// Load dữ liệu ban đầu khi khởi tạo Medicine
        /// </summary>
        private void LoadInitialStockData()
        {
            try
            {
                if (Medicine != null)
                {
                    // Lấy dữ liệu chi tiết kho từ Medicine hiện tại
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
                MessageBox.Show($"Lỗi khi tải dữ liệu ban đầu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>();
            }
        }

        /// <summary>
        /// Refresh dữ liệu từ database mà không gây vòng lặp
        /// </summary>
        private void RefreshMedicineData()
        {
            try
            {
                if (Medicine == null) return;

                // Lấy dữ liệu mới nhất từ database
                var refreshedMedicine = DataProvider.Instance.Context.Medicines
                    .Include(m => m.StockIns)
                    .Include(m => m.InvoiceDetails)
                    .Include(m => m.Category)
                    .Include(m => m.Unit)
                    .Include(m => m.Supplier)
                    .FirstOrDefault(m => m.MedicineId == Medicine.MedicineId);

                if (refreshedMedicine != null)
                {
                    // Cập nhật các thuộc tính của Medicine hiện tại
                    UpdateMedicineProperties(refreshedMedicine);

                    // Cập nhật DetailedStockList
                    var details = refreshedMedicine.GetDetailedStock();
                    DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>(
                        details.OrderByDescending(d => d.ImportDate));

                    // Cập nhật các dropdown selection
                    UpdateDropdownSelections();

                    OnPropertyChanged(nameof(DialogTitle));
                }
                else
                {
                    MessageBox.Show("Không tìm thấy thông tin thuốc trong cơ sở dữ liệu.", "Thông báo",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi làm mới dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cập nhật các thuộc tính của Medicine mà không kích hoạt setter
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

            // Notify UI về thay đổi
            OnPropertyChanged(nameof(Medicine));
        }

        /// <summary>
        /// Load dữ liệu cho các dropdown
        /// </summary>
        private void LoadDropdownData()
        {
            try
            {
                var context = DataProvider.Instance.Context;

                CategoryList = new ObservableCollection<MedicineCategory>(
                    context.MedicineCategories);

                UnitList = new ObservableCollection<Unit>(
                    context.Units);

                SupplierList = new ObservableCollection<Supplier>(
                    context.Suppliers);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu dropdown: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cập nhật thông tin Medicine và các dropdown selection
        /// </summary>
        private void UpdateMedicineInfo()
        {
            if (Medicine == null) return;

            UpdateDropdownSelections();
            UpdateExpiryDateTime();
        }

        /// <summary>
        /// Cập nhật các lựa chọn trong dropdown
        /// </summary>
        private void UpdateDropdownSelections()
        {
            if (Medicine == null) return;

            SelectedCategory = CategoryList?.FirstOrDefault(c => c.CategoryId == Medicine.CategoryId);
            SelectedUnit = UnitList?.FirstOrDefault(u => u.UnitId == Medicine.UnitId);
            SelectedSupplier = SupplierList?.FirstOrDefault(s => s.SupplierId == Medicine.SupplierId);
        }

        /// <summary>
        /// Cập nhật ExpiryDateDateTime từ Medicine.ExpiryDate
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

                // Lấy thuốc từ cơ sở dữ liệu
                var medicineToUpdate = dbContext.Medicines.Find(Medicine.MedicineId);

                if (medicineToUpdate != null)
                {
                    // Cập nhật các trường thuốc
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

                    dbContext.SaveChanges();

                    MessageBox.Show(
                        "Thông tin thuốc đã được cập nhật thành công!",
                        "Thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Làm mới dữ liệu sau khi lưu
                    RefreshMedicineData();
                }
                else
                {
                    MessageBox.Show(
                        "Không tìm thấy thuốc trong cơ sở dữ liệu để cập nhật.",
                        "Lỗi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi lưu thông tin thuốc: {ex.Message}",
                    "Lỗi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion
    }
}