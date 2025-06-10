using ClinicManagement.Models;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    // Subscribe to collection changes
                    _cartItems.CollectionChanged += CartItems_CollectionChanged;
                }
                return _cartItems;
            }
            set
            {
                if (_cartItems != null)
                {
                    // Unsubscribe from old collection
                    _cartItems.CollectionChanged -= CartItems_CollectionChanged;
                    
                    // Unsubscribe from PropertyChanged events of each item
                    foreach (var item in _cartItems)
                    {
                        item.PropertyChanged -= CartItem_PropertyChanged;
                    }
                }
                
                _cartItems = value;
                
                if (_cartItems != null)
                {
                    // Subscribe to new collection
                    _cartItems.CollectionChanged += CartItems_CollectionChanged;
                    
                    // Subscribe to PropertyChanged event of each item
                    foreach (var item in _cartItems)
                    {
                        item.PropertyChanged += CartItem_PropertyChanged;
                    }
                }
                
                OnPropertyChanged();
                UpdateCartTotals();
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
                OnPropertyChanged(nameof(_invoiceNumber));
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

        private void LoadData()
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
                var medicines = DataProvider.Instance.Context.Medicines
                    .AsNoTracking() // Sử dụng AsNoTracking để không theo dõi thay đổi
                    .Where(m => m.IsDeleted != true)
                    .Include(m => m.Category)
                    .Include(m => m.StockIns)
                    .Include(m => m.InvoiceDetails)
                    .Include(m => m.Unit)
                    .ToList();

                // Thêm thuộc tính động sau khi đã truy vấn từ database xong
                foreach (var medicine in medicines)
                {
                    // Sử dụng TypeDescriptor để thêm thuộc tính vào đối tượng đã tồn tại
                    TypeDescriptor.AddAttributes(medicine, new DefaultValueAttribute(1));
                  ;
                }

                MedicineList = new ObservableCollection<Medicine>(medicines);

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
                MessageBox.Show($"Không thể tải danh sách thuốc: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Không thể tải danh sách loại thuốc: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    var cartItem = new CartItem(detail);
                    CartItems.Add(cartItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải thông tin chi tiết hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Lấy số lượng đã chọn từ thuộc tính TempQuantity đã được thêm trực tiếp vào Medicine
            int quantity = medicine.TempQuantity > 0 ? medicine.TempQuantity : 1;

            // Kiểm tra nếu số lượng vượt quá tồn kho
            if (quantity > medicine.TotalStockQuantity)
            {
                MessageBox.Show($"Số lượng yêu cầu ({quantity}) vượt quá số lượng tồn kho ({medicine.TotalStockQuantity}).",
                                "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Kiểm tra xem thuốc đã có trong giỏ hàng chưa
            var existingItem = CartItems.FirstOrDefault(i => i.Medicine.MedicineId == medicine.MedicineId);
            if (existingItem != null)
            {
                // Cập nhật số lượng nếu đã có trong giỏ
                existingItem.Quantity += quantity;

                // Kiểm tra lại tổng số lượng so với tồn kho
                if (existingItem.Quantity > medicine.TotalStockQuantity)
                {
                    existingItem.Quantity = medicine.TotalStockQuantity;
                    MessageBox.Show($"Số lượng đã được điều chỉnh theo tồn kho hiện có ({medicine.TotalStockQuantity}).",
                                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                // Thêm mới vào giỏ hàng
                var cartItem = new CartItem(medicine, quantity);
                CartItems.Add(cartItem);
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
                    MessageBox.Show("Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.",
                                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    // Cập nhật hóa đơn hiện có
                    invoice = context.Invoices.Find(CurrentInvoice.InvoiceId);
                    if (invoice == null)
                    {
                        MessageBox.Show("Không thể tìm thấy hóa đơn để cập nhật.",
                                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Cập nhật thông tin hóa đơn
                    invoice.PatientId = patient?.PatientId;
                    invoice.TotalAmount = TotalAmount - Discount;
                    invoice.Discount = Discount;
                    invoice.Status = "Chưa thanh toán";

                    // Xóa chi tiết hóa đơn cũ (chỉ xóa các mục thuốc)
                    var existingDetails = context.InvoiceDetails
                        .Where(d => d.InvoiceId == invoice.InvoiceId && d.MedicineId != null)
                        .ToList();

                    foreach (var detail in existingDetails)
                    {
                        context.InvoiceDetails.Remove(detail);
                    }
                }
                else
                {
                    // Tạo hóa đơn mới
                    invoice = new Invoice
                    {
                        PatientId = patient?.PatientId,
                        TotalAmount = TotalAmount - Discount,
                        InvoiceDate = DateTime.Now,
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
                    var medicine = context.Medicines
                        .Include(m => m.StockIns)
                        .Include(m => m.InvoiceDetails)
                        .FirstOrDefault(m => m.MedicineId == item.Medicine.MedicineId);

                    if (medicine != null)
                    {
                        // Lấy danh sách các lô thuốc còn hàng theo FIFO
                        var availableStockIns = medicine.GetDetailedStock()
                            .Where(s => s.RemainingQuantity > 0)
                            .OrderBy(s => s.ImportDate)
                            .ToList();

                        var invoiceDetail = item.ToInvoiceDetail(invoice.InvoiceId);

                        // Gán StockInId cho InvoiceDetail từ lô nhập cũ nhất còn hàng
                        if (availableStockIns.Any())
                        {
                            invoiceDetail.StockInId = availableStockIns.First().StockIn.StockInId;
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
                // Kiểm tra trạng thái hóa đơn trực tiếp thay vì dựa vào dialogResult
                // Cần refresh lại đối tượng invoice từ database để có thông tin mới nhất
                var refreshedInvoice = context.Invoices.Find(invoice.InvoiceId);
                
                if (refreshedInvoice.Status == "Đã thanh toán")
                {
                    // Cập nhật lại Stock nếu cần
                    UpdateStockAfterSale(refreshedInvoice);

                    ClearCart();
                    PatientName = null;
                    Phone = null;
                    Discount = 0;
                    CurrentInvoice = null;

                    // Cập nhật số hóa đơn mới sau khi thanh toán thành công
                    UpdateInvoiceNumber();

                    // Cập nhật lại danh sách thuốc để có số lượng chính xác
                    LoadMedicines();

                    MessageBox.Show("Thanh toán thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (refreshedInvoice.Status == "Chưa thanh toán")
                {
                    // Cập nhật lại Stock nếu cần
                    UpdateStockAfterSale(refreshedInvoice);

                    ClearCart();
                    PatientName = null;
                    Phone = null;
                    Discount = 0;
                    CurrentInvoice = null;

                    // Cập nhật số hóa đơn mới sau khi thanh toán thành công
                    UpdateInvoiceNumber();

                    // Cập nhật lại danh sách thuốc để có số lượng chính xác
                    LoadMedicines();
                    // Hóa đơn vẫn chưa thanh toán
                    MessageBox.Show($"Hóa đơn #{refreshedInvoice.InvoiceId} đã được tạo nhưng chưa thanh toán.",
                                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xử lý thanh toán: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Phương thức mới để cập nhật Stock sau khi thanh toán
        private void UpdateStockAfterSale(Invoice invoice)
        {
            try
            {
                var context = DataProvider.Instance.Context;

                // Lấy tất cả chi tiết hóa đơn đã thanh toán
                var invoiceDetails = context.InvoiceDetails
                    .Where(id => id.InvoiceId == invoice.InvoiceId && id.MedicineId.HasValue)
                    .Include(id => id.Medicine)
                    .ToList();

                // Cập nhật Stock cho từng mục thuốc
                foreach (var detail in invoiceDetails)
                {
                    var medicine = detail.Medicine;

                    // Đảm bảo dữ liệu được làm mới
                    context.Entry(medicine).State = EntityState.Detached;

                    // Tải lại thông tin thuốc để có số liệu mới nhất
                    var refreshedMedicine = context.Medicines
                        .Include(m => m.StockIns)
                        .Include(m => m.InvoiceDetails)
                        .FirstOrDefault(m => m.MedicineId == detail.MedicineId);

                    if (refreshedMedicine != null)
                    {
                        // Kiểm tra nếu có Stock hiện tại và cập nhật
                        var currentStock = context.Stocks
                            .FirstOrDefault(s => s.MedicineId == detail.MedicineId);

                        if (currentStock != null)
                        {
                            // Cập nhật số lượng tồn kho
                            currentStock.Quantity = refreshedMedicine.TotalStockQuantity;
                            currentStock.LastUpdated = DateTime.Now;
                        }
                        else
                        {
                            // Tạo mới bản ghi Stock nếu chưa tồn tại
                            var newStock = new Stock
                            {
                                MedicineId = detail.MedicineId.Value,
                                Quantity = refreshedMedicine.TotalStockQuantity,
                                LastUpdated = DateTime.Now
                            };
                            context.Stocks.Add(newStock);
                        }

                        // Lưu các thay đổi
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật tồn kho: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    var result = MessageBox.Show(
                        $"Không tìm thấy bệnh nhân với thông tin đã nhập. Bạn có muốn tạo bệnh nhân mới?",
                        "Thông báo", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
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
                MessageBox.Show($"Lỗi khi xử lý thông tin bệnh nhân: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Lỗi khi cập nhật số hóa đơn mới: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
