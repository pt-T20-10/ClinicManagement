using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace ClinicManagement.ViewModels
{
    public class InvoiceDetailsViewModel : BaseViewModel
    {
        #region Properties
        private Window _paymentWindow;
        // The main invoice being displayed
        private Invoice _invoice;
        public Invoice Invoice
        {
            get => _invoice;
            set
            {
                _invoice = value;
                OnPropertyChanged();
                LoadInvoiceDetails();
                CheckMedicalRecord();
                CheckPatient();
            }
        }

        // Collection of invoice line items
        private ObservableCollection<InvoiceDetail> _invoiceDetails;
        public ObservableCollection<InvoiceDetail> InvoiceDetails
        {
            get => _invoiceDetails ??= new ObservableCollection<InvoiceDetail>();
            set
            {
                _invoiceDetails = value;
                OnPropertyChanged();
                CalculateInvoiceTotals();
            }
        }

        // Related medical record (if this is a medical examination invoice)
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

        // Flags for conditional UI display
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

        private bool _isPaid;
        public bool IsPaid
        {
            get => _isPaid;
            set
            {
                _isPaid = value;
                OnPropertyChanged();
                // Also update IsNotPaid to ensure they are always opposite
                IsNotPaid = !value;
            }
        }

        // Payment related properties
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

        // For payment dialog
        private bool _isCashPayment = true;
        public bool IsCashPayment
        {
            get => _isCashPayment;
            set
            {
                _isCashPayment = value;
                OnPropertyChanged();
                // If toggling payment type, need to set BankTransfer to opposite
                if (_isBankTransfer == _isCashPayment)
                {
                    _isBankTransfer = !value;
                    OnPropertyChanged(nameof(IsBankTransfer));
                }
            }
        }

        private bool _isBankTransfer;
        public bool IsBankTransfer
        {
            get => _isBankTransfer;
            set
            {
                _isBankTransfer = value;
                OnPropertyChanged();
                // If toggling payment type, need to set CashPayment to opposite
                if (_isCashPayment == _isBankTransfer)
                {
                    _isCashPayment = !value;
                    OnPropertyChanged(nameof(IsCashPayment));
                }
            }
        }

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

        // Financial calculations
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

        public decimal TotalAmount
        {
            get
            {
                // Calculate the total from components:
                // Subtotal - Discount + Tax
                return SubTotal - DiscountAmount + TaxAmount;
            }
        }

        // Properties needed for the updated XAML
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

        private decimal _discountAmount;
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                _discountAmount = value;
                OnPropertyChanged();
                // Update TotalAmount when DiscountAmount changes
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand ProcessPaymentCommand { get; set; }
        public ICommand CloseWindow { get; set; }
        public ICommand GenerateQRCodeCommand { get; set; }
        public ICommand ConfirmPaymentCommand { get; set; }
        public ICommand ApplyPatientDiscountCommand { get; set; }
        public ICommand RecalculateTotalsCommand { get; set; }
        public ICommand EditSaleCommand { get; set; }
        public ICommand ExportInvoiceCommand { get; set; } 

        #endregion

        // Constructor
        public InvoiceDetailsViewModel(Invoice invoice = null)
        {
            InitializeCommands();
           
            if (invoice != null)
            {
                Invoice = invoice;
            }
        }
        

        private void InitializeCommands()
        {
           

            ProcessPaymentCommand = new RelayCommand<object>(
                p => ProcessPayment(),
                p => Invoice != null && Invoice.Status == "Chưa thanh toán");

            CloseWindow = new RelayCommand<object>(
                p =>
                {
                    // Try to find the window through different methods
                    Window window = null;

                    // Method 1: If parameter is a Button or other UIElement
                    if (p is UIElement element)
                    {
                        window = Window.GetWindow(element);
                    }
                    // Method 2: If parameter is already a Window
                    else if (p is Window w)
                    {
                        window = w;
                    }
                    // Method 3: Find active window through Application
                    else if (Application.Current.Windows.Count > 0)
                    {
                        // Try to get the window with focus first, otherwise get the main window
                        window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                            ?? Application.Current.MainWindow;
                    }

                    // Close the window if found
                    window?.Close();
                    
                },
                p => true
            );
            ExportInvoiceCommand = new RelayCommand<object>(
              p => ExportInvoiceToPdf(),
              p => Invoice != null
          );

        
            ConfirmPaymentCommand = new RelayCommand<Window>(
                p => ConfirmPayment(null),
                p => Invoice != null);

            ApplyPatientDiscountCommand = new RelayCommand<object>(
                p => ApplyPatientDiscount(),
                p => Invoice != null && Invoice.Patient?.PatientType != null && PatientTypeDiscount.HasValue);

            RecalculateTotalsCommand = new RelayCommand<object>(
                p => RecalculateTotals(),
                p => Invoice != null);

            EditSaleCommand = new RelayCommand<Window>(
           parameter => EditSale(parameter),
           parameter => CanEditMedicineSale
       );
        }

        private void LoadInvoiceDetails()
        {
            if (Invoice == null) return;

            try
            {
                // Load related invoice details
                var invoiceDetails = DataProvider.Instance.Context.InvoiceDetails
                    .Where(d => d.InvoiceId == Invoice.InvoiceId)
                    .ToList();

                InvoiceDetails = new ObservableCollection<InvoiceDetail>(invoiceDetails);

                // Check if invoice is already paid
                IsPaid = Invoice.Status == "Đã thanh toán";
                IsNotPaid = !IsPaid;

                // Ensure discount and tax values are initialized
                if (Invoice.Discount == null) Invoice.Discount = 0;
                if (Invoice.Tax == null) Invoice.Tax = 10; // Default 10%

                // Calculate totals initially
                CalculateInvoiceTotals();
                if (IsPaid)
                {
                    // Store payment information in the Notes field of Invoice
                    // Format example: "PAYMENT_METHOD:Tiền mặt;PAYMENT_DATE:2023-06-06 14:30:00"
                    if (!string.IsNullOrEmpty(Invoice.Notes) && Invoice.Notes.Contains("Phương thức thanh toán:"))
                    {
                        try
                        {
                            string notes = Invoice.Notes;

                            // Extract payment method
                            int methodStartIndex = notes.IndexOf("Phương thức thanh toán:") + "Phương thức thanh toán:".Length;
                            int methodEndIndex = notes.IndexOf(';', methodStartIndex);
                            if (methodEndIndex == -1) methodEndIndex = notes.Length;

                            PaymentMethod = notes.Substring(methodStartIndex, methodEndIndex - methodStartIndex);

                            // Extract payment date if available
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
                            // If parsing fails, just use default values
                            PaymentMethod = "Không xác định";
                            PaymentDate = Invoice.InvoiceDate;
                        }
                    }
                    else
                    {
                        // Default values if no payment information is stored
                        PaymentMethod = "Không xác định";
                        PaymentDate = Invoice.InvoiceDate;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải chi tiết hóa đơn: {ex.Message}", "Lỗi"   );
            }
        }
        // Method to handle editing the sale
        private void EditSale(Window window)
        {
            try
            {
                // Access the MedicineSellViewModel from App resources
                var medicineSellVM = Application.Current.Resources["MedicineSellVM"] as MedicineSellViewModel;

                if (medicineSellVM == null)
                {
                    MessageBoxService.ShowError("Không thể khởi tạo màn hình bán thuốc.",
                                    "Lỗi"    );
                    return;
                }

                // Set the current invoice for editing
                medicineSellVM.CurrentInvoice = Invoice;

                // Find and activate the Medicine Sale tab
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null) return;

                var tabControl = LogicalTreeHelper.FindLogicalNode(mainWindow, "MainTabControl") as TabControl;
                if (tabControl == null) return;

                // Find the "Bán thuốc" tab
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
                                            // Switch to medicine sales tab
                                            tabControl.SelectedItem = tabItem;

                                            // Close the current window
                                            window?.Close();
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // If we couldn't find the tab
                MessageBoxService.ShowWarning("Không tìm thấy tab Bán thuốc.",
                              "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi chuyển sang màn hình bán thuốc: {ex.Message}",
                               "Lỗi"   );
            }
        }

        private void CheckPatient()
        {
            if (Invoice == null) return;

            HasPatient = Invoice.Patient != null;

            // Lấy thông tin giảm giá từ loại khách hàng nếu có
            if (HasPatient && Invoice.Patient?.PatientType != null)
            {
                PatientTypeDiscount = Invoice.Patient.PatientType.Discount;
            }
            else
            {
                PatientTypeDiscount = null;
            }
        }

        private void ApplyPatientDiscount()
        {
            if (Invoice != null && PatientTypeDiscount.HasValue)
            {
                Invoice.Discount = PatientTypeDiscount;
                RecalculateTotals();
            }
        }

        private void CheckMedicalRecord()
        {
            if (Invoice == null) return;

            // Only medical examination invoices have medical records
            if (Invoice.InvoiceType == "Khám bệnh" || Invoice.InvoiceType == "Khám và bán thuốc")
            {
                try
                {
                    // Check if the invoice has a medical record ID
                    if (Invoice.MedicalRecordId.HasValue)
                    {
                        // Find the medical record using MedicalRecordId from Invoice
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
                    MessageBoxService.ShowError($"Lỗi khi tải hồ sơ bệnh án: {ex.Message}", "Lỗi"   );
                    HasMedicalRecord = false;
                }
            }
            else
            {
                HasMedicalRecord = false;
            }
        }

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

            // Calculate subtotal (before discounts)
            SubTotal = InvoiceDetails.Sum(d => d.SalePrice * (d.Quantity ?? 1)) ?? 0;

            // Calculate discount amount from invoice level discount
            decimal discountPercent = Invoice?.Discount ?? 0;
            DiscountAmount = SubTotal * (discountPercent / 100m);

            // Calculate tax amount from invoice level tax
            decimal taxPercent = Invoice?.Tax ?? 10; // Default to 10% if not set
            decimal amountAfterDiscount = SubTotal - DiscountAmount;
            TaxAmount = amountAfterDiscount * (taxPercent / 100m);

            OnPropertyChanged(nameof(TotalAmount));

            // If we are recalculating, update the invoice totalAmount
            if (!IsPaid && Invoice != null)
            {
                Invoice.TotalAmount = TotalAmount;
            }
        }

        private void RecalculateTotals()
        {
            CalculateInvoiceTotals();
        }

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
                Content = CreatePaymentControl()
            };

            _paymentWindow.ShowDialog();
        }

        private UIElement CreatePaymentControl()
        {
            // Tạo một control thanh toán đơn giản để hiển thị trong cửa sổ thanh toán
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

            // --- QR CODE SECTION ---
            // Try to load a QR code image from the project resources
            bool qrImageLoaded = false;
            try
            {
                // Get the executable's directory
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


        // Add this method to update stock after payment
        // Add this to InvoiceDetailsViewModel.cs
        private void UpdateStockAfterPayment()
        {
            try
            {
                
                var context = DataProvider.Instance.Context;

                // Get all invoice details with medicines for this invoice
                var invoiceDetails = context.InvoiceDetails
                    .Where(id => id.InvoiceId == Invoice.InvoiceId && id.MedicineId.HasValue)
                    .Include(id => id.Medicine)
                    .ToList();

                

                // Update stock for each medicine
                foreach (var detail in invoiceDetails)
                {
                   

                    // Detach any loaded entities to avoid EF tracking conflicts
                    if (detail.Medicine != null)
                        context.Entry(detail.Medicine).State = EntityState.Detached;

                    // Get fresh medicine data with all relationships
                    var medicine = context.Medicines
                        .Include(m => m.StockIns)
                        .Include(m => m.InvoiceDetails)
                        .ThenInclude(id => id.Invoice) // Include Invoice to check status
                        .FirstOrDefault(m => m.MedicineId == detail.MedicineId);

                    if (medicine == null)
                    {
                  
                        continue;
                    }

                    // Clear cache to ensure fresh calculations
                    medicine._availableStockInsCache = null;

                    // Calculate correct physical stock quantity
                    int totalStockIn = medicine.StockIns?.Sum(si => si.Quantity) ?? 0;
                    int totalSold = medicine.InvoiceDetails
                        .Where(id => id.Invoice.Status == "Đã thanh toán")
                        .Sum(id => id.Quantity ?? 0);
                    int actualQuantity = Math.Max(0, totalStockIn - totalSold);

                    // Calculate usable quantity with proper expiry date filtering
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


                    // Update or create Stock record
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

                // Save all changes at once
                context.SaveChanges();
          
            }
            catch (Exception ex)
            {
                
                MessageBoxService.ShowError($"Lỗi khi cập nhật tồn kho: {ex.Message}", "Lỗi");
            }
        }

        public bool CanEditMedicineSale => Invoice != null &&
                                    Invoice.Status == "Chưa thanh toán" &&
                                    (Invoice.InvoiceType == "Bán thuốc" || Invoice.InvoiceType == "Khám và bán thuốc");
       #region PDF
        private void ExportInvoiceToPdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            try
            {
                // Create save file dialog to let the user choose where to save the PDF
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

                    // Create and show progress dialog
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Start export operation in background thread
                    Task.Run(() =>
                    {
                        try
                        {
                            // Report progress: 10% - Starting
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                            // Report progress: 30% - Preparing document
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(30));
                            Thread.Sleep(100); // Small delay for visibility

                            // Report progress: 60% - Generating content
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(60));

                            // Generate PDF using QuestPDF - với phương thức đơn giản hóa
                            GenerateSimplePdfDocument(filePath);

                            // Report progress: 90% - Saving file
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));
                            Thread.Sleep(100); // Small delay for visibility

                            // Report progress: 100% - Complete
                            Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                            Thread.Sleep(300); // Show 100% briefly

                            // Close progress dialog and show success message
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();

                                MessageBoxService.ShowSuccess(
                                    $"Đã xuất hóa đơn #{Invoice.InvoiceId} thành công!\nĐường dẫn: {filePath}",
                                    "Xuất hóa đơn"
                                );

                                // Open the PDF file with the default PDF viewer
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

                    // Show dialog - this will block until the dialog is closed
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xuất hóa đơn: {ex.Message}", "Lỗi xuất hóa đơn");
            }
        }

        // Phương thức tạo PDF đơn giản, không chia nhỏ
        private void GenerateSimplePdfDocument(string filePath)
        {
            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Main content container - use Column to allow adding multiple items
                    page.Content().Column(column =>
                    {
                        // HEADER SECTION with Row layout
                        column.Item().Row(row =>
                        {
                            // Left side - clinic info
                            row.RelativeItem(2).Column(col =>
                            {
                                col.Item().Text("PHÒNG KHÁM ABC")
                                    .FontSize(16).Bold();
                                col.Item().Text("Địa chỉ: 123 Đường 456, Quận 789, TP.XYZ")
                                    .FontSize(10);
                                col.Item().Text("SĐT: 028.1234.5678 | Email: email@gmail.com")
                                    .FontSize(10);
                            });
                       
                            // Right side - invoice info
                            row.RelativeItem(1).Column(col =>
                            {
                                col.Item().AlignRight().Text($"HÓA ĐƠN #{Invoice.InvoiceId}")
                                    .FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                                col.Item().AlignRight().Text($"Ngày: {Invoice.InvoiceDate:dd/MM/yyyy HH:mm}")
                                    .FontSize(10);
                                col.Item().AlignRight().Text($"Loại hóa đơn: {Invoice.InvoiceType}")
                                     .FontSize(10);
                                col.Item().AlignRight().Text($"Trạng thái: {Invoice.Status}")
                                    .FontSize(10)
                                    .FontColor(Invoice.Status == "Đã thanh toán" ? Colors.Green.Medium : Colors.Orange.Medium);

                            });
                        });

                        // Separator line
                        column.Item().PaddingVertical(10)
                            .BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                        // Patient information with 2 columns
                        if (HasPatient)
                        {
                            column.Item().PaddingTop(15).Border(1)
                                .BorderColor(Colors.Grey.Lighten3)
                                .Background(Colors.Grey.Lighten5)
                                .Padding(10)
                                .Column(patientCol =>
                                {
                                    patientCol.Item().Text("THÔNG TIN KHÁCH HÀNG").Bold();

                                    patientCol.Item().PaddingTop(5).Row(patientInfoRow =>
                                    {
                                        // Left column of patient info
                                        patientInfoRow.RelativeItem().Column(leftCol =>
                                        {
                                            leftCol.Item().Text($"Họ tên: {Invoice.Patient?.FullName}");
                                            leftCol.Item().Text($"Số điện thoại: {Invoice.Patient?.Phone}");
                                        });

                                        // Right column of patient info
                                        patientInfoRow.RelativeItem().Column(rightCol =>
                                        {
                                            if (!string.IsNullOrEmpty(Invoice.Patient?.InsuranceCode))
                                                rightCol.Item().Text($"Mã BHYT: {Invoice.Patient?.InsuranceCode}");

                                            if (Invoice.Patient?.PatientType != null)
                                                rightCol.Item().Text($"Loại khách hàng: {Invoice.Patient?.PatientType?.TypeName}");
                                        });
                                    });
                                });
                        }

                        // Invoice details section
                        column.Item().PaddingTop(20).Column(detailsCol =>
                        {
                            detailsCol.Item().Text("CHI TIẾT HÓA ĐƠN").Bold().FontSize(12);

                            detailsCol.Item().PaddingTop(5).Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3); // Item name
                                    columns.RelativeColumn(1); // Quantity
                                    columns.RelativeColumn(2); // Unit price
                                    columns.RelativeColumn(2); // Total price
                                });

                                // Add header row
                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tên thuốc/dịch vụ").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Số lượng").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Đơn giá").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Thành tiền").Bold();
                                });

                                // Add data rows
                                foreach (var detail in InvoiceDetails)
                                {
                                    string itemName = !string.IsNullOrEmpty(detail.ServiceName)
                                        ? detail.ServiceName
                                        : detail.Medicine?.Name ?? "Không xác định";

                                    int quantity = detail.Quantity ?? 1;
                                    decimal unitPrice = detail.SalePrice ?? 0;
                                    decimal totalPrice = unitPrice * quantity;

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                        .Padding(5).Text(itemName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                        .Padding(5).AlignCenter().Text(quantity.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                        .Padding(5).AlignRight().Text($"{unitPrice:N0} VNĐ");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                        .Padding(5).AlignRight().Text($"{totalPrice:N0} VNĐ");
                                }
                            });
                        });

                        // Payment Summary
                        column.Item().PaddingTop(20).AlignRight().Table(summaryTable =>
                        {
                            // Define table columns
                            summaryTable.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1); // Label column
                                columns.RelativeColumn(1); // Value column
                            });

                            // Subtotal row
                            summaryTable.Cell().AlignRight().Padding(8).Text("Tạm tính:").Bold();
                            summaryTable.Cell().AlignRight().Padding(8).Text($"{SubTotal:N0} VNĐ");

                            // Discount row (if applicable)
                            if (DiscountAmount > 0)
                            {
                                summaryTable.Cell().AlignRight().Padding(8).Text("Giảm giá:").Bold();
                                summaryTable.Cell().AlignRight().Padding(8).Text($"{DiscountAmount:N0} VNĐ").FontColor(Colors.Green.Medium);
                            }

                            // Tax row (if applicable)
                            if (TaxAmount > 0)
                            {
                                summaryTable.Cell().AlignRight().Padding(8).Text($"Thuế VAT ({Invoice.Tax}%):").Bold();
                                summaryTable.Cell().AlignRight().Padding(8).Text($"{TaxAmount:N0} VNĐ");
                            }

                            // Separator
                            summaryTable.Cell().ColumnSpan(2).BorderBottom(2)
                                .BorderColor(Colors.Grey.Medium)
                                .PaddingVertical(8);

                            // Total amount row
                            summaryTable.Cell().AlignRight().Padding(8).Text("TỔNG CỘNG:").Bold().FontSize(12);
                            summaryTable.Cell().AlignRight().Padding(8).Text($"{TotalAmount:N0} VNĐ")
                                .Bold().FontSize(12).FontColor(Colors.Red.Medium);
                        });

                        // Payment Information
                        if (IsPaid && PaymentDate.HasValue)
                        {
                            column.Item().PaddingTop(10)
                                .AlignCenter()
                                .Text($"Thanh toán bằng {PaymentMethod} ngày {PaymentDate:dd/MM/yyyy HH:mm}")
                                .FontColor(Colors.Green.Medium);
                        }

                        // Doctor's notes (if applicable)
                        if (HasMedicalRecord && MedicalRecord != null && !string.IsNullOrWhiteSpace(MedicalRecord.DoctorAdvice))
                        {
                            column.Item().PaddingTop(15)
                                .Border(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(10)
                                .Column(notesCol =>
                                {
                                    notesCol.Item().Text("LỜI DẶN CỦA BÁC SĨ").Bold().FontSize(11);
                                    notesCol.Item().PaddingTop(5).Text(MedicalRecord.DoctorAdvice).FontSize(10);
                                    notesCol.Item().PaddingTop(5).AlignRight()
                                        .Text($"Bác sĩ: {MedicalRecord.Doctor?.FullName ?? "Không xác định"}")
                                        .FontSize(10).Bold();
                                });
                        }

                        // Footer
                        column.Item().PaddingTop(20)
                            .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                            .PaddingTop(10)
                            .Row(footerRow =>
                            {
                                footerRow.RelativeItem().Column(footerCol =>
                                {
                                    footerCol.Item().Text("Xin cám ơn quý khách đã sử dụng dịch vụ của phòng khám chúng tôi!")
                                        .FontSize(9).Italic();

                                    // Display notes if available
                                    if (!string.IsNullOrEmpty(Invoice.Notes))
                                    {
                                        string displayNotes = Invoice.Notes;
                                        // Remove payment info from displayed notes
                                        if (displayNotes.Contains("Phương thức thanh toán:"))
                                        {
                                            int index = displayNotes.IndexOf("Phương thức thanh toán:");
                                            displayNotes = displayNotes.Substring(0, index).Trim();
                                        }

                                        if (!string.IsNullOrWhiteSpace(displayNotes))
                                        {
                                            footerCol.Item().Text($"Ghi chú: {displayNotes}")
                                                .FontSize(8).FontColor(Colors.Grey.Medium);
                                        }
                                    }
                                });

                                footerRow.RelativeItem().AlignRight().Text(text =>
                                {
                                    text.Span("Trang ").FontSize(9);
                                    text.CurrentPageNumber().FontSize(9);
                                    text.Span(" / ").FontSize(9);
                                    text.TotalPages().FontSize(9);
                                });
                            });
                    });
                });
            })
            .GeneratePdf(filePath);
        }
        #endregion

    }
}
