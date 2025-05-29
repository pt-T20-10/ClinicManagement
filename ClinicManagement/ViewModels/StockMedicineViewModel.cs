using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class StockMedicineViewModel : BaseViewModel, IDataErrorInfo
    {
        public string Error => null;

        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false; // Flag to control when to validate

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

        #region StockProperties

        private ObservableCollection<Stock> _allStockMedicine;
        private string _SearchStockMedicine;
        public string SearchStockMedicine
        {
            get => _SearchStockMedicine;
            set
            {
                _SearchStockMedicine = value;
                OnPropertyChanged();
                ExecuteSearchStockMedicine();
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
                ExecuteSearchStockMedicine();
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
                ExecuteSearchStockMedicine();
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
                ExecuteSearchStockMedicine();
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
            //StockMedicine Commands
            SearchStockMedicineCommand = new RelayCommand<object>
            (   (p) => ExecuteSearchStockMedicine(),
                (p) => true
            );//StockMedicine Commands
            ResetStockFiltersCommand = new RelayCommand<object>
            (   (p) => ExecuteResetStockFilters(),
                (p) => true
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
        #endregion
        private void ExecuteSupplierRefresh()
        {
            SupplierList = new ObservableCollection<Supplier>(
                   DataProvider.Instance.Context.Suppliers
                       .Where(s => (bool)!s.IsDeleted)
                       .ToList()
               );
            ClearForm();

        }
        private void ExecuteResetStockFilters()
        {
            
            SelectedStockCategoryName = null;
            SelectedStockSupplier = null;
            SelectedStockUnitId = null;
            SearchStockMedicine = ""; // Clear search text
            // Reset the stock medicine list to show all items
            ListStockMedicine = new ObservableCollection<Stock>(_allStockMedicine);
        }
        private void ExecuteSearchStockMedicine()
        {
            if (_allStockMedicine == null || _allStockMedicine.Count == 0)
            {
                ListStockMedicine = new ObservableCollection<Stock>();
                return;
            }

            // Start with all items
            var filteredList = _allStockMedicine.AsEnumerable();

            // Filter by category if selected
            if (SelectedStockCategoryId.HasValue && SelectedStockCategoryId.Value > 0)
            {
                filteredList = filteredList.Where(s =>
                    s.Medicine?.CategoryId == SelectedStockCategoryId.Value);
            }
            if(SelectedStockSupplierId.HasValue && SelectedStockSupplierId.Value > 0)
            {
                filteredList = filteredList.Where(s =>
                    s.Medicine?.SupplierId == SelectedStockSupplierId.Value);
            }
            if(SelectedStockUnitId.HasValue && SelectedStockUnitId.Value > 0)
            {
                filteredList = filteredList.Where(s =>
                    s.Medicine?.UnitId == SelectedStockUnitId.Value);
            }  

            // Filter by search text if provided
            if (!string.IsNullOrWhiteSpace(SearchStockMedicine))
            {
                var searchTerm = SearchStockMedicine.ToLower().Trim();
                filteredList = filteredList.Where(s =>
                    s.Medicine?.Name != null &&
                    s.Medicine.Name.ToLower().Contains(searchTerm));
            }

            // Apply filters and update the list
            ListStockMedicine = new ObservableCollection<Stock>(filteredList.ToList());
        }


     
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

            ListStockMedicine = new ObservableCollection<Stock>(
              DataProvider.Instance.Context.Stocks
              .Include(d => d.Medicine)
                  .ThenInclude(m => m.InvoiceDetails) // Cần để tính số lượng đã bán
              .Include(d => d.Medicine.Category)
              .Include(d => d.Medicine.Unit)
              .Include(d => d.Medicine.Supplier)
              .Include(d => d.Medicine.StockIns)
              .ToList()
             );
            // Initialize _allStockMedicine with the same data as ListMedicine
            _allStockMedicine = new ObservableCollection<Stock>(ListMedicine);

            // Initialize ListStockMedicine with the same data initially
            ListStockMedicine = new ObservableCollection<Stock>(ListMedicine);
        } 
        
        #endregion
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

    }
}
