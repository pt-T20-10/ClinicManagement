using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel quản lý chức năng bán thuốc
    /// Hỗ trợ tạo hóa đơn bán thuốc mới và chỉnh sửa hóa đơn khám bệnh
    /// Sử dụng lazy initialization để tránh tải dữ liệu trước khi đăng nhập
    /// </summary>
    public class MedicineSellViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Properties

        /// <summary>
        /// Cờ kiểm soát việc khởi tạo dữ liệu
        /// Đảm bảo dữ liệu chỉ được tải sau khi đăng nhập thành công
        /// </summary>
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

        /// <summary>
        /// Danh sách thuốc có sẵn để bán
        /// Chỉ hiển thị thuốc còn tồn kho và chưa bị xóa
        /// </summary>
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

        /// <summary>
        /// Danh sách các mặt hàng trong giỏ hàng
        /// Hỗ trợ theo dõi thay đổi để cập nhật tổng tiền tự động
        /// </summary>
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

        /// <summary>
        /// Tài khoản người dùng hiện tại
        /// Được sử dụng để ghi nhận người tạo hóa đơn
        /// </summary>
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

        /// <summary>
        /// Xử lý sự kiện thay đổi trong collection giỏ hàng (thêm/xóa item)
        /// Đăng ký/hủy đăng ký event listener cho PropertyChanged của từng item
        /// </summary>
        private void CartItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Đăng ký theo dõi các item mới được thêm vào
            if (e.NewItems != null)
            {
                foreach (CartItem item in e.NewItems)
                {
                    item.PropertyChanged += CartItem_PropertyChanged;
                }
            }

            // Hủy đăng ký theo dõi các item bị xóa
            if (e.OldItems != null)
            {
                foreach (CartItem item in e.OldItems)
                {
                    item.PropertyChanged -= CartItem_PropertyChanged;
                }
            }

            // Cập nhật tổng tiền
            UpdateCartTotals();
        }

        /// <summary>
        /// Xử lý sự kiện thay đổi thuộc tính trong các cart item (ví dụ: thay đổi số lượng)
        /// Tự động cập nhật tổng tiền khi có thay đổi
        /// </summary>
        private void CartItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Nếu số lượng hoặc tổng tiền của item thay đổi, cập nhật tổng giỏ hàng
            if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.LineTotal))
            {
                UpdateCartTotals();
            }
        }

        /// <summary>
        /// Cập nhật tổng tiền và số lượng item trong giỏ hàng
        /// Thông báo UI để cập nhật hiển thị
        /// </summary>
        private void UpdateCartTotals()
        {
            OnPropertyChanged(nameof(TotalItems));
            OnPropertyChanged(nameof(TotalAmount));
        }

        /// <summary>
        /// Danh sách loại thuốc để lọc
        /// Hỗ trợ tìm kiếm thuốc theo phân loại
        /// </summary>
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

        /// <summary>
        /// Tổng số tiền của tất cả item trong giỏ hàng
        /// Calculated property - tự động tính toán dựa trên CartItems
        /// </summary>
        public decimal TotalAmount => CartItems?.Sum(i => i.LineTotal) ?? 0;

        /// <summary>
        /// Tổng số lượng item trong giỏ hàng
        /// Calculated property - tự động tính toán dựa trên CartItems
        /// </summary>
        public int TotalItems => CartItems?.Sum(i => i.Quantity) ?? 0;

        /// <summary>
        /// Số hóa đơn hiển thị
        /// Có thể là số hóa đơn hiện tại (khi chỉnh sửa) hoặc số đề xuất (khi tạo mới)
        /// </summary>
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

        /// <summary>
        /// Tên bệnh nhân
        /// Hỗ trợ validation và tìm kiếm bệnh nhân
        /// </summary>
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

        /// <summary>
        /// Số điện thoại bệnh nhân
        /// Hỗ trợ validation định dạng và tìm kiếm bệnh nhân
        /// </summary>
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

        /// <summary>
        /// Thuốc được chọn trong danh sách
        /// Sử dụng để thêm vào giỏ hàng
        /// </summary>
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

        /// <summary>
        /// Loại thuốc được chọn để lọc
        /// Tự động kích hoạt lọc danh sách thuốc khi thay đổi
        /// </summary>
        private MedicineCategory _selectedCategory;
        public MedicineCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();
                FilterMedicines(); // Tự động lọc khi thay đổi loại thuốc
            }
        }

        /// <summary>
        /// Từ khóa tìm kiếm thuốc
        /// Hỗ trợ tìm theo tên, mã thuốc, barcode
        /// </summary>
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

        /// <summary>
        /// Số tiền giảm giá cho hóa đơn
        /// Có thể áp dụng theo loại bệnh nhân hoặc nhập thủ công
        /// </summary>
        private decimal _discount;
        public decimal Discount
        {
            get => _discount;
            set
            {
                _discount = value;
                OnPropertyChanged();
                UpdateCartTotals(); // Cập nhật tổng tiền khi giảm giá thay đổi
            }
        }

        /// <summary>
        /// Hóa đơn hiện tại khi chỉnh sửa hóa đơn có sẵn
        /// Null khi tạo hóa đơn mới
        /// </summary>
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
                    // Cập nhật thông tin từ hóa đơn hiện tại
                    InvoiceNumber = $"{_currentInvoice.InvoiceId}";

                    if (_currentInvoice.Patient != null)
                    {
                        PatientName = _currentInvoice.Patient.FullName;
                        Phone = _currentInvoice.Patient.Phone;
                    }

                    Discount = _currentInvoice.Discount ?? 0;

                    // Tải các item từ hóa đơn vào giỏ hàng
                    LoadInvoiceItems();
                }
            }
        }

        /// <summary>
        /// Bệnh nhân đã được chọn/tìm thấy
        /// Tự động cập nhật thông tin tên, số điện thoại và giảm giá
        /// </summary>
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

        // === VALIDATION PROPERTIES ===

        /// <summary>
        /// Error property cho IDataErrorInfo
        /// Trả về null vì validation được thực hiện per-property
        /// </summary>
        public string Error => null;

        /// <summary>
        /// Set theo dõi các field đã được người dùng tương tác
        /// Chỉ validate các field này để tránh hiển thị lỗi ngay khi mở form
        /// </summary>
        private HashSet<string> _touchedFields = new HashSet<string>();

        /// <summary>
        /// Cờ bật/tắt validation
        /// True = thực hiện validation, False = bỏ qua validation
        /// </summary>
        private bool _isValidating = false;

        /// <summary>
        /// Indexer cho IDataErrorInfo - thực hiện validation cho từng property
        /// Chỉ validate khi field đã được touched hoặc đang trong chế độ validating
        /// </summary>
        public string this[string columnName]
        {
            get
            {
                // Chỉ validate khi user đã tương tác với field hoặc đang trong chế độ validating
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

        /// <summary>
        /// Kiểm tra xem có lỗi validation nào không
        /// True = có lỗi, False = không có lỗi
        /// </summary>
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

        /// <summary>
        /// Lệnh tìm kiếm thuốc theo từ khóa
        /// </summary>
        public ICommand SearchCommand { get; set; }

        /// <summary>
        /// Lệnh thêm thuốc vào giỏ hàng
        /// </summary>
        public ICommand AddToCartCommand { get; set; }

        /// <summary>
        /// Lệnh xóa item khỏi giỏ hàng
        /// </summary>
        public ICommand RemoveFromCartCommand { get; set; }

        /// <summary>
        /// Lệnh xóa tất cả item trong giỏ hàng
        /// </summary>
        public ICommand ClearCartCommand { get; set; }

        /// <summary>
        /// Lệnh thanh toán - tạo/cập nhật hóa đơn
        /// </summary>
        public ICommand CheckoutCommand { get; set; }

        /// <summary>
        /// Lệnh thêm bệnh nhân mới
        /// </summary>
        public ICommand AddNewPatientCommand { get; set; }

        /// <summary>
        /// Lệnh tìm kiếm bệnh nhân theo tên/số điện thoại
        /// </summary>
        public ICommand FindPatientCommand { get; set; }

        /// <summary>
        /// Lệnh xử lý khi UserControl được load
        /// Khởi tạo dữ liệu nếu đã đăng nhập
        /// </summary>
        public ICommand LoadedUCCommand { get; set; }

        /// <summary>
        /// Lệnh khởi tạo dữ liệu thủ công
        /// Sử dụng khi cần force load dữ liệu
        /// </summary>
        public ICommand InitializeDataCommand { get; set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor mặc định - Không tự động load dữ liệu
        /// Sử dụng lazy initialization để tránh tải dữ liệu trước khi đăng nhập
        /// </summary>
        public MedicineSellViewModel()
        {
            InitializeCommands();
            // Không gọi LoadData() ở đây để tránh tải dữ liệu trước khi đăng nhập
        }

        /// <summary>
        /// Constructor với tham số lazy initialization
        /// Cho phép kiểm soát việc tải dữ liệu ngay lập tức hay trì hoãn
        /// </summary>
        /// <param name="lazyInitialization">True = trì hoãn tải dữ liệu, False = tải ngay</param>
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

        /// <summary>
        /// Constructor với Invoice - Luôn khởi tạo dữ liệu ngay lập tức
        /// Sử dụng khi chỉnh sửa hóa đơn có sẵn
        /// </summary>
        /// <param name="invoice">Hóa đơn cần chỉnh sửa</param>
        public MedicineSellViewModel(Invoice invoice)
        {
            InitializeCommands();
            IsInitialized = true;
            LoadData();
            CurrentInvoice = invoice;
        }
        #endregion

        /// <summary>
        /// Khởi tạo tất cả các command và thiết lập logic thực thi
        /// Các command có điều kiện CanExecute dựa trên trạng thái IsInitialized
        /// </summary>
        private void InitializeCommands()
        {
            // Command khởi tạo dữ liệu thủ công
            InitializeDataCommand = new RelayCommand<object>(
                p => {
                    if (!IsInitialized)
                    {
                        IsInitialized = true;
                        LoadData();
                    }
                },
                p => !IsInitialized // Chỉ có thể thực thi khi chưa khởi tạo
            );

            // Command xử lý khi UserControl được load
            LoadedUCCommand = new RelayCommand<UserControl>(
               (userControl) => {
                   if (userControl != null)
                   {
                       // Lấy MainViewModel từ Application resources
                       var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                       if (mainVM != null && mainVM.CurrentAccount != null)
                       {
                           // Cập nhật tài khoản hiện tại
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
               (userControl) => true // Luôn có thể thực thi
           );

            // Các command khác đều yêu cầu IsInitialized = true
            SearchCommand = new RelayCommand<object>(
                p => SearchMedicines(),
                p => IsInitialized // Chỉ có thể tìm kiếm khi đã khởi tạo dữ liệu
            );

            AddToCartCommand = new RelayCommand<Medicine>(
                p => AddToCart(p),
                p => IsInitialized && p != null && p.TotalStockQuantity > 0 // Cần khởi tạo, có thuốc và còn tồn kho
            );

            RemoveFromCartCommand = new RelayCommand<CartItem>(
                p => RemoveFromCart(p),
                p => IsInitialized && p != null // Cần khởi tạo và có item để xóa
            );

            ClearCartCommand = new RelayCommand<object>(
                p => ClearCart(),
                p => IsInitialized && CartItems != null && CartItems.Count > 0 // Cần khởi tạo và có item trong giỏ
            );

            CheckoutCommand = new RelayCommand<object>(
                p => Checkout(),
                p => IsInitialized && CartItems != null && CartItems.Count > 0 // Cần khởi tạo và có item để thanh toán
            );

            AddNewPatientCommand = new RelayCommand<object>(
                p => AddNewPatient(),
                p => IsInitialized // Chỉ cần khởi tạo
            );

            FindPatientCommand = new RelayCommand<object>(
                p => FindPatient(),
                p => IsInitialized && (!string.IsNullOrWhiteSpace(PatientName) || !string.IsNullOrWhiteSpace(Phone)) // Cần khởi tạo và có thông tin tìm kiếm
            );
        }

        /// <summary>
        /// Tải dữ liệu chính cho ViewModel
        /// Bao gồm danh sách thuốc, loại thuốc và đề xuất số hóa đơn
        /// </summary>
        public void LoadData()
        {
            // Kiểm tra xem đã khởi tạo chưa
            if (!IsInitialized)
            {
                return;
            }

            try
            {
                LoadMedicines();     // Tải danh sách thuốc
                LoadCategories();    // Tải danh sách loại thuốc

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
                    // Log lỗi nhưng không hiển thị MessageBox khi ứng dụng chưa sẵn sàng
                    System.Diagnostics.Debug.WriteLine($"LoadData Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Tải danh sách thuốc có sẵn để bán
        /// Chỉ hiển thị thuốc chưa bị xóa và đang được set để bán
        /// Bao gồm cảnh báo về thuốc hết hạn và sắp hết hạn
        /// </summary>
        private void LoadMedicines()
        {
            if (!IsInitialized)
                return;

            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);

                // Tải thuốc với eager loading và điều kiện có lô đang bán
                var medicines = DataProvider.Instance.Context.Medicines
                    .AsNoTracking()
                    .Where(m => m.IsDeleted != true &&
                               m.StockIns.Any(si => si.IsSelling && si.RemainQuantity > 0)) // Chỉ lấy thuốc có lô đang bán
                    .Include(m => m.Category)
                    .Include(m => m.Unit)
                    .ToList();

                // Xử lý dữ liệu tồn kho
                foreach (var medicine in medicines)
                {
                    // Tải StockIns, ưu tiên lô đang bán
                    var stockIns = DataProvider.Instance.Context.StockIns
                        .AsNoTracking()
                        .Where(si => si.MedicineId == medicine.MedicineId)
                        .OrderByDescending(si => si.IsSelling) // Ưu tiên lô đang bán lên đầu
                        .ToList();

                    medicine.StockIns = stockIns;
                    medicine._availableStockInsCache = null;
                    medicine.TempQuantity = 1;
                }

                // Lọc ra các thuốc có lô đang bán và còn hàng
                var validMedicines = medicines.Where(m =>
                    m.StockIns.Any(si => si.IsSelling && si.RemainQuantity > 0)
                ).ToList();

                // Hiển thị cảnh báo nếu cần
                if (Application.Current?.MainWindow?.IsLoaded == true)
                {
                    foreach (var medicine in validMedicines)
                    {
                        var sellingBatch = medicine.StockIns.FirstOrDefault(si => si.IsSelling);
                        if (sellingBatch != null)
                        {
                            // Cảnh báo lô đang bán đã hết hạn
                            if (sellingBatch.ExpiryDate.HasValue && sellingBatch.ExpiryDate.Value < today)
                            {
                                MessageBoxService.ShowWarning(
                                    $"Cảnh báo: Lô đang bán của {medicine.Name} đã hết hạn. Vui lòng chọn lô khác để bán.",
                                    "Cảnh báo thuốc hết hạn"
                                );
                            }
                            // Cảnh báo lô đang bán sắp hết hạn
                            else if (sellingBatch.ExpiryDate.HasValue &&
                                   sellingBatch.ExpiryDate.Value <= today.AddDays(Medicine.MinimumDaysBeforeExpiry))
                            {
                                int daysRemaining = (sellingBatch.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) -
                                                   today.ToDateTime(TimeOnly.MinValue)).Days;

                                MessageBoxService.ShowWarning(
                                    $"Lưu ý: Lô đang bán của {medicine.Name} sẽ hết hạn sau {daysRemaining} ngày. " +
                                    "Vui lòng chọn lô khác hoặc nhập thêm hàng.",
                                    "Cảnh báo hạn sử dụng"
                                );
                            }
                        }
                    }
                }

                MedicineList = new ObservableCollection<Medicine>(validMedicines);

                // Áp dụng bộ lọc tìm kiếm
                CollectionViewSource.GetDefaultView(MedicineList).Filter = item =>
                {
                    return item is Medicine medicine && FilterMedicine(medicine);
                };
            }
            catch (Exception ex)
            {
                if (Application.Current?.MainWindow?.IsLoaded == true)
                {
                    MessageBoxService.ShowError(
                        $"Không thể tải danh sách thuốc: {ex.Message}",
                        "Lỗi"
                    );
                }
            }
        }

        /// <summary>
        /// Tải danh sách loại thuốc để lọc
        /// Thêm option "Tất cả loại thuốc" ở đầu danh sách
        /// </summary>
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

                // Thêm option "Tất cả loại thuốc" ở đầu
                categories.Insert(0, new MedicineCategory { CategoryId = 0, CategoryName = "-- Tất cả loại thuốc --" });

                CategoryList = new ObservableCollection<MedicineCategory>(categories);
                SelectedCategory = CategoryList.FirstOrDefault(); // Chọn "Tất cả loại thuốc" làm mặc định
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
                    System.Diagnostics.Debug.WriteLine($"LoadCategories Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Tải các item từ hóa đơn hiện tại vào giỏ hàng
        /// Sử dụng khi chỉnh sửa hóa đơn có sẵn
        /// Điều chỉnh số lượng nếu vượt quá tồn kho hiện tại
        /// </summary>
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
                    // Tải thông tin thuốc đầy đủ với dữ liệu RemainQuantity mới nhất
                    var medicine = DataProvider.Instance.Context.Medicines
                        .Include(m => m.StockIns)
                        .FirstOrDefault(m => m.MedicineId == detail.MedicineId);

                    if (medicine != null)
                    {
                        // Tạo cart item từ invoice detail
                        var cartItem = new CartItem(detail);

                        // Số lượng gốc từ hóa đơn
                        int originalQty = detail.Quantity ?? 0;

                        // Tính tồn kho khả dụng cộng số lượng gốc
                        // (vì khi chỉnh sửa, chúng ta ít nhất phải giữ được số lượng gốc)
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

        /// <summary>
        /// Lọc thuốc dựa trên loại thuốc được chọn và từ khóa tìm kiếm
        /// Sử dụng cho CollectionViewSource.Filter
        /// </summary>
        /// <param name="medicine">Thuốc cần kiểm tra</param>
        /// <returns>True nếu thuốc thỏa mãn điều kiện lọc</returns>
        private bool FilterMedicine(Medicine medicine)
        {
            // Lọc theo loại thuốc nếu đã chọn (không phải "Tất cả loại thuốc")
            if (SelectedCategory != null && SelectedCategory.CategoryId != 0 &&
                medicine.CategoryId != SelectedCategory.CategoryId)
            {
                return false;
            }

            // Lọc theo từ khóa tìm kiếm nếu có
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                return medicine.Name.ToLower().Contains(searchLower) ||           // Tìm theo tên
                       $"M{medicine.MedicineId}".ToLower().Contains(searchLower) || // Tìm theo mã thuốc
                       (medicine.BarCode != null && medicine.BarCode.ToLower().Contains(searchLower)); // Tìm theo barcode
            }

            return true; // Hiển thị tất cả nếu không có điều kiện lọc
        }

        /// <summary>
        /// Thực hiện tìm kiếm thuốc
        /// Refresh CollectionView để áp dụng bộ lọc mới
        /// </summary>
        private void SearchMedicines()
        {
            CollectionViewSource.GetDefaultView(MedicineList).Refresh();
        }

        /// <summary>
        /// Thực hiện lọc thuốc theo loại
        /// Refresh CollectionView để áp dụng bộ lọc mới
        /// </summary>
        private void FilterMedicines()
        {
            CollectionViewSource.GetDefaultView(MedicineList).Refresh();
        }

        /// <summary>
        /// Thêm thuốc vào giỏ hàng
        /// Kiểm tra tồn kho và cảnh báo nếu cần
        /// Cập nhật số lượng nếu thuốc đã có trong giỏ
        /// </summary>
        /// <param name="medicine">Thuốc cần thêm vào giỏ</param>
        private void AddToCart(Medicine medicine)
        {
            if (medicine == null) return;

            try
            {
                // Đảm bảo có thông tin tồn kho mới nhất với giá trị RemainQuantity fresh
                var context = DataProvider.Instance.Context;
                var refreshedMedicine = context.Medicines
                    .Include(m => m.StockIns)
                    .FirstOrDefault(m => m.MedicineId == medicine.MedicineId);

                if (refreshedMedicine == null)
                {
                    MessageBoxService.ShowError("Không thể tải thông tin thuốc từ cơ sở dữ liệu.", "Lỗi");
                    return;
                }

                // Reset cache để đảm bảo tính toán mới
                refreshedMedicine._availableStockInsCache = null;

                // Kiểm tra xem đây có phải là lô cuối cùng không
                if (refreshedMedicine.IsLastestStockIn)
                {
                    MessageBoxService.ShowWarning(
                        $"Lưu ý: {refreshedMedicine.Name} đang sử dụng lô cuối cùng. Vui lòng nhập thêm hàng sớm.",
                        "Cảnh báo tồn kho thấp"
                    );
                }

                // Lấy số lượng tồn kho vật lý chính xác sử dụng RemainQuantity trực tiếp
                int actualStock = refreshedMedicine.TotalPhysicalStockQuantity;

                // Lấy số lượng người dùng yêu cầu
                int requestedQuantity = medicine.TempQuantity > 0 ? medicine.TempQuantity : 1;

                // Kiểm tra xem thuốc này đã có trong giỏ hàng chưa
                var existingItem = CartItems.FirstOrDefault(i => i.Medicine.MedicineId == medicine.MedicineId);
                int quantityInCart = existingItem?.Quantity ?? 0;

                // Tính số lượng có thể thêm (tồn kho - số lượng đã có trong giỏ)
                int availableToAdd = actualStock - quantityInCart;

                // Kiểm tra số lượng yêu cầu có vượt quá tồn kho không
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

                // Cập nhật hoặc thêm vào giỏ hàng
                if (existingItem != null)
                {
                    // Cập nhật số lượng của item đã có
                    existingItem.Quantity += requestedQuantity;
                }
                else
                {
                    // Thêm item mới vào giỏ với thông tin giá chính xác
                    var cartItem = new CartItem(refreshedMedicine, requestedQuantity);
                    CartItems.Add(cartItem);
                }

                // Reset số lượng tạm thời cho lần thêm tiếp theo
                medicine.TempQuantity = 1;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi thêm vào giỏ hàng: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Xóa item khỏi giỏ hàng
        /// </summary>
        /// <param name="item">Item cần xóa</param>
        private void RemoveFromCart(CartItem item)
        {
            if (item == null) return;
            CartItems.Remove(item);
        }

        /// <summary>
        /// Xóa tất cả item trong giỏ hàng
        /// </summary>
        private void ClearCart()
        {
            if (CartItems == null || CartItems.Count == 0) return;
            CartItems.Clear();
        }

        /// <summary>
        /// Mở cửa sổ thêm bệnh nhân mới
        /// Truyền thông tin đã nhập vào ViewModel của cửa sổ mới
        /// Cập nhật thông tin bệnh nhân và giảm giá sau khi thêm thành công
        /// </summary>
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

        /// <summary>
        /// Tìm kiếm bệnh nhân theo tên hoặc số điện thoại
        /// Thực hiện validation trước khi tìm kiếm
        /// Đề xuất tạo bệnh nhân mới nếu không tìm thấy
        /// </summary>
        private void FindPatient()
        {
            try
            {
                // Bật validation cho các field bệnh nhân
                _isValidating = true;
                _touchedFields.Add(nameof(PatientName));
                _touchedFields.Add(nameof(Phone));

                // Kích hoạt validation
                OnPropertyChanged(nameof(PatientName));
                OnPropertyChanged(nameof(Phone));

                // Kiểm tra lỗi validation
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

                // Tìm theo số điện thoại nếu có (ưu tiên vì chính xác hơn)
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
                    // Tìm thấy bệnh nhân - cập nhật thông tin
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
                    // Không tìm thấy - đề xuất tạo mới
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

                // Cho phép bán thuốc không cần thông tin bệnh nhân
                Patient patient = null;

                // Nếu có nhập thông tin bệnh nhân thì mới validate và tìm kiếm
                if (!string.IsNullOrWhiteSpace(PatientName) || !string.IsNullOrWhiteSpace(Phone))
                {
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

                    // Tìm bệnh nhân đã chọn hoặc theo thông tin nhập
                    patient = SelectedPatient;

                    if (patient == null)
                    {
                        var query = DataProvider.Instance.Context.Patients
                            .Include(p => p.PatientType)
                            .Where(p => p.IsDeleted != true);

                        if (!string.IsNullOrWhiteSpace(Phone))
                        {
                            patient = query.FirstOrDefault(p => p.Phone == Phone.Trim());
                        }

                        if (patient == null && !string.IsNullOrWhiteSpace(PatientName))
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
                                patient = SelectedPatient;
                            }
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

                            // Cập nhật thông tin hóa đơn
                            invoice.PatientId = patient?.PatientId;
                            invoice.TotalAmount = TotalAmount - Discount;
                            invoice.StaffPrescriberId = CurrentAccount.StaffId;
                            invoice.Discount = Discount;
                            invoice.Status = "Chưa thanh toán";

                            if (invoice.InvoiceType == "Khám bệnh")
                            {
                                invoice.InvoiceType = "Khám và bán thuốc";
                            }

             
                            foreach (var detail in originalDetails)
                            {
                                context.InvoiceDetails.Remove(detail);
                            }

                           
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
                            // Tải thông tin thuốc đầy đủ với dữ liệu RemainQuantity mới nhất
                            var medicine = context.Medicines
                                .Include(m => m.StockIns)
                                .FirstOrDefault(m => m.MedicineId == item.Medicine.MedicineId);

                            if (medicine != null)
                            {
                                medicine._availableStockInsCache = null;
                                var sellingStockIn = medicine.SellingStockIn;

                                var invoiceDetail = item.ToInvoiceDetail(invoice.InvoiceId);

                                //Gán StockInId từ lô đang bán
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
                            // Cập nhật số lượng tồn kho sau khi thanh toán
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
                        throw;
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
