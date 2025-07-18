using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel quản lý cài đặt hệ thống và thông tin tài khoản cá nhân
    /// Cung cấp chức năng xem/chỉnh sửa thông tin nhân viên, đổi mật khẩu và đăng xuất
    /// Triển khai IDataErrorInfo để validation dữ liệu thông tin cá nhân
    /// Hỗ trợ touched-based validation để UX tốt hơn
    /// </summary>
    public class SettingViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Doctor Information Properties

        /// <summary>
        /// Set theo dõi các field đã được người dùng tương tác
        /// Chỉ validate các field này để tránh hiển thị lỗi ngay khi mở form
        /// </summary>
        private HashSet<string> _touchedFields = new HashSet<string>();

        /// <summary>
        /// Cờ bật/tắt validation
        /// True = thực hiện validation, False = bỏ qua validation
        /// </summary>
        private bool _isValidating = false;

        /// <summary>
        /// ID nhân viên hiện tại - chỉ đọc
        /// </summary>
        private int _StaffId;
        public int StaffId
        {
            get => _StaffId;
            set
            {
                _StaffId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Họ và tên nhân viên - có validation và theo dõi touched state
        /// Trường bắt buộc với độ dài tối thiểu 2 ký tự
        /// </summary>
        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_fullName);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    // Thêm vào touched fields khi người dùng nhập giá trị không rỗng
                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(FullName));
                    // Xóa khỏi touched fields khi field trở thành rỗng
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(FullName));

                    _fullName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Chuyên khoa được chọn cho nhân viên
        /// Chỉ áp dụng cho bác sĩ, các vai trò khác có thể để null
        /// </summary>
        private DoctorSpecialty _selectedSpecialty;
        public DoctorSpecialty SelectedSpecialty
        {
            get => _selectedSpecialty;
            set
            {
                _selectedSpecialty = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách chuyên khoa có sẵn trong hệ thống
        /// Sử dụng cho ComboBox chọn chuyên khoa
        /// </summary>
        private ObservableCollection<DoctorSpecialty> _specialtyList = new();
        public ObservableCollection<DoctorSpecialty> SpecialtyList
        {
            get => _specialtyList;
            set
            {
                _specialtyList = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Số điện thoại nhân viên - có validation và theo dõi touched state
        /// Trường bắt buộc với định dạng số điện thoại Việt Nam
        /// </summary>
        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set
            {
                if (_phone != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_phone);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(Phone));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(Phone));

                    _phone = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Lịch làm việc của nhân viên - có validation định dạng phức tạp
        /// Hỗ trợ nhiều định dạng: "T2-T6: 8h-17h", "T2, T3: 7h-13h", "T2: 8h-12h, 13h-17h"
        /// </summary>
        private string _schedule = string.Empty;
        public string Schedule
        {
            get => _schedule;
            set
            {
                if (_schedule != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_schedule);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(Schedule));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(Schedule));

                    _schedule = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Địa chỉ nhân viên - trường tùy chọn, không có validation
        /// </summary>
        private string _address = string.Empty;
        public string Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Đường dẫn tới chứng chỉ/bằng cấp - có validation và theo dõi touched state
        /// Trường bắt buộc cho bác sĩ
        /// </summary>
        private string _certificateLink = string.Empty;
        public string CertificateLink
        {
            get => _certificateLink;
            set
            {
                if (_certificateLink != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_certificateLink);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(CertificateLink));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(CertificateLink));

                    _certificateLink = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Tên đăng nhập - chỉ đọc, lấy từ tài khoản hiện tại
        /// </summary>
        private string _userName = string.Empty;
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Vai trò/chức vụ - chỉ đọc, lấy từ tài khoản hiện tại
        /// </summary>
        private string _role = string.Empty;
        public string Role
        {
            get => _role;
            set
            {
                _role = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Email nhân viên - có validation định dạng email và theo dõi touched state
        /// Trường tùy chọn nhưng nếu nhập phải đúng định dạng
        /// </summary>
        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    bool wasEmpty = string.IsNullOrWhiteSpace(_email);
                    bool isEmpty = string.IsNullOrWhiteSpace(value);

                    if (wasEmpty && !isEmpty)
                        _touchedFields.Add(nameof(Email));
                    else if (!wasEmpty && isEmpty)
                        _touchedFields.Remove(nameof(Email));

                    _email = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Tham chiếu đến MainViewModel để thực hiện đăng xuất
        /// </summary>
        private MainViewModel _mainViewModel;

        #endregion

        #region Commands

        /// <summary>
        /// Lệnh cập nhật thông tin nhân viên
        /// </summary>
        public ICommand UpdateDoctorInfoCommand { get; set; }

        /// <summary>
        /// Lệnh đổi mật khẩu - mở cửa sổ đổi mật khẩu
        /// </summary>
        public ICommand ChangePasswordCommand { get; set; }

        /// <summary>
        /// Lệnh xử lý khi control được tải - làm mới dữ liệu
        /// </summary>
        public ICommand LoadedCommand { get; set; }

        /// <summary>
        /// Lệnh đăng xuất - delegate đến MainViewModel
        /// </summary>
        public ICommand SignOutCommand { get; set; }

        /// <summary>
        /// Lệnh làm mới thông tin từ database
        /// </summary>
        public ICommand RefreshDataCommand { get; set; }

        #endregion

        /// <summary>
        /// Tài khoản hiện tại đang đăng nhập
        /// </summary>
        private Account _currentAccount;

        /// <summary>
        /// Thông tin nhân viên tương ứng với tài khoản hiện tại
        /// </summary>
        private Staff _currentDoctor;

        /// <summary>
        /// Constructor khởi tạo SettingViewModel
        /// Thiết lập commands và đăng ký lắng nghe thay đổi CurrentAccount từ MainViewModel
        /// </summary>
        public SettingViewModel()
        {
            _mainViewModel = Application.Current.Resources["MainVM"] as MainViewModel;
            // Khởi tạo commands
            InitializeCommands();

            // Tải thông tin nhân viên khi MainViewModel's CurrentAccount thay đổi
            MainViewModel mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
            if (mainVM != null)
            {
                // Đăng ký lắng nghe sự kiện PropertyChanged của MainViewModel
                mainVM.PropertyChanged += (s, e) => {
                    if (e.PropertyName == nameof(mainVM.CurrentAccount))
                    {
                        LoadDoctorInformation(mainVM.CurrentAccount);
                    }
                };

                // Tải dữ liệu ban đầu nếu CurrentAccount đã được set
                if (mainVM.CurrentAccount != null)
                {
                    LoadDoctorInformation(mainVM.CurrentAccount);
                }
            }
        }

        /// <summary>
        /// Khởi tạo tất cả các command với logic thực thi và điều kiện kích hoạt
        /// </summary>
        private void InitializeCommands()
        {
            // Command cập nhật thông tin nhân viên - luôn có thể thực thi
            UpdateDoctorInfoCommand = new RelayCommand<object>(
                p => UpdateDoctorInfo(),
                p => true
            );

            // Command đổi mật khẩu - chỉ khi có tài khoản hiện tại
            ChangePasswordCommand = new RelayCommand<object>(
                p => ChangePassword(),
                p => _currentAccount != null
            );

            // Command xử lý khi control được tải - làm mới dữ liệu
            LoadedCommand = new RelayCommand<object>(
                p => {
                    // Refresh dữ liệu khi control được tải
                    MainViewModel mainVM = Application.Current.Resources["MainVM"] as MainViewModel;
                    if (mainVM != null && mainVM.CurrentAccount != null)
                    {
                        LoadDoctorInformation(mainVM.CurrentAccount);
                    }
                },
                p => true
            );

            // Command đăng xuất - delegate đến MainViewModel.SignOutCommand
            SignOutCommand = new RelayCommand<object>(
                p => ExecuteSignOut(),
                p => CanExecuteSignOut()
            );

            // Command làm mới thông tin - chỉ khi có tài khoản hiện tại
            RefreshDataCommand = new RelayCommand<object>(
       p => RefreshDoctorInformation(),
       p => _currentAccount != null
   );
        }

        /// <summary>
        /// Kiểm tra điều kiện có thể đăng xuất
        /// Yêu cầu có MainViewModel và tài khoản hiện tại
        /// </summary>
        /// <returns>True nếu có thể đăng xuất</returns>
        private bool CanExecuteSignOut()
        {
            return _mainViewModel != null && _mainViewModel.CurrentAccount != null;
        }

        /// <summary>
        /// Thực hiện đăng xuất với xác nhận từ người dùng
        /// Xử lý toàn bộ flow đăng xuất và đăng nhập lại
        /// Delegate logic chính đến MainViewModel.SignOut()
        /// </summary>
        private void ExecuteSignOut()
        {
            try
            {
                // Lấy MainViewModel reference mới nhất
                var mainViewModel = Application.Current.Resources["MainVM"] as MainViewModel;

                if (mainViewModel == null || mainViewModel.CurrentAccount == null)
                {
                    MessageBoxService.ShowWarning("Không thể đăng xuất vào lúc này.", "Thông báo");
                    return;
                }

                // Hiển thị hộp thoại xác nhận
                bool result = MessageBoxService.ShowQuestion(
                    "Bạn có chắc chắn muốn đăng xuất?",
                    "Xác nhận đăng xuất");

                if (result)
                {
                    // Gọi phương thức SignOut của MainViewModel thay vì duplicate logic
                    mainViewModel.SignOut();

                    // Lấy MainWindow
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        // Ẩn MainWindow
                        mainWindow.Hide();

                        // Hiển thị màn hình đăng nhập
                        LoginWindow loginWindow = new LoginWindow();
                        loginWindow.ShowDialog();

                        // Lấy MainViewModel reference mới sau khi đăng nhập
                        mainViewModel = Application.Current.Resources["MainVM"] as MainViewModel;

                        // Kiểm tra kết quả đăng nhập
                        var loginVM = loginWindow.DataContext as LoginViewModel;

                        // Nếu đăng nhập thành công (IsLogin = true và CurrentAccount khác null), hiển thị lại MainWindow
                        if (loginVM != null && loginVM.IsLogin && mainViewModel != null && mainViewModel.CurrentAccount != null)
                        {
                            mainWindow.Show();
                            // Không cần hiển thị welcome message ở đây - đã được hiển thị trong LoginViewModel.Login
                        }
                        else
                        {
                            // Nếu đã ấn Cancel hoặc đóng cửa sổ đăng nhập mà không đăng nhập lại thành công
                            mainWindow.Close();
                            Application.Current.Shutdown();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi đăng xuất: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Tải thông tin nhân viên từ tài khoản đăng nhập
        /// Cập nhật tất cả thuộc tính UI và danh sách chuyên khoa
        /// </summary>
        /// <param name="account">Tài khoản cần tải thông tin</param>
        public void LoadDoctorInformation(Account account)
        {
            if (account == null) return;

            _currentAccount = account;

            try
            {
                // Tải thông tin nhân viên từ tài khoản
                _currentDoctor = DataProvider.Instance.Context.Staffs
                    .FirstOrDefault(d => d.StaffId == account.StaffId && d.IsDeleted != true);

                if (_currentDoctor != null)
                {
                    // Cập nhật thông tin cá nhân
                    StaffId = _currentDoctor.StaffId;
                    FullName = _currentDoctor.FullName;
                    Phone = _currentDoctor.Phone ?? string.Empty;
                    Email = _currentDoctor.Email ?? string.Empty; // Thêm dòng này để tải Email
                    Schedule = _currentDoctor.Schedule ?? string.Empty;
                    Address = _currentDoctor.Address ?? string.Empty;
                    CertificateLink = _currentDoctor.CertificateLink ?? string.Empty;

                    // Tải danh sách chuyên khoa
                    SpecialtyList = new ObservableCollection<DoctorSpecialty>(
                        DataProvider.Instance.Context.DoctorSpecialties
                        .Where(s => s.IsDeleted != true)
                        .ToList()
                    );

                    // Thiết lập chuyên khoa được chọn
                    SelectedSpecialty = SpecialtyList.FirstOrDefault(s => s.SpecialtyId == _currentDoctor.SpecialtyId);
                }

                // Thiết lập thông tin tài khoản
                UserName = account.Username;
                Role = account.Role ?? string.Empty;

                // Cập nhật trạng thái command
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải thông tin bác sĩ: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên vào cơ sở dữ liệu
        /// Bao gồm validation đầy đủ, kiểm tra trùng lặp và sử dụng transaction
        /// </summary>
        private void UpdateDoctorInfo()
        {
            try
            {
                if (_currentDoctor == null)
                {
                    MessageBoxService.ShowError("Không tìm thấy thông tin bác sĩ!", "Lỗi");
                    return;
                }

                // Bật validation cho tất cả field
                _isValidating = true;
                _touchedFields.Add(nameof(FullName));
                _touchedFields.Add(nameof(Phone));
                _touchedFields.Add(nameof(Email));
                _touchedFields.Add(nameof(Schedule));
                _touchedFields.Add(nameof(CertificateLink));

                // Kích hoạt validation cho các field bắt buộc
                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(Phone));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(Schedule));
                OnPropertyChanged(nameof(CertificateLink));

                // Kiểm tra có lỗi validation không
                if (HasErrors)
                {
                    MessageBoxService.ShowWarning("Vui lòng sửa các lỗi nhập liệu trước khi cập nhật thông tin.", "Lỗi dữ liệu");
                    return;
                }

                // Sử dụng transaction để đảm bảo tính nhất quán dữ liệu
                using (var transaction = DataProvider.Instance.Context.Database.BeginTransaction())
                {
                    try
                    {
                        // Kiểm tra email đã tồn tại chưa (trừ nhân viên hiện tại)
                        if (!string.IsNullOrWhiteSpace(Email))
                        {
                            bool emailExists = DataProvider.Instance.Context.Staffs
                                .Any(d => d.Email == Email.Trim() && d.StaffId != _currentDoctor.StaffId && d.IsDeleted == false);

                            if (emailExists)
                            {
                                MessageBoxService.ShowError("Email này đã được sử dụng bởi một nhân viên khác.", "Lỗi dữ liệu");
                                return;
                            }
                        }

                        // Kiểm tra số điện thoại đã tồn tại chưa (trừ nhân viên hiện tại)
                        bool phoneExists = DataProvider.Instance.Context.Staffs
                            .Any(d => d.Phone == Phone.Trim() && d.StaffId != _currentDoctor.StaffId && d.IsDeleted == false);

                        if (phoneExists)
                        {
                            MessageBoxService.ShowError("Số điện thoại này đã được sử dụng bởi một nhân viên khác.", "Lỗi dữ liệu");
                            return;
                        }

                        // Lấy bản ghi nhân viên từ database
                        var doctorToUpdate = DataProvider.Instance.Context.Staffs
                            .FirstOrDefault(d => d.StaffId == _currentDoctor.StaffId);

                        if (doctorToUpdate != null)
                        {
                            // Cập nhật các thuộc tính
                            doctorToUpdate.FullName = FullName.Trim();
                            doctorToUpdate.SpecialtyId = SelectedSpecialty?.SpecialtyId;
                            doctorToUpdate.CertificateLink = CertificateLink?.Trim();
                            doctorToUpdate.Email = Email?.Trim();
                            doctorToUpdate.Schedule = Schedule?.Trim();
                            doctorToUpdate.Phone = Phone?.Trim();
                            doctorToUpdate.Address = Address?.Trim();

                            // Lưu thay đổi
                            DataProvider.Instance.Context.SaveChanges();

                            // Commit transaction
                            transaction.Commit();

                            // Cập nhật bản sao local
                            _currentDoctor = doctorToUpdate;

                            MessageBoxService.ShowSuccess("Đã cập nhật thông tin thành công!", "Thành công");
                        }
                        else
                        {
                            MessageBoxService.ShowError("Không tìm thấy thông tin trong cơ sở dữ liệu!", "Lỗi");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction nếu có lỗi xảy ra
                        transaction.Rollback();
                        throw; // Ném lại để được xử lý bởi catch block bên ngoài
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Đã xảy ra lỗi khi cập nhật thông tin: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Kiểm tra xem có lỗi validation nào không
        /// Kết hợp tất cả field có thể có lỗi
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return !string.IsNullOrEmpty(this[nameof(FullName)]) ||
                       !string.IsNullOrEmpty(this[nameof(Phone)]) ||
                       !string.IsNullOrEmpty(this[nameof(Email)]) ||
                       !string.IsNullOrEmpty(this[nameof(Schedule)]) ||
                       !string.IsNullOrEmpty(this[nameof(CertificateLink)]);
            }
        }

        /// <summary>
        /// Mở cửa sổ đổi mật khẩu
        /// Tạo ViewModel riêng cho chức năng đổi mật khẩu
        /// </summary>
        private void ChangePassword()
        {
            var viewModel = new ChangeDoctorPasswordViewModel();
            viewModel.SetAccount(_currentAccount);

            var changePasswordWindow = new ChangePasswordWindow();
            changePasswordWindow.DataContext = viewModel; // Gán DataContext trực tiếp

            changePasswordWindow.ShowDialog();
        }

        /// <summary>
        /// Làm mới thông tin nhân viên từ database
        /// Reset validation state và tải lại dữ liệu
        /// </summary>
        private void RefreshDoctorInformation()
        {
            try
            {
                if (_currentAccount != null)
                {
                    // Xóa tất cả touched fields để reset validation
                    _touchedFields.Clear();
                    _isValidating = false;

                    // Tải lại thông tin nhân viên từ database
                    LoadDoctorInformation(_currentAccount);

                    // Hiển thị thông báo cho người dùng
                    MessageBoxService.ShowSuccess(
                        "Thông tin đã được làm mới thành công.",
                        "Thành công"
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError(
                    $"Đã xảy ra lỗi khi làm mới thông tin: {ex.Message}",
                    "Lỗi"
                );
            }
        }

        #region Validation

        /// <summary>
        /// Error property cho IDataErrorInfo - trả về null vì validation per-property
        /// </summary>
        public string Error
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Indexer cho IDataErrorInfo - thực hiện validation cho từng property
        /// Chỉ validate khi người dùng đã tương tác với form hoặc khi submit
        /// </summary>
        public string this[string columnName]
        {
            get
            {
                // Chỉ validate khi user đã tương tác với form hoặc khi submit
                if (!_isValidating && !_touchedFields.Contains(columnName))
                    return null;

                string error = null;

                switch (columnName)
                {
                    case nameof(FullName):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(FullName))
                        {
                            error = "Họ và tên không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(FullName) && FullName.Trim().Length < 2)
                        {
                            error = "Họ và tên phải có ít nhất 2 ký tự";
                        }
                        break;

                    case nameof(Phone):
                        if (_touchedFields.Contains(columnName) && string.IsNullOrWhiteSpace(Phone))
                        {
                            error = "Số điện thoại không được để trống";
                        }
                        else if (!string.IsNullOrWhiteSpace(Phone) &&
                                !Regex.IsMatch(Phone.Trim(), @"^(0[3|5|7|8|9])[0-9]{8}$"))
                        {
                            error = "Số điện thoại không đúng định dạng (VD: 0901234567)";
                        }
                        break;

                    case nameof(Email):
                        if (!string.IsNullOrWhiteSpace(Email) &&
                            !Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        {
                            error = "Email không đúng định dạng";
                        }
                        break;

                    case nameof(Schedule):
                        if (!string.IsNullOrWhiteSpace(Schedule) && !IsValidScheduleFormat(Schedule))
                        {
                            error = "Lịch làm việc không đúng định dạng. Vui lòng nhập theo mẫu: T2, T3, T4: 7h-13h";
                        }
                        break;

                    case nameof(UserName):
                        if (!string.IsNullOrWhiteSpace(UserName) && UserName.Trim().Length < 4)
                        {
                            error = "Tên đăng nhập phải có ít nhất 4 ký tự";
                        }
                        break;
                }

                return error;
            }
        }

        /// <summary>
        /// Validation cho định dạng lịch làm việc
        /// Hỗ trợ nhiều định dạng phức tạp cho lịch làm việc
        /// Ví dụ: "T2, T3, T4: 7h-13h", "T2-T6: 8h-17h", "T2: 8h-12h, 13h30-17h"
        /// </summary>
        /// <param name="schedule">Chuỗi lịch làm việc cần validate</param>
        /// <returns>True nếu định dạng hợp lệ</returns>
        private bool IsValidScheduleFormat(string schedule)
        {
            if (string.IsNullOrWhiteSpace(schedule))
                return true; // Lịch rỗng là hợp lệ (không bắt buộc)

            // Hỗ trợ nhiều pattern
            string pattern1 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";
            string pattern2 = @"^T[2-7]-T[2-7]: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";
            string pattern3 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?(, \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?)+$";
            string pattern4 = @"^T[2-7]-T[2-7]: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?(, \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?)+$";
            string pattern5 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h\d{2}-\d{1,2}h\d{2}(, \d{1,2}h\d{2}-\d{1,2}h\d{2})*$";

            if (Regex.IsMatch(schedule, pattern1) ||
                Regex.IsMatch(schedule, pattern2) ||
                Regex.IsMatch(schedule, pattern3) ||
                Regex.IsMatch(schedule, pattern4) ||
                Regex.IsMatch(schedule, pattern5))
            {
                try
                {
                    // Parse tất cả time slots và kiểm tra từng slot có start < end
                    string[] parts = schedule.Split(':');
                    if (parts.Length < 2)
                        return false;

                    string timeSection = string.Join(":", parts.Skip(1)).Trim();
                    var timeRanges = timeSection.Split(',');

                    foreach (var range in timeRanges)
                    {
                        var times = range.Trim().Split('-');
                        if (times.Length == 2)
                        {
                            var start = ParseTimeString(times[0].Trim());
                            var end = ParseTimeString(times[1].Trim());
                            if (start == TimeSpan.Zero && end == TimeSpan.Zero)
                                return false; // Định dạng thời gian không hợp lệ
                            if (start >= end)
                                return false; // Thời gian bắt đầu phải trước thời gian kết thúc
                        }
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Helper method: parse chuỗi thời gian thành TimeSpan
        /// Hỗ trợ "8h", "8h30", "13h", "13h30" etc.
        /// </summary>
        /// <param name="timeStr">Chuỗi thời gian</param>
        /// <returns>TimeSpan tương ứng hoặc TimeSpan.Zero nếu không parse được</returns>
        private TimeSpan ParseTimeString(string timeStr)
        {
            timeStr = timeStr.Replace("h", ":").Replace(" ", "");
            if (timeStr.EndsWith(":")) timeStr += "00";
            var parts = timeStr.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
                return new TimeSpan(h, m, 0);
            if (parts.Length == 1 && int.TryParse(parts[0], out h))
                return new TimeSpan(h, 0, 0);
            return TimeSpan.Zero;
        }
        #endregion    
    }
}