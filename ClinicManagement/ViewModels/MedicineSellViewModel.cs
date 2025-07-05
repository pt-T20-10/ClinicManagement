using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class MedicineSellViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties
        // Cờ kiểm soát việc khởi tạo dữ liệu
        private bool _isInitialized = false;
        public bool IsInitialized
        {
            get => _isInitialized;
            private set
            {
                _isInitialized = value;
                OnPropertyChanged();
            }
        }
        // Danh sách thuốc
        private ObservableCollection<Medicine> _medicineList;
        public ObservableCollection<Medicine> MedicineList
        {
            get => _medicineList ??= new ObservableCollection<Medicine>();
            set
            {
                _medicineList = value;
                OnPropertyChanged();
            }
        }

        // Danh sách giỏ hàng
        private ObservableCollection<CartItem> _cartItems;
        public ObservableCollection<CartItem> CartItems
        {
            get
            {
                if (_cartItems == null)
                {
                    _cartItems = new ObservableCollection<CartItem>();
                    _cartItems.CollectionChanged += CartItems_CollectionChanged;
                }
                return _cartItems;
            }
            set
            {
                if (_cartItems != null)
                {
                    _cartItems.CollectionChanged -= CartItems_CollectionChanged;

                    foreach (var item in _cartItems)
                    {
                        item.PropertyChanged -= CartItem_PropertyChanged;
                    }
                }

                _cartItems = value;

                if (_cartItems != null)
                {
                    _cartItems.CollectionChanged += CartItems_CollectionChanged;

                    foreach (var item in _cartItems)
                    {
                        item.PropertyChanged += CartItem_PropertyChanged;
                    }
                }

                OnPropertyChanged();
                UpdateCartTotals();
            }
        }

        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
            }
        }

        // Handle collection changes (items added or removed)
        private void CartItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Subscribe to new items
            if (e.NewItems != null)
            {
                foreach (CartItem item in e.NewItems)
                {
                    item.PropertyChanged += CartItem_PropertyChanged;
                }
            }

            // Unsubscribe from removed items
            if (e.OldItems != null)
            {
                foreach (CartItem item in e.OldItems)
                {
                    item.PropertyChanged -= CartItem_PropertyChanged;
                }
            }

            // Update totals
            UpdateCartTotals();
        }

        // Handle property changes within cart items (e.g., quantity changes)
        private void CartItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // If the Quantity or LineTotal property changed, update the cart totals
            if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.LineTotal))
            {
                UpdateCartTotals();
            }
        }

        // Update the cart total properties and notify UI
        private void UpdateCartTotals()
        {
            OnPropertyChanged(nameof(TotalItems));
            OnPropertyChanged(nameof(TotalAmount));
        }

        // Danh sách loại thuốc để lọc
        private ObservableCollection<MedicineCategory> _categoryList;
        public ObservableCollection<MedicineCategory> CategoryList
        {
            get => _categoryList ??= new ObservableCollection<MedicineCategory>();
            set
            {
                _categoryList = value;
                OnPropertyChanged();
            }
        }

        // Tổng số tiền
        public decimal TotalAmount => CartItems?.Sum(i => i.LineTotal) ?? 0;

        // Tổng số mục trong giỏ
        public int TotalItems => CartItems?.Sum(i => i.Quantity) ?? 0;

        // Số hóa đơn (khi sửa hóa đơn hoặc đề xuất số mới)
        private string _invoiceNumber;
        public string InvoiceNumber
        {
            get => _invoiceNumber;
            set
            {
                _invoiceNumber = value;
                OnPropertyChanged();
            }
        }

        // Thông tin bệnh nhân
        private string _patientName;
        public string PatientName
        {
            get => _patientName;
            set
            {
                _patientName = value;
                OnPropertyChanged();
                if (_touchedFields.Contains(nameof(PatientName)))
                    OnPropertyChanged(nameof(Error));
            }
        }

        private string _phone;
        public string Phone
        {
            get => _phone;
            set
            {
                _phone = value;
                OnPropertyChanged();
                if (_touchedFields.Contains(nameof(Phone)))
                    OnPropertyChanged(nameof(Error));
            }
        }

        // Thuốc được chọn
        private Medicine _selectedMedicine;
        public Medicine SelectedMedicine
        {
            get => _selectedMedicine;
            set
            {
                _selectedMedicine = value;
                OnPropertyChanged();
            }
        }

        // Loại thuốc được chọn
        private MedicineCategory _selectedCategory;
        public MedicineCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                FilterMedicines();
            }
        }

        // Text tìm kiếm
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        // Giảm giá
        private decimal _discount;
        public decimal Discount
        {
            get => _discount;
            set
            {
                _discount = value;
                OnPropertyChanged();
                UpdateCartTotals();
            }
        }

        // Hóa đơn (khi được truyền vào từ hóa đơn có sẵn)
        private Invoice _currentInvoice;
        public Invoice CurrentInvoice
        {
            get => _currentInvoice;
            set
            {
                _currentInvoice = value;
                OnPropertyChanged(nameof(_currentInvoice));

                if (_currentInvoice != null)
                {
                    // Cập nhật thông tin từ hóa đơn
                    InvoiceNumber = $"{_currentInvoice.InvoiceId}";

                    if (_currentInvoice.Patient != null)
                    {
                        PatientName = _currentInvoice.Patient.FullName;
                        Phone = _currentInvoice.Patient.Phone;
                    }

                    Discount = _currentInvoice.Discount ?? 0;

                    // Tải các mục trong hóa đơn
                    LoadInvoiceItems();
                }
            }
        }

        // Bệnh nhân đã chọn
        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                OnPropertyChanged();

                if (_selectedPatient != null)
                {
                    PatientName = _selectedPatient.FullName;
                    Phone = _selectedPatient.Phone;

                    // Áp dụng giảm giá theo loại bệnh nhân nếu có
                    if (_selectedPatient.PatientType != null)
                    {
                        Discount = _selectedPatient.PatientType.Discount ?? 0;
                    }
                }
            }
        }

        // Validation properties
        public string Error => null;
        private HashSet<string> _touchedFields = new HashSet<string>();
        private bool _isValidating = false;

        public string this[string columnName]
        {
            get
            {
                // Don't validate until user has interacted with the field
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(PatientName):
                        if (!string.IsNullOrWhiteSpace(PatientName))
                        {
                            if (PatientName.Length < 2)
                            {
                                error = "Tên bệnh nhân phải có ít nhất 2 ký tự";
                            }
                            else if (PatientName.Length > 100)
                            {
                                error = "Tên bệnh nhân không được vượt quá 100 ký tự";
                            }
                            else if (!Regex.IsMatch(PatientName, @"^[a-zA-ZÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỂẾưăạảấầẩẫậắằẳẵặẹẻẽềểếỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ\s]+$"))
                            {
                                error = "Tên bệnh nhân chỉ được chứa chữ cái và khoảng trắng";
                            }
                        }
                        break;

                    case nameof(Phone):
                        if (!string.IsNullOrWhiteSpace(Phone) &&
                            !Regex.IsMatch(Phone, @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            error = "Số điện thoại không đúng định dạng (VD: 0901234567)";
                        }
                        break;
                }

                return error;
            }
        }

        public bool HasErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(PatientName)]) ||
                       !string.IsNullOrEmpty(this[nameof(Phone)]);
            }
        }

        #endregion

        #region Commands
        public ICommand SearchCommand { get; set; }
        public ICommand AddToCartCommand { get; set; }
        public ICommand RemoveFromCartCommand { get; set; }
        public ICommand ClearCartCommand { get; set; }
        public ICommand CheckoutCommand { get; set; }
        public ICommand AddNewPatientCommand { get; set; }
        public ICommand FindPatientCommand { get; set; }
        public ICommand LoadedUCCommand { get; set; }
        public ICommand InitializeDataCommand { get; set; }
        #endregion

        #region Constructors
        // Constructor mặc định - Không tự động load dữ liệu
        public MedicineSellViewModel()
        {
            InitializeCommands();
            // Không gọi LoadData() ở đây để tránh tải dữ liệu trước khi đăng nhập
        }

        // Constructor với parameter lazy initialization
        public MedicineSellViewModel(bool lazyInitialization)
        {
            InitializeCommands();
            // Chỉ khởi tạo dữ liệu nếu không yêu cầu lazy initialization
            if (!lazyInitialization)
            {
                IsInitialized = true;
                LoadData();
            }
        }

        // Constructor với Invoice - Luôn khởi tạo dữ liệu ngay lập tức
        public MedicineSellViewModel(Invoice invoice)
        {
            InitializeCommands();
            IsInitialized = true;
            LoadData();
            CurrentInvoice = invoice;
        }
        #endregion

        private void InitializeCommands()
        {
            // Command khởi tạo dữ liệu - mới
            InitializeDataCommand = new RelayCommand<object>(
                p => {
                    if (!IsInitialized)
                    {
                        IsInitialized = true;
                        LoadData();
                    }
                },
                p => !IsInitialized
            );

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

                           // Khởi tạo dữ liệu khi user control được load và đã đăng nhập
                           if (!IsInitialized)
                           {
                               IsInitialized = true;
                               LoadData();
                           }
                       }
                   }
               },
               (userControl) => true
           );

            // Các command khác cần kiểm tra IsInitialized
            SearchCommand = new RelayCommand<object>(
                p => SearchMedicines(),
                p => IsInitialized // Thêm điều kiện kiểm tra
            );

            AddToCartCommand = new RelayCommand<Medicine>(
                p => AddToCart(p),
                p => IsInitialized && p != null && p.TotalStockQuantity > 0 // Thêm điều kiện kiểm tra
            );

            RemoveFromCartCommand = new RelayCommand<CartItem>(
                p => RemoveFromCart(p),
                p => IsInitialized && p != null // Thêm điều kiện kiểm tra
            );

            ClearCartCommand = new RelayCommand<object>(
                p => ClearCart(),
                p => IsInitialized && CartItems != null && CartItems.Count > 0 // Thêm điều kiện kiểm tra
            );

            CheckoutCommand = new RelayCommand<object>(
                p => Checkout(),
                p => IsInitialized && CartItems != null && CartItems.Count > 0 // Thêm điều kiện kiểm tra
            );

            AddNewPatientCommand = new RelayCommand<object>(
                p => AddNewPatient(),
                p => IsInitialized // Thêm điều kiện kiểm tra
            );

            FindPatientCommand = new RelayCommand<object>(
                p => FindPatient(),
                p => IsInitialized && (!string.IsNullOrWhiteSpace(PatientName) || !string.IsNullOrWhiteSpace(Phone)) // Thêm điều kiện kiểm tra
            );
        }


        public void LoadData()
        {
            // Kiểm tra xem đã khởi tạo chưa
            if (!IsInitialized)
            {
                return;
            }

            try
            {
                LoadMedicines();
                LoadCategories();

                // Đề xuất số hóa đơn mới nếu không có hóa đơn hiện tại
                if (string.IsNullOrEmpty(InvoiceNumber))
                {
                    var lastInvoiceId = DataProvider.Instance.Context.Invoices
                        .OrderByDescending(i => i.InvoiceId)
                        .Select(i => i.InvoiceId)
                        .FirstOrDefault();

                    InvoiceNumber = $"#{lastInvoiceId + 1} (Mới)";
                }
            }
            catch (Exception ex)
            {
                // Chỉ hiển thị lỗi khi ứng dụng đã được khởi tạo hoàn toàn
                if (Application.Current != null &&
                    Application.Current.MainWindow != null &&
                    Application.Current.MainWindow.IsLoaded)
                {
                    MessageBoxService.ShowError($"Không thể tải dữ liệu: {ex.Message}", "Lỗi");
                }
                else
                {
               
                }
            }
        }

        private void LoadMedicines()
        {
            // Kiểm tra xem đã khởi tạo chưa
            if (!IsInitialized)
            {
                return;
            }

            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);

                // Load medicines with eager loading of related entities
                var medicines = DataProvider.Instance.Context.Medicines
                    .AsNoTracking() // Use AsNoTracking for better performance
                    .Where(m => m.IsDeleted != true)
                    .Include(m => m.Category)
                    .Include(m => m.Unit)
                    .ToList();

                // Process stock data separately to avoid LINQ translation errors
                foreach (var medicine in medicines)
                {
                    // Manually load StockIns to get RemainQuantity data
                    var stockIns = DataProvider.Instance.Context.StockIns
                        .AsNoTracking()
                        .Where(si => si.MedicineId == medicine.MedicineId)
                        .ToList();

                    // Replace the collections with our manually loaded data
                    medicine.StockIns = stockIns;

                    // Reset the cache to ensure fresh calculations
                    medicine._availableStockInsCache = null;

                    // Set default quantity to 1
                    medicine.TempQuantity = 1;
                }

                // Filter out medicines that don't have valid stock
                var validMedicines = medicines.Where(m =>
                    m.StockIns.Any(si => si.RemainQuantity > 0)
                ).ToList();

                // Chỉ hiển thị cảnh báo khi ứng dụng đã hoàn toàn tải xong
                if (Application.Current != null &&
                    Application.Current.MainWindow != null &&
                    Application.Current.MainWindow.IsLoaded)
                {
                    // Check and warn about medicines that may be near/past expiry but still have stock
                    foreach (var medicine in validMedicines)
                    {
                        // Kiểm tra có lô hết hạn chưa được tiêu hủy không
                        bool hasNonTerminatedExpiredStock = medicine.StockIns.Any(si =>
                            si.RemainQuantity > 0 &&
                            si.ExpiryDate.HasValue &&
                            si.ExpiryDate.Value < today &&
                            !si.IsTerminated);

                        if (hasNonTerminatedExpiredStock)
                        {
                            MessageBoxService.ShowWarning(
                                $"Lưu ý: {medicine.Name} có lô thuốc đã hết hạn nhưng chưa được tiêu hủy. Vui lòng tiêu hủy các lô đã hết hạn.",
                                "Cảnh báo thuốc hết hạn"
                            );
                        }
                        // Kiểm tra xem có lô sắp hết hạn và là lô cuối cùng không
                        else if (medicine.HasNearExpiryStock && medicine.IsLastestStockIn)
                        {
                            // Lấy lô đang sử dụng để bán và sắp hết hạn
                            var nearExpirySellingBatch = medicine.StockIns.FirstOrDefault(si =>
                                si.IsSelling &&
                                si.RemainQuantity > 0 &&
                                si.ExpiryDate.HasValue &&
                                si.ExpiryDate.Value >= today &&
                                si.ExpiryDate.Value <= today.AddDays(Medicine.MinimumDaysBeforeExpiry) &&
                                !si.IsTerminated);

                            if (nearExpirySellingBatch != null)
                            {
                                int daysRemaining = (nearExpirySellingBatch.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).Days;

                                MessageBoxService.ShowWarning(
                                    $"Lưu ý: {medicine.Name} đang sử dụng lô cuối cùng và sẽ hết hạn sau {daysRemaining} ngày. Cần nhập thêm hàng.",
                                    "Cảnh báo hạn sử dụng"
                                );
                            }
                        }

                        // Kiểm tra nếu lô đang bán đã bị tiêu hủy
                        var sellingBatchIsTerminated = medicine.StockIns.Any(si =>
                            si.IsSelling &&
                            si.IsTerminated);

                        if (sellingBatchIsTerminated)
                        {
                            MessageBoxService.ShowWarning(
                                $"Cảnh báo: {medicine.Name} có lô đang bán đã được đánh dấu là tiêu hủy. Cần chọn lô khác để bán.",
                                "Cảnh báo lô đã tiêu hủy"
                            );
                        }
                    }
                }

                MedicineList = new ObservableCollection<Medicine>(validMedicines);

                // Apply view filtering for searching
                CollectionViewSource.GetDefaultView(MedicineList).Filter = item =>
                {
                    if (item is Medicine medicine)
                    {
                        return FilterMedicine(medicine);
                    }
                    return false;
                };
            }
            catch (Exception ex)
            {
                // Chỉ hiển thị lỗi khi ứng dụng đã được khởi tạo hoàn toàn
                if (Application.Current != null &&
                    Application.Current.MainWindow != null &&
                    Application.Current.MainWindow.IsLoaded)
                {
                    MessageBoxService.ShowError($"Không thể tải danh sách thuốc: {ex.Message}", "Lỗi");
                }
            }
        }




        private void LoadCategories()
        {
            // Kiểm tra xem đã khởi tạo chưa
            if (!IsInitialized)
            {
                return;
            }

            try
            {
                var categories = DataProvider.Instance.Context.MedicineCategories
                    .Where(c => c.IsDeleted != true)
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                // Add "All Categories" option
                categories.Insert(0, new MedicineCategory { CategoryId = 0, CategoryName = "-- Tất cả loại thuốc --" });

                CategoryList = new ObservableCollection<MedicineCategory>(categories);
                SelectedCategory = CategoryList.FirstOrDefault();
            }
            catch (Exception ex)
            {
                // Chỉ hiển thị lỗi khi ứng dụng đã được khởi tạo hoàn toàn
                if (Application.Current != null &&
                    Application.Current.MainWindow != null &&
                    Application.Current.MainWindow.IsLoaded)
                {
                    MessageBoxService.ShowError($"Không thể tải danh sách loại thuốc: {ex.Message}", "Lỗi");
                }
                else
                {
                   
                }
            }
        }

        private void LoadInvoiceItems()
        {
            if (CurrentInvoice == null || CurrentInvoice.InvoiceDetails == null)
                return;

            try
            {
                // Đảm bảo tải đầy đủ thông tin liên quan của hóa đơn
                var invoiceWithDetails = DataProvider.Instance.Context.Invoices
                    .Include(i => i.InvoiceDetails)
                        .ThenInclude(id => id.Medicine)
                            .ThenInclude(m => m.Category)
                    .Include(i => i.InvoiceDetails)
                        .ThenInclude(id => id.Medicine.StockIns)
                    .FirstOrDefault(i => i.InvoiceId == CurrentInvoice.InvoiceId);

                if (invoiceWithDetails == null)
                    return;

                // Xóa các item cũ trong giỏ
                ClearCart();

                // Thêm các item từ hóa đơn vào giỏ
                foreach (var detail in invoiceWithDetails.InvoiceDetails.Where(d => d.MedicineId.HasValue))
                {
                    // Load full medicine data with fresh RemainQuantity information
                    var medicine = DataProvider.Instance.Context.Medicines
                        .Include(m => m.StockIns)
                        .FirstOrDefault(m => m.MedicineId == detail.MedicineId);

                    if (medicine != null)
                    {
                        // Create cart item from invoice detail
                        var cartItem = new CartItem(detail);

                        // Original quantity from invoice
                        int originalQty = detail.Quantity ?? 0;

                        // Calculate available stock plus original quantity
                        // (since when editing, we should be able to keep at least our original quantity)
                        int availableStock = medicine.TotalPhysicalStockQuantity + originalQty;

                        if (cartItem.Quantity > availableStock)
                        {
                            cartItem.Quantity = availableStock;
                            MessageBoxService.ShowWarning(
                                $"Số lượng của {medicine.Name} đã được điều chỉnh xuống {availableStock} do hạn chế tồn kho.",
                                "Thông báo"
                            );
                        }

                        CartItems.Add(cartItem);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Không thể tải thông tin chi tiết hóa đơn: {ex.Message}", "Lỗi");
            }
        }

        private bool FilterMedicine(Medicine medicine)
        {
            // Filter by category if selected
            if (SelectedCategory != null && SelectedCategory.CategoryId != 0 &&
                medicine.CategoryId != SelectedCategory.CategoryId)
            {
                return false;
            }

            // Filter by search text if provided
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                return medicine.Name.ToLower().Contains(searchLower) ||
                       $"M{medicine.MedicineId}".ToLower().Contains(searchLower) ||
                       (medicine.BarCode != null && medicine.BarCode.ToLower().Contains(searchLower));
            }

            return true;
        }

        private void SearchMedicines()
        {
            CollectionViewSource.GetDefaultView(MedicineList).Refresh();
        }

        private void FilterMedicines()
        {
            CollectionViewSource.GetDefaultView(MedicineList).Refresh();
        }

        private void AddToCart(Medicine medicine)
        {
            if (medicine == null) return;

            try
            {
                // Ensure we have the most up-to-date stock information with fresh RemainQuantity values
                var context = DataProvider.Instance.Context;
                var refreshedMedicine = context.Medicines
                    .Include(m => m.StockIns)
                    .FirstOrDefault(m => m.MedicineId == medicine.MedicineId);

                if (refreshedMedicine == null)
                {
                    MessageBoxService.ShowError("Không thể tải thông tin thuốc từ cơ sở dữ liệu.", "Lỗi");
                    return;
                }

                // Reset cache to ensure fresh calculations
                refreshedMedicine._availableStockInsCache = null;

                // Kiểm tra xem đây có phải là lô cuối cùng không
                if (refreshedMedicine.IsLastestStockIn)
                {
                    MessageBoxService.ShowWarning(
                        $"Lưu ý: {refreshedMedicine.Name} đang sử dụng lô cuối cùng. Vui lòng nhập thêm hàng sớm.",
                        "Cảnh báo tồn kho thấp"
                    );
                }

                // Reset cache to ensure fresh calculations
                refreshedMedicine._availableStockInsCache = null;

                // Get accurate physical stock quantity using RemainQuantity directly
                int actualStock = refreshedMedicine.TotalPhysicalStockQuantity;

                // Get user-requested quantity
                int requestedQuantity = medicine.TempQuantity > 0 ? medicine.TempQuantity : 1;

                // Check if any of this medicine is already in cart
                var existingItem = CartItems.FirstOrDefault(i => i.Medicine.MedicineId == medicine.MedicineId);
                int quantityInCart = existingItem?.Quantity ?? 0;

                // Calculate available stock considering what's already in cart
                int availableToAdd = actualStock - quantityInCart;

                // Check if requested quantity exceeds available stock
                if (requestedQuantity > availableToAdd)
                {
                    if (availableToAdd <= 0)
                    {
                        MessageBoxService.ShowWarning(
                            $"Không thể thêm {medicine.Name} vào giỏ hàng vì đã hết hàng hoặc đã thêm hết số lượng có sẵn.",
                            "Cảnh báo");
                        return;
                    }

                    MessageBoxService.ShowWarning(
                        $"Số lượng yêu cầu ({requestedQuantity}) vượt quá số lượng tồn kho còn lại ({availableToAdd}).\n" +
                        $"Chỉ có thể thêm {availableToAdd} sản phẩm vào giỏ hàng.",
                        "Cảnh báo");

                    requestedQuantity = availableToAdd;
                }

                // Update or add to cart
                if (existingItem != null)
                {
                    // Update quantity of existing item
                    existingItem.Quantity += requestedQuantity;
                }
                else
                {
                    // Add new item to cart with accurate price information
                    var cartItem = new CartItem(refreshedMedicine, requestedQuantity);
                    CartItems.Add(cartItem);
                }

                // Reset temp quantity for next add
                medicine.TempQuantity = 1;


            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi thêm vào giỏ hàng: {ex.Message}", "Lỗi");
            }
        }

        private void RemoveFromCart(CartItem item)
        {
            if (item == null) return;
            CartItems.Remove(item);
        }

        private void ClearCart()
        {
            if (CartItems == null || CartItems.Count == 0) return;
            CartItems.Clear();
        }

        private void AddNewPatient()
        {
            try
            {
                // Mở cửa sổ thêm bệnh nhân mới
                var addPatientWindow = new AddPatientWindow();
                var viewModel = new AddPatientViewModel();

                // Nếu đã nhập tên hoặc số điện thoại, truyền vào viewModel
                if (!string.IsNullOrWhiteSpace(PatientName))
                    viewModel.FullName = PatientName;

                if (!string.IsNullOrWhiteSpace(Phone))
                    viewModel.Phone = Phone;

                addPatientWindow.DataContext = viewModel;
                addPatientWindow.ShowDialog();

                // Sau khi cửa sổ đóng, kiểm tra xem bệnh nhân mới có được tạo không
                if (viewModel.NewPatient != null)
                {
                    // Cập nhật UI với bệnh nhân mới tạo
                    SelectedPatient = viewModel.NewPatient;
                    PatientName = viewModel.NewPatient.FullName;
                    Phone = viewModel.NewPatient.Phone;

                    // Cập nhật giảm giá theo loại bệnh nhân nếu có
                    if (viewModel.NewPatient.PatientType != null)
                    {
                        Discount = viewModel.NewPatient.PatientType.Discount ?? 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi thêm bệnh nhân mới: {ex.Message}", "Lỗi");
            }
        }

        private void FindPatient()
        {
            try
            {
                // Enable validation for patient fields
                _isValidating = true;
                _touchedFields.Add(nameof(PatientName));
                _touchedFields.Add(nameof(Phone));

                // Trigger validation
                OnPropertyChanged(nameof(PatientName));
                OnPropertyChanged(nameof(Phone));

                // Check for validation errors
                if (HasErrors)
                {
                    MessageBoxService.ShowWarning("Vui lòng sửa các lỗi nhập liệu trước khi tìm kiếm bệnh nhân.", "Lỗi dữ liệu");
                    return;
                }

                // Kiểm tra có thông tin tìm kiếm không
                if (string.IsNullOrWhiteSpace(PatientName) && string.IsNullOrWhiteSpace(Phone))
                {
                    MessageBoxService.ShowWarning("Vui lòng nhập tên hoặc số điện thoại để tìm kiếm.", "Thiếu thông tin");
                    return;
                }

                // Tìm bệnh nhân theo thông tin
                var query = DataProvider.Instance.Context.Patients
                    .Include(p => p.PatientType)
                    .Where(p => p.IsDeleted != true);

                // Tìm theo số điện thoại nếu có
                if (!string.IsNullOrWhiteSpace(Phone))
                {
                    query = query.Where(p => p.Phone == Phone.Trim());
                }
                // Nếu không có số điện thoại, tìm theo tên
                else if (!string.IsNullOrWhiteSpace(PatientName))
                {
                    query = query.Where(p => p.FullName.Contains(PatientName.Trim()));
                }

                var patient = query.FirstOrDefault();

                if (patient != null)
                {
                    SelectedPatient = patient;
                    PatientName = patient.FullName;
                    Phone = patient.Phone;

                    // Cập nhật giảm giá theo loại bệnh nhân
                    if (patient.PatientType != null)
                    {
                        Discount = patient.PatientType.Discount ?? 0;
                    }

                    MessageBoxService.ShowSuccess(
                        $"Đã tìm thấy bệnh nhân {patient.FullName}",
                        "Tìm kiếm thành công"
                    );
                }
                else
                {
                    MessageBoxService.ShowWarning(
                        "Không tìm thấy bệnh nhân nào phù hợp với thông tin đã nhập.",
                        "Không tìm thấy"
                    );

                    // Hỏi người dùng có muốn tạo bệnh nhân mới không
                    bool wantToCreate = MessageBoxService.ShowQuestion(
                        "Bạn có muốn thêm mới bệnh nhân này không?",
                        "Thêm bệnh nhân mới"
                    );

                    if (wantToCreate)
                    {
                        AddNewPatient();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tìm kiếm bệnh nhân: {ex.Message}", "Lỗi");
            }
        }

        private void Checkout()
        {
            try
            {
                // Kiểm tra có sản phẩm trong giỏ không
                if (CartItems.Count == 0)
                {
                    MessageBoxService.ShowWarning("Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.",
                                    "Thông báo");
                    return;
                }

                // Validation cho thông tin bệnh nhân
                _isValidating = true;
                _touchedFields.Add(nameof(PatientName));
                _touchedFields.Add(nameof(Phone));

                OnPropertyChanged(nameof(PatientName));
                OnPropertyChanged(nameof(Phone));

                if (HasErrors)
                {
                    MessageBoxService.ShowWarning("Vui lòng sửa các lỗi nhập liệu trước khi thanh toán.", "Lỗi dữ liệu");
                    return;
                }

                // Kiểm tra đã chọn bệnh nhân chưa
                if (string.IsNullOrWhiteSpace(PatientName))
                {
                    MessageBoxService.ShowWarning("Vui lòng chọn hoặc nhập thông tin bệnh nhân trước khi thanh toán.", "Thiếu thông tin");
                    return;
                }

                // 1. Tìm bệnh nhân đã chọn
                Patient patient = SelectedPatient;

                // Nếu chưa chọn bệnh nhân nhưng đã nhập thông tin, tìm kiếm
                if (patient == null && !string.IsNullOrWhiteSpace(PatientName))
                {
                    var query = DataProvider.Instance.Context.Patients
                        .Include(p => p.PatientType)
                        .Where(p => p.IsDeleted != true);

                    if (!string.IsNullOrWhiteSpace(Phone))
                    {
                        patient = query.FirstOrDefault(p => p.Phone == Phone.Trim());
                    }

                    if (patient == null)
                    {
                        patient = query.FirstOrDefault(p => p.FullName == PatientName.Trim());
                    }

                    // Nếu vẫn không tìm thấy, hỏi người dùng có muốn tạo mới không
                    if (patient == null)
                    {
                        bool createNew = MessageBoxService.ShowQuestion(
                            "Không tìm thấy thông tin bệnh nhân. Bạn có muốn thêm bệnh nhân mới không?",
                            "Thêm bệnh nhân mới"
                        );

                        if (createNew)
                        {
                            AddNewPatient();
                            // Sau khi thêm, bệnh nhân mới sẽ được gán vào SelectedPatient
                            patient = SelectedPatient;
                        }

                        // Nếu người dùng không muốn tạo mới hoặc việc tạo mới thất bại
                        if (patient == null)
                        {
                            MessageBoxService.ShowWarning(
                                "Không thể thanh toán khi chưa chọn bệnh nhân.",
                                "Thiếu thông tin"
                            );
                            return;
                        }
                    }
                }

                // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // 2. Tạo hoặc cập nhật hóa đơn 
                        Invoice invoice;
                        var context = DataProvider.Instance.Context;

                        if (CurrentInvoice != null)
                        {
                            // Load hóa đơn có sẵn 
                            invoice = context.Invoices.Find(CurrentInvoice.InvoiceId);
                            if (invoice == null)
                            {
                                MessageBoxService.ShowError("Không thể tìm thấy hóa đơn để cập nhật.",
                                                "Lỗi");
                                return;
                            }

                            // Lấy chi tiết hóa đơn gốc để xóa sau này
                            var originalDetails = context.InvoiceDetails
                                .Where(d => d.InvoiceId == invoice.InvoiceId && d.MedicineId != null)
                                .ToList();

                            // Update invoice details
                            invoice.PatientId = patient?.PatientId;
                            invoice.TotalAmount = TotalAmount - Discount;
                            invoice.StaffPrescriberId = CurrentAccount.StaffId;
                            invoice.Discount = Discount;
                            invoice.Status = "Chưa thanh toán";

                            // Update invoice type if needed
                            if (invoice.InvoiceType == "Khám bệnh")
                            {
                                invoice.InvoiceType = "Khám và bán thuốc";
                            }

                            // Remove all existing medicine invoice details
                            foreach (var detail in originalDetails)
                            {
                                context.InvoiceDetails.Remove(detail);
                            }

                            // Save changes to remove old items
                            context.SaveChanges();
                        }
                        else
                        {
                            // Tạo hóa đơn mới
                            invoice = new Invoice
                            {
                                PatientId = patient?.PatientId,
                                TotalAmount = TotalAmount - Discount,
                                InvoiceDate = DateTime.Now,
                                StaffPrescriberId = CurrentAccount.StaffId,
                                Status = "Chưa thanh toán",
                                InvoiceType = "Bán thuốc",
                                Discount = Discount,
                                Tax = 0
                            };

                            context.Invoices.Add(invoice);
                            context.SaveChanges(); // Lưu để có InvoiceId
                        }

                        // 3. Thêm chi tiết hóa đơn và xác định StockInId từ lô đang bán
                        foreach (var item in CartItems)
                        {
                            // Load medicine with fresh StockIn data including RemainQuantity
                            var medicine = context.Medicines
                                .Include(m => m.StockIns)
                                .FirstOrDefault(m => m.MedicineId == item.Medicine.MedicineId);

                            if (medicine != null)
                            {
                                medicine._availableStockInsCache = null;
                                var sellingStockIn = medicine.SellingStockIn;

                                var invoiceDetail = item.ToInvoiceDetail(invoice.InvoiceId);

                                // Assign StockInId from the selling stock in
                                if (sellingStockIn != null)
                                {
                                    invoiceDetail.StockInId = sellingStockIn.StockInId;
                                }

                                context.InvoiceDetails.Add(invoiceDetail);
                            }
                        }

                        // 4. Lưu thay đổi
                        context.SaveChanges();

                        // Hoàn thành transaction
                        transaction.Commit();

                        // 5. Mở cửa sổ chi tiết hóa đơn
                        var invoiceDetailsWindow = new InvoiceDetailsWindow();
                        invoiceDetailsWindow.DataContext = new InvoiceDetailsViewModel(invoice);
                        invoiceDetailsWindow.ShowDialog();

                        // 6. Làm mới dữ liệu sau khi thanh toán
                        var refreshedInvoice = context.Invoices.Find(invoice.InvoiceId);

                        if (refreshedInvoice.Status == "Đã thanh toán")
                        {
                            // Update StockIn.RemainQuantity values for each medicine
                            UpdateStockInRemainQuantity(refreshedInvoice);

                            ClearCart();
                            PatientName = null;
                            Phone = null;
                            Discount = 0;
                            CurrentInvoice = null;
                            SelectedPatient = null;

                            // Cập nhật số hóa đơn mới
                            UpdateInvoiceNumber();

                            // Cập nhật lại danh sách thuốc
                            LoadMedicines();

                            MessageBoxService.ShowSuccess("Thanh toán thành công!", "Thông báo");
                        }
                        else if (refreshedInvoice.Status == "Chưa thanh toán")
                        {
                            ClearCart();
                            PatientName = null;
                            Phone = null;
                            Discount = 0;
                            CurrentInvoice = null;
                            SelectedPatient = null;

                            // Cập nhật số hóa đơn mới
                            UpdateInvoiceNumber();

                            // Cập nhật lại danh sách thuốc
                            LoadMedicines();

                            MessageBoxService.ShowWarning($"Hóa đơn #{refreshedInvoice.InvoiceId} đã được tạo nhưng chưa thanh toán.",
                                            "Thông báo");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Hoàn tác transaction nếu có lỗi
                        transaction.Rollback();
                        throw; // Ném lại ngoại lệ để bắt ở khối catch bên ngoài
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xử lý thanh toán: {ex.Message}", "Lỗi");
            }
        }

        // Phương thức cập nhật số lượng còn lại của StockIn sau khi bán thuốc
        private void UpdateStockInRemainQuantity(Invoice invoice)
        {
            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
            {
                try
                {
                    var context = DataProvider.Instance.Context;

                    // Lấy tất cả chi tiết hóa đơn đã thanh toán cho hóa đơn này
                    var invoiceDetails = context.InvoiceDetails
                        .Where(id => id.InvoiceId == invoice.InvoiceId && id.MedicineId.HasValue)
                        .Include(id => id.StockIn)
                        .ToList();

                    // Cập nhật RemainQuantity cho từng StockIn trực tiếp
                    foreach (var detail in invoiceDetails.Where(d => d.StockInId.HasValue))
                    {
                        var stockIn = detail.StockIn;
                        if (stockIn != null)
                        {
                            // Giảm RemainQuantity theo số lượng đã bán
                            stockIn.RemainQuantity -= detail.Quantity ?? 0;

                            // Đảm bảo RemainQuantity không âm
                            if (stockIn.RemainQuantity < 0)
                                stockIn.RemainQuantity = 0;
                        }
                    }

                    // Lưu thay đổi để cập nhật giá trị RemainQuantity
                    context.SaveChanges();

                    // Cập nhật bản ghi Stock với tổng số chính xác
                    foreach (var detail in invoiceDetails)
                    {
                        var medicine = context.Medicines
                            .Include(m => m.StockIns)
                            .FirstOrDefault(m => m.MedicineId == detail.MedicineId);

                        if (medicine != null)
                        {
                            // Tính tổng số lượng tồn kho sử dụng RemainQuantity trực tiếp
                            int totalPhysicalStock = medicine.StockIns.Sum(si => si.RemainQuantity);

                            // Tính toán số lượng có thể sử dụng với bộ lọc ngày hết hạn thích hợp
                            var today = DateOnly.FromDateTime(DateTime.Today);
                            var minimumExpiryDate = today.AddDays(Medicine.MinimumDaysBeforeExpiry);
                            int usableStock = medicine.StockIns
                                .Where(si => !si.ExpiryDate.HasValue || si.ExpiryDate.Value >= minimumExpiryDate)
                                .Sum(si => si.RemainQuantity);

                            // Cập nhật hoặc tạo bản ghi Stock
                            var stock = context.Stocks.FirstOrDefault(s => s.MedicineId == detail.MedicineId);
                            if (stock != null)
                            {
                                stock.Quantity = totalPhysicalStock;
                                stock.UsableQuantity = usableStock;
                                stock.LastUpdated = DateTime.Now;
                            }
                            else
                            {
                                context.Stocks.Add(new Stock
                                {
                                    MedicineId = detail.MedicineId.Value,
                                    Quantity = totalPhysicalStock,
                                    UsableQuantity = usableStock,
                                    LastUpdated = DateTime.Now
                                });
                            }
                        }
                    }

                    // Lưu thay đổi lần nữa để cập nhật bản ghi Stock
                    context.SaveChanges();

                    // Hoàn thành transaction
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // Hoàn tác transaction nếu có lỗi
                    transaction.Rollback();

                    MessageBoxService.ShowError($"Lỗi khi cập nhật tồn kho: {ex.Message}", "Lỗi");
                }
            }
        }


        // Thêm phương thức mới để cập nhật số hóa đơn
        private void UpdateInvoiceNumber()
        {
            try
            {
                var lastInvoiceId = DataProvider.Instance.Context.Invoices
                    .OrderByDescending(i => i.InvoiceId)
                    .Select(i => i.InvoiceId)
                    .FirstOrDefault();

                InvoiceNumber = $"#{lastInvoiceId + 1} (Mới)";
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi cập nhật số hóa đơn mới: {ex.Message}", "Lỗi");
            }
        }
    }
}
