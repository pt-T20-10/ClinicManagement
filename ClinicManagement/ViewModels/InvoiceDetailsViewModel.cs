using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;


namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel quản lý chi tiết hóa đơn - hiển thị thông tin chi tiết và xử lý thanh toán
    /// Triển khai từ BaseViewModel để hỗ trợ INotifyPropertyChanged
    /// </summary>
    public class InvoiceDetailsViewModel : BaseViewModel
    {
        #region Properties
        /// <summary>
        /// Cửa sổ thanh toán - được tạo động khi người dùng chọn thanh toán
        /// </summary>
        private Window _paymentWindow;

        /// <summary>
        /// Hóa đơn chính được hiển thị - thuộc tính trung tâm của ViewModel
        /// Khi được thiết lập sẽ tự động tải các thông tin liên quan
        /// </summary>
        private Invoice _invoice;
        public Invoice Invoice
        {
            get => _invoice;
            set
            {
                _invoice = value;
                OnPropertyChanged();
                // Tự động tải các thông tin liên quan khi hóa đơn được thiết lập
                LoadInvoiceDetails();
                CheckMedicalRecord();
                CheckPatient();
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



        /// <summary>
        /// Danh sách các chi tiết hóa đơn (thuốc, dịch vụ) 
        /// Sử dụng ObservableCollection để tự động cập nhật UI khi có thay đổi
        /// </summary>
        private ObservableCollection<InvoiceDetail> _invoiceDetails;
        public ObservableCollection<InvoiceDetail> InvoiceDetails
        {
            get => _invoiceDetails ??= new ObservableCollection<InvoiceDetail>();
            set
            {
                _invoiceDetails = value;
                OnPropertyChanged();
                // Tự động tính toán lại tổng tiền khi danh sách chi tiết thay đổi
                CalculateInvoiceTotals();
            }
        }

        /// <summary>
        /// Hồ sơ bệnh án liên quan (nếu hóa đơn này là hóa đơn khám bệnh)
        /// Chỉ có giá trị với hóa đơn loại "Khám bệnh" hoặc "Khám và bán thuốc"
        /// </summary>
        private MedicalRecord _medicalRecord;
        public MedicalRecord MedicalRecord
        {
            get => _medicalRecord;
            set
            {
                _medicalRecord = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Cờ hiệu kiểm tra xem hóa đơn có thông tin bệnh nhân hay không
        /// Được sử dụng để điều khiển hiển thị UI có điều kiện
        /// </summary>
        private bool _hasPatient;
        public bool HasPatient
        {
            get => _hasPatient;
            set
            {
                _hasPatient = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Cờ hiệu kiểm tra xem hóa đơn có hồ sơ bệnh án liên quan hay không
        /// Được sử dụng để điều khiển hiển thị thông tin bệnh án trong UI
        /// </summary>
        private bool _hasMedicalRecord;
        public bool HasMedicalRecord
        {
            get => _hasMedicalRecord;
            set
            {
                _hasMedicalRecord = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Trạng thái thanh toán của hóa đơn
        /// True = đã thanh toán, False = chưa thanh toán
        /// </summary>
        private bool _isPaid;
        public bool IsPaid
        {
            get => _isPaid;
            set
            {
                _isPaid = value;
                OnPropertyChanged();
                // Đảm bảo IsNotPaid luôn là trạng thái ngược lại
                IsNotPaid = !value;
            }
        }

        /// <summary>
        /// Phương thức thanh toán đã được sử dụng (Tiền mặt, Chuyển khoản, etc.)
        /// Được lưu trữ và hiển thị sau khi thanh toán hoàn tất
        /// </summary>
        private string _paymentMethod;
        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                _paymentMethod = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Ngày giờ thực hiện thanh toán
        /// Được ghi nhận tự động khi thực hiện thanh toán thành công
        /// </summary>
        private DateTime? _paymentDate;
        public DateTime? PaymentDate
        {
            get => _paymentDate;
            set
            {
                _paymentDate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Lựa chọn thanh toán bằng tiền mặt trong giao diện thanh toán
        /// Mặc định là true (được chọn sẵn)
        /// </summary>
        private bool _isCashPayment = true;
        public bool IsCashPayment
        {
            get => _isCashPayment;
            set
            {
                _isCashPayment = value;
                OnPropertyChanged();
                // Đảm bảo chỉ một phương thức thanh toán được chọn tại một thời điểm
                if (_isBankTransfer == _isCashPayment)
                {
                    _isBankTransfer = !value;
                    OnPropertyChanged(nameof(IsBankTransfer));
                }
            }
        }

        /// <summary>
        /// Lựa chọn thanh toán bằng chuyển khoản ngân hàng
        /// Khi được chọn sẽ hiển thị mã QR cho việc thanh toán
        /// </summary>
        private bool _isBankTransfer;
        public bool IsBankTransfer
        {
            get => _isBankTransfer;
            set
            {
                _isBankTransfer = value;
                OnPropertyChanged();
                // Đảm bảo chỉ một phương thức thanh toán được chọn tại một thời điểm
                if (_isCashPayment == _isBankTransfer)
                {
                    _isCashPayment = !value;
                    OnPropertyChanged(nameof(IsCashPayment));
                }
            }
        }

        /// <summary>
        /// Hình ảnh mã QR để thanh toán (khi chọn chuyển khoản)
        /// Hiện tại sử dụng hình ảnh tĩnh từ resources
        /// </summary>
        private BitmapImage _qrCodeImage;
        public BitmapImage QrCodeImage
        {
            get => _qrCodeImage;
            set
            {
                _qrCodeImage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tổng tiền trước khi áp dụng giảm giá và thuế
        /// Được tính từ tổng các mục trong InvoiceDetails
        /// </summary>
        private decimal _subTotal;
        public decimal SubTotal
        {
            get => _subTotal;
            set
            {
                _subTotal = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tổng giảm giá (thuộc tính cũ, hiện không sử dụng)
        /// Được thay thế bởi DiscountAmount
        /// </summary>
        private decimal _totalDiscount;
        public decimal TotalDiscount
        {
            get => _totalDiscount;
            set
            {
                _totalDiscount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Số tiền thuế VAT được tính từ phần trăm thuế trong hóa đơn
        /// Áp dụng sau khi đã trừ giảm giá
        /// </summary>
        private decimal _taxAmount;
        public decimal TaxAmount
        {
            get => _taxAmount;
            set
            {
                _taxAmount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Tổng số tiền cuối cùng phải thanh toán
        /// Được tính theo công thức: SubTotal - DiscountAmount + TaxAmount
        /// </summary>
        public decimal TotalAmount
        {
            get
            {
                // Công thức tính: Tạm tính - Giảm giá + Thuế
                return SubTotal - DiscountAmount + TaxAmount;
            }
        }

        /// <summary>
        /// Trạng thái chưa thanh toán (ngược lại của IsPaid)
        /// Được sử dụng để điều khiển hiển thị UI
        /// </summary>
        private bool _isNotPaid;
        public bool IsNotPaid
        {
            get => _isNotPaid;
            set
            {
                _isNotPaid = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Phần trăm giảm giá theo loại bệnh nhân
        /// Được lấy từ PatientType của bệnh nhân (nếu có)
        /// </summary>
        private decimal? _patientTypeDiscount;
        public decimal? PatientTypeDiscount
        {
            get => _patientTypeDiscount;
            set
            {
                _patientTypeDiscount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Số tiền giảm giá thực tế (đã tính theo phần trăm)
        /// Được tính từ SubTotal và phần trăm giảm giá
        /// </summary>
        private decimal _discountAmount;
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                _discountAmount = value;
                OnPropertyChanged();
                // Cập nhật TotalAmount khi số tiền giảm giá thay đổi
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command xử lý thanh toán - mở cửa sổ thanh toán
        /// Chỉ khả dụng khi hóa đơn chưa thanh toán
        /// </summary>
        public ICommand ProcessPaymentCommand { get; set; }

        /// <summary>
        /// Command đóng cửa sổ hiện tại
        /// Tìm và đóng cửa sổ thông qua nhiều phương thức khác nhau
        /// </summary>
        public ICommand CloseWindow { get; set; }

        /// <summary>
        /// Command tạo mã QR (hiện chưa được sử dụng)
        /// Dự phòng cho tính năng tạo mã QR động
        /// </summary>
        public ICommand GenerateQRCodeCommand { get; set; }

        /// <summary>
        /// Command xác nhận thanh toán - thực hiện thanh toán cuối cùng
        /// Cập nhật trạng thái hóa đơn và tồn kho
        /// </summary>
        public ICommand ConfirmPaymentCommand { get; set; }

        /// <summary>
        /// Command áp dụng giảm giá theo loại bệnh nhân
        /// Chỉ khả dụng khi có thông tin loại bệnh nhân
        /// </summary>
        public ICommand ApplyPatientDiscountCommand { get; set; }

        /// <summary>
        /// Command tính lại tổng tiền
        /// Được sử dụng khi có thay đổi về giảm giá hoặc thuế
        /// </summary>
        public ICommand RecalculateTotalsCommand { get; set; }

        /// <summary>
        /// Command chỉnh sửa hóa đơn bán thuốc
        /// Chuyển về tab bán thuốc để chỉnh sửa hóa đơn
        /// </summary>
        public ICommand EditSaleCommand { get; set; }

        /// <summary>
        /// Command xuất hóa đơn ra file PDF
        /// Sử dụng thư viện QuestPDF để tạo báo cáo
        /// </summary>
        public ICommand ExportInvoiceCommand { get; set; }

        #endregion
        public ICommand LoadedWindowCommand { get; set; }
        /// <summary>
        /// Constructor khởi tạo ViewModel với hóa đơn cụ thể
        /// </summary>
        /// <param name="invoice">Hóa đơn cần hiển thị chi tiết. Có thể null.</param>
        public InvoiceDetailsViewModel(Invoice invoice = null)
        {
            InitializeCommands();

            if (invoice != null)
            {
                Invoice = invoice;
            }
            // Khởi tạo LoadedUCCommand để tải tài khoản đúng cách
            LoadedWindowCommand = new RelayCommand<Window>(
                (w) => {
                    if (w != null)
                    {
                        // Lấy MainViewModel từ Application resources
                        var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                        if (mainVM != null && mainVM.CurrentAccount != null)
                        {
                            // Cập nhật tài khoản hiện tại
                            CurrentAccount = mainVM.CurrentAccount;
                        }
                    }
                },
                (w) => true
            );
        }

        /// <summary>
        /// Khởi tạo tất cả các command với logic thực thi và điều kiện kích hoạt
        /// </summary>
        private void InitializeCommands()
        {
            // Command xử lý thanh toán - chỉ khả dụng khi hóa đơn chưa thanh toán
            ProcessPaymentCommand = new RelayCommand<object>(
                p => ProcessPayment(),
                p => Invoice != null && Invoice.Status == "Chưa thanh toán");

            // Command đóng cửa sổ - luôn khả dụng
            CloseWindow = new RelayCommand<object>(
                p =>
                {
                    // Tìm cửa sổ để đóng thông qua nhiều phương thức khác nhau
                    Window window = null;

                    // Phương thức 1: Nếu tham số là Button hoặc UIElement khác
                    if (p is UIElement element)
                    {
                        window = Window.GetWindow(element);
                    }
                    // Phương thức 2: Nếu tham số đã là Window
                    else if (p is Window w)
                    {
                        window = w;
                    }
                    // Phương thức 3: Tìm cửa sổ đang active thông qua Application
                    else if (Application.Current.Windows.Count > 0)
                    {
                        // Ưu tiên cửa sổ đang focus, nếu không có thì lấy MainWindow
                        window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                            ?? Application.Current.MainWindow;
                    }

                    // Đóng cửa sổ nếu tìm thấy
                    window?.Close();

                },
                p => true
            );

            // Command xuất PDF - chỉ khả dụng khi có hóa đơn
            ExportInvoiceCommand = new RelayCommand<object>(
              p => ExportInvoiceToPdf(),
              p => Invoice != null
          );

            // Command xác nhận thanh toán - chỉ khả dụng khi có hóa đơn
            ConfirmPaymentCommand = new RelayCommand<Window>(
                p => ConfirmPayment(null),
                p => Invoice != null);

            // Command áp dụng giảm giá bệnh nhân - chỉ khả dụng khi có thông tin giảm giá
            ApplyPatientDiscountCommand = new RelayCommand<object>(
                p => ApplyPatientDiscount(),
                p => Invoice != null && Invoice.Patient?.PatientType != null && PatientTypeDiscount.HasValue);

            // Command tính lại tổng tiền - chỉ khả dụng khi có hóa đơn
            RecalculateTotalsCommand = new RelayCommand<object>(
                p => RecalculateTotals(),
                p => Invoice != null);

            // Command chỉnh sửa bán thuốc - chỉ khả dụng với hóa đơn bán thuốc chưa thanh toán
            EditSaleCommand = new RelayCommand<Window>(
           parameter => EditSale(parameter),
           parameter => CanEditMedicineSale
       );
        }

        /// <summary>
        /// Tải chi tiết hóa đơn từ cơ sở dữ liệu
        /// Bao gồm thông tin thanh toán nếu hóa đơn đã được thanh toán
        /// </summary>
        private void LoadInvoiceDetails()
        {
            if (Invoice == null) return;

            try
            {
                // Tải danh sách chi tiết hóa đơn từ database
                var invoiceDetails = DataProvider.Instance.Context.InvoiceDetails
                    .Where(d => d.InvoiceId == Invoice.InvoiceId)
                    .ToList();

                InvoiceDetails = new ObservableCollection<InvoiceDetail>(invoiceDetails);

                // Kiểm tra trạng thái thanh toán
                IsPaid = Invoice.Status == "Đã thanh toán";
                IsNotPaid = !IsPaid;

                // Đảm bảo giá trị giảm giá và thuế được khởi tạo
                if (Invoice.Discount == null) Invoice.Discount = 0;
                if (Invoice.Tax == null) Invoice.Tax = 10; // Mặc định 10%

                // Tính toán tổng tiền ban đầu
                CalculateInvoiceTotals();

                // Nếu đã thanh toán, trích xuất thông tin thanh toán từ trường Notes
                if (IsPaid)
                {
                    // Thông tin thanh toán được lưu trong trường Notes theo định dạng:
                    // "Phương thức thanh toán:Tiền mặt;Ngày thanh toán:2023-06-06 14:30:00"
                    if (!string.IsNullOrEmpty(Invoice.Notes) && Invoice.Notes.Contains("Phương thức thanh toán:"))
                    {
                        try
                        {
                            string notes = Invoice.Notes;

                            // Trích xuất phương thức thanh toán
                            int methodStartIndex = notes.IndexOf("Phương thức thanh toán:") + "Phương thức thanh toán:".Length;
                            int methodEndIndex = notes.IndexOf(';', methodStartIndex);
                            if (methodEndIndex == -1) methodEndIndex = notes.Length;

                            PaymentMethod = notes.Substring(methodStartIndex, methodEndIndex - methodStartIndex);

                            // Trích xuất ngày thanh toán nếu có
                            if (notes.Contains("Ngày thanh toán:"))
                            {
                                int dateStartIndex = notes.IndexOf("Ngày thanh toán:") + "Ngày thanh toán:".Length;
                                int dateEndIndex = notes.IndexOf(';', dateStartIndex);
                                if (dateEndIndex == -1) dateEndIndex = notes.Length;

                                string dateStr = notes.Substring(dateStartIndex, dateEndIndex - dateStartIndex);
                                if (DateTime.TryParse(dateStr, out DateTime paymentDate))
                                {
                                    PaymentDate = paymentDate;
                                }
                            }
                        }
                        catch
                        {
                            // Nếu phân tích thất bại, sử dụng giá trị mặc định
                            PaymentMethod = "Không xác định";
                            PaymentDate = Invoice.InvoiceDate;
                        }
                    }
                    else
                    {
                        // Giá trị mặc định nếu không có thông tin thanh toán được lưu trữ
                        PaymentMethod = "Không xác định";
                        PaymentDate = Invoice.InvoiceDate;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải chi tiết hóa đơn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Xử lý chức năng chỉnh sửa hóa đơn bán thuốc
        /// Chuyển đến tab bán thuốc với hóa đơn hiện tại để chỉnh sửa
        /// </summary>
        /// <param name="window">Cửa sổ hiện tại để đóng sau khi chuyển tab</param>
        private void EditSale(Window window)
        {
            try
            {
                // Truy cập MedicineSellViewModel từ App resources
                var medicineSellVM = Application.Current.Resources["MedicineSellVM"] as MedicineSellViewModel;

                if (medicineSellVM == null)
                {
                    MessageBoxService.ShowError("Không thể khởi tạo màn hình bán thuốc.",
                                    "Lỗi");
                    return;
                }

                // Thiết lập hóa đơn hiện tại để chỉnh sửa
                medicineSellVM.CurrentInvoice = Invoice;

                // Tìm và kích hoạt tab Bán thuốc
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null) return;

                var tabControl = LogicalTreeHelper.FindLogicalNode(mainWindow, "MainTabControl") as TabControl;
                if (tabControl == null) return;

                // Tìm tab "Bán thuốc"
                foreach (var item in tabControl.Items)
                {
                    if (item is TabItem tabItem)
                    {
                        var header = tabItem.Header as Grid;
                        if (header != null)
                        {
                            foreach (var child in LogicalTreeHelper.GetChildren(header))
                            {
                                if (child is Grid childGrid && (int)childGrid.GetValue(Grid.RowProperty) == 0)
                                {
                                    foreach (var textBlockElement in LogicalTreeHelper.GetChildren(childGrid))
                                    {
                                        if (textBlockElement is TextBlock textBlock && textBlock.Text == "Bán thuốc")
                                        {
                                            // Chuyển đến tab bán thuốc
                                            tabControl.SelectedItem = tabItem;

                                            // Đóng cửa sổ hiện tại
                                            window?.Close();
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Nếu không tìm thấy tab
                MessageBoxService.ShowWarning("Không tìm thấy tab Bán thuốc.",
                              "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi chuyển sang màn hình bán thuốc: {ex.Message}",
                               "Lỗi");
            }
        }

        /// <summary>
        /// Kiểm tra và thiết lập thông tin bệnh nhân
        /// Bao gồm thông tin giảm giá theo loại bệnh nhân
        /// </summary>
        private void CheckPatient()
        {
            if (Invoice == null) return;

            HasPatient = Invoice.Patient != null;

            // Lấy thông tin giảm giá từ loại bệnh nhân nếu có
            if (HasPatient && Invoice.Patient?.PatientType != null)
            {
                PatientTypeDiscount = Invoice.Patient.PatientType.Discount;
            }
            else
            {
                PatientTypeDiscount = null;
            }
        }

        /// <summary>
        /// Áp dụng giảm giá theo loại bệnh nhân vào hóa đơn
        /// </summary>
        private void ApplyPatientDiscount()
        {
            if (Invoice != null && PatientTypeDiscount.HasValue)
            {
                Invoice.Discount = PatientTypeDiscount;
                RecalculateTotals();
            }
        }

        /// <summary>
        /// Kiểm tra và tải hồ sơ bệnh án liên quan (chỉ với hóa đơn khám bệnh)
        /// </summary>
        private void CheckMedicalRecord()
        {
            if (Invoice == null) return;

            // Chỉ hóa đơn khám bệnh mới có hồ sơ bệnh án
            if (Invoice.InvoiceType == "Khám bệnh" || Invoice.InvoiceType == "Khám và bán thuốc")
            {
                try
                {
                    // Kiểm tra xem hóa đơn có liên kết với hồ sơ bệnh án không
                    if (Invoice.MedicalRecordId.HasValue)
                    {
                        // Tìm hồ sơ bệnh án sử dụng MedicalRecordId từ Invoice
                        var medicalRecord = DataProvider.Instance.Context.MedicalRecords
                            .FirstOrDefault(m => m.RecordId == Invoice.MedicalRecordId);

                        if (medicalRecord != null)
                        {
                            MedicalRecord = medicalRecord;
                            HasMedicalRecord = true;
                        }
                        else
                        {
                            HasMedicalRecord = false;
                        }
                    }
                    else
                    {
                        HasMedicalRecord = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxService.ShowError($"Lỗi khi tải hồ sơ bệnh án: {ex.Message}", "Lỗi");
                    HasMedicalRecord = false;
                }
            }
            else
            {
                HasMedicalRecord = false;
            }
        }

        /// <summary>
        /// Tính toán các tổng tiền trong hóa đơn
        /// Bao gồm: tạm tính, giảm giá, thuế, và tổng cộng
        /// </summary>
        private void CalculateInvoiceTotals()
        {
            if (InvoiceDetails == null || !InvoiceDetails.Any())
            {
                SubTotal = 0;
                DiscountAmount = 0;
                TaxAmount = 0;
                OnPropertyChanged(nameof(TotalAmount));
                return;
            }

            // Tính tạm tính (trước giảm giá)
            SubTotal = InvoiceDetails.Sum(d => d.SalePrice * (d.Quantity ?? 1)) ?? 0;

            // Tính số tiền giảm giá từ phần trăm giảm giá của hóa đơn
            decimal discountPercent = Invoice?.Discount ?? 0;
            DiscountAmount = SubTotal * (discountPercent / 100m);

            // Tính thuế VAT từ phần trăm thuế của hóa đơn
            decimal taxPercent = Invoice?.Tax ?? 10; // Mặc định 10% nếu không được thiết lập
            decimal amountAfterDiscount = SubTotal - DiscountAmount;
            TaxAmount = amountAfterDiscount * (taxPercent / 100m);

            OnPropertyChanged(nameof(TotalAmount));

            // Nếu đang tính lại, cập nhật tổng tiền của hóa đơn
            if (!IsPaid && Invoice != null)
            {
                Invoice.TotalAmount = TotalAmount;
            }
        }

        /// <summary>
        /// Tính lại tất cả các tổng tiền
        /// Wrapper method cho CalculateInvoiceTotals
        /// </summary>
        private void RecalculateTotals()
        {
            CalculateInvoiceTotals();
        }

        /// <summary>
        /// Mở cửa sổ thanh toán với giao diện thanh toán tùy chỉnh
        /// </summary>
        private void ProcessPayment()
        {
            if (Invoice == null || Invoice.Status == "Đã thanh toán") return;

            _paymentWindow = new Window
            {
                Title = "Thanh toán hóa đơn",
                Width = 500,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                DataContext = this,
                Content = CreatePaymentControl() // Tạo giao diện thanh toán động
            };

            _paymentWindow.ShowDialog();
        }

        /// <summary>
        /// Tạo giao diện thanh toán động với các tùy chọn thanh toán
        /// Bao gồm: tiền mặt, chuyển khoản (với QR code)
        /// </summary>
        /// <returns>UIElement chứa toàn bộ giao diện thanh toán</returns>
        private UIElement CreatePaymentControl()
        {
            // Tạo panel chính chứa toàn bộ giao diện thanh toán
            var panel = new StackPanel
            {
                Margin = new Thickness(20) // Tạo khoảng cách 20px xung quanh nội dung so với viền cửa sổ
            };

            // --- PHẦN TIÊU ĐỀ ---
            var paymentOptionTitle = new TextBlock
            {
                Text = "PHƯƠNG THỨC THANH TOÁN", // Nội dung tiêu đề
                FontWeight = FontWeights.Bold,   // Kiểu chữ in đậm
                FontSize = 16,                   // Cỡ chữ lớn hơn mặc định
                Margin = new Thickness(0, 0, 0, 10) // Thêm khoảng cách 10px ở dưới tiêu đề
            };
            panel.Children.Add(paymentOptionTitle); // Thêm tiêu đề vào panel chính

            // --- LỰA CHỌN TIỀN MẶT ---
            var cashOption = new RadioButton
            {
                Content = "Tiền mặt",            // Nhãn hiển thị cho tùy chọn tiền mặt
                IsChecked = true,                // Mặc định chọn phương thức này
                Margin = new Thickness(0, 5, 0, 5), // Khoảng cách trên dưới 5px
                GroupName = "PaymentMethod"      // Đặt vào cùng nhóm với các RadioButton khác
            };
            // Liên kết trạng thái của RadioButton với thuộc tính IsCashPayment trong ViewModel
            cashOption.SetBinding(RadioButton.IsCheckedProperty, new System.Windows.Data.Binding("IsCashPayment")
            {
                Mode = System.Windows.Data.BindingMode.TwoWay // Cho phép cập nhật 2 chiều
            });
            panel.Children.Add(cashOption); // Thêm tùy chọn tiền mặt vào panel

            // --- LỰA CHỌN CHUYỂN KHOẢN ---
            var bankOption = new RadioButton
            {
                Content = "Chuyển khoản ngân hàng", // Nhãn hiển thị
                Margin = new Thickness(0, 5, 0, 15), // Khoảng cách trên 5px, dưới 15px (lớn hơn để tách biệt với phần tiếp theo)
                GroupName = "PaymentMethod"          // Cùng nhóm với RadioButton tiền mặt
            };
            // Liên kết với thuộc tính IsBankTransfer trong ViewModel
            bankOption.SetBinding(RadioButton.IsCheckedProperty, new System.Windows.Data.Binding("IsBankTransfer")
            {
                Mode = System.Windows.Data.BindingMode.TwoWay // Cho phép cập nhật 2 chiều
            });
            panel.Children.Add(bankOption);

            // --- PHẦN MÃ QR CHỈ HIỂN THỊ KHI CHỌN CHUYỂN KHOẢN ---
            var qrCodePanel = new StackPanel(); // Panel con chứa các thành phần liên quan đến QR

            // Thiết lập ẩn/hiện dựa trên giá trị của IsBankTransfer
            qrCodePanel.SetBinding(UIElement.VisibilityProperty, new System.Windows.Data.Binding("IsBankTransfer")
            {
                // Chuyển đổi giá trị Boolean sang Visibility (True -> Visible, False -> Collapsed)
                Converter = new Converter.BooleanToVisibilityConverter()
            });

            // Tiêu đề phần mã QR
            var qrCodeTitle = new TextBlock
            {
                Text = "Quét mã QR để thanh toán:", // Hướng dẫn người dùng quét mã
                Margin = new Thickness(0, 0, 0, 10) // Thêm khoảng cách dưới 10px
            };
            qrCodePanel.Children.Add(qrCodeTitle);

            // --- PHẦN HIỂN THỊ MÃ QR ---
            // Thử tải hình ảnh mã QR từ resources của dự án
            bool qrImageLoaded = false;
            try
            {
                // Lấy thư mục thực thi
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri("pack://application:,,,/ResourceXAML/Images/paymentqr.png");
                bitmap.EndInit();

                var qrImage = new System.Windows.Controls.Image
                {
                    Source = bitmap,
                    Width = 150,
                    Height = 150,
                    Stretch = System.Windows.Media.Stretch.Uniform
                };
                qrCodePanel.Children.Add(qrImage);
                qrImageLoaded = true;
            }
            catch
            {
                qrImageLoaded = false;
            }

            // Hiển thị thông tin thanh toán dưới mã QR
            var qrInfoText = new TextBlock
            {
                Text = $"Số tiền: {TotalAmount:N0} VNĐ\nNội dung: TT-{Invoice?.InvoiceId}",
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };
            qrCodePanel.Children.Add(qrInfoText);

            // --- NÚT TÁC VỤ CHO PHƯƠNG THỨC QR (HỦY/ĐÃ THANH TOÁN) ---
            var qrButtonsPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Nút hủy thanh toán QR
            var qrCancelButton = new System.Windows.Controls.Button
            {
                Content = "Hủy",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(15, 5, 15, 5)
            };
            qrCancelButton.SetBinding(System.Windows.Controls.Button.CommandProperty, "CloseWindow");
            qrCancelButton.CommandParameter = qrCancelButton.TemplatedParent;
            qrButtonsPanel.Children.Add(qrCancelButton);

            // Nút xác nhận đã thanh toán QR
            var qrConfirmButton = new System.Windows.Controls.Button
            {
                Content = "Đã thanh toán QR",
                Padding = new Thickness(10, 5, 10, 5),
                Background = System.Windows.Media.Brushes.Green,
                Foreground = System.Windows.Media.Brushes.White
            };
            qrConfirmButton.SetBinding(System.Windows.Controls.Button.CommandProperty,
                new System.Windows.Data.Binding("ConfirmPaymentCommand"));
            qrButtonsPanel.Children.Add(qrConfirmButton);

            // Thêm panel chứa nút vào panel QR
            qrCodePanel.Children.Add(qrButtonsPanel);

            // Thêm panel mã QR vào panel chính
            panel.Children.Add(qrCodePanel);

            // --- PHẦN HIỂN THỊ SỐ TIỀN THANH TOÁN ---
            var amountInfoPanel = new StackPanel
            {
                Margin = new Thickness(0, 20, 0, 20)
            };

            // Hiển thị số tiền cần thanh toán
            var amountText = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                FontSize = 16
            };
            amountText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("TotalAmount")
            {
                StringFormat = "Số tiền cần thanh toán: {0:N0} VNĐ"
            });
            amountInfoPanel.Children.Add(amountText);
            panel.Children.Add(amountInfoPanel);

            // --- PHẦN NÚT TÁC VỤ CHO PHƯƠNG THỨC TIỀN MẶT (HỦY/XÁC NHẬN) ---
            // Chỉ hiển thị khi chọn phương thức thanh toán tiền mặt
            var cashButtonsPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            // Thiết lập ẩn/hiện dựa trên giá trị của IsCashPayment
            cashButtonsPanel.SetBinding(UIElement.VisibilityProperty, new System.Windows.Data.Binding("IsCashPayment")
            {
                // Chuyển đổi giá trị Boolean sang Visibility (True -> Visible, False -> Collapsed)
                Converter = new Converter.BooleanToVisibilityConverter()
            });

            // Nút hủy thanh toán
            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Hủy",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(15, 5, 15, 5)
            };
            cancelButton.SetBinding(System.Windows.Controls.Button.CommandProperty, "CloseWindow");
            cancelButton.CommandParameter = cancelButton.TemplatedParent;
            cashButtonsPanel.Children.Add(cancelButton);

            // Nút xác nhận thanh toán
            var confirmButton = new System.Windows.Controls.Button
            {
                Content = "Xác nhận thanh toán",
                Padding = new Thickness(15, 5, 15, 5),
                Background = System.Windows.Media.Brushes.Green,
                Foreground = System.Windows.Media.Brushes.White
            };
            confirmButton.SetBinding(System.Windows.Controls.Button.CommandProperty,
                new System.Windows.Data.Binding("ConfirmPaymentCommand"));
            cashButtonsPanel.Children.Add(confirmButton);

            // Thêm panel chứa các nút vào panel chính
            panel.Children.Add(cashButtonsPanel);

            // Trả về toàn bộ giao diện đã tạo
            return panel;
        }

        /// <summary>
        /// Xác nhận và xử lý thanh toán cuối cùng
        /// Cập nhật trạng thái hóa đơn, lưu thông tin thanh toán và cập nhật tồn kho
        /// </summary>
        /// <param name="paymentWindow">Cửa sổ thanh toán (hiện không sử dụng)</param>
        private void ConfirmPayment(Window paymentWindow)
        {
            if (Invoice == null) return;

            // Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
            using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
            {
                try
                {
                    // Đảm bảo giảm giá và thuế có giá trị và tính toán lại tổng tiền
                    if (Invoice.Discount == null) Invoice.Discount = 0;
                    if (Invoice.Tax == null) Invoice.Tax = 10;
                    RecalculateTotals();

                    // Cập nhật trạng thái hóa đơn và tổng số tiền
                    Invoice.StaffCashierId = CurrentAccount.StaffId; // Ghi nhận nhân viên thu ngân
                    Invoice.Status = "Đã thanh toán";
                    Invoice.TotalAmount = TotalAmount; // Đặt tổng số tiền đã tính toán

                    // Lưu thông tin thanh toán vào trường Notes
                    string paymentMethod = IsCashPayment ? "Tiền mặt" : "Chuyển khoản";
                    string paymentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    // Giữ lại ghi chú hiện có nếu có
                    string existingNotes = string.IsNullOrEmpty(Invoice.Notes) ? "" : Invoice.Notes + " | ";

                    // Thêm thông tin thanh toán vào ghi chú
                    Invoice.Notes = existingNotes + $"Phương thức thanh toán:{paymentMethod};Ngày thanh toán:{paymentDate}";

                    // Cập nhật cơ sở dữ liệu với thông tin thanh toán
                    DataProvider.Instance.Context.SaveChanges();

                    // Cập nhật tồn kho sau khi thanh toán được ghi lại
                    UpdateStockAfterPayment();

                    // Hoàn thành giao dịch nếu mọi thứ thành công
                    transaction.Commit();

                    // Cập nhật thuộc tính của view model
                    IsPaid = true;
                    IsNotPaid = false;
                    PaymentMethod = paymentMethod;
                    PaymentDate = DateTime.Now;

                    // Hiển thị thông báo thành công
                    MessageBoxService.ShowSuccess($"Thanh toán hóa đơn #{Invoice.InvoiceId} thành công!", "Thành công");

                    // Đóng cửa sổ thanh toán
                    _paymentWindow?.Close();
                    _paymentWindow = null;
                }
                catch (Exception ex)
                {
                    // Hoàn tác giao dịch nếu có lỗi xảy ra
                    transaction.Rollback();

                    // Hiển thị thông báo lỗi
                    MessageBoxService.ShowError($"Lỗi khi xử lý thanh toán: {ex.Message}", "Lỗi");
                }
            }
        }

        /// <summary>
        /// Cập nhật tồn kho sau khi thanh toán thành công
        /// Giảm số lượng RemainQuantity trong các lô nhập và cập nhật bảng Stock
        /// </summary>
        private void UpdateStockAfterPayment()
        {
            try
            {
                var context = DataProvider.Instance.Context;

                // Lấy tất cả chi tiết hóa đơn có chứa thuốc cho hóa đơn này
                var invoiceDetails = context.InvoiceDetails
                    .Where(id => id.InvoiceId == Invoice.InvoiceId && id.MedicineId.HasValue)
                    .Include(id => id.Medicine)
                    .ToList();

                // Cập nhật tồn kho cho từng loại thuốc
                foreach (var detail in invoiceDetails)
                {
                    // Detach các entity đã được load để tránh xung đột tracking của EF
                    if (detail.Medicine != null)
                        context.Entry(detail.Medicine).State = EntityState.Detached;

                    // Lấy dữ liệu thuốc mới với tất cả các mối quan hệ
                    var medicine = context.Medicines
                        .Include(m => m.StockIns)
                        .Include(m => m.InvoiceDetails)
                        .ThenInclude(id => id.Invoice) // Include Invoice để kiểm tra trạng thái
                        .FirstOrDefault(m => m.MedicineId == detail.MedicineId);

                    if (medicine == null)
                    {
                        continue;
                    }

                    // Xóa cache để đảm bảo tính toán mới
                    medicine._availableStockInsCache = null;

                    // Tính số lượng tồn kho vật lý chính xác
                    int totalStockIn = medicine.StockIns?.Sum(si => si.Quantity) ?? 0;
                    int totalSold = medicine.InvoiceDetails
                        .Where(id => id.Invoice.Status == "Đã thanh toán")
                        .Sum(id => id.Quantity ?? 0);
                    int actualQuantity = Math.Max(0, totalStockIn - totalSold);

                    // Tính số lượng có thể sử dụng với lọc ngày hết hạn phù hợp
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var minimumExpiryDate = today.AddDays(Medicine.MinimumDaysBeforeExpiry);

                    var validStockIns = medicine.StockIns
                        .Where(si => !si.ExpiryDate.HasValue || si.ExpiryDate.Value >= minimumExpiryDate)
                        .OrderBy(si => si.ImportDate);

                    int usableStock = 0;
                    int remainingToSubtract = totalSold;

                    foreach (var stockIn in validStockIns)
                    {
                        int remainingInThisLot = stockIn.Quantity - Math.Min(remainingToSubtract, stockIn.Quantity);
                        usableStock += Math.Max(0, remainingInThisLot);
                        remainingToSubtract = Math.Max(0, remainingToSubtract - stockIn.Quantity);
                    }

                    // Cập nhật hoặc tạo bản ghi Stock
                    var stock = context.Stocks.FirstOrDefault(s => s.MedicineId == detail.MedicineId);
                    if (stock != null)
                    {
                        stock.Quantity = actualQuantity;
                        stock.UsableQuantity = usableStock;
                        stock.LastUpdated = DateTime.Now;
                    }
                    else
                    {
                        var newStock = new Stock
                        {
                            MedicineId = detail.MedicineId.Value,
                            Quantity = actualQuantity,
                            UsableQuantity = usableStock,
                            LastUpdated = DateTime.Now
                        };
                        context.Stocks.Add(newStock);
                    }
                }

                // Lưu tất cả thay đổi cùng một lúc
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi cập nhật tồn kho: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Thuộc tính kiểm tra xem có thể chỉnh sửa hóa đơn bán thuốc hay không
        /// Chỉ cho phép chỉnh sửa hóa đơn chưa thanh toán và có bán thuốc
        /// </summary>
        public bool CanEditMedicineSale => Invoice != null &&
                                    Invoice.Status == "Chưa thanh toán" &&
                                    (Invoice.InvoiceType == "Bán thuốc" || Invoice.InvoiceType == "Khám và bán thuốc");

        #region PDF
        /// <summary>
        /// Xuất hóa đơn ra file PDF với thanh tiến trình
        /// Sử dụng thư viện QuestPDF để tạo báo cáo professional
        /// </summary>
        private void ExportInvoiceToPdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            try
            {
                // Tạo hộp thoại lưu file để người dùng chọn nơi lưu PDF
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    FileName = $"HoaDon_{Invoice.InvoiceId}_{DateTime.Now:yyyyMMdd}.pdf",
                    Title = "Lưu hóa đơn PDF"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    // Tạo và hiển thị hộp thoại tiến trình
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Bắt đầu thao tác xuất trong background thread
                    Task.Run(() =>
                    {
                        try
                        {
                            // Báo cáo tiến trình: 10% - Bắt đầu
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                            // Báo cáo tiến trình: 30% - Chuẩn bị tài liệu
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(30));
                            Thread.Sleep(100); // Delay nhỏ để hiển thị

                            // Báo cáo tiến trình: 60% - Tạo nội dung
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(60));

                            // Tạo PDF sử dụng QuestPDF - với phương thức đơn giản hóa
                            GenerateSimplePdfDocument(filePath);

                            // Báo cáo tiến trình: 90% - Lưu file
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));
                            Thread.Sleep(100); // Delay nhỏ để hiển thị

                            // Báo cáo tiến trình: 100% - Hoàn thành
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                            Thread.Sleep(300); // Hiển thị 100% trong chốc lát

                            // Đóng hộp thoại tiến trình và hiển thị thông báo thành công
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();

                                MessageBoxService.ShowSuccess(
                                    $"Đã xuất hóa đơn #{Invoice.InvoiceId} thành công!\nĐường dẫn: {filePath}",
                                    "Xuất hóa đơn"
                                );

                                // Mở file PDF bằng ứng dụng xem PDF mặc định
                                if (MessageBoxService.ShowQuestion("Bạn có muốn mở file PDF không?", "Mở file"))
                                {
                                    try
                                    {
                                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                        {
                                            FileName = filePath,
                                            UseShellExecute = true
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBoxService.ShowError($"Không thể mở file: {ex.Message}", "Lỗi");
                                    }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError($"Lỗi khi xuất hóa đơn: {ex.Message}", "Lỗi xuất hóa đơn");
                            });
                        }
                    });

                    // Hiển thị dialog - sẽ block cho đến khi dialog được đóng
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xuất hóa đơn: {ex.Message}", "Lỗi xuất hóa đơn");
            }
        }

     /// <summary>
        /// Tạo tài liệu PDF đơn giản cho hóa đơn
        /// Sử dụng thư viện QuestPDF để tạo layout professional với đầy đủ thông tin hóa đơn
        /// </summary>
        /// <param name="filePath">Đường dẫn file PDF sẽ được lưu</param>
        private void GenerateSimplePdfDocument(string filePath)
        {
            // Tạo tài liệu PDF mới bằng QuestPDF
            Document.Create(document =>
            {
                document.Page(page =>
                {
                    // Thiết lập kích thước trang A4 với margin 50px
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(11)); // Font mặc định 11pt

                    // Container chính sử dụng layout Column để xếp chồng các phần tử theo chiều dọc
                    page.Content().Column(column =>
                    {
                        // === PHẦN HEADER VỚI LAYOUT ROW ===
                        column.Item().Row(row =>
                        {
                            // Bên trái - thông tin phòng khám (chiếm 2/3 chiều rộng)
                            row.RelativeItem(2).Column(col =>
                            {
                                col.Item().Text("PHÒNG KHÁM ABC")
                                    .FontSize(16).Bold(); // Tên phòng khám in đậm, cỡ chữ lớn
                                col.Item().Text("Địa chỉ: 123 Đường 456, Quận 789, TP.XYZ")
                                    .FontSize(10); // Địa chỉ với font nhỏ hơn
                                col.Item().Text("SĐT: 028.1234.5678 | Email: email@gmail.com")
                                    .FontSize(10); // Thông tin liên hệ
                            });
                       
                            // Bên phải - thông tin hóa đơn (chiếm 1/3 chiều rộng)
                            row.RelativeItem(1).Column(col =>
                            {
                                // Số hóa đơn with màu xanh nổi bật
                                col.Item().AlignRight().Text($"HÓA ĐƠN #{Invoice.InvoiceId}")
                                    .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                                // Ngày tạo hóa đơn
                                col.Item().AlignRight().Text($"Ngày: {Invoice.InvoiceDate:dd/MM/yyyy HH:mm}")
                                    .FontSize(10);
                                // Loại hóa đơn (Khám bệnh, Bán thuốc, etc.)
                                col.Item().AlignRight().Text($"Loại hóa đơn: {Invoice.InvoiceType}")
                                     .FontSize(10);
                                // Trạng thái với màu sắc tương ứng (xanh = đã thanh toán, cam = chưa thanh toán)
                                col.Item().AlignRight().Text($"Trạng thái: {Invoice.Status}")
                                    .FontSize(10)
                                    .FontColor(Invoice.Status == "Đã thanh toán" ? Colors.Green.Medium : Colors.Orange.Medium);
                            });
                        });

                        // === ĐƯỜNG KẺ PHÂN CÁCH ===
                        column.Item().PaddingVertical(10) // Khoảng cách trên dưới 10px
                            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2); // Đường kẻ màu xám nhạt

                        // === THÔNG TIN BỆNH NHÂN (CHỈ HIỂN THỊ NẾU CÓ) ===
                        if (HasPatient)
                        {
                            column.Item().PaddingTop(15).Border(1) // Viền xung quanh
                                .BorderColor(Colors.Grey.Lighten3) // Màu viền xám nhạt
                                .Background(Colors.Grey.Lighten5)  // Nền xám rất nhạt
                                .Padding(10) // Khoảng cách trong 10px
                                .Column(patientCol =>
                                {
                                    // Tiêu đề phần thông tin khách hàng
                                    patientCol.Item().Text("THÔNG TIN KHÁCH HÀNG").Bold();

                                    // Layout 2 cột cho thông tin bệnh nhân
                                    patientCol.Item().PaddingTop(5).Row(patientInfoRow =>
                                    {
                                        // Cột trái - thông tin cơ bản
                                        patientInfoRow.RelativeItem().Column(leftCol =>
                                        {
                                            leftCol.Item().Text($"Họ tên: {Invoice.Patient?.FullName}");
                                            leftCol.Item().Text($"Số điện thoại: {Invoice.Patient?.Phone}");
                                        });

                                        // Cột phải - thông tin bổ sung
                                        patientInfoRow.RelativeItem().Column(rightCol =>
                                        {
                                            // Chỉ hiển thị mã BHYT nếu có
                                            if (!string.IsNullOrEmpty(Invoice.Patient?.InsuranceCode))
                                                rightCol.Item().Text($"Mã BHYT: {Invoice.Patient?.InsuranceCode}");

                                            // Chỉ hiển thị loại khách hàng nếu có
                                            if (Invoice.Patient?.PatientType != null)
                                                rightCol.Item().Text($"Loại khách hàng: {Invoice.Patient?.PatientType?.TypeName}");
                                        });
                                    });
                                });
                        }

                        // === PHẦN CHI TIẾT HÓA ĐƠN ===
                        column.Item().PaddingTop(20).Column(detailsCol =>
                        {
                            // Tiêu đề phần chi tiết
                            detailsCol.Item().Text("CHI TIẾT HÓA ĐƠN").Bold().FontSize(12);

                            // Bảng chi tiết các sản phẩm/dịch vụ
                            detailsCol.Item().PaddingTop(5).Table(table =>
                            {
                                // Định nghĩa cấu trúc cột của bảng
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3); // Tên sản phẩm - chiếm nhiều nhất
                                    columns.RelativeColumn(1); // Số lượng - nhỏ
                                    columns.RelativeColumn(2); // Đơn giá - trung bình
                                    columns.RelativeColumn(2); // Thành tiền - trung bình
                                });

                                // Tạo hàng tiêu đề của bảng
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tên thuốc/dịch vụ").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Số lượng").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Đơn giá").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Thành tiền").Bold();
                                });

                                // Tạo các hàng dữ liệu cho từng item trong hóa đơn
                                foreach (var detail in InvoiceDetails)
                                {
                                    // Xác định tên sản phẩm (ưu tiên tên dịch vụ nếu có, không thì lấy tên thuốc)
                                    string itemName = !string.IsNullOrEmpty(detail.ServiceName)
                                        ? detail.ServiceName
                                        : detail.Medicine?.Name ?? "Không xác định";

                                    // Lấy thông tin số lượng, đơn giá và tính thành tiền
                                    int quantity = detail.Quantity ?? 1;
                                    decimal unitPrice = detail.SalePrice ?? 0;
                                    decimal totalPrice = unitPrice * quantity;

                                    // Tạo 4 ô cho mỗi hàng dữ liệu
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                        .Padding(5).Text(itemName); // Tên sản phẩm
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                        .Padding(5).AlignCenter().Text(quantity.ToString()); // Số lượng (căn giữa)
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                        .Padding(5).AlignRight().Text($"{unitPrice:N0} VNĐ"); // Đơn giá (căn phải)
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                        .Padding(5).AlignRight().Text($"{totalPrice:N0} VNĐ"); // Thành tiền (căn phải)
                                }
                            });
                        });

                        // === PHẦN TỔNG KẾT THANH TOÁN ===
                        column.Item().PaddingTop(20).AlignRight().Table(summaryTable =>
                        {
                            // Bảng tổng kết với 2 cột: nhãn và giá trị
                            summaryTable.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1); // Cột nhãn (Tạm tính, Giảm giá, etc.)
                                columns.RelativeColumn(1); // Cột giá trị
                            });

                            // Hàng tạm tính (tổng tiền trước giảm giá và thuế)
                            summaryTable.Cell().AlignRight().Padding(8).Text("Tạm tính:").Bold();
                            summaryTable.Cell().AlignRight().Padding(8).Text($"{SubTotal:N0} VNĐ");

                            // Hàng giảm giá (chỉ hiển thị nếu có giảm giá)
                            if (DiscountAmount > 0)
                            {
                                summaryTable.Cell().AlignRight().Padding(8).Text("Giảm giá:").Bold();
                                summaryTable.Cell().AlignRight().Padding(8).Text($"{DiscountAmount:N0} VNĐ").FontColor(Colors.Green.Medium);
                            }

                            // Hàng thuế VAT (chỉ hiển thị nếu có thuế)
                            if (TaxAmount > 0)
                            {
                                summaryTable.Cell().AlignRight().Padding(8).Text($"Thuế VAT ({Invoice.Tax}%):").Bold();
                                summaryTable.Cell().AlignRight().Padding(8).Text($"{TaxAmount:N0} VNĐ");
                            }

                            // Đường kẻ phân cách trước tổng cộng
                            summaryTable.Cell().ColumnSpan(2).BorderBottom(2) // Kéo dài qua 2 cột, đường kẻ dày 2px
                                .BorderColor(Colors.Grey.Medium)
                                .PaddingVertical(8); // Khoảng cách trên dưới

                            // Hàng tổng cộng cuối cùng (nổi bật với font lớn và màu đỏ)
                            summaryTable.Cell().AlignRight().Padding(8).Text("TỔNG CỘNG:").Bold().FontSize(12);
                            summaryTable.Cell().AlignRight().Padding(8).Text($"{TotalAmount:N0} VNĐ")
                                .Bold().FontSize(12).FontColor(Colors.Red.Medium);
                        });

                        // === THÔNG TIN THANH TOÁN (CHỈ HIỂN THỊ NẾU ĐÃ THANH TOÁN) ===
                        if (IsPaid && PaymentDate.HasValue)
                        {
                            column.Item().PaddingTop(10)
                                .AlignCenter() // Căn giữa
                                .Text($"Thanh toán bằng {PaymentMethod} ngày {PaymentDate:dd/MM/yyyy HH:mm}")
                                .FontColor(Colors.Green.Medium); // Màu xanh cho thông tin đã thanh toán
                        }

                        // === LỜI DẶN CỦA BÁC SĨ (CHỈ HIỂN THỊ NẾU CÓ HỒ SƠ BỆNH ÁN) ===
                        if (HasMedicalRecord && MedicalRecord != null && !string.IsNullOrWhiteSpace(MedicalRecord.DoctorAdvice))
                        {
                            column.Item().PaddingTop(15)
                                .Border(1).BorderColor(Colors.Grey.Lighten3) // Viền xung quanh
                                .Padding(10) // Khoảng cách trong
                                .Column(notesCol =>
                                {
                                    // Tiêu đề phần lời dặn
                                    notesCol.Item().Text("LỜI DẶN CỦA BÁC SĨ").Bold().FontSize(11);
                                    // Nội dung lời dặn
                                    notesCol.Item().PaddingTop(5).Text(MedicalRecord.DoctorAdvice).FontSize(10);
                                    // Tên bác sĩ (căn phải)
                                    notesCol.Item().PaddingTop(5).AlignRight()
                                        .Text($"Bác sĩ: {MedicalRecord.Doctor?.FullName ?? "Không xác định"}")
                                        .FontSize(10).Bold();
                                });
                        }

                        // === FOOTER CỦA TÀI LIỆU ===
                        column.Item().PaddingTop(20)
                            .BorderTop(1).BorderColor(Colors.Grey.Lighten2) // Đường kẻ trên
                            .PaddingTop(10)
                            .Row(footerRow =>
                            {
                                // Bên trái - thông điệp cảm ơn và ghi chú
                                footerRow.RelativeItem().Column(footerCol =>
                                {
                                    // Thông điệp cảm ơn
                                    footerCol.Item().Text("Xin cám ơn quý khách đã sử dụng dịch vụ của phòng khám chúng tôi!")
                                        .FontSize(9).Italic(); // Font nhỏ và in nghiêng

                                    // Hiển thị ghi chú nếu có (loại bỏ thông tin thanh toán khỏi ghi chú)
                                    if (!string.IsNullOrEmpty(Invoice.Notes))
                                    {
                                        string displayNotes = Invoice.Notes;
                                        // Loại bỏ thông tin thanh toán khỏi ghi chú hiển thị
                                        if (displayNotes.Contains("Phương thức thanh toán:"))
                                        {
                                            int index = displayNotes.IndexOf("Phương thức thanh toán:");
                                            displayNotes = displayNotes.Substring(0, index).Trim();
                                        }

                                        // Chỉ hiển thị ghi chú nếu còn nội dung sau khi loại bỏ thông tin thanh toán
                                        if (!string.IsNullOrWhiteSpace(displayNotes))
                                        {
                                            footerCol.Item().Text($"Ghi chú: {displayNotes}")
                                                .FontSize(8).FontColor(Colors.Grey.Medium);
                                        }
                                    }
                                });

                                // Bên phải - thông tin phân trang
                                footerRow.RelativeItem().AlignRight().Text(text =>
                                {
                                    text.Span("Trang ").FontSize(9);      // Chữ "Trang"
                                    text.CurrentPageNumber().FontSize(9); // Số trang hiện tại
                                    text.Span(" / ").FontSize(9);         // Dấu phân cách
                                    text.TotalPages().FontSize(9);        // Tổng số trang
                                });
                            });
                    });
                });
            })
            .GeneratePdf(filePath); // Tạo file PDF tại đường dẫn đã chỉ định
        }
        #endregion

    }
}
