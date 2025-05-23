
using ClinicManagement.Models;
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
                  (p) =>
                 {
                    //await Login(p);
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
        //async Task Login(Window p)
        //{
        //    if(p==null)
        //        return;

        //    string password = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(Password));
        //    var accCount = DataProvider.Instance.Context.Users
        //        .Count(u => u.UserName == UserName && u.Password == password);



        //    if (accCount > 0)
        //    {
        //        IsLogin = true;
        //        p.Close();
        //    }
        //    else
        //    {
        //        IsLogin = false;
        //        MessageBox.Show("Sai tài khoản hoặc mật khẩu!");
                    
        //    }

           
        //}
    

    }
}