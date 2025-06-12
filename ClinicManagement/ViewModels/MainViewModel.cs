
using ClinicManagement.Models;
using ClinicManagement.SubWindow;
using ClinicManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static MaterialDesignThemes.Wpf.Theme;

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
        // Thêm flag để hiển thị tất cả tab trong quá trình phát triển
        //private bool _showAllTabsForDevelopment = true;
        //public bool ShowAllTabsForDevelopment
        //{
        //    get => _showAllTabsForDevelopment;
        //    set
        //    {
        //        _showAllTabsForDevelopment = value;
        //        OnPropertyChanged();
        //        UpdateTabVisibility();
        //    }
        //}


        public bool Isloaded = false;
        public string _TotalStock;
        public string TotalStock { get => _TotalStock; set { _TotalStock = value; OnPropertyChanged();  } }
        public ICommand LoadedWindowCommand { get; set; }
        public ICommand AddPatientCommand { get; set; } 
        public ICommand AddAppointmentCommand { get; set; }


        // mọi thứ xử lý sẽ nằm trong này
        public MainViewModel()
        {
            LoadedWindowCommand = new RelayCommand<Window>(

                 (p) =>
                 {
                     Isloaded = true;
                     if (p == null)
                         return;

                     p.Hide();
                     LoginWindow loginWindow = new LoginWindow();
                     loginWindow.ShowDialog();
                     if (loginWindow.DataContext == null)
                         return;

                     var loginVM = loginWindow.DataContext as LoginViewModel;
                     if (loginVM.IsLogin)
                     {
                         p.Show();
                        ;

                     }
                     else
                     {
                         p.Close();
                         CurrentAccount = null;
                     }
                 },
                 (p) => true
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
        }
        // Add this method to your MainViewModel
        public void EnsureValidTabSelected(System.Windows.Controls.TabControl tabControl)
        {
            if (tabControl == null || AllowedTabs.Count == 0)
                return;

            // Check if current tab is allowed
            var currentTabItem = tabControl.SelectedItem as TabItem;
            if (currentTabItem != null)
            {
                string tabName = GetTabName(currentTabItem);
                if (AllowedTabs.Contains(tabName))
                    return; // Current tab is valid
            }

            // Find first visible tab and select it
            foreach (TabItem item in tabControl.Items)
            {
                string tabName = GetTabName(item);
                if (AllowedTabs.Contains(tabName))
                {
                    tabControl.SelectedItem = item;
                    break;
                }
            }
        }
        // Update tab visibility based on current user's role

        // Rồi sửa UpdateTabVisibility
        // Trong file MainViewModel.cs
        private void UpdateTabVisibility()
        {
            AllowedTabs.Clear();

            // Hiển thị tất cả tab trong quá trình phát triển
            AllowedTabs.Add("DashboardTab");
            AllowedTabs.Add("PatientTab");
            AllowedTabs.Add("ExamineTab");
            AllowedTabs.Add("AppointmentTab");
            AllowedTabs.Add("InventoryTab");
            AllowedTabs.Add("InvoiceTab");
            AllowedTabs.Add("MedicineSellTab");
            AllowedTabs.Add("DoctorTab");
            AllowedTabs.Add("StatisticsTab");
            AllowedTabs.Add("SettingsTab");


            // Phần phân quyền thực tế
            if (CurrentAccount != null && !string.IsNullOrEmpty(CurrentAccount.Role))
            {
                string role = CurrentAccount.Role.Trim(); // Loại bỏ khoảng trắng

           

                if (UserRoles.RoleTabPermissions.ContainsKey(role))
                {
                    foreach (var tab in UserRoles.RoleTabPermissions[role])
                    {
                        string cleanTabName = tab.Trim(); // Loại bỏ khoảng trắng
                        AllowedTabs.Add(cleanTabName);
                     
                    }
                }
                else
                {
                    Console.WriteLine($"Role '{role}' not found in permissions");

                    // Hiển thị thông báo lỗi
                    MessageBox.Show($"Không tìm thấy quyền cho vai trò: '{role}'", "Lỗi phân quyền");
                }
            }
            else
            {
                Console.WriteLine("CurrentAccount is null or Role is empty");
            }

        }
        // Helper to get tab name from TabItem
        private string GetTabName(TabItem tabItem)
        {
            // Return TabItem's Name directly
            return tabItem.Name;
        }



    }
}       
