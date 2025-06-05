using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace ClinicManagement.ViewModels
{
    public class InvoiceViewModel : BaseViewModel
    {
        #region Properties
        private UserControl _userControl;

        // Collection for Invoices with pagination support
        private ObservableCollection<Invoice> _invoices;
        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices ??= new ObservableCollection<Invoice>();
            set
            {
                _invoices = value;
                OnPropertyChanged();
            }
        }

        // Selected Invoice
        private Invoice _selectedInvoice;
        public Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                _selectedInvoice = value;
                OnPropertyChanged();
            }
        }

        // Filter properties
        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
                // Tự động lọc khi ngày bắt đầu thay đổi
                FilterInvoicesAuto();
            }
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
                // Tự động lọc khi ngày kết thúc thay đổi
                FilterInvoicesAuto();
            }
        }

        // Search text
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                // Không tự động lọc khi nhập text, chỉ khi nhấn nút Search
            }
        }

        // Status items for filter
        private ObservableCollection<StatusItem> _invoiceStatusList;
        public ObservableCollection<StatusItem> InvoiceStatusList
        {
            get => _invoiceStatusList ??= InitializeInvoiceStatusList();
            set
            {
                _invoiceStatusList = value;
                OnPropertyChanged();
            }
        }

        private StatusItem _selectedInvoiceStatus;
        public StatusItem SelectedInvoiceStatus
        {
            get => _selectedInvoiceStatus;
            set
            {
                _selectedInvoiceStatus = value;
                OnPropertyChanged();
                // Tự động lọc khi trạng thái thay đổi
                FilterInvoicesAuto();
            }
        }

        // Invoice Type items for filter
        private ObservableCollection<StatusItem> _invoiceTypeList;
        public ObservableCollection<StatusItem> InvoiceTypeList
        {
            get => _invoiceTypeList ??= InitializeInvoiceTypeList();
            set
            {
                _invoiceTypeList = value;
                OnPropertyChanged();
            }
        }

        private StatusItem _selectedInvoiceType;
        public StatusItem SelectedInvoiceType
        {
            get => _selectedInvoiceType;
            set
            {
                _selectedInvoiceType = value;
                OnPropertyChanged();
                // Tự động lọc khi loại hóa đơn thay đổi
                FilterInvoicesAuto();
            }
        }

        // Pagination properties
        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageInfoText));
                    LoadInvoices();
                }
            }
        }

        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            set
            {
                _totalPages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfoText));
                UpdatePageNumbers();
            }
        }

        private int _totalItems;
        public int TotalItems
        {
            get => _totalItems;
            set
            {
                _totalItems = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfoText));
            }
        }

        private ObservableCollection<int> _pageNumbers;
        public ObservableCollection<int> PageNumbers
        {
            get => _pageNumbers ??= new ObservableCollection<int>();
            set
            {
                _pageNumbers = value;
                OnPropertyChanged();
            }
        }

        public List<int> ItemsPerPageOptions { get; } = new List<int> { 10, 20, 50, 100 };

        private int _selectedItemsPerPage = 20;
        public int SelectedItemsPerPage
        {
            get => _selectedItemsPerPage;
            set
            {
                if (_selectedItemsPerPage != value)
                {
                    _selectedItemsPerPage = value;
                    OnPropertyChanged();
                    CurrentPage = 1;  // Reset to first page when changing items per page
                    LoadInvoices();
                }
            }
        }

        public string PageInfoText => $"Trang {CurrentPage}/{TotalPages} (Tổng {TotalItems} hóa đơn)";

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand LoadedUCCommand { get; set; }
        public ICommand SearchCommand { get; set; }
        public ICommand ResetFiltersCommand { get; set; }
        public ICommand ViewInvoiceDetailsCommand { get; set; }
        public ICommand PrintInvoiceCommand { get; set; }
        public ICommand ProcessPaymentCommand { get; set; }
        public ICommand SellMedicineCommand { get; set; }

        // Pagination commands
        public ICommand FirstPageCommand { get; set; }
        public ICommand PreviousPageCommand { get; set; }
        public ICommand NextPageCommand { get; set; }
        public ICommand LastPageCommand { get; set; }
        public ICommand GoToPageCommand { get; set; }
        #endregion

        public InvoiceViewModel()
        {
            InitializeCommands();
            SelectedInvoiceStatus = InvoiceStatusList.FirstOrDefault();
            SelectedInvoiceType = InvoiceTypeList.FirstOrDefault();
        }

        #region Initialization Methods
        private void InitializeCommands()
        {
            LoadedUCCommand = new RelayCommand<UserControl>(
                p => { _userControl = p; LoadData(); },
                p => true);

            SearchCommand = new RelayCommand<object>(
                p => SearchInvoices(),
                p => true);

            ResetFiltersCommand = new RelayCommand<object>(
                p => ResetFilters(),
                p => true);

            ViewInvoiceDetailsCommand = new RelayCommand<Invoice>(
                p => ViewInvoiceDetails(p),
                p => p != null);

            PrintInvoiceCommand = new RelayCommand<Invoice>(
                p => PrintInvoice(p),
                p => p != null);

            ProcessPaymentCommand = new RelayCommand<Invoice>(
                p => ProcessPayment(p),
                p => p != null && p.Status == "Chưa thanh toán");

            SellMedicineCommand = new RelayCommand<Invoice>(
                p => SellMedicine(p),
                p => p != null && p.InvoiceType == "Khám và bán thuốc");

            // Pagination commands
            FirstPageCommand = new RelayCommand<object>(
                p => CurrentPage = 1,
                p => CurrentPage > 1);

            PreviousPageCommand = new RelayCommand<object>(
                p => CurrentPage--,
                p => CurrentPage > 1);

            NextPageCommand = new RelayCommand<object>(
                p => CurrentPage++,
                p => CurrentPage < TotalPages);

            LastPageCommand = new RelayCommand<object>(
                p => CurrentPage = TotalPages,
                p => CurrentPage < TotalPages);

            GoToPageCommand = new RelayCommand<int>(
                p => CurrentPage = p,
                p => p > 0 && p <= TotalPages && p != CurrentPage);
        }

        private ObservableCollection<StatusItem> InitializeInvoiceStatusList()
        {
            return new ObservableCollection<StatusItem>
            {
                new StatusItem("", "Tất cả trạng thái"),
                new StatusItem("Đã thanh toán", "Đã thanh toán"),
                new StatusItem("Chưa thanh toán", "Chưa thanh toán")
            };
        }

        private ObservableCollection<StatusItem> InitializeInvoiceTypeList()
        {
            return new ObservableCollection<StatusItem>
            {
                new StatusItem("", "Tất cả loại"),
                new StatusItem("Khám bệnh", "Khám bệnh"),
                new StatusItem("Bán thuốc", "Bán thuốc"),
                new StatusItem("Khám và bán thuốc", "Khám và bán thuốc")
            };
        }
        #endregion

        #region Data Loading Methods
        private void LoadData()
        {
            LoadInvoices();
        }

        private void LoadInvoices()
        {
            try
            {
                IsLoading = true;

                // Create base query
                var query = DataProvider.Instance.Context.Invoices
                    .Include(i => i.Patient)
                    .AsQueryable();

                // Apply filters
                query = ApplyFilters(query);

                // Get total count for pagination
                TotalItems = query.Count();
                TotalPages = (int)Math.Ceiling(TotalItems / (double)SelectedItemsPerPage);

                // Apply pagination
                var paginatedInvoices = query
                    .OrderByDescending(i => i.InvoiceDate)
                    .Skip((CurrentPage - 1) * SelectedItemsPerPage)
                    .Take(SelectedItemsPerPage)
                    .ToList();

                // Update the observable collection
                Invoices = new ObservableCollection<Invoice>(paginatedInvoices);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải dữ liệu hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private IQueryable<Invoice> ApplyFilters(IQueryable<Invoice> query)
        {
            // Apply date range filter
            if (StartDate != DateTime.MinValue && EndDate != DateTime.MinValue)
            {
                var endDatePlus = EndDate.AddDays(1);
                query = query.Where(i => i.InvoiceDate >= StartDate && i.InvoiceDate < endDatePlus);
            }

            // Apply status filter if selected
            if (SelectedInvoiceStatus != null && !string.IsNullOrEmpty(SelectedInvoiceStatus.Status))
            {
                query = query.Where(i => i.Status == SelectedInvoiceStatus.Status);
            }

            // Apply type filter if selected
            if (SelectedInvoiceType != null && !string.IsNullOrEmpty(SelectedInvoiceType.Status))
            {
                query = query.Where(i => i.InvoiceType == SelectedInvoiceType.Status);
            }

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower().Trim();
                query = query.Where(i =>
                    (i.Patient.FullName != null && i.Patient.FullName.ToLower().Contains(searchLower)) ||
                    (i.Patient.InsuranceCode != null && i.Patient.InsuranceCode.ToLower().Contains(searchLower)) ||
                    (i.Patient.Phone != null && i.Patient.Phone.ToLower().Contains(searchLower))
                );
            }

            return query;
        }

        private void UpdatePageNumbers()
        {
            PageNumbers.Clear();

            if (TotalPages <= 7)
            {
                // If 7 or fewer pages, show all page numbers
                for (int i = 1; i <= TotalPages; i++)
                {
                    PageNumbers.Add(i);
                }
            }
            else
            {
                // Always show first page
                PageNumbers.Add(1);

                if (CurrentPage > 4)
                {
                    // Add ellipsis if current page is far from start
                    PageNumbers.Add(-1); // Use -1 as ellipsis indicator
                }

                // Calculate range around current page
                int startPage = Math.Max(2, CurrentPage - 2);
                int endPage = Math.Min(TotalPages - 1, CurrentPage + 2);

                // Adjust range for edge cases
                if (CurrentPage <= 4)
                {
                    endPage = 5;
                }
                else if (CurrentPage >= TotalPages - 3)
                {
                    startPage = TotalPages - 4;
                }

                // Add range around current page
                for (int i = startPage; i <= endPage; i++)
                {
                    PageNumbers.Add(i);
                }

                if (CurrentPage < TotalPages - 3)
                {
                    // Add ellipsis if current page is far from end
                    PageNumbers.Add(-1); // Use -1 as ellipsis indicator
                }

                // Always show last page
                PageNumbers.Add(TotalPages);
            }
        }
        #endregion

        #region Action Methods
        private void FilterInvoicesAuto()
        {
            // Đặt lại trang về 1 mỗi khi thay đổi bộ lọc
            CurrentPage = 1;
            LoadInvoices();
        }

        private void SearchInvoices()
        {
            // Phương thức này được gọi khi nhấn nút Search
            // Đặt lại trang về 1 và áp dụng tất cả các bộ lọc bao gồm cả SearchText
            CurrentPage = 1;
            LoadInvoices();
        }

        private void ResetFilters()
        {
            // Tắt cơ chế tự động lọc tạm thời để tránh nhiều lần gọi API
            bool oldAutoFilter = true;

            try
            {
                SearchText = string.Empty;
                StartDate = DateTime.Now.AddMonths(-1);
                EndDate = DateTime.Now;
                SelectedInvoiceStatus = InvoiceStatusList.FirstOrDefault();
                SelectedInvoiceType = InvoiceTypeList.FirstOrDefault();
                CurrentPage = 1;

                // Sau khi đặt lại tất cả các bộ lọc, chỉ gọi LoadInvoices một lần
                LoadInvoices();
            }
            finally
            {
                // Khôi phục cơ chế tự động lọc
            }
        }

        private void ViewInvoiceDetails(Invoice invoice)
        {
            if (invoice == null) return;

            MessageBox.Show($"Xem chi tiết hóa đơn #{invoice.InvoiceId} - {invoice.Patient?.FullName}",
                           "Thông tin hóa đơn",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);

            // TODO: Implement actual invoice details view
            // Example:
            // var invoiceDetailsWindow = new InvoiceDetailsWindow();
            // invoiceDetailsWindow.DataContext = new InvoiceDetailsViewModel(invoice);
            // invoiceDetailsWindow.ShowDialog();
        }

        private void PrintInvoice(Invoice invoice)
        {
            if (invoice == null) return;

            MessageBox.Show($"Đang in hóa đơn #{invoice.InvoiceId} - {invoice.Patient?.FullName}",
                           "In hóa đơn",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);

            // TODO: Implement actual printing functionality
        }

        private void ProcessPayment(Invoice invoice)
        {
            if (invoice == null) return;

            MessageBoxResult result = MessageBox.Show(
                $"Xác nhận thanh toán hóa đơn #{invoice.InvoiceId} cho khách hàng {invoice.Patient?.FullName}?\n\nSố tiền: {invoice.TotalAmount:N0} VNĐ",
                "Xác nhận thanh toán",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Update invoice status in database
                    var invoiceToUpdate = DataProvider.Instance.Context.Invoices
                        .FirstOrDefault(i => i.InvoiceId == invoice.InvoiceId);

                    if (invoiceToUpdate != null)
                    {
                        invoiceToUpdate.Status = "Đã thanh toán";
                        DataProvider.Instance.Context.SaveChanges();

                        // Refresh the invoice list
                        LoadInvoices();

                        MessageBox.Show("Thanh toán hóa đơn thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi thanh toán hóa đơn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SellMedicine(Invoice invoice)
        {
            if (invoice == null) return;

            MessageBox.Show($"Bán thuốc cho hóa đơn #{invoice.InvoiceId} - Bệnh nhân {invoice.Patient?.FullName}",
                           "Bán thuốc",
                           MessageBoxButton.OK,
                           MessageBoxImage.Information);

            // TODO: Navigate to medicine selling functionality
            // Example:
            // var mainWindow = Application.Current.MainWindow as MainWindow;
            // if (mainWindow?.MainTabControl != null)
            // {
            //     mainWindow.MainTabControl.SelectedIndex = 6; // Index of medicine sell tab
            //     var medicineSellVM = mainWindow.MainTabControl.Items[6].DataContext as MedicineSellViewModel;
            //     medicineSellVM?.LoadInvoiceData(invoice);
            // }
        }
        #endregion
    }

    // Define StatusItem class if not already defined elsewhere

}
