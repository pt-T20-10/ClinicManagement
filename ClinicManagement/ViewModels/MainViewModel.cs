using ClinicManagement.Models;
using ClinicManagement.SubWindow;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClinicManagement.Converter;
using ClinicManagement.Services;

namespace ClinicManagement.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private Account _currentAccount;
        public Account CurrentAccount
        {
            get => _currentAccount;
            set
            {
                _currentAccount = value;
                OnPropertyChanged();
                // When CurrentAccount changes, update tab visibility
                UpdateTabVisibility();
            }
        }


        // Collection of tabs that should be visible to the current user
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

        // Boolean properties for each tab's permission
        private bool _canViewDashboard = false;
        public bool CanViewDashboard
        {
            get => _canViewDashboard;
            set { _canViewDashboard = value; OnPropertyChanged(); }
        }

        private bool _canViewPatient = false;
        public bool CanViewPatient
        {
            get => _canViewPatient;
            set { _canViewPatient = value; OnPropertyChanged(); }
        }

        private bool _canViewExamine = false;
        public bool CanViewExamine
        {
            get => _canViewExamine;
            set { _canViewExamine = value; OnPropertyChanged(); }
        }

        private bool _canViewAppointment = false;
        public bool CanViewAppointment
        {
            get => _canViewAppointment;
            set { _canViewAppointment = value; OnPropertyChanged(); }
        }

        private bool _canViewInventory = false;
        public bool CanViewInventory
        {
            get => _canViewInventory;
            set { _canViewInventory = value; OnPropertyChanged(); }
        }

        private bool _canViewInvoice = false;
        public bool CanViewInvoice
        {
            get => _canViewInvoice;
            set { _canViewInvoice = value; OnPropertyChanged(); }
        }

        private bool _canViewMedicineSell = false;
        public bool CanViewMedicineSell
        {
            get => _canViewMedicineSell;
            set { _canViewMedicineSell = value; OnPropertyChanged(); }
        }

        private bool _canViewDoctor = false;
        public bool CanViewDoctor
        {
            get => _canViewDoctor;
            set { _canViewDoctor = value; OnPropertyChanged(); }
        }

        private bool _canViewStatistics = false;
        public bool CanViewStatistics
        {
            get => _canViewStatistics;
            set { _canViewStatistics = value; OnPropertyChanged(); }
        }

        private bool _canViewSettings = false;
        public bool CanViewSettings
        {
            get => _canViewSettings;
            set { _canViewSettings = value; OnPropertyChanged(); }
        }

        public bool Isloaded = false;

        public string _TotalStock;
        public string TotalStock { get => _TotalStock; set { _TotalStock = value; OnPropertyChanged(); } }


        public ICommand LoadedWindowCommand { get; set; }
        public ICommand AddPatientCommand { get; set; }
        public ICommand AddAppointmentCommand { get; set; }
        public ICommand SignOutCommand { get; set; }
        public ICommand WindowClosingCommand { get; set; }
        public ICommand TabSelectedCommand { get; set; }

        // mọi thứ xử lý sẽ nằm trong này
        public MainViewModel()
        {
            LoadedWindowCommand = new RelayCommand<Window>(
                 (p) =>
                 {
                     Isloaded = true;
                     if (p == null)
                         return;

                     // Đăng ký sự kiện Closing cho window
                     p.Closing += MainWindow_Closing;

                     p.Hide();
                     LoginWindow loginWindow = new LoginWindow();
                     loginWindow.ShowDialog();
                     if (loginWindow.DataContext == null)
                         return;

                     var loginVM = loginWindow.DataContext as LoginViewModel;
                     if (loginVM.IsLogin)
                     {
                         p.Show();

                     }
                     else
                     {
                         p.Close();
                     }
                 },
                 (p) => true
            );

            // Xử lý khi cửa sổ đóng
            WindowClosingCommand = new RelayCommand<CancelEventArgs>(
                (e) =>
                {
                    // Xác định xem có đang đăng xuất không
                    if (CurrentAccount != null)
                    {
                        // Hiển thị hộp thoại xác nhận
                        MessageBoxResult result = MessageBox.Show(
                            "Bạn có chắc chắn muốn đăng xuất?",
                            "Xác nhận",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.No)
                        {
                            // Người dùng không muốn đăng xuất, hủy sự kiện
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

            // Cập nhật SignOutCommand để gọi phương thức SignOut
            SignOutCommand = new RelayCommand<Window>(
                (window) =>
                {
                    SignOut();

                    if (window == null)
                        return;

                    window.Hide();

                    // Show login window again
                    LoginWindow loginWindow = new LoginWindow();
                    loginWindow.ShowDialog();

                    if (loginWindow.DataContext is LoginViewModel loginVM && loginVM.IsLogin)
                    {
                        window.Show();
                    }
                    else
                    {
                        window.Close();
                    }
                },
                (p) => CurrentAccount != null // Chỉ cho phép đăng xuất khi đã đăng nhập
            );

            AddPatientCommand = new RelayCommand<Window>(
                (p) =>
                {
                    AddPatientWindow addPatientWindow = new AddPatientWindow();
                    addPatientWindow.ShowDialog();
                },
                (p) => true
            );

            AddAppointmentCommand = new RelayCommand<System.Windows.Controls.TabControl>(
                (tabControl) =>
                {
                    if (tabControl != null)
                    {
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
            InitializeTabSelectionSystem();
        }

        // Xử lý sự kiện đóng cửa sổ chính
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Sử dụng WindowClosingCommand để xử lý
            WindowClosingCommand.Execute(e);
        }

        // Phương thức đăng xuất tập trung
        private void SignOut()
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đăng xuất: {ex.Message}",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Updated to use boolean properties
        public void EnsureValidTabSelected(System.Windows.Controls.TabControl tabControl)
        {
            if (tabControl == null)
                return;

            // Check if current tab is allowed
            var currentTabItem = tabControl.SelectedItem as TabItem;
            if (currentTabItem != null)
            {
                string tabName = GetTabName(currentTabItem);
                if (IsTabAllowed(tabName))
                    return; // Current tab is valid
            }

            // Find first visible tab and select it
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

        // Helper method to check if a tab is allowed based on boolean properties
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
                default: return false;
            }
        }

        // Update visibility based on user role
        private void UpdateTabVisibility()
        {
            // Reset all permissions first
            ResetAllPermissions();

            // For backward compatibility - keep updating AllowedTabs
            AllowedTabs.Clear();

            if (CurrentAccount == null || string.IsNullOrEmpty(CurrentAccount.Role))
            {
                Console.WriteLine("CurrentAccount is null or Role is empty");
                return;
            }

            string role = CurrentAccount.Role.Trim();

            // Set boolean permissions based on role
            if (UserRoles.RoleTabPermissions.ContainsKey(role))
            {
                foreach (var tab in UserRoles.RoleTabPermissions[role])
                {
                    string cleanTabName = tab.Trim();

                    // Set the appropriate boolean property
                    SetTabPermission(cleanTabName, true);

                    // For backward compatibility
                    AllowedTabs.Add(cleanTabName);
                }
            }
            else
            {
                Console.WriteLine($"Role '{role}' not found in permissions");
                MessageBox.Show($"Không tìm thấy quyền cho vai trò: '{role}'", "Lỗi phân quyền");
            }
        }

        // Reset all permission booleans to false
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

        // Set permission for specific tab
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

        // Helper to get tab name from TabItem
        private string GetTabName(TabItem tabItem)
        {
            // Return TabItem's Name directly
            return tabItem.Name;
        }

        private void InitializeTabSelectionSystem()
        {
            // Initialize the command
            TabSelectedCommand = new RelayCommand<string>(
                (tabName) =>
                {
                    if (string.IsNullOrEmpty(tabName))
                        return;

                    TabSelectionManager.Instance.TabSelected(tabName);
                },
                (tabName) => true
            );

            // Register refresh actions for all tabs that need updating
            TabSelectionManager.Instance.RegisterTabReloadAction("PatientTab", () =>
            {
                var patientVM = Application.Current.Resources["PatientVM"] as PatientViewModel;
                patientVM?.LoadData();
            });


            TabSelectionManager.Instance.RegisterTabReloadAction("AppointmentTab", () =>
            {
                var appointmentVM = Application.Current.Resources["AppointmentVM"] as AppointmentViewModel;
                appointmentVM?.LoadData();
            });

            TabSelectionManager.Instance.RegisterTabReloadAction("StockTab", () =>
            {
                var stockVM = Application.Current.Resources["StockMedicineVM"] as StockMedicineViewModel;
                stockVM?.LoadData();
            });

            TabSelectionManager.Instance.RegisterTabReloadAction("InvoiceTab", () =>
            {
                var invoiceVM = Application.Current.Resources["InvoiceVM"] as InvoiceViewModel;
                invoiceVM?.LoadInvoices();
            });

            TabSelectionManager.Instance.RegisterTabReloadAction("MedicineSellTab", () =>
            {
                var medicineSellVM = Application.Current.Resources["MedicineSellVM"] as MedicineSellViewModel;
                medicineSellVM?.LoadData();
            });

            TabSelectionManager.Instance.RegisterTabReloadAction("DoctorTab", () =>
            {
                var doctorVM = Application.Current.Resources["DoctorVM"] as DoctorViewModel;
                doctorVM?.LoadData();
            });

            TabSelectionManager.Instance.RegisterTabReloadAction("StatisticsTab", () =>
            {
                var statisticsVM = Application.Current.Resources["StatisticsVM"] as StatisticsViewModel;
                statisticsVM?.LoadStatisticsAsync();
            });

            
        }
    }
}