using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel chính của ứng dụng - quản lý trạng thái toàn cục và phân quyền người dùng
    /// Điều khiển hiển thị các tab dựa trên vai trò của người dùng đăng nhập
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        #region Properties - Thuộc tính dữ liệu

        /// <summary>
        /// Tài khoản người dùng hiện tại đang đăng nhập
        /// Khi thay đổi sẽ tự động cập nhật quyền truy cập các tab
        /// </summary>
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                // Khi CurrentAccount thay đổi, cập nhật khả năng hiển thị tab
                UpdateTabVisibility();
            }
        }

        /// <summary>
        /// Danh sách các tab được phép hiển thị cho người dùng hiện tại
        /// Được duy trì để tương thích ngược với code cũ
        /// </summary>
        private ObservableCollection<string> _AllowedTabs = new ObservableCollection<string>();
        public ObservableCollection<string> AllowedTabs
        {
            get => _AllowedTabs;
            set
            {
                _AllowedTabs = value;
                OnPropertyChanged();
            }
        }

        // === CÁC THUỘC TÍNH BOOLEAN KIỂM SOÁT QUYỀN TRUY CẬP TAB ===
        // Mỗi thuộc tính tương ứng với một tab trong ứng dụng

        /// <summary>
        /// Quyền xem tab Dashboard (Trang chủ/Tổng quan)
        /// </summary>
        private bool _canViewDashboard = false;
        public bool CanViewDashboard
        {
            get => _canViewDashboard;
            set { _canViewDashboard = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Quyền xem tab Quản lý bệnh nhân
        /// </summary>
        private bool _canViewPatient = false;
        public bool CanViewPatient
        {
            get => _canViewPatient;
            set { _canViewPatient = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Quyền xem tab Khám bệnh
        /// </summary>
        private bool _canViewExamine = false;
        public bool CanViewExamine
        {
            get => _canViewExamine;
            set { _canViewExamine = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Quyền xem tab Quản lý lịch hẹn
        /// </summary>
        private bool _canViewAppointment = false;
        public bool CanViewAppointment
        {
            get => _canViewAppointment;
            set { _canViewAppointment = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Quyền xem tab Quản lý kho thuốc
        /// </summary>
        private bool _canViewInventory = false;
        public bool CanViewInventory
        {
            get => _canViewInventory;
            set { _canViewInventory = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Quyền xem tab Quản lý hóa đơn
        /// </summary>
        private bool _canViewInvoice = false;
        public bool CanViewInvoice
        {
            get => _canViewInvoice;
            set { _canViewInvoice = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Quyền xem tab Bán thuốc
        /// </summary>
        private bool _canViewMedicineSell = false;
        public bool CanViewMedicineSell
        {
            get => _canViewMedicineSell;
            set { _canViewMedicineSell = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Quyền xem tab Quản lý nhân viên/bác sĩ
        /// </summary>
        private bool _canViewDoctor = false;
        public bool CanViewDoctor
        {
            get => _canViewDoctor;
            set { _canViewDoctor = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Quyền xem tab Thống kê báo cáo
        /// </summary>
        private bool _canViewStatistics = false;
        public bool CanViewStatistics
        {
            get => _canViewStatistics;
            set { _canViewStatistics = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Quyền xem tab Cài đặt hệ thống
        /// </summary>
        private bool _canViewSettings = false;
        public bool CanViewSettings
        {
            get => _canViewSettings;
            set { _canViewSettings = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Cờ kiểm tra ứng dụng đã được tải chưa
        /// </summary>
        public bool Isloaded = false;

        /// <summary>
        /// Thông tin tổng kho thuốc (hiện chưa được sử dụng)
        /// </summary>
        public string _TotalStock;
        public string TotalStock { get => _TotalStock; set { _TotalStock = value; OnPropertyChanged(); } }

        #endregion

        #region Commands - Các lệnh xử lý sự kiện

        /// <summary>
        /// Lệnh xử lý khi cửa sổ chính được tải - hiển thị màn hình đăng nhập
        /// </summary>
        public ICommand LoadedWindowCommand { get; set; }
        
        /// <summary>
        /// Lệnh thêm bệnh nhân mới - mở cửa sổ thêm bệnh nhân
        /// </summary>
        public ICommand AddPatientCommand { get; set; }
        
        /// <summary>
        /// Lệnh thêm lịch hẹn mới - chuyển đến tab lịch hẹn
        /// </summary>
        public ICommand AddAppointmentCommand { get; set; }
        
        /// <summary>
        /// Lệnh đăng xuất - hiển thị lại màn hình đăng nhập
        /// </summary>
        public ICommand SignOutCommand { get; set; }
        
        /// <summary>
        /// Lệnh xử lý khi cửa sổ chính đóng - xác nhận đăng xuất
        /// </summary>
        public ICommand WindowClosingCommand { get; set; }
        
        /// <summary>
        /// Lệnh xử lý khi chọn tab - tải dữ liệu cho tab được chọn
        /// </summary>
        public ICommand TabSelectedCommand { get; set; }

        #endregion

        /// <summary>
        /// Constructor khởi tạo MainViewModel
        /// Thiết lập tất cả các command và hệ thống quản lý tab
        /// </summary>
        public MainViewModel()
        {

            InitializeCommand();   
            // Khởi tạo hệ thống quản lý tab
            InitializeTabSelectionSystem();
        }
    
        void InitializeCommand()
        {
            // Command xử lý khi cửa sổ chính được tải
            LoadedWindowCommand = new RelayCommand<Window>(
                (p) =>
                {
                    Isloaded = true;
                    if (p == null)
                        return;

                    // Đăng ký sự kiện đóng cửa sổ
                    p.Closing -= MainWindow_Closing;
                    p.Closing += MainWindow_Closing;

                    // Ẩn cửa sổ chính và hiển thị màn hình đăng nhập
                    p.Hide();
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.ShowDialog();

                    if (loginWindow.DataContext == null)
                        return;

                    // Kiểm tra kết quả đăng nhập
                    var loginVM = loginWindow.DataContext as LoginViewModel;
                    if (loginVM.IsLogin)
                    {
                        // Đăng nhập thành công - hiển thị lại cửa sổ chính
                        p.Show();
                    }
                    else
                    {
                        // Đăng nhập thất bại - đóng ứng dụng
                        p.Close();
                    }
                },
                (p) => true
            );

            // Command xử lý khi cửa sổ đóng
            WindowClosingCommand = new RelayCommand<CancelEventArgs>(
                (e) =>
                {
                    // Xác định xem có đang đăng xuất không
                    if (CurrentAccount != null)
                    {
                        // Sử dụng MessageBoxService để hiển thị hộp thoại xác nhận
                        bool result = ClinicManagement.Services.MessageBoxService.ShowQuestion(
                            "Bạn có chắc chắn muốn đăng xuất?",
                            "Xác nhận đăng xuất");

                        if (!result)
                        {
                            // Người dùng không muốn đăng xuất, hủy sự kiện đóng cửa sổ
                            if (e != null)
                                e.Cancel = true;
                        }
                        else
                        {
                            // Người dùng đồng ý đăng xuất
                            SignOut();
                        }
                    }
                },
                (e) => true
            );

            // Command đăng xuất
            SignOutCommand = new RelayCommand<Window>(
                (window) =>
                {
                    SignOut();

                    if (window == null)
                        return;

                    // Ẩn cửa sổ và hiển thị màn hình đăng nhập
                    window.Hide();

                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.ShowDialog();

                    // Kiểm tra kết quả đăng nhập lại
                    if (loginWindow.DataContext is LoginViewModel loginVM && loginVM.IsLogin)
                    {
                        window.Show();
                        // Hiển thị thông báo chào mừng
                        if (CurrentAccount != null)
                        {
                            MessageBoxService.ShowInfo($"Chào mừng {CurrentAccount.Username}!", "Thông báo đăng nhập thành công");
                        }
                    }
                    else
                    {
                        // Đăng nhập thất bại - đóng ứng dụng
                        window.Close();
                    }
                },
                (p) => CurrentAccount != null // Chỉ cho phép đăng xuất khi đã đăng nhập
            );

            // Command thêm bệnh nhân mới
            AddPatientCommand = new RelayCommand<Window>(
                (p) =>
                {
                    AddPatientWindow addPatientWindow = new AddPatientWindow();
                    addPatientWindow.ShowDialog();
                },
                (p) => true
            );

            // Command thêm lịch hẹn - chuyển đến tab lịch hẹn
            AddAppointmentCommand = new RelayCommand<System.Windows.Controls.TabControl>(
                (tabControl) =>
                {
                    if (tabControl != null)
                    {
                        // Tìm và chọn tab lịch hẹn
                        foreach (var item in tabControl.Items)
                        {
                            if (item is TabItem tabItem && tabItem.Name == "AppointmentTab")
                            {
                                tabControl.SelectedItem = tabItem;
                                break;
                            }
                        }
                    }
                },
                (p) => true
            );
        }
        /// <summary>
        /// Xử lý sự kiện đóng cửa sổ chính
        /// Gọi WindowClosingCommand để xử lý logic đăng xuất
        /// </summary>
        /// <param name="sender">Đối tượng gửi sự kiện</param>
        /// <param name="e">Tham số sự kiện đóng cửa sổ</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Sử dụng WindowClosingCommand để xử lý
            WindowClosingCommand.Execute(e);
        }

        /// <summary>
        /// Phương thức đăng xuất tập trung
        /// Xóa thông tin người dùng hiện tại và reset trạng thái ứng dụng
        /// </summary>
        public void SignOut()
        {
            try
            {
                // Đăng xuất người dùng khỏi hệ thống
                if (CurrentAccount != null)
                {
                    // Cập nhật trạng thái đăng nhập trong database nếu cần
                    var accountToUpdate = DataProvider.Instance.Context.Accounts
                        .FirstOrDefault(a => a.Username == CurrentAccount.Username);

                    if (accountToUpdate != null)
                    {
                        accountToUpdate.IsLogined = false;
                        DataProvider.Instance.Context.SaveChanges();
                    }

                    // Reset current account và danh sách tab được phép
                    CurrentAccount = null;
                    AllowedTabs.Clear();

                    // Reset ViewModelLocator để các ViewModel được tạo mới khi đăng nhập lại
                    var viewModelLocator = Application.Current.Resources["ViewModelLocator"] as ViewModelLocator;
                    viewModelLocator?.Reset();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                     $"Lỗi khi đăng xuất: {ex.Message}",
                     "Lỗi");
            }
        }

        /// <summary>
        /// Đảm bảo tab được chọn là tab hợp lệ (người dùng có quyền truy cập)
        /// Nếu tab hiện tại không hợp lệ, chuyển sang tab đầu tiên được phép
        /// </summary>
        /// <param name="tabControl">TabControl cần kiểm tra</param>
        public void EnsureValidTabSelected(System.Windows.Controls.TabControl tabControl)
        {
            if (tabControl == null)
                return;

            // Kiểm tra tab hiện tại có được phép không
            var currentTabItem = tabControl.SelectedItem as TabItem;
            if (currentTabItem != null)
            {
                string tabName = GetTabName(currentTabItem);
                if (IsTabAllowed(tabName))
                    return; // Tab hiện tại hợp lệ
            }

            // Tìm tab đầu tiên được phép hiển thị và chọn nó
            foreach (TabItem item in tabControl.Items)
            {
                string tabName = GetTabName(item);
                if (IsTabAllowed(tabName))
                {
                    tabControl.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// Phương thức helper kiểm tra xem một tab có được phép hiển thị không
        /// Dựa trên các thuộc tính boolean quyền truy cập
        /// </summary>
        /// <param name="tabName">Tên tab cần kiểm tra</param>
        /// <returns>True nếu được phép, False nếu không được phép</returns>
        private bool IsTabAllowed(string tabName)
        {
            switch (tabName)
            {
                case "DashboardTab": return CanViewDashboard;
                case "PatientTab": return CanViewPatient;
                case "ExamineTab": return CanViewExamine;
                case "AppointmentTab": return CanViewAppointment;
                case "StockTab": return CanViewInventory;
                case "InvoiceTab": return CanViewInvoice;
                case "MedicineSellTab": return CanViewMedicineSell;
                case "DoctorTab": return CanViewDoctor;
                case "StatisticsTab": return CanViewStatistics;
                case "SettingsTab": return CanViewSettings;
                default: return false; // Mặc định không cho phép tab không xác định
            }
        }

        /// <summary>
        /// Cập nhật khả năng hiển thị tab dựa trên vai trò người dùng
        /// Được gọi tự động khi CurrentAccount thay đổi
        /// </summary>
        private void UpdateTabVisibility()
        {
            // Reset tất cả quyền về false trước
            ResetAllPermissions();

            // Duy trì tương thích ngược - xóa danh sách AllowedTabs
            AllowedTabs.Clear();

            // Kiểm tra điều kiện đầu vào
            if (CurrentAccount == null || string.IsNullOrEmpty(CurrentAccount.Role))
            {
                return;
            }

            string role = CurrentAccount.Role.Trim();

            // Thiết lập quyền boolean dựa trên vai trò
            if (UserRoles.RoleTabPermissions.ContainsKey(role))
            {
                foreach (var tab in UserRoles.RoleTabPermissions[role])
                {
                    string cleanTabName = tab.Trim();

                    // Thiết lập thuộc tính boolean tương ứng
                    SetTabPermission(cleanTabName, true);

                    // Duy trì tương thích ngược
                    AllowedTabs.Add(cleanTabName);
                }
            }
            else
            {
             
                MessageBoxService.ShowWarning($"Không tìm thấy quyền cho vai trò: '{role}'", "Lỗi phân quyền");
            }
        }

        /// <summary>
        /// Reset tất cả các thuộc tính quyền boolean về false
        /// Được gọi trước khi thiết lập quyền mới
        /// </summary>
        private void ResetAllPermissions()
        {
            CanViewDashboard = false;
            CanViewPatient = false;
            CanViewExamine = false;
            CanViewAppointment = false;
            CanViewInventory = false;
            CanViewInvoice = false;
            CanViewMedicineSell = false;
            CanViewDoctor = false;
            CanViewStatistics = false;
            CanViewSettings = false;
        }

        /// <summary>
        /// Thiết lập quyền cho tab cụ thể
        /// Mapping từ tên tab sang thuộc tính boolean tương ứng
        /// </summary>
        /// <param name="tabName">Tên tab cần thiết lập quyền</param>
        /// <param name="isAllowed">Có được phép hay không</param>
        private void SetTabPermission(string tabName, bool isAllowed)
        {
            switch (tabName)
            {
                case "DashboardTab": CanViewDashboard = isAllowed; break;
                case "PatientTab": CanViewPatient = isAllowed; break;
                case "ExamineTab": CanViewExamine = isAllowed; break;
                case "AppointmentTab": CanViewAppointment = isAllowed; break;
                case "StockTab": CanViewInventory = isAllowed; break;
                case "InvoiceTab": CanViewInvoice = isAllowed; break;
                case "MedicineSellTab": CanViewMedicineSell = isAllowed; break;
                case "DoctorTab": CanViewDoctor = isAllowed; break;
                case "StatisticsTab": CanViewStatistics = isAllowed; break;
                case "SettingsTab": CanViewSettings = isAllowed; break;
            }
        }

        /// <summary>
        /// Helper method lấy tên tab từ TabItem
        /// </summary>
        /// <param name="tabItem">TabItem cần lấy tên</param>
        /// <returns>Tên của tab</returns>
        private string GetTabName(TabItem tabItem)
        {
            // Trả về thuộc tính Name của TabItem
            return tabItem.Name;
        }

        /// <summary>
        /// Khởi tạo hệ thống quản lý chọn tab
        /// Đăng ký các action để tải dữ liệu khi chuyển tab
        /// </summary>
        private void InitializeTabSelectionSystem()
        {
            // Khởi tạo command xử lý khi chọn tab
            TabSelectedCommand = new RelayCommand<string>(
                (tabName) =>
                {
                    if (string.IsNullOrEmpty(tabName))
                        return;

                    // Gọi TabSelectionManager để xử lý việc tải dữ liệu cho tab
                    TabSelectionManager.Instance.TabSelected(tabName);
                },
                (tabName) => true
            );

            // Đăng ký các action tải dữ liệu cho từng tab khi được chọn

            // Tab Quản lý bệnh nhân
            TabSelectionManager.Instance.RegisterTabReloadAction("PatientTab", () =>
            {
                var patientVM = Application.Current.Resources["PatientVM"] as PatientViewModel;
                patientVM?.LoadData();
            });

            // Tab Lịch hẹn
            TabSelectionManager.Instance.RegisterTabReloadAction("AppointmentTab", () =>
            {
                var appointmentVM = Application.Current.Resources["AppointmentVM"] as AppointmentViewModel;
                if (appointmentVM != null)
                {
                    // Nếu CurrentAccount chưa được set trong ViewModel nhưng có sẵn trong MainViewModel
                    if (appointmentVM.CurrentAccount == null && CurrentAccount != null)
                    {
                        appointmentVM.CurrentAccount = CurrentAccount;
                    }
                    appointmentVM.LoadAppointments();
                }
            });

            // Tab Quản lý kho thuốc
            TabSelectionManager.Instance.RegisterTabReloadAction("StockTab", () =>
            {
                var stockVM = Application.Current.Resources["StockMedicineVM"] as StockMedicineViewModel;
                if (stockVM != null)
                {
                    // Tải dữ liệu thường xuyên
                    stockVM.LoadData();
                }
            });

            // Tab Quản lý hóa đơn
            TabSelectionManager.Instance.RegisterTabReloadAction("InvoiceTab", () =>
            {
                var invoiceVM = Application.Current.Resources["InvoiceVM"] as InvoiceViewModel;
                invoiceVM?.LoadInvoices();
            });

            // Tab Bán thuốc
            TabSelectionManager.Instance.RegisterTabReloadAction("MedicineSellTab", () =>
            {
                var medicineSellVM = Application.Current.Resources["MedicineSellVM"] as MedicineSellViewModel;
                medicineSellVM?.LoadData();
            });

            // Tab Quản lý nhân viên/bác sĩ
            TabSelectionManager.Instance.RegisterTabReloadAction("DoctorTab", () =>
            {
                var StaffVM = Application.Current.Resources["StaffVM"] as StaffViewModel;
                StaffVM?.LoadData();
            });

            // Tab Thống kê báo cáo
            TabSelectionManager.Instance.RegisterTabReloadAction("StatisticsTab", () =>
            {
                var statisticsVM = Application.Current.Resources["StatisticsVM"] as StatisticsViewModel;
                statisticsVM?.LoadDashBoard();
                statisticsVM?.LoadStatisticsAsync();
            });
        }
    }
}