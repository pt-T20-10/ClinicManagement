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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class MedicineSellViewModel : BaseViewModel
    {
        #region Properties

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
        #endregion

        #region Constructors
        // Constructor mặc định
        public MedicineSellViewModel()
        {
            InitializeCommands();
            LoadData();
        }

        // Constructor với Invoice
        public MedicineSellViewModel(Invoice invoice)
        {
            InitializeCommands();
            LoadData();
            CurrentInvoice = invoice;
        }
        #endregion

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
                       }
                   }
               },
               (userControl) => true
           );

            SearchCommand = new RelayCommand<object>(
                p => SearchMedicines(),
                p => true
            );

            AddToCartCommand = new RelayCommand<Medicine>(
                p => AddToCart(p),
                p => p != null && p.TotalStockQuantity > 0
            );

            RemoveFromCartCommand = new RelayCommand<CartItem>(
                p => RemoveFromCart(p),
                p => p != null
            );

            ClearCartCommand = new RelayCommand<object>(
                p => ClearCart(),
                p => CartItems != null && CartItems.Count > 0
            );

            CheckoutCommand = new RelayCommand<object>(
                p => Checkout(),
                p => CartItems != null && CartItems.Count > 0
            );
        }

        public void LoadData()
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

        private void LoadMedicines()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var minimumExpiryDate = today.AddDays(8);

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

                // Filter out medicines that don't have valid expiry dates or stock
                var validMedicines = medicines.Where(m =>
                    m.StockIns.Any(si =>
                        si.RemainQuantity > 0 &&
                        (!si.ExpiryDate.HasValue || si.ExpiryDate.Value >= minimumExpiryDate)
                    )
                ).ToList();

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
                MessageBoxService.ShowError($"Không thể tải danh sách thuốc: {ex.Message}", "Lỗi");
            }
        }

        private void LoadCategories()
        {
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
                MessageBoxService.ShowError($"Không thể tải danh sách loại thuốc: {ex.Message}", "Lỗi");
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

                // 1. Tìm hoặc tạo bệnh nhân nếu có thông tin
                Patient patient = null;
                if (!string.IsNullOrWhiteSpace(PatientName))
                {
                    // Tìm bệnh nhân theo tên và số điện thoại
                    patient = FindOrCreatePatient();
                }

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

                // 3. Thêm chi tiết hóa đơn và xác định StockInId theo FIFO
                foreach (var item in CartItems)
                {
                    // Load medicine with fresh StockIn data including RemainQuantity
                    var medicine = context.Medicines
                        .Include(m => m.StockIns)
                        .FirstOrDefault(m => m.MedicineId == item.Medicine.MedicineId);

                    if (medicine != null)
                    {
                        // Get active StockIn following FIFO principles
                        medicine._availableStockInsCache = null; // Reset cache
                        var activeStockIn = medicine.ActiveStockIn;

                        var invoiceDetail = item.ToInvoiceDetail(invoice.InvoiceId);

                        // Assign StockInId from the active StockIn (FIFO)
                        if (activeStockIn != null)
                        {
                            invoiceDetail.StockInId = activeStockIn.StockInId;
                        }

                        context.InvoiceDetails.Add(invoiceDetail);
                    }
                }

                // 4. Lưu thay đổi
                context.SaveChanges();

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
                MessageBoxService.ShowError($"Lỗi khi xử lý thanh toán: {ex.Message}", "Lỗi");
            }
        }

        // New method for updating StockIn.RemainQuantity values
        private void UpdateStockInRemainQuantity(Invoice invoice)
        {
            try
            {
                var context = DataProvider.Instance.Context;

                // Get all paid invoice details for this invoice
                var invoiceDetails = context.InvoiceDetails
                    .Where(id => id.InvoiceId == invoice.InvoiceId && id.MedicineId.HasValue)
                    .Include(id => id.StockIn)
                    .ToList();

                // Update RemainQuantity for each StockIn directly
                foreach (var detail in invoiceDetails.Where(d => d.StockInId.HasValue))
                {
                    var stockIn = detail.StockIn;
                    if (stockIn != null)
                    {
                        // Reduce RemainQuantity by the quantity sold
                        stockIn.RemainQuantity -= detail.Quantity ?? 0;

                        // Ensure RemainQuantity doesn't go below zero
                        if (stockIn.RemainQuantity < 0)
                            stockIn.RemainQuantity = 0;
                    }
                }

                // Save changes to update RemainQuantity values
                context.SaveChanges();

                // Now update Stock records with correct totals
                foreach (var detail in invoiceDetails)
                {
                    var medicine = context.Medicines
                        .Include(m => m.StockIns)
                        .FirstOrDefault(m => m.MedicineId == detail.MedicineId);

                    if (medicine != null)
                    {
                        // Calculate total physical stock using RemainQuantity directly
                        int totalPhysicalStock = medicine.StockIns.Sum(si => si.RemainQuantity);

                        // Calculate usable stock with proper expiry date filtering
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var minimumExpiryDate = today.AddDays(Medicine.MinimumDaysBeforeExpiry);
                        int usableStock = medicine.StockIns
                            .Where(si => !si.ExpiryDate.HasValue || si.ExpiryDate.Value >= minimumExpiryDate)
                            .Sum(si => si.RemainQuantity);

                        // Update or create Stock record
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

                // Save changes again to update Stock records
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi cập nhật tồn kho: {ex.Message}", "Lỗi");
            }
        }

        private Patient FindOrCreatePatient()
        {
            try
            {
                var context = DataProvider.Instance.Context;
                Patient patient = null;

                // Đã có bệnh nhân được chọn từ trước
                if (SelectedPatient != null)
                {
                    return SelectedPatient;
                }

                // Tìm theo số điện thoại (nếu có)
                if (!string.IsNullOrWhiteSpace(Phone))
                {
                    patient = context.Patients
                        .FirstOrDefault(p => p.Phone == Phone && (p.IsDeleted == null || p.IsDeleted == false));
                }

                // Nếu không tìm thấy theo số điện thoại, tìm theo tên
                if (patient == null && !string.IsNullOrWhiteSpace(PatientName))
                {
                    patient = context.Patients
                        .FirstOrDefault(p => p.FullName == PatientName && (p.IsDeleted == null || p.IsDeleted == false));
                }

                // Nếu không tìm thấy bệnh nhân, tạo mới
                if (patient == null)
                {
                    var result = MessageBoxService.ShowQuestion(
                        $"Không tìm thấy bệnh nhân với thông tin đã nhập. Bạn có muốn tạo bệnh nhân mới?",
                        "Thông báo");

                    if (result)
                    {
                        // Tạo bệnh nhân mới với thông tin cơ bản
                        patient = new Patient
                        {
                            FullName = PatientName,
                            Phone = Phone,
                            CreatedAt = DateTime.Now,
                            IsDeleted = false
                        };

                        context.Patients.Add(patient);
                        context.SaveChanges();
                    }
                }

                return patient;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xử lý thông tin bệnh nhân: {ex.Message}", "Lỗi");
                return null;
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
