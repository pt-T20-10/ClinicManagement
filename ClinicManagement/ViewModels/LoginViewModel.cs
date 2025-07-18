using ClinicManagement.Models;
using ClinicManagement.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel quản lý chức năng đăng nhập của ứng dụng
    /// Xử lý xác thực người dùng và khởi tạo session làm việc
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        /// <summary>
        /// Trạng thái đăng nhập thành công
        /// True = đã đăng nhập thành công, False = chưa đăng nhập hoặc thất bại
        /// </summary>
        public bool IsLogin { get; set; }

        #region Commands - Các lệnh xử lý sự kiện

        /// <summary>
        /// Command xử lý đăng nhập - nhận tham số là Window để đóng sau khi đăng nhập thành công
        /// </summary>
        public ICommand LoginCommand { get; set; }
        
        /// <summary>
        /// Command xử lý sự kiện thay đổi mật khẩu từ PasswordBox
        /// Cần thiết vì PasswordBox không hỗ trợ binding trực tiếp
        /// </summary>
        public ICommand PasswordChangedCommand { get; set; }
        
        /// <summary>
        /// Command đóng cửa sổ đăng nhập
        /// </summary>
        public ICommand CloseCommand { get; set; }

        #endregion

        #region Properties - Thuộc tính dữ liệu

        /// <summary>
        /// Tên đăng nhập người dùng nhập vào
        /// Có backing field để kích hoạt PropertyChanged khi thay đổi
        /// </summary>
        private string _UserName;
        public string UserName 
        { 
            get => _UserName; 
            set 
            { 
                _UserName = value; 
                OnPropertyChanged(); // Thông báo UI cập nhật khi giá trị thay đổi
            } 
        }

        /// <summary>
        /// Mật khẩu người dùng nhập vào
        /// Được cập nhật thông qua PasswordChangedCommand do hạn chế của PasswordBox
        /// </summary>
        private string _Password;
        public string Password 
        { 
            get => _Password; 
            set 
            { 
                _Password = value; 
                OnPropertyChanged(); // Thông báo UI cập nhật khi giá trị thay đổi
            } 
        }

        #endregion

        /// <summary>
        /// Constructor khởi tạo LoginViewModel
        /// Thiết lập giá trị mặc định và khởi tạo các command
        /// </summary>
        public LoginViewModel()
        {
            // Khởi tạo trạng thái ban đầu
            IsLogin = false;
            UserName = "";
            Password = "";
            
            // Khởi tạo command đăng nhập với async handler
            LoginCommand = new RelayCommand<Window>(
                 async (p) =>
                 {
                     await Login(p); // Gọi phương thức đăng nhập bất đồng bộ
                 },
                 (p) => true // Luôn cho phép thực thi command
            );
            
            // Khởi tạo command xử lý thay đổi mật khẩu
            PasswordChangedCommand = new RelayCommand<PasswordBox>(
                 (p) =>
                 {
                     Password = p.Password; // Lấy mật khẩu từ PasswordBox và cập nhật property
                 },
                 (p) => true // Luôn cho phép thực thi command
            );
            
            // Khởi tạo command đóng cửa sổ
            CloseCommand = new RelayCommand<Window>(
                 (p) =>
                 {
                     p.Close(); // Đóng cửa sổ được truyền vào
                 },
                 (p) => true // Luôn cho phép thực thi command
            );
        }
        
        /// <summary>
        /// Phương thức xử lý đăng nhập bất đồng bộ
        /// Thực hiện xác thực người dùng và khởi tạo session nếu thành công
        /// </summary>
        /// <param name="p">Cửa sổ đăng nhập để đóng sau khi thành công</param>
        async Task Login(Window p)
        {
            // Kiểm tra tham số đầu vào
            if (p == null)
                return;

            // Mã hóa mật khẩu theo chuẩn SHA256 kết hợp với Base64
            // Đầu tiên encode Base64, sau đó hash SHA256 để bảo mật
            string password = HashUtility.ComputeSha256Hash(HashUtility.Base64Encode(Password));
            
            // Tìm kiếm tài khoản trong database với username, password và điều kiện chưa bị xóa
            var account = DataProvider.Instance.Context.Accounts
                .FirstOrDefault(u => u.Username == UserName && 
                                   u.Password == password && 
                                   u.IsDeleted != true);

            // Xử lý kết quả đăng nhập
            if (account != null)
            {
                // Đăng nhập thành công
                IsLogin = true;

                // Khởi tạo ViewModelLocator để chuẩn bị các ViewModel cần thiết
                var viewModelLocator = Application.Current.Resources["ViewModelLocator"] as ViewModelLocator;
                viewModelLocator?.Initialize();

                // Thiết lập tài khoản hiện tại trong MainViewModel
                var mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                if (mainVM != null)
                {
                    // Đặt account về null trước để buộc sự kiện PropertyChanged được kích hoạt
                    // Điều này đảm bảo UI được cập nhật chính xác
                    mainVM.CurrentAccount = null;

                    // Thiết lập tài khoản thực tế
                    mainVM.CurrentAccount = account;

                    // Lấy MainTabControl và đảm bảo tab hợp lệ được chọn
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        // Tìm TabControl chính trong MainWindow
                        var tabControl = LogicalTreeHelper.FindLogicalNode(mainWindow, "MainTabControl") as TabControl;
                        if (tabControl != null)
                        {
                            // Đảm bảo tab được chọn phù hợp với quyền của người dùng
                            mainVM.EnsureValidTabSelected(tabControl);
                        }
                    }
                }

                // Đóng cửa sổ đăng nhập sau khi hoàn tất
                p.Close();
            }
            else
            {
                // Đăng nhập thất bại - hiển thị thông báo lỗi
                MessageBoxService.ShowWarning("Tên đăng nhập hoặc mật khẩu không đúng!", "Thông báo");
            }
        }
    }
}