
using ClinicManagement.Models;
using ClinicManagement.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace ClinicManagement.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {

        public bool IsLogin { get; set; }

        public ICommand LoginCommand { get; set; }
        public ICommand PasswordChangedCommand { get; set; }
        public ICommand CloseCommand { get; set; }

        private string _UserName;
        public string UserName { get => _UserName; set { _UserName = value; OnPropertyChanged(); } }

        private string _Password;
        public string Password { get => _Password; set { _Password = value; OnPropertyChanged(); } }

        // mọi thứ xử lý sẽ nằm trong này
        public LoginViewModel()
        {
            IsLogin = false;
            UserName = "";
            Password = "";
            LoginCommand = new RelayCommand<Window>(
                 async (p) =>
                 {
                     await Login(p);
                 },
                 (p) => true
            );
            PasswordChangedCommand = new RelayCommand<PasswordBox>(
                 (p) =>
                 {
                     Password = p.Password;
                 },
                 (p) => true
            );
            CloseCommand = new RelayCommand<Window>(
                 (p) =>
                 {
                     p.Close();
                 },
                 (p) => true
            );

        }
        async Task Login(Window p)
        {
            if (p == null)
                return;

            string password = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(Password));
            var account = DataProvider.Instance.Context.Accounts
                .FirstOrDefault(u => u.Username == UserName && u.Password == password && u.IsDeleted != true);

            if (account != null)
            {
                IsLogin = true;

                // Set the current account in MainViewModel
                var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                if (mainVM != null)
                {
                    // Make sure to set the account to null first to force the property change event
                    mainVM.CurrentAccount = null;

                    // Now set the actual account
                    mainVM.CurrentAccount = account;

                    // Show welcome message here where we know CurrentAccount is properly set
                    MessageBoxService.ShowInfo($"Chào mừng {account.Username}!", "Thông báo đăng nhập thành công");

                    // Get the MainTabControl and ensure a valid tab is selected
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        var tabControl = LogicalTreeHelper.FindLogicalNode(mainWindow, "MainTabControl") as TabControl;
                        if (tabControl != null)
                        {
                            mainVM.EnsureValidTabSelected(tabControl);
                        }
                    }
                }

                p.Close();
            }
            else
            {
                MessageBoxService.ShowWarning("Tên đăng nhập hoặc mật khẩu không đúng!", "Thông báo");
            }
        }

    }
}