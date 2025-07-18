using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public class ChangeDoctorPasswordViewModel : BaseViewModel
{
    #region Properties - Các thuộc tính của ViewModel

    // Theo dõi các trường người dùng đã tương tác để chỉ validate khi cần thiết
    private HashSet<string> _touchedFields = new HashSet<string>();

    #region Thông tin tài khoản

    // Tài khoản được truyền từ MainViewModel - chứa thông tin người dùng hiện tại
    private Account _account;
    public Account Account
    {
        get => _account;
        set { _account = value; OnPropertyChanged(); }
    }

    #endregion

    #region Các thuộc tính mật khẩu

    // Mật khẩu hiện tại - trường bắt buộc để xác thực
    private string _currentPassword;
    public string CurrentPassword
    {
        get => _currentPassword;
        set { _currentPassword = value; OnPropertyChanged(); }
    }

    // Mật khẩu mới - phải đáp ứng các yêu cầu bảo mật
    private string _newPassword;
    public string NewPassword
    {
        get => _newPassword;
        set { _newPassword = value; OnPropertyChanged(); }
    }

    // Xác nhận mật khẩu mới - phải khớp với mật khẩu mới
    private string _confirmPassword;
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set { _confirmPassword = value; OnPropertyChanged(); }
    }

    #endregion

    #region Các thuộc tính hiển thị lỗi

    // Thông báo lỗi cho mật khẩu hiện tại
    private string _currentPasswordError;
    public string CurrentPasswordError
    {
        get => _currentPasswordError;
        set { _currentPasswordError = value; OnPropertyChanged(); }
    }

    // Thông báo lỗi cho mật khẩu mới
    private string _newPasswordError;
    public string NewPasswordError
    {
        get => _newPasswordError;
        set { _newPasswordError = value; OnPropertyChanged(); }
    }

    // Thông báo lỗi cho xác nhận mật khẩu
    private string _confirmPasswordError;
    public string ConfirmPasswordError
    {
        get => _confirmPasswordError;
        set { _confirmPasswordError = value; OnPropertyChanged(); }
    }

    #endregion

    #region Commands - Các lệnh xử lý sự kiện

    public ICommand CurrentPasswordChangedCommand { get; set; }    // Lệnh khi mật khẩu hiện tại thay đổi
    public ICommand NewPasswordChangedCommand { get; set; }        // Lệnh khi mật khẩu mới thay đổi
    public ICommand ConfirmPasswordChangedCommand { get; set; }    // Lệnh khi xác nhận mật khẩu thay đổi
    public ICommand ChangePasswordCommand { get; set; }            // Lệnh thực hiện đổi mật khẩu
    public ICommand CancelCommand { get; set; }                   // Lệnh hủy và reset form
    public ICommand LoadedWindowCommand { get; set; }             // Lệnh khi cửa sổ được tải

    #endregion

    #endregion

    /// <summary>
    /// Constructor khởi tạo ViewModel và các command
    /// </summary>
    public ChangeDoctorPasswordViewModel()
    {
        InitializeCommands();
    }

    #region Initialization - Khởi tạo

    /// <summary>
    /// Khởi tạo tất cả các command cho ViewModel
    /// </summary>
    private void InitializeCommands()
    {
        // Command xử lý khi mật khẩu hiện tại thay đổi
        CurrentPasswordChangedCommand = new RelayCommand<PasswordBox>(
          p => {
              if (p != null)
              {
                  // Đánh dấu trường đã được tương tác
                  _touchedFields.Add(nameof(CurrentPassword));
                  CurrentPassword = p.Password;
                  ValidateCurrentPassword();
              }
          });

        // Command xử lý khi mật khẩu mới thay đổi
        NewPasswordChangedCommand = new RelayCommand<PasswordBox>(
            p => {
                if (p != null)
                {
                    // Đánh dấu trường đã được tương tác
                    _touchedFields.Add(nameof(NewPassword));
                    NewPassword = p.Password;
                    ValidateNewPassword();
                    
                    // Cũng validate xác nhận mật khẩu nếu đã được nhập
                    if (_touchedFields.Contains(nameof(ConfirmPassword)))
                        ValidateConfirmPassword();
                }
            });

        // Command xử lý khi xác nhận mật khẩu thay đổi
        ConfirmPasswordChangedCommand = new RelayCommand<PasswordBox>(
            p => {
                if (p != null)
                {
                    // Đánh dấu trường đã được tương tác
                    _touchedFields.Add(nameof(ConfirmPassword));
                    ConfirmPassword = p.Password;
                    ValidateConfirmPassword();
                }
            });

        // Command thực hiện đổi mật khẩu
        ChangePasswordCommand = new RelayCommand<object>(
            p => ExecuteChangePassword(),
            p => CanExecuteChangePassword());

        // Command hủy và reset form
        CancelCommand = new RelayCommand<object>(
            p => 
            {
                // Xóa tất cả mật khẩu đã nhập
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            });

        // Command khi cửa sổ được tải
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

    #endregion

    #region Validation Methods - Các phương thức xác thực

    /// <summary>
    /// Xác thực mật khẩu hiện tại
    /// Kiểm tra không được để trống và phải khớp với mật khẩu trong database
    /// </summary>
    private void ValidateCurrentPassword()
    {
        // Chỉ validate nếu trường đã được tương tác
        if (!_touchedFields.Contains(nameof(CurrentPassword)))
        {
            CurrentPasswordError = null;
            return;
        }

        // Kiểm tra không được để trống
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            CurrentPasswordError = "Vui lòng nhập mật khẩu hiện tại";
            return;
        }

        // Kiểm tra mật khẩu hiện tại có đúng không
        if (Account != null)
        {
            // Hash mật khẩu nhập vào để so sánh với database
            string hashedPassword = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(CurrentPassword));
            if (Account.Password != hashedPassword)
            {
                CurrentPasswordError = "Mật khẩu hiện tại không đúng";
                return;
            }
        }

        // Xóa lỗi nếu validation thành công
        CurrentPasswordError = null;
    }

    /// <summary>
    /// Xác thực mật khẩu mới
    /// Kiểm tra độ mạnh mật khẩu theo regex pattern
    /// </summary>
    private void ValidateNewPassword()
    {
        // Chỉ validate nếu trường đã được tương tác
        if (!_touchedFields.Contains(nameof(NewPassword)))
        {
            NewPasswordError = null;
            return;
        }

        // Regex kiểm tra mật khẩu mạnh: ít nhất 8 ký tự, có chữ in hoa và ký tự đặc biệt
        string pattern = @"^(?=.*[A-Z])(?=.*[\W_]).{8,}$";

        // Kiểm tra không được để trống
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            NewPasswordError = "Vui lòng nhập mật khẩu mới";
        }
        // Kiểm tra không được trùng với mật khẩu hiện tại
        else if (NewPassword == CurrentPassword)
        {
            NewPasswordError = "Mật khẩu mới không được trùng với mật khẩu hiện tại";
        }
        // Kiểm tra độ mạnh mật khẩu
        else if (!Regex.IsMatch(NewPassword, pattern))
        {
            NewPasswordError = "Mật khẩu phải có ít nhất 8 ký tự, chứa chữ in hoa và ký tự đặc biệt";
        }
        else
        {
            // Xóa lỗi nếu validation thành công
            NewPasswordError = null;
        }
    }

    /// <summary>
    /// Xác thực xác nhận mật khẩu
    /// Kiểm tra phải khớp với mật khẩu mới
    /// </summary>
    private void ValidateConfirmPassword()
    {
        // Chỉ validate nếu trường đã được tương tác
        if (!_touchedFields.Contains(nameof(ConfirmPassword)))
        {
            ConfirmPasswordError = null;
            return;
        }

        // Kiểm tra không được để trống
        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ConfirmPasswordError = "Vui lòng xác nhận mật khẩu mới";
        }
        // Kiểm tra phải khớp với mật khẩu mới
        else if (ConfirmPassword != NewPassword)
        {
            ConfirmPasswordError = "Mật khẩu xác nhận không khớp với mật khẩu mới";
        }
        else
        {
            // Xóa lỗi nếu validation thành công
            ConfirmPasswordError = null;
        }
    }

    /// <summary>
    /// Kiểm tra điều kiện để có thể thực hiện đổi mật khẩu
    /// </summary>
    /// <returns>True nếu có thể đổi mật khẩu, False nếu chưa đủ điều kiện</returns>
    private bool CanExecuteChangePassword()
    {
        // Chỉ validate các trường đã được tương tác
        if (_touchedFields.Contains(nameof(CurrentPassword)))
            ValidateCurrentPassword();

        if (_touchedFields.Contains(nameof(NewPassword)))
            ValidateNewPassword();

        if (_touchedFields.Contains(nameof(ConfirmPassword)))
            ValidateConfirmPassword();

        // Chỉ cho phép đổi mật khẩu khi tất cả điều kiện được thỏa mãn
        return string.IsNullOrEmpty(CurrentPasswordError) &&
               string.IsNullOrEmpty(NewPasswordError) &&
               string.IsNullOrEmpty(ConfirmPasswordError) &&
               !string.IsNullOrWhiteSpace(CurrentPassword) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               !string.IsNullOrWhiteSpace(ConfirmPassword);
    }

    #endregion

    #region Command Execution - Thực thi các lệnh

    /// <summary>
    /// Thực hiện đổi mật khẩu
    /// Sử dụng transaction để đảm bảo tính toàn vẹn dữ liệu
    /// </summary>
    private void ExecuteChangePassword()
    {
        try
        {
            // Kiểm tra xem tài khoản đã được khởi tạo hay chưa
            if (Account == null)
            {
                MessageBoxService.ShowError("Không tìm thấy thông tin tài khoản!", "Lỗi");
                return;
            }

            // Kiểm tra lại mật khẩu hiện tại một lần nữa để đảm bảo an toàn
            string hashedCurrentPassword = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(CurrentPassword));
            if (Account.Password != hashedCurrentPassword)
            {
                MessageBoxService.ShowError("Mật khẩu hiện tại không đúng!", "Lỗi");
                return;
            }

            // Sử dụng transaction để đảm bảo tính toàn vẹn của dữ liệu
            using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
            {
                try
                {
                    // Tìm tài khoản trong cơ sở dữ liệu
                    var accountToUpdate = DataProvider.Instance.Context.Accounts
                        .FirstOrDefault(a => a.Username == Account.Username && a.IsDeleted != true);

                    if (accountToUpdate == null)
                    {
                        MessageBoxService.ShowError("Không tìm thấy tài khoản trong hệ thống!", "Lỗi");
                        return;
                    }

                    // Hash mật khẩu mới và lưu vào cơ sở dữ liệu
                    string hashedNewPassword = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(NewPassword));
                    accountToUpdate.Password = hashedNewPassword;

                    // Ghi nhật ký đổi mật khẩu (nếu cần thiết)
                    // TODO: Có thể thêm code ghi nhật ký ở đây

                    // Lưu thay đổi vào cơ sở dữ liệu
                    DataProvider.Instance.Context.SaveChanges();

                    // Hoàn tất transaction
                    transaction.Commit();

                    // Cập nhật lại Account trong memory để đảm bảo đồng bộ
                    Account.Password = hashedNewPassword;

                    // Hiển thị thông báo thành công
                    MessageBoxService.ShowSuccess("Đổi mật khẩu thành công!", "Thông báo");

                    // Tự động đóng cửa sổ đổi mật khẩu
                    Window window = Application.Current.Windows.OfType<Window>()
                        .FirstOrDefault(w => w.DataContext == this);
                    window?.Close();
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi xảy ra, hoàn tác lại toàn bộ thay đổi
                    transaction.Rollback();

                    // Ghi log lỗi để debug
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi đổi mật khẩu: {ex.Message}");

                    // Ném lại ngoại lệ để được xử lý ở khối catch bên ngoài
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            // Xử lý và hiển thị lỗi cho người dùng
            MessageBoxService.ShowError($"Lỗi khi đổi mật khẩu: {ex.Message}", "Lỗi");
        }
    }

    #endregion

    #region Public Methods - Các phương thức công khai

    /// <summary>
    /// Phương thức công khai để truyền Account từ bên ngoài
    /// Thường được gọi từ ViewModel cha khi khởi tạo cửa sổ đổi mật khẩu
    /// </summary>
    /// <param name="account">Tài khoản cần đổi mật khẩu</param>
    public void SetAccount(Account account)
    {
        Account = account;
    }

    #endregion
}
