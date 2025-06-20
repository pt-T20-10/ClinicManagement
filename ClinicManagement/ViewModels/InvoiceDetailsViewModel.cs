using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClinicManagement.Models;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace ClinicManagement.ViewModels
{
    public class InvoiceDetailsViewModel : BaseViewModel
    {
        #region Properties

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
                OnPropertyChanged(nameof(TotalAmount));
            }
        }

        #endregion

        #region Commands

        public ICommand PrintInvoiceCommand { get; set; }
        public ICommand ProcessPaymentCommand { get; set; }
        public ICommand CloseWindow { get; set; }
        public ICommand GenerateQRCodeCommand { get; set; }
        public ICommand ConfirmPaymentCommand { get; set; }
        public ICommand ApplyPatientDiscountCommand { get; set; }
        public ICommand RecalculateTotalsCommand { get; set; }
        public ICommand EditSaleCommand { get; set; }

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
            PrintInvoiceCommand = new RelayCommand<object>(
                p => PrintInvoice(),
                p => Invoice != null);

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

            GenerateQRCodeCommand = new RelayCommand<object>(
                p => GenerateQRCode(),
                p => Invoice != null && IsBankTransfer);

            ConfirmPaymentCommand = new RelayCommand<Window>(
                p => ConfirmPayment(p),
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
                MessageBox.Show($"Lỗi khi tải chi tiết hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("Không thể khởi tạo màn hình bán thuốc.",
                                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("Không tìm thấy tab Bán thuốc.",
                              "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi chuyển sang màn hình bán thuốc: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show($"Lỗi khi tải hồ sơ bệnh án: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void PrintInvoice()
        {
            if (Invoice == null) return;

            MessageBox.Show($"Đang in hóa đơn #{Invoice.InvoiceId}", "In hóa đơn",
                MessageBoxButton.OK, MessageBoxImage.Information);

            // TODO: Implement actual printing functionality
        }

        private void ProcessPayment()
        {
            if (Invoice == null || Invoice.Status == "Đã thanh toán") return;

            var paymentWindow = new Window
            {
                Title = "Thanh toán hóa đơn",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                DataContext = this,
                Content = CreatePaymentControl()
            };

            paymentWindow.ShowDialog();
        }

        private UIElement CreatePaymentControl()
        {
            // TODO: Create a proper Payment Control
            // This is a simplified version

            var panel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            // Payment options
            var paymentOptionTitle = new TextBlock
            {
                Text = "PHƯƠNG THỨC THANH TOÁN",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(paymentOptionTitle);

            // Cash option
            var cashOption = new RadioButton
            {
                Content = "Tiền mặt",
                IsChecked = true,
                Margin = new Thickness(0, 5, 0, 5),
                GroupName = "PaymentMethod"
            };
            cashOption.SetBinding(RadioButton.IsCheckedProperty, new System.Windows.Data.Binding("IsCashPayment")
            {
                Mode = System.Windows.Data.BindingMode.TwoWay
            });
            panel.Children.Add(cashOption);

            // Bank transfer option
            var bankOption = new RadioButton
            {
                Content = "Chuyển khoản ngân hàng",
                Margin = new Thickness(0, 5, 0, 15),
                GroupName = "PaymentMethod"
            };
            bankOption.SetBinding(RadioButton.IsCheckedProperty, new System.Windows.Data.Binding("IsBankTransfer")
            {
                Mode = System.Windows.Data.BindingMode.TwoWay
            });
            panel.Children.Add(bankOption);

            // QR Code area (visible only for bank transfers)
            var qrCodePanel = new StackPanel();
            qrCodePanel.SetBinding(UIElement.VisibilityProperty, new System.Windows.Data.Binding("IsBankTransfer")
            {
                Converter = new Converter.BooleanToVisibilityConverter()
            });

            var qrCodeTitle = new TextBlock
            {
                Text = "Quét mã QR để thanh toán:",
                Margin = new Thickness(0, 0, 0, 10)
            };
            qrCodePanel.Children.Add(qrCodeTitle);

            var qrCodeImage = new System.Windows.Controls.Image
            {
                Width = 200,
                Height = 200,
                Stretch = System.Windows.Media.Stretch.Uniform
            };
            qrCodeImage.SetBinding(System.Windows.Controls.Image.SourceProperty, "QrCodeImage");
            qrCodePanel.Children.Add(qrCodeImage);

            var generateQrButton = new System.Windows.Controls.Button
            {
                Content = "Tạo mã QR",
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            generateQrButton.SetBinding(System.Windows.Controls.Button.CommandProperty, "GenerateQRCodeCommand");
            qrCodePanel.Children.Add(generateQrButton);

            panel.Children.Add(qrCodePanel);

            // Amount information
            var amountInfoPanel = new StackPanel
            {
                Margin = new Thickness(0, 20, 0, 20)
            };

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

            // Buttons panel
            var buttonsPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Hủy",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(15, 5, 15, 5)
            };
            cancelButton.SetBinding(System.Windows.Controls.Button.CommandProperty, "CloseWindow");
            cancelButton.CommandParameter = cancelButton.TemplatedParent;
            buttonsPanel.Children.Add(cancelButton);

            var confirmButton = new System.Windows.Controls.Button
            {
                Content = "Xác nhận thanh toán",
                Padding = new Thickness(15, 5, 15, 5),
                Background = System.Windows.Media.Brushes.Green,
                Foreground = System.Windows.Media.Brushes.White
            };
            confirmButton.SetBinding(System.Windows.Controls.Button.CommandProperty, "ConfirmPaymentCommand");
            confirmButton.CommandParameter = confirmButton.TemplatedParent;
            buttonsPanel.Children.Add(confirmButton);

            panel.Children.Add(buttonsPanel);

            return panel;
        }

        private void GenerateQRCode()
        {
            // TODO: Generate real QR code for payment
            // This is just a placeholder
            MessageBox.Show("Tính năng tạo mã QR sẽ được xử lý sau.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ConfirmPayment(Window paymentWindow)
        {
            if (Invoice == null) return;

            try
            {
                // Ensure discount and tax have values and recalculate totals one final time
                if (Invoice.Discount == null) Invoice.Discount = 0;
                if (Invoice.Tax == null) Invoice.Tax = 10;
                RecalculateTotals();

                // Update invoice status and total amount
                Invoice.Status = "Đã thanh toán";
                Invoice.TotalAmount = TotalAmount; // Set the calculated total amount

                // Store payment information in the Notes field
                string paymentMethod = IsCashPayment ? "Tiền mặt" : "Chuyển khoản";
                string paymentDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Preserve existing notes if any
                string existingNotes = string.IsNullOrEmpty(Invoice.Notes) ? "" : Invoice.Notes + " | ";

                // Add payment information to notes
                Invoice.Notes = existingNotes + $"Phương thức thanh toán:{paymentMethod};Ngày thanh toán:{paymentDate}";

                // Update database
                DataProvider.Instance.Context.SaveChanges();

                // Update view model properties
                IsPaid = true;
                IsNotPaid = false;
                PaymentMethod = paymentMethod;
                PaymentDate = DateTime.Now;

                MessageBox.Show($"Thanh toán hóa đơn #{Invoice.InvoiceId} thành công!", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Close payment dialog
                paymentWindow?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xử lý thanh toán: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool CanEditMedicineSale => Invoice != null &&
                                    Invoice.Status == "Chưa thanh toán" &&
                                    (Invoice.InvoiceType == "Bán thuốc" || Invoice.InvoiceType == "Khám và bán thuốc");
    }
}
