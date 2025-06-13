using ClinicManagement.Models;
using ClinicManagement.ViewModels;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public class ChangeDoctorPasswordViewModel : BaseViewModel
{
    // Account được truyền từ MainViewModel
    private Account _account;
    public Account Account
    {
        get => _account;
        set { _account = value; OnPropertyChanged(); }
    }

    // Password properties
    private string _currentPassword;
    public string CurrentPassword
    {
        get => _currentPassword;
        set { _currentPassword = value; OnPropertyChanged(); }
    }

    private string _newPassword;
    public string NewPassword
    {
        get => _newPassword;
        set { _newPassword = value; OnPropertyChanged(); }
    }

    private string _confirmPassword;
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set { _confirmPassword = value; OnPropertyChanged(); }
    }

    // Error properties
    private string _currentPasswordError;
    public string CurrentPasswordError
    {
        get => _currentPasswordError;
        set { _currentPasswordError = value; OnPropertyChanged(); }
    }

    private string _newPasswordError;
    public string NewPasswordError
    {
        get => _newPasswordError;
        set { _newPasswordError = value; OnPropertyChanged(); }
    }

    private string _confirmPasswordError;
    public string ConfirmPasswordError
    {
        get => _confirmPasswordError;
        set { _confirmPasswordError = value; OnPropertyChanged(); }
    }

    // Commands
    public ICommand CurrentPasswordChangedCommand { get; set; }
    public ICommand NewPasswordChangedCommand { get; set; }
    public ICommand ConfirmPasswordChangedCommand { get; set; }
    public ICommand ChangePasswordCommand { get; set; }
    public ICommand CancelCommand { get; set; }
    public ICommand LoadedWindowCommand { get; set; }

    // Constructor
    public ChangeDoctorPasswordViewModel()
    {
        CurrentPasswordChangedCommand = new RelayCommand<PasswordBox>(
            p => {
                if (p != null)
                {
                    CurrentPassword = p.Password;
                    ValidateCurrentPassword();
                }
            });

        NewPasswordChangedCommand = new RelayCommand<PasswordBox>(
            p => {
                if (p != null)
                {
                    NewPassword = p.Password;
                    ValidateNewPassword();
                    // Also validate confirm password since it depends on new password
                    ValidateConfirmPassword();
                }
            });

        ConfirmPasswordChangedCommand = new RelayCommand<PasswordBox>(
            p => {
                if (p != null)
                {
                    ConfirmPassword = p.Password;
                    ValidateConfirmPassword();
                }
            });

        ChangePasswordCommand = new RelayCommand<object>(
            p => ExecuteChangePassword(),
            p => CanExecuteChangePassword());

        CancelCommand = new RelayCommand<object>(
            p => 
            {
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            });

        LoadedWindowCommand = new RelayCommand<Window>(
            p => {
                // Lấy tài khoản từ MainViewModel khi window được load
                if (Account == null)
                {
                    var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                    if (mainVM != null && mainVM.CurrentAccount != null)
                    {
                        Account = mainVM.CurrentAccount;
                    }
                }
            });
    }

    // Validation methods
    private void ValidateCurrentPassword()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            CurrentPasswordError = "Vui lòng nhập mật khẩu hiện tại";
            return;
        }

        // Kiểm tra mật khẩu hiện tại có đúng không
        if (Account != null)
        {
            string hashedPassword = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(CurrentPassword));
            if (Account.Password != hashedPassword)
            {
                CurrentPasswordError = "Mật khẩu hiện tại không đúng";
                return;
            }
        }

        CurrentPasswordError = null;
    }

    private void ValidateNewPassword()
    {
        // Regex tổng hợp:
        string pattern = @"^(?=.*[A-Z])(?=.*[\W_]).{8,}$";

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            NewPasswordError = "Vui lòng nhập mật khẩu mới";
        }
        else if (NewPassword == CurrentPassword)
        {
            NewPasswordError = "Mật khẩu mới không được trùng với mật khẩu hiện tại";
        }
        else if (!Regex.IsMatch(NewPassword, pattern))
        {
            NewPasswordError = "Mật khẩu phải có ít nhất 8 ký tự, chứa chữ in hoa và ký tự đặc biệt";
        }
        else
        {
            NewPasswordError = null;
        }
    }


    private void ValidateConfirmPassword()
    {
        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ConfirmPasswordError = "Vui lòng xác nhận mật khẩu mới";
        }
        else if (ConfirmPassword != NewPassword)
        {
            ConfirmPasswordError = "Mật khẩu xác nhận không khớp với mật khẩu mới";
        }
        else
        {
            ConfirmPasswordError = null;
        }
    }

    private bool CanExecuteChangePassword()
    {
        ValidateCurrentPassword();
        ValidateNewPassword();
        ValidateConfirmPassword();

        return string.IsNullOrEmpty(CurrentPasswordError) &&
               string.IsNullOrEmpty(NewPasswordError) &&
               string.IsNullOrEmpty(ConfirmPasswordError) &&
               !string.IsNullOrWhiteSpace(CurrentPassword) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               !string.IsNullOrWhiteSpace(ConfirmPassword);
    }

    private void ExecuteChangePassword()
    {
        try
        {
            if (Account == null)
            {
                MessageBox.Show("Không tìm thấy thông tin tài khoản!",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Double-check mật khẩu hiện tại
            string hashedCurrentPassword = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(CurrentPassword));
            if (Account.Password != hashedCurrentPassword)
            {
                MessageBox.Show("Mật khẩu hiện tại không đúng!",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Tìm tài khoản trong database
            var accountToUpdate = DataProvider.Instance.Context.Accounts
                .FirstOrDefault(a => a.Username == Account.Username && a.IsDeleted != true);

            if (accountToUpdate != null)
            {
                // Hash mật khẩu mới và lưu vào database
                string hashedNewPassword = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(NewPassword));
                accountToUpdate.Password = hashedNewPassword;

                // Lưu thay đổi vào database
                DataProvider.Instance.Context.SaveChanges();

                // Cập nhật lại Account trong memory
                Account.Password = hashedNewPassword;

                // Hiển thị thông báo thành công
                MessageBox.Show("Đổi mật khẩu thành công!",
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                // Đóng window
                Window window = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.DataContext == this);
                window?.Close();
            }
            else
            {
                MessageBox.Show("Không tìm thấy tài khoản trong hệ thống!",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi khi đổi mật khẩu: {ex.Message}",
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Phương thức public để truyền Account từ bên ngoài
    public void SetAccount(Account account)
    {
        Account = account;
    }
}
