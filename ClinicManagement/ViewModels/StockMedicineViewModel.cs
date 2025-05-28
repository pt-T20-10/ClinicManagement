using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class StockMedicineViewModel : BaseViewModel
    {
        #region Properties
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

        private string _TotalQuantity;
        public string TotalQuantity
        {
            get => _TotalQuantity;
            set
            {
                _TotalQuantity = value;
                OnPropertyChanged();
                var totalValue = ListMedicine.Sum(m => m.Medicine.CurrentUnitPrice).ToString() ?? "0";
                _TotalValue = (Convert.ToInt32(TotalQuantity) * Convert.ToDecimal(totalValue)).ToString();
            }
        }
        private string _TotalValue;
        public string TotalValue
        {
            get => _TotalValue;
            set
            {
                _TotalValue = value;
                OnPropertyChanged();
            
            }
        }


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

        #endregion
        #region Commands
        // Unit Commands
        public ICommand AddUnitCommand { get; set; }
        public ICommand EditUnitCommand { get; set; }
        public ICommand DeleteUnitCommand { get; set; }

        // Supplier Commands
        public ICommand AddSuplierCommand { get; set; }
        public ICommand EditSuplierCommand { get; set; }
        public ICommand DeleteSuplierCommand { get; set; }

        // Category Commands
        public ICommand AddCategoryCommand { get; set; }
        public ICommand EditCategoryCommand { get; set; }
        public ICommand DeleteCategoryCommand { get; set; }



        public ICommand OpenDoctorDetailsCommand { get; set; }

        // Specialty Commands
        public ICommand AddCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        #endregion
        #endregion

        public StockMedicineViewModel()
        {
            LoadData();
            InitializeCommands();   
        }

        #region Methods
        #region InitializeCommands
        private void InitializeCommands()
        {
            // Specialty Commands
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
        }

        #region Unit Methods
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
                    $"Bạn có chắc muốn sửa Đơn vị '{SelectedUnit.UnitName}' thành '{UnitName}' không?",
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
                    MessageBox.Show("Tên Đơn vị này đã tồn tại.");
                    return;
                }

                // Update specialty
                var specialtyToUpdate = DataProvider.Instance.Context.Units
                    .FirstOrDefault(s => s.UnitId == SelectedUnit.UnitId);

                if (specialtyToUpdate == null)
                {
                    MessageBox.Show("Không tìm thấy đơn vị cần sửa.");
                    return;
                }

                specialtyToUpdate.UnitName = UnitName;
                specialtyToUpdate.Description = UnitDescription ?? "";
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



        #endregion
        #region LoadData
        public void LoadData()
        {
            ListMedicine = new ObservableCollection<Stock>(
               DataProvider.Instance.Context.Stocks
               .Include(d => d.Medicine)
                   .ThenInclude(m => m.InvoiceDetails) // Cần để tính số lượng đã bán
               .Include(d => d.Medicine.Category)
               .Include(d => d.Medicine.Unit)
               .Include(d => d.Medicine.Supplier)
               .Include(d => d.Medicine.StockIns)
               .ToList()
              );
               TotalQuantity = ListMedicine.Sum(m => m.Quantity).ToString() ?? "0";

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
        }
        #endregion
        #endregion
    }
}
