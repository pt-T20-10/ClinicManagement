using ClinicManagement.Models;
using ClinicManagement.SubWindow;
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
using ClinicManagement.Services;

namespace ClinicManagement.ViewModels
{
    public class InvoiceViewModel : BaseViewModel
    {

        //
        //ObservableCollection<T> triển khai giao diện INotifyCollectionChanged, và khi danh sách bị thay đổi, nó sẽ raise (gửi) sự kiện CollectionChanged.
        //Nó là một collection (bộ sưu tập) giống như List<T>, nhưng có tính năng đặc biệt là thông báo tự động khi có sự thay đổi trong danh sách, ví dụ như khi thêm, xóa hoặc sửa phần tử.
        #region Properties

        //Áp dụng đóng gói cho toàn bộ Properties để bảo vệ dữ liệu và đảm bảo rằng các thay đổi được thông báo đến giao diện người dùng thông qua INotifyPropertyChanged. Theo chuẩn MVVM
        //Class BaseViewModel đã được cấu hình để có các phương thức OnPropertyChanged() và SetProperty() để thông báo cho giao diện người dùng về các thay đổi trong thuộc tính. 
        //Cũng như RelayCommand để cài đặt và sử dụng Command 

        // Collection Hóa đơn là Itemsource cho DataGrid
        private ObservableCollection<Invoice> _invoices;
        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices ??= new ObservableCollection<Invoice>();
            set
            {
                _invoices = value;
                OnPropertyChanged(); // Thông báo cho giao diện biết rằng danh sách đã thay đổi  
            }
        }

        // Selected Invoice để Binding nhận và giữ giá trị của hóa đơn được chọn trong DataGrid
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
        #region Filter properties
        // StartDate và EndDate để lọc hóa đơn theo khoảng thời gian StarDate mặc định là cách ngày hiện tại 1 tháng
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

        // Search text để lọc hóa đơn theo tên bệnh nhân, mã bảo hiểm hoặc số điện thoại Binding textBox tìm kiếm ở View
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

        // Collection trạng thái hóa đơn là ItemSource cho ComboBox trạng thái hóa đơn
        private ObservableCollection<StatusItem> _invoiceStatusList;
        public ObservableCollection<StatusItem> InvoiceStatusList
        {
            get => _invoiceStatusList ??= InitializeInvoiceStatusList(); // Khởi tạo danh sách trạng thái hóa đơn nếu chưa được khởi tạo,
                                                                         // InitializeInvoiceStatusList là phương thức khởi tạo Collection hóa trạng thái hóa đơn
            set
            {
                _invoiceStatusList = value;
                OnPropertyChanged();
            }
        }
        // Selected Invoice Status để Binding nhận và giữ giá trị của trạng thái hóa đơn được chọn trong ComboBox để thực hiện lọc theo trạng thái hóa đơn
        private StatusItem _selectedInvoiceStatus;
        public StatusItem SelectedInvoiceStatus
        {
            get => _selectedInvoiceStatus;
            set
            {
                _selectedInvoiceStatus = value;
                OnPropertyChanged();

                // Tự động lọc khi trạng thái thay đổi hay khi trạng thái được set, Binding Mode mặc định của ComboBox là TwoWay 
                FilterInvoicesAuto();
            }
        }

        // Collection loại hóa đơn là ItemSource cho ComboBox loại hóa đơn
        private ObservableCollection<StatusItem> _invoiceTypeList;
        public ObservableCollection<StatusItem> InvoiceTypeList
        {
            get => _invoiceTypeList ??= InitializeInvoiceTypeList();// Khởi tạo danh sách loại hóa đơn nếu chưa được khởi tạo,
                                                              // InitializeInvoiceTypeList là phương thức khởi tạo Collection loại hóa đơn
            set
            {
                _invoiceTypeList = value;
                OnPropertyChanged();
            }
        }

        // Selected Invoice Type để Binding nhận và giữ giá trị của loại hóa đơn được chọn trong ComboBox để thực hiện lọc theo loại hóa đơn
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

        #endregion

        #region Navigation properties

        //Property trang hiện tại mặc định là 1 
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

                    //PageInfoText, CanNavigateForward, CanNavigateBackward sẽ được cập nhật khi CurrentPage thay đổi vì được tính toán phụ thuộc vao CurrentPage
                    OnPropertyChanged(nameof(PageInfoText)); 
                    OnPropertyChanged(nameof(CanNavigateForward)); 
                    OnPropertyChanged(nameof(CanNavigateBackward));
                    // Tải lại hóa đơn khi trang hiện tại thay đổi
                    LoadInvoices();
                }
            }
        }

        //Tổng số trang được tính toán theo số lượng Item muốn hiển thị trong 1 trang và tổng số mục (hóa đơn) hiện có
        private int _totalPages;
        public int TotalPages
        {
            get => _totalPages;
            set
            {
                _totalPages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfoText));
                OnPropertyChanged(nameof(CanNavigateForward));
                OnPropertyChanged(nameof(CanNavigateBackward));
            
            }
        }

        //Tổng số mục (hóa đơn) được tính toán từ cơ sở dữ liệu và sẽ được cập nhật khi tải hóa đơn
        private int _totalItems;
        public int TotalItems
        {
            get => _totalItems;
            set
            {
                _totalItems = value;
                OnPropertyChanged();
                // Cập nhật PageInfoText khi TotalItems thay đổi vì PageInfoText có thể hiện tổng số mục hiện có
                OnPropertyChanged(nameof(PageInfoText));
            }
        }


        //Danh sách các lựa chọn về số lượng mục hiển thị mỗi trang, có thể được sử dụng trong ComboBox hoặc các điều khiển tương tự
        public List<int> ItemsPerPageOptions { get; } = new List<int> { 10, 20, 50, 100 };

        //Giá trị số mục hiển thị mỗi trang
        private int _selectedItemsPerPage = 10; // Mặc định là 10 mục mỗi trang
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
        // Thuộc tính mới để kiểm tra khả năng điều hướng tiến
        public bool CanNavigateForward => TotalPages > 1 && CurrentPage < TotalPages;

        // Thuộc tính mới để kiểm tra khả năng điều hướng lùi
        public bool CanNavigateBackward => CurrentPage > 1;

        //Thuộc tính để hiển thị thông tin trang hiện tại và tổng số trang, sẽ được sử dụng trong giao diện người dùng
        public string PageInfoText => $"Trang {CurrentPage}/{TotalPages} (Tổng {TotalItems} hóa đơn)";

        // Thuộc tính để kiểm tra trạng thái đang tải dữ liệu, sẽ được sử dụng để hiển thị thông báo hoặc vòng xoay tải trong giao diện người dùng
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

       
        // Phương thức để kiểm tra xem trang hiện tại có phải là trang đầu tiên hay không
        public bool IsFirstPage => CurrentPage <= 1;

        // Phương thức để kiểm tra xem trang hiện tại có phải là trang cuối cùng hay không
        public bool IsLastPage => TotalPages > 0 && CurrentPage >= TotalPages;

        #endregion

        #endregion

        #region Commands
        //Khai lệnh (Commands) để thực hiện các hành động trong ViewModel, sẽ được liên kết với các nút hoặc sự kiện trong giao diện người dùng

       
        public ICommand SearchCommand { get; set; } // Command để tìm kiếm hóa đơn, sẽ được liên kết với nút tìm kiếm trong giao diện người dùng

        public ICommand ResetFiltersCommand { get; set; } //Command để đặt lại bộ lọc, sẽ được liên kết với nút đặt lại trong giao diện người dùng

        public ICommand ViewInvoiceDetailsCommand { get; set; }// Command để xem chi tiết hóa đơn, sẽ được liên kết với sự kiện chọn hóa đơn trong DataGrid

        public ICommand PrintInvoiceCommand { get; set; }// Command để in hóa đơn, sẽ được liên kết với nút in trong giao diện người dùng

        public ICommand ProcessPaymentCommand { get; set; }// Command để xử lý thanh toán hóa đơn, sẽ được liên kết với nút thanh toán trong giao diện người dùng

        public ICommand SellMedicineCommand { get; set; }//Command để bán thuốc cho hóa đơn, sẽ được liên kết với nút bán thuốc trong giao diện người dùng

        // Pagination commands
        // Các lệnh để điều hướng trang trong giao diện người dùng, sẽ được liên kết với các nút điều hướng trang
        public ICommand FirstPageCommand { get; set; }
        public ICommand PreviousPageCommand { get; set; }
        public ICommand NextPageCommand { get; set; }
        public ICommand LastPageCommand { get; set; }
    
        #endregion
        //Constructor khởi tạo ViewModel
        public InvoiceViewModel()
        {
            InitializeCommands(); //Phương thức này sẽ khởi tạo tất cả các lệnh (Commands) được sử dụng trong ViewModel

            SelectedInvoiceStatus = InvoiceStatusList.FirstOrDefault();// Đặt trạng thái hóa đơn mặc định là "Tất cả trạng thái"

            SelectedInvoiceType = InvoiceTypeList.FirstOrDefault();// Đặt loại hóa đơn mặc định là "Tất cả loại"
        }

        #region Initialization Methods

        //Phương thức khởi tạo tất cả các lệnh (Commands) trong ViewModel, sẽ được gọi trong constructor
        private void InitializeCommands()
        {
            //Invoice commands
            SearchCommand = new RelayCommand<object>(
                p => SearchInvoices(), // Execute method SearchInvoices thực hiện tìm kiếm hóa đơn
                p => true);//CanExecute luôn trả về true, nghĩa là lệnh này luôn có thể thực hiện được

            ResetFiltersCommand = new RelayCommand<object>(
                p => ResetFilters(),// Execute method ResetFillters thực hiện đặt lại tất cả các bộ lọc về giá trị mặc định
                p => true);//CanExecute luôn trả về true, nghĩa là lệnh này luôn có thể thực hiện được

            ViewInvoiceDetailsCommand = new RelayCommand<Invoice>(
                p => ViewInvoiceDetails(p),//Execute method ViewInvoiceDetails để xem chi tiết hóa đơn với Parameter truyền vào là Invoice
                p => p != null);//CanExecute kiểm tra xem hóa đơn có khác null hay không, nếu khác null thì lệnh này có thể thực hiện được

            PrintInvoiceCommand = new RelayCommand<Invoice>(
                p => PrintInvoice(p),// Execute method PrintInvoice để in hóa đơn với Parameter truyền vào là Invoice
                p => p != null);// CanExecute kiểm tra xem hóa đơn có khác null hay không, nếu khác null thì lệnh này có thể thực hiện được

            ProcessPaymentCommand = new RelayCommand<Invoice>(
                p => ProcessPayment(p),//Execute method ProcessPayment để xử lý thanh toán hóa đơn với Parameter truyền vào là Invoice
                p => p != null && p.Status == "Chưa thanh toán"); //CanExecute kiểm tra xem hóa đơn có khác null và trạng thái là "Chưa thanh toán" hay không, nếu đúng thì lệnh này có thể thực hiện được

            SellMedicineCommand = new RelayCommand<Invoice>(
                p => SellMedicine(p),// Execute method SellMedicine để bán thuốc cho hóa đơn với Parameter truyền vào là Invoice
                p => p != null && p.InvoiceType == "Khám và bán thuốc");// // CanExecute kiểm tra xem hóa đơn có khác null và loại hóa đơn là "Khám và bán thuốc" hay không, nếu đúng thì lệnh này có thể thực hiện được

            // Pagination commands
            FirstPageCommand = new RelayCommand<object>(
                p => CurrentPage = 1, // Đặt CurrentPage về 1
                p => CanNavigateBackward);// CanExecute kiểm tra xem có thể điều hướng lùi hay không, nếu có thì lệnh này có thể thực hiện được

            PreviousPageCommand = new RelayCommand<object>(
                p => CurrentPage--, // Giảm CurrentPage đi 1
                p => CanNavigateBackward);// CanExecute kiểm tra xem có thể điều hướng lùi hay không, nếu có thì lệnh này có thể thực hiện được

            NextPageCommand = new RelayCommand<object>(
                p => CurrentPage++,// Tăng CurrentPage lên 1
                p => CanNavigateForward);// CanExecute kiểm tra xem có thể điều hướng tiến hay không, nếu có thì lệnh này có thể thực hiện được

            LastPageCommand = new RelayCommand<object>(
                p => CurrentPage = TotalPages,//Đặt trang hiện tại thành tổng số trang  
                p => CanNavigateForward);// CanExecute kiểm tra xem có thể điều hướng tiến hay không, nếu có thì lệnh này có thể thực hiện được

        
        }

        //Phương thức khởi tạo Collection trạng thái hóa đơn và loại hóa đơn, 
        private ObservableCollection<StatusItem> InitializeInvoiceStatusList()
        {
            return new ObservableCollection<StatusItem>
            {
                new StatusItem("", "Tất cả trạng thái"),
                new StatusItem("Đã thanh toán", "Đã thanh toán"),
                new StatusItem("Chưa thanh toán", "Chưa thanh toán")
            };
        }

        //Phương thức khởi tạo Collection loại hóa đơn
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

        //Phương thức Load hóa đơn
        public void LoadInvoices()
        {
            try // Bắt lỗi khi tải hóa đơn
            {
                IsLoading = true;

                // Tạo query để lấy danh sách hóa đơn từ cơ sở dữ liệu
                var query = DataProvider.Instance.Context.Invoices
                    .Include(i => i.Patient)
                    .Include(i => i.StaffPrescriber)
                    .Include(i => i.StaffCashier)
                    .AsQueryable(); // AsQueryable() cho phép chúng ta áp dụng các bộ lọc và phân trang trên danh sách hóa đơn

                // Áp dụng bộ lọc cho query
                query = ApplyFilters(query);

                //Lấy tổng số mục hiện tại theo bộ lọc đã áp dụng
                TotalItems = query.Count();
                //Tính tổng số trang theo số lượng mục hiển thị mỗi trang
                TotalPages = (int)Math.Ceiling(TotalItems / (double)SelectedItemsPerPage);

                // Kiểm tra nếu tổng số trang là 0 thì đặt CurrentPage về 1
                CommandManager.InvalidateRequerySuggested();

                //Áp dụng chuyển trang và sắp xếp theo ngày hóa đơn giảm dần
                var paginatedInvoices = query
                    .OrderByDescending(i => i.InvoiceDate)
                    .Skip((CurrentPage - 1) * SelectedItemsPerPage)// Bỏ qua các mục trước đó theo trang hiện tại
                    .Take(SelectedItemsPerPage)// Lấy số lượng mục theo SelectedItemsPerPage
                    .ToList();

                // Cập nhật ObservableCollection Invoices với danh sách hóa đơn đã phân trang
                Invoices = new ObservableCollection<Invoice>(paginatedInvoices);
            }
            catch (Exception ex) // thực hiện bắt lỗi
            {
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu hóa đơn: {ex.Message}", "Lỗi"    );
            }
            finally // Đảm bảo IsLoading được đặt về false sau khi hoàn thành việc tải hóa đơn, bất kể có lỗi hay không
            {
                IsLoading = false;
            }
        }

        // Phương thức này áp dụng các bộ lọc cho danh sách hóa đơn dựa trên các thuộc tính đã được thiết lập
        //IQueryable <Invoice> cho phép chúng ta áp dụng các bộ lọc và phân trang trên danh sách hóa đơn
        private IQueryable<Invoice> ApplyFilters(IQueryable<Invoice> query)
        {
            // Đầu tiên là áp dụng lọc theo ngày bắt đầu và kết thúc nếu có giá trị
            if (StartDate != DateTime.MinValue && EndDate != DateTime.MinValue)
            {
                var endDatePlus = EndDate.AddDays(1);
                query = query.Where(i => i.InvoiceDate >= StartDate && i.InvoiceDate < endDatePlus);
            }

            // Áp dụng trạng thái lọc nếu đã chọn
            if (SelectedInvoiceStatus != null && !string.IsNullOrEmpty(SelectedInvoiceStatus.Status))
            {
                query = query.Where(i => i.Status == SelectedInvoiceStatus.Status);
            }

            // Áp dụng loại hóa đơn lọc nếu đã chọn
            if (SelectedInvoiceType != null && !string.IsNullOrEmpty(SelectedInvoiceType.Status))
            {
                query = query.Where(i => i.InvoiceType == SelectedInvoiceType.Status);
            }

            // Aps dụng lọc theo văn bản tìm kiếm nếu có
            if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string searchLower = SearchText.ToLower().Trim();
                    query = query.Where(i =>
                        (i.Patient.FullName != null && i.Patient.FullName.ToLower().Contains(searchLower)) ||
                        (i.Patient.InsuranceCode != null && i.Patient.InsuranceCode.ToLower().Contains(searchLower)) ||
                        (i.Patient.Phone != null && i.Patient.Phone.ToLower().Contains(searchLower))
                    );
            }
            //Logic lọc đa tiêu chí và sẽ tự động khi 1 trong các tiêu chí được chọn hay thay đổi

            return query;
        }


        #endregion

        #region Action Methods
        // Phương thức này sẽ được gọi khi thay đổi bộ lọc tự động, ví dụ như khi thay đổi ngày bắt đầu, ngày kết thúc, trạng thái hóa đơn hoặc loại hóa đơn
        private void FilterInvoicesAuto()
        {
            // Đặt lại trang về 1 mỗi khi thay đổi bộ lọc
            CurrentPage = 1;
            LoadInvoices();
        }

        // Phương thức này sẽ được gọi khi nhấn nút tìm kiếm
        private void SearchInvoices()
        {
            // Phương thức này được gọi khi nhấn nút Search
            // Đặt lại trang về 1 và áp dụng tất cả các bộ lọc bao gồm cả SearchText
            CurrentPage = 1;
            LoadInvoices();
        }

        // Phương thức này sẽ được gọi khi nhấn nút đặt lại bộ lọc
        private void ResetFilters()
        {
            // Tắt cơ chế tự động lọc tạm thời để tránh nhiều lần gọi API
            bool oldAutoFilter = true;

            try
            {
                //Đặt lại tất cả các bộ lọc về giá trị mặc định
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

        //Phương thức ViewInvoiceDetails để xem chi tiết hóa đơn do ViewInvoiceDetailsCommand gọi
        private void ViewInvoiceDetails(Invoice invoice)
        {
            if (invoice == null) return;



            var invoiceDetailsWindow = new InvoiceDetailsWindow(); //Tạo mới cửa sổ chi tiết hóa đơn
            invoiceDetailsWindow.DataContext = new InvoiceDetailsViewModel(invoice);//Gán DataContext của cửa sổ chi tiết hóa đơn là InvoiceDetailsViewModel với hóa đơn đã chọn
            invoiceDetailsWindow.ShowDialog();
            LoadInvoices();
        }

        // Phương thức PrintInvoice để in hóa đơn do PrintInvoiceCommand gọi
        private void PrintInvoice(Invoice invoice)
        {
            if (invoice == null) return;

            MessageBoxService.ShowInfo($"Đang in hóa đơn #{invoice.InvoiceId} - {invoice.Patient?.FullName}",
                           "In hóa đơn"
                            
                             );

            
        }
        // Phương thức ProcessPayment để xử lý thanh toán hóa đơn do ProcessPaymentCommand gọi
        private void ProcessPayment(Invoice invoice)
        {
            if (invoice == null) return;

            //Tạo mới InvoiceDetailsViewModel với hóa đơn đã chọn
            var invoiceDetailsViewModel = new InvoiceDetailsViewModel(invoice);

            // Tạo mới cửa sổ InvoiceDetailsWindow và gán DataContext là invoiceDetailsViewModel
            var invoiceDetailsWindow = new InvoiceDetailsWindow
            {
                DataContext = invoiceDetailsViewModel
            };
    
            invoiceDetailsWindow.ShowDialog();

            // Tải lại hóa đơn sau khi xử lý thanh toán 
            LoadInvoices();
        }

  
        // Phương thức SellMedicine để bán thuốc cho hóa đơn do SellMedicineCommand gọi
        private void SellMedicine(Invoice invoice)
        {
            if (invoice == null) return;

            try
            {
                // Lấy MainWindow
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null) return;

                // Tìm TabControl chính
                var tabControl = LogicalTreeHelper.FindLogicalNode(mainWindow, "MainTabControl") as TabControl;
                if (tabControl == null) return;

                // Tìm tab "Bán thuốc" và chuyển đến đó
                foreach (var item in tabControl.Items)
                {
                    if (item is TabItem tabItem)
                    {
                        // Kiểm tra header của tab
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
                                            // Truyền hóa đơn vào MedicineSellViewModel từ Resources
                                            var medicineSellVM = Application.Current.Resources["MedicineSellVM"] as MedicineSellViewModel;
                                            if (medicineSellVM != null)
                                            {
                                                medicineSellVM.CurrentInvoice = invoice;
                                            }

                                            // Chuyển đến tab bán thuốc
                                            tabControl.SelectedItem = tabItem;
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Không tìm thấy tab
                MessageBoxService.ShowWarning($"Đã chọn hóa đơn #{invoice.InvoiceId} - {invoice.Patient?.FullName} nhưng không thể chuyển đến tab Bán thuốc",
                              "Thông báo"     );
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi chuyển sang màn hình bán thuốc: {ex.Message}",
                               "Lỗi"   );
            }
        }

        #endregion
    }


}
